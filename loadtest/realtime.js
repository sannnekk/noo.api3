import { check, sleep } from 'k6'
import { Counter, Trend } from 'k6/metrics'
import { WebSocket } from 'k6/experimental/websockets'

import { BASE_URL, login } from './lib.js'

// Unlike the per-role scenarios, this one measures what an *idle* connection costs. Each VU
// opens one hub connection, holds it for the whole iteration answering keep-alives, and pings
// occasionally. The number to watch is not throughput but the API pod's RSS and file-descriptor
// count divided by the connection count.

/** SignalR terminates every JSON message with 0x1e. */
const RECORD_SEPARATOR = String.fromCharCode(30)

const handshakeTime = new Trend('signalr_handshake_time', true)
const pingRoundTrip = new Trend('signalr_ping_round_trip', true)
const pushLatency = new Trend('signalr_push_latency', true)
const connectionErrors = new Counter('signalr_connection_errors')
const pushesReceived = new Counter('signalr_pushes_received')

const PROFILES = {
  // One connection, enough to prove the scenario works before spending money on it.
  smoke: { vus: 1, duration: '30s' },
  // A few hundred sockets is where per-connection memory becomes measurable.
  load: { vus: 500, duration: '3m' },
  // Ramp until something gives: descriptors, memory, or the backplane.
  stress: {
    stages: [
      { duration: '2m', target: 2000 },
      { duration: '3m', target: 5000 },
      { duration: '1m', target: 0 }
    ]
  }
}

const profile = PROFILES[__ENV.PROFILE || 'load'] || PROFILES.load

export const options = {
  ...profile,
  thresholds: {
    signalr_handshake_time: ['p(95)<2000'],
    signalr_ping_round_trip: ['p(95)<1000'],
    signalr_connection_errors: ['count<1']
  }
}

export function setup() {
  // One login for the whole run: this measures connections, not the auth endpoint.
  return { accessToken: login().accessToken }
}

/** How long one iteration holds its connection open before closing it. */
const HOLD_SECONDS = Number(__ENV.HOLD_SECONDS || 60)

/** Seconds between pings on a held connection. */
const PING_EVERY_SECONDS = Number(__ENV.PING_EVERY_SECONDS || 15)

function hubUrl(accessToken) {
  // The token rides in the query string because browsers cannot set headers on a WebSocket
  // handshake — the same reason the server reads it from there.
  const wsBase = BASE_URL.replace(/^http/, 'ws')

  return `${wsBase}/hubs/ping?access_token=${encodeURIComponent(accessToken)}`
}

/** SignalR frames one or more JSON messages per WebSocket message, separated by 0x1e. */
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

  let handshakeStartedAt = 0
  let handshakeDone = false
  let pingSentAt = 0
  let invocationId = 0

  function ping() {
    invocationId += 1
    pingSentAt = Date.now()
    send(socket, {
      type: 1,
      invocationId: String(invocationId),
      target: 'PingAsync',
      arguments: []
    })
  }

  socket.onopen = () => {
    handshakeStartedAt = Date.now()
    // Skipping negotiation: one connection is one server, so no sticky routing is involved and
    // the measurement is not muddied by a negotiate round-trip.
    send(socket, { protocol: 'json', version: 1 })
  }

  socket.onmessage = (event) => {
    for (const message of parseFrames(event.data)) {
      // The handshake response is the first frame and carries no "type".
      if (!handshakeDone && message.type === undefined) {
        handshakeDone = true
        handshakeTime.add(Date.now() - handshakeStartedAt)
        check(message, { 'handshake succeeded': (m) => !m.error })

        if (message.error) {
          connectionErrors.add(1)
          socket.close()
          return
        }

        ping()
        continue
      }

      switch (message.type) {
        case 1: // server-to-client invocation
          if (message.target === 'PongAsync') {
            pushesReceived.add(1)
            if (pingSentAt > 0) {
              pushLatency.add(Date.now() - pingSentAt)
            }
          }
          break
        case 3: // completion of our invocation
          if (pingSentAt > 0) {
            pingRoundTrip.add(Date.now() - pingSentAt)
            pingSentAt = 0
          }
          check(message, { 'ping did not error': (m) => !m.error })
          break
        case 6: // keep-alive; answering it is what holds the connection open
          send(socket, { type: 6 })
          break
        default:
          break
      }
    }
  }

  socket.onerror = () => {
    connectionErrors.add(1)
  }

  const pingTimer = socket.setInterval(() => {
    if (handshakeDone) {
      ping()
    }
  }, PING_EVERY_SECONDS * 1000)

  socket.setTimeout(() => {
    socket.clearInterval(pingTimer)
    socket.close()
  }, HOLD_SECONDS * 1000)

  // Keeps the iteration alive while the socket callbacks run.
  sleep(HOLD_SECONDS + 1)
}
