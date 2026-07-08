import {
    makeOptions,
    pick,
    login,
    requireRole,
    apiGet,
    getJson,
    fetchIds,
    collectContentRefs,
    runIteration,
} from './lib.js';

const ACTIONS = [
    {
        name: 'GET /course',
        weight: 15,
        run: () => apiGet('/course?page=1&perPage=20', 'GET /course'),
    },
    {
        name: 'GET /course/:id',
        weight: 15,
        enabled: (ctx) => ctx.courseIds.length > 0,
        run: (ctx) => apiGet(`/course/${pick(ctx.courseIds)}`, 'GET /course/:id'),
    },
    {
        name: 'GET /course/:courseId/content/:contentId',
        weight: 12,
        enabled: (ctx) => ctx.contentRefs.length > 0,
        run: (ctx) => {
            const ref = pick(ctx.contentRefs);
            apiGet(
                `/course/${ref.courseId}/content/${ref.contentId}`,
                'GET /course/:courseId/content/:contentId'
            );
        },
    },
    {
        name: 'GET /work',
        weight: 10,
        run: () => apiGet('/work?page=1&perPage=20', 'GET /work'),
    },
    {
        name: 'GET /work/:id',
        weight: 6,
        enabled: (ctx) => ctx.workIds.length > 0,
        run: (ctx) => apiGet(`/work/${pick(ctx.workIds)}`, 'GET /work/:id'),
    },
    {
        name: 'GET /work/:id/statistics',
        weight: 5,
        enabled: (ctx) => ctx.workIds.length > 0,
        run: (ctx) => apiGet(`/work/${pick(ctx.workIds)}/statistics`, 'GET /work/:id/statistics'),
    },
    {
        name: 'GET /assigned-work',
        weight: 8,
        run: () => apiGet('/assigned-work?page=1&perPage=20', 'GET /assigned-work'),
    },
    {
        name: 'GET /course/membership',
        weight: 8,
        run: () => apiGet('/course/membership?page=1&perPage=20', 'GET /course/membership'),
    },
    {
        name: 'GET /user',
        weight: 8,
        run: () => apiGet('/user?page=1&perPage=20', 'GET /user'),
    },
    {
        name: 'GET /user/:id',
        weight: 4,
        enabled: (ctx) => ctx.userIds.length > 0,
        run: (ctx) => apiGet(`/user/${pick(ctx.userIds)}`, 'GET /user/:id'),
    },
    {
        name: 'GET /statistics/platform',
        weight: 4,
        run: () => apiGet('/statistics/platform', 'GET /statistics/platform'),
    },
    {
        name: 'GET /subject',
        weight: 3,
        run: () => apiGet('/subject?page=1&perPage=50', 'GET /subject'),
    },
    {
        name: 'GET /notification',
        weight: 2,
        run: () => apiGet('/notification?page=1&perPage=20', 'GET /notification'),
    },
];

export const options = makeOptions(ACTIONS);

export function setup() {
    const auth = requireRole(login(), 'teacher');
    const token = auth.accessToken;

    const courseIds = fetchIds('/course?page=1&perPage=50', token);

    const contentRefs = [];
    for (const courseId of courseIds.slice(0, 5)) {
        const course = getJson(`/course/${courseId}`, token);
        if (course) {
            contentRefs.push(...collectContentRefs(course));
        }
    }

    return {
        userId: auth.userId,
        courseIds,
        contentRefs,
        workIds: fetchIds('/work?page=1&perPage=50', token),
        userIds: fetchIds('/user?page=1&perPage=50', token),
    };
}

export default function (ctx) {
    runIteration(ctx, ACTIONS);
}
