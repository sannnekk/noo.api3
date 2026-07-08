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
        name: 'GET /user',
        weight: 15,
        run: () => apiGet('/user?page=1&perPage=20', 'GET /user'),
    },
    {
        name: 'GET /user/:id',
        weight: 8,
        enabled: (ctx) => ctx.userIds.length > 0,
        run: (ctx) => apiGet(`/user/${pick(ctx.userIds)}`, 'GET /user/:id'),
    },
    {
        name: 'GET /course',
        weight: 12,
        run: () => apiGet('/course?page=1&perPage=20', 'GET /course'),
    },
    {
        name: 'GET /course/:id',
        weight: 10,
        enabled: (ctx) => ctx.courseIds.length > 0,
        run: (ctx) => apiGet(`/course/${pick(ctx.courseIds)}`, 'GET /course/:id'),
    },
    {
        name: 'GET /course/:courseId/content/:contentId',
        weight: 8,
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
        name: 'GET /course/membership',
        weight: 8,
        run: () => apiGet('/course/membership?page=1&perPage=20', 'GET /course/membership'),
    },
    {
        name: 'GET /assigned-work',
        weight: 8,
        run: () => apiGet('/assigned-work?page=1&perPage=20', 'GET /assigned-work'),
    },
    {
        name: 'GET /statistics/platform',
        weight: 8,
        run: () => apiGet('/statistics/platform', 'GET /statistics/platform'),
    },
    {
        name: 'GET /statistics/user/:userId',
        weight: 5,
        enabled: (ctx) => ctx.userIds.length > 0,
        run: (ctx) => apiGet(`/statistics/user/${pick(ctx.userIds)}`, 'GET /statistics/user/:userId'),
    },
    {
        name: 'GET /work',
        weight: 5,
        run: () => apiGet('/work?page=1&perPage=20', 'GET /work'),
    },
    {
        name: 'GET /subject',
        weight: 4,
        run: () => apiGet('/subject?page=1&perPage=50', 'GET /subject'),
    },
    {
        name: 'GET /snippet',
        weight: 2,
        run: () => apiGet('/snippet?page=1&perPage=20', 'GET /snippet'),
    },
    {
        name: 'GET /platform/version',
        weight: 2,
        run: () => apiGet('/platform/version', 'GET /platform/version'),
    },
];

export const options = makeOptions(ACTIONS);

export function setup() {
    const auth = requireRole(login(), 'admin');
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
        userIds: fetchIds('/user?page=1&perPage=50', token),
    };
}

export default function (ctx) {
    runIteration(ctx, ACTIONS);
}
