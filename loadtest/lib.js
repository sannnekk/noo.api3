import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5001';
const USER = __ENV.NOO_USER || '';
const PASSWORD = __ENV.NOO_PASSWORD || '';
const PROFILE = __ENV.PROFILE || 'load';

const unauthorized = new Counter('status_401');
const forbidden = new Counter('status_403');
const rateLimited = new Counter('status_429');

const PROFILES = {
    smoke: { vus: 1, duration: '30s' },
    load: { vus: 10, duration: '2m' },
    stress: {
        stages: [
            { duration: '30s', target: 10 },
            { duration: '1m', target: 30 },
            { duration: '1m', target: 60 },
            { duration: '30s', target: 0 },
        ],
    },
};

let vuToken = null;
let vuActions = null;

export function makeOptions(actions) {
    if (!PROFILES[PROFILE]) {
        throw new Error(`Unknown PROFILE "${PROFILE}". Use one of: ${Object.keys(PROFILES).join(', ')}`);
    }

    const thresholds = {
        http_req_failed: ['rate<0.10'],
        checks: ['rate>0.90'],
        http_req_duration: ['p(95)<3000'],
    };

    for (const action of actions) {
        thresholds[`http_req_duration{name:${action.name}}`] = ['p(95)<3000'];
    }

    return {
        insecureSkipTLSVerify: true,
        summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
        thresholds,
        ...PROFILES[PROFILE],
    };
}

export function pick(array) {
    return array[Math.floor(Math.random() * array.length)];
}

export function login() {
    if (!USER || !PASSWORD) {
        throw new Error('NOO_USER and NOO_PASSWORD environment variables are required');
    }

    const res = http.post(
        `${BASE_URL}/auth/login`,
        JSON.stringify({ usernameOrEmail: USER, password: PASSWORD }),
        {
            headers: { 'Content-Type': 'application/json' },
            tags: { name: 'POST /auth/login' },
        }
    );

    if (res.status !== 200) {
        throw new Error(`Login failed with status ${res.status}: ${res.body}`);
    }

    return res.json('data');
}

export function requireRole(auth, role) {
    if (auth.userRole !== role) {
        throw new Error(`NOO_USER must be a ${role} account, but has role "${auth.userRole}"`);
    }

    return auth;
}

function trackStatus(res) {
    if (res.status === 401) {
        unauthorized.add(1);
        vuToken = null;
    } else if (res.status === 403) {
        forbidden.add(1);
    } else if (res.status === 429) {
        rateLimited.add(1);
    }
}

export function apiGet(path, name) {
    const res = http.get(`${BASE_URL}${path}`, {
        headers: {
            Authorization: `Bearer ${vuToken}`,
            Accept: 'application/json',
        },
        tags: { name },
    });

    trackStatus(res);
    check(res, { 'status is 2xx': (r) => r.status >= 200 && r.status < 300 }, { name });

    return res;
}

export function apiPost(path, name, body) {
    const res = http.post(`${BASE_URL}${path}`, JSON.stringify(body), {
        headers: {
            Authorization: `Bearer ${vuToken}`,
            'Content-Type': 'application/json',
            Accept: 'application/json',
        },
        tags: { name },
    });

    trackStatus(res);
    check(res, { 'status is 2xx': (r) => r.status >= 200 && r.status < 300 }, { name });

    return res;
}

export function getJson(path, token) {
    const res = http.get(`${BASE_URL}${path}`, {
        headers: { Authorization: `Bearer ${token}`, Accept: 'application/json' },
        tags: { name: 'setup' },
    });

    if (res.status !== 200) {
        return null;
    }

    return res.json('data');
}

export function fetchIds(path, token) {
    const data = getJson(path, token);

    if (!Array.isArray(data)) {
        return [];
    }

    return data.map((item) => item.id).filter(Boolean);
}

export function collectContentRefs(course) {
    const refs = [];

    const walk = (chapters) => {
        for (const chapter of chapters || []) {
            for (const material of chapter.materials || []) {
                if (material.contentId) {
                    refs.push({ courseId: course.id, contentId: material.contentId });
                }
            }
            walk(chapter.subChapters);
        }
    };

    walk(course.chapters);

    return refs;
}

export function randomDelta() {
    return {
        $type: 'delta',
        ops: [{ insert: `autosave draft ${Math.random().toString(36).slice(2)} ${Date.now()}\n` }],
    };
}

export function runIteration(ctx, actions) {
    if (!vuToken) {
        vuToken = login().accessToken;
    }

    if (!vuActions) {
        vuActions = actions.filter((action) => !action.enabled || action.enabled(ctx));
        if (vuActions.length === 0) {
            throw new Error('No runnable actions: all id pools discovered in setup are empty');
        }
    }

    const totalWeight = vuActions.reduce((sum, action) => sum + action.weight, 0);
    let roll = Math.random() * totalWeight;

    for (const action of vuActions) {
        roll -= action.weight;
        if (roll <= 0) {
            action.run(ctx);
            break;
        }
    }

    sleep(0.3 + Math.random() * 0.7);
}
