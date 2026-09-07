import { check, sleep } from 'k6'
import { Counter, Trend } from 'k6/metrics'
import http from 'k6/http'
import { WebSocket } from 'k6/experimental/websockets'

import { BASE_URL, login } from './lib.js'

// What a room of editors costs. Every VU is one more editor in the *same* work, so the number
// that grows is not connections but fan-out: each operation and each CRDT frame is delivered to
// everyone else in the room, and with a backplane every one of those is a Redis publish.
//
// Watch the API pod's RSS and the backplane's ops/sec against `collab_frames_sent`. The knob if
// it is too expensive is the client's coalescing window, not this script: frames per editor per
// second is what the design bounds, and it should sit near 10 no matter how fast anyone types.

/** SignalR terminates every JSON message with 0x1e. */
const RECORD_SEPARATOR = String.fromCharCode(30)

const joinTime = new Trend('collab_join_time', true)
const opRoundTrip = new Trend('collab_op_round_trip', true)
const relayLatency = new Trend('collab_relay_latency', true)
const framesSent = new Counter('collab_frames_sent')
const framesReceived = new Counter('collab_frames_received')
const opsReceived = new Counter('collab_ops_received')
const errors = new Counter('collab_errors')

const PROFILES = {
  // Two editors is the smallest room that relays anything at all.
  smoke: { vus: 2, duration: '30s' },
  // A plausible worst case for one document: a whole department in the same work.
  load: { vus: 10, duration: '3m' },
  // Well past what a real room holds, to find where fan-out stops being linear.
  stress: {
    stages: [
      { duration: '1m', target: 20 },
      { duration: '2m', target: 50 },
      { duration: '1m', target: 0 }
    ]
  }
}

const profile = PROFILES[__ENV.PROFILE || 'load'] || PROFILES.load

export const options = {
  ...profile,
  thresholds: {
    collab_join_time: ['p(95)<3000'],
    collab_op_round_trip: ['p(95)<1500'],
    collab_errors: ['count<1']
  }
}

/** The work every VU edits. Set it, or the run has nothing to open. */
const WORK_ID = __ENV.WORK_ID

/** How long one editor stays in the room. */
const HOLD_SECONDS = Number(__ENV.HOLD_SECONDS || 60)

/** The client coalescing window this scenario imitates. */
const FRAME_EVERY_MS = Number(__ENV.FRAME_EVERY_MS || 100)

/** How often an editor commits a scalar field, as a real one would on blur. */
const OP_EVERY_SECONDS = Number(__ENV.OP_EVERY_SECONDS || 10)

export function setup() {
  if (!WORK_ID) {
    throw new Error('WORK_ID is required: point it at a work the account may edit.')
  }

  const auth = login()

  // Fail early and clearly rather than through a hub error every VU would repeat.
  const room = http.get(`${BASE_URL}/collaboration/work/${WORK_ID}`, {
    headers: { Authorization: `Bearer ${auth.accessToken}` },
    tags: { name: 'GET /collaboration/work/{id}' }
  })

  if (room.status !== 200) {
    throw new Error(`Cannot open work ${WORK_ID}: ${room.status} ${room.body}`)
  }

  return { accessToken: auth.accessToken }
}

function hubUrl(accessToken) {
  const wsBase = BASE_URL.replace(/^http/, 'ws')

  return `${wsBase}/hubs/collaboration?access_token=${encodeURIComponent(accessToken)}`
}

function parseFrames(data) {
  return String(data)
    .split(RECORD_SEPARATOR)
    .filter((part) => part.length > 0)
    .map((part) => {
      try {
        return JSON.parse(part)
      } catch {
        return null
      }
    })
    .filter((message) => message !== null)
}

function send(socket, message) {
  socket.send(JSON.stringify(message) + RECORD_SEPARATOR)
}

export default function (data) {
  const socket = new WebSocket(hubUrl(data.accessToken))

  // Every VU edits the same task, so its CRDT scope is the busiest one a room can have.
  const scope = __ENV.SCOPE || 'loadtest-task'
  const payload = 'AQIDBAUGBwgJCgsMDQ4PEA=='

  let handshakeDone = false
  let joined = false
  let invocationId = 0
  const pending = {}

  function invoke(target, args) {
    invocationId += 1
    pending[String(invocationId)] = { target, sentAt: Date.now() }
    send(socket, {
      type: 1,
      invocationId: String(invocationId),
      target,
      arguments: args
    })
  }

  socket.onopen = () => {
    send(socket, { protocol: 'json', version: 1 })
  }

  socket.onmessage = (event) => {
    for (const message of parseFrames(event.data)) {
      if (!handshakeDone && message.type === undefined) {
        handshakeDone = true
        check(message, { 'handshake succeeded': (m) => !m.error })

        if (message.error) {
          errors.add(1)
          socket.close()
          return
        }

        invoke('JoinAsync', ['work', WORK_ID])
        continue
      }

      switch (message.type) {
        case 1:
          if (message.target === 'YjsFrameAsync') {
            framesReceived.add(1)
            // The frame carries the moment its sender coalesced it, so this is end-to-end
            // relay time including the backplane hop.
            const sentAt = Number(message.arguments?.[0]?.kind)
            if (sentAt > 0) {
              relayLatency.add(Date.now() - sentAt)
            }
          } else if (message.target === 'OpsAppliedAsync') {
            opsReceived.add(1)
          }
          break
        case 3: {
          const call = pending[message.invocationId]
          delete pending[message.invocationId]

          if (message.error) {
            errors.add(1)
            break
          }

          if (call?.target === 'JoinAsync') {
            joined = true
            joinTime.add(Date.now() - call.sentAt)
            invoke('JoinScopeAsync', [scope])
          } else if (call?.target === 'PushOpsAsync') {
            opRoundTrip.add(Date.now() - call.sentAt)
          }
          break
        }
        case 6:
          send(socket, { type: 6 })
          break
        default:
          break
      }
    }
  }

  socket.onerror = () => {
    errors.add(1)
  }

  // Typing: one coalesced frame per window, which is what the real client sends however fast
  // the keys arrive.
  const frameTimer = socket.setInterval(() => {
    if (!joined) {
      return
    }

    framesSent.add(1)
    send(socket, {
      type: 1,
      target: 'PushYjsAsync',
      arguments: [{ scope, kind: String(Date.now()), payload }]
    })
  }, FRAME_EVERY_MS)

  // Committing a scalar field, as an editor does on blur. Each VU writes its own path so the
  // scenario measures fan-out rather than lease contention.
  const opTimer = socket.setInterval(() => {
    if (!joined) {
      return
    }

    invoke('PushOpsAsync', [
      [{ op: 'replace', path: '/description', value: `vu-${__VU}-${Date.now()}` }]
    ])
  }, OP_EVERY_SECONDS * 1000)

  const heartbeatTimer = socket.setInterval(() => {
    if (joined) {
      invoke('HeartbeatAsync', [])
    }
  }, 7000)

  socket.setTimeout(() => {
    socket.clearInterval(frameTimer)
    socket.clearInterval(opTimer)
    socket.clearInterval(heartbeatTimer)
    socket.close()
  }, HOLD_SECONDS * 1000)

  sleep(HOLD_SECONDS + 1)
}
