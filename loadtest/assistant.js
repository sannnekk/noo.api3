import {
    makeOptions,
    pick,
    login,
    requireRole,
    apiGet,
    fetchIds,
    runIteration,
} from './lib.js';

const ACTIONS = [
    {
        name: 'GET /assigned-work',
        weight: 20,
        run: () => apiGet('/assigned-work?page=1&perPage=20', 'GET /assigned-work'),
    },
    {
        name: 'GET /assigned-work/:id',
        weight: 10,
        enabled: (ctx) => ctx.assignedWorkIds.length > 0,
        run: (ctx) => apiGet(`/assigned-work/${pick(ctx.assignedWorkIds)}`, 'GET /assigned-work/:id'),
    },
    {
        name: 'GET /statistics/platform',
        weight: 10,
        run: () => apiGet('/statistics/platform', 'GET /statistics/platform'),
    },
    {
        name: 'GET /statistics/user/:userId',
        weight: 8,
        enabled: (ctx) => ctx.userIds.length > 0,
        run: (ctx) => apiGet(`/statistics/user/${pick(ctx.userIds)}`, 'GET /statistics/user/:userId'),
    },
    {
        name: 'GET /user',
        weight: 8,
        run: () => apiGet('/user?page=1&perPage=20', 'GET /user'),
    },
    {
        name: 'GET /user/:id',
        weight: 5,
        enabled: (ctx) => ctx.userIds.length > 0,
        run: (ctx) => apiGet(`/user/${pick(ctx.userIds)}`, 'GET /user/:id'),
    },
    {
        name: 'GET /course',
        weight: 5,
        run: () => apiGet('/course?page=1&perPage=20', 'GET /course'),
    },
    {
        name: 'GET /notification',
        weight: 3,
        run: () => apiGet('/notification?page=1&perPage=20', 'GET /notification'),
    },
];

export const options = makeOptions(ACTIONS);

export function setup() {
    const auth = requireRole(login(), 'assistant');
    const token = auth.accessToken;

    return {
        userId: auth.userId,
        assignedWorkIds: fetchIds('/assigned-work?page=1&perPage=50', token),
        userIds: fetchIds('/user?page=1&perPage=50', token),
    };
}

export default function (ctx) {
    runIteration(ctx, ACTIONS);
}
