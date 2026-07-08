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
        name: 'GET /assigned-work',
        weight: 20,
        run: () => apiGet('/assigned-work?page=1&perPage=20', 'GET /assigned-work'),
    },
    {
        name: 'GET /assigned-work/:id',
        weight: 15,
        enabled: (ctx) => ctx.assignedWorkIds.length > 0,
        run: (ctx) => apiGet(`/assigned-work/${pick(ctx.assignedWorkIds)}`, 'GET /assigned-work/:id'),
    },
    {
        name: 'GET /assigned-work/:workAssignmentId/progress',
        weight: 10,
        enabled: (ctx) => ctx.workAssignmentIds.length > 0,
        run: (ctx) =>
            apiGet(
                `/assigned-work/${pick(ctx.workAssignmentIds)}/progress`,
                'GET /assigned-work/:workAssignmentId/progress'
            ),
    },
    {
        name: 'GET /assigned-work/:id/history',
        weight: 8,
        enabled: (ctx) => ctx.assignedWorkIds.length > 0,
        run: (ctx) =>
            apiGet(
                `/assigned-work/${pick(ctx.assignedWorkIds)}/history?page=1&perPage=20`,
                'GET /assigned-work/:id/history'
            ),
    },
    {
        name: 'GET /course',
        weight: 8,
        run: () => apiGet('/course?page=1&perPage=20', 'GET /course'),
    },
    {
        name: 'GET /course/:id',
        weight: 6,
        enabled: (ctx) => ctx.courseIds.length > 0,
        run: (ctx) => apiGet(`/course/${pick(ctx.courseIds)}`, 'GET /course/:id'),
    },
    {
        name: 'GET /course/:courseId/content/:contentId',
        weight: 6,
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
        name: 'GET /assigned-work/:userId/metadata',
        weight: 5,
        run: (ctx) => apiGet(`/assigned-work/${ctx.userId}/metadata`, 'GET /assigned-work/:userId/metadata'),
    },
    {
        name: 'GET /notification',
        weight: 5,
        run: () => apiGet('/notification?page=1&perPage=20', 'GET /notification'),
    },
    {
        name: 'GET /calendar/:userId/:year/:month',
        weight: 3,
        run: (ctx) => {
            const now = new Date();
            apiGet(
                `/calendar/${ctx.userId}/${now.getFullYear()}/${now.getMonth() + 1}`,
                'GET /calendar/:userId/:year/:month'
            );
        },
    },
    {
        name: 'GET /user/:id',
        weight: 3,
        run: (ctx) => apiGet(`/user/${ctx.userId}`, 'GET /user/:id'),
    },
];

export const options = makeOptions(ACTIONS);

export function setup() {
    const auth = requireRole(login(), 'mentor');
    const token = auth.accessToken;

    const courseIds = fetchIds('/course?page=1&perPage=50', token);

    const contentRefs = [];
    for (const courseId of courseIds.slice(0, 3)) {
        const course = getJson(`/course/${courseId}`, token);
        if (course) {
            contentRefs.push(...collectContentRefs(course));
        }
    }

    const workAssignmentIds = [];
    for (const ref of contentRefs.slice(0, 10)) {
        const content = getJson(`/course/${ref.courseId}/content/${ref.contentId}`, token);
        for (const assignment of (content && content.workAssignments) || []) {
            if (assignment.id) {
                workAssignmentIds.push(assignment.id);
            }
        }
    }

    const assignedWorkIds = fetchIds('/assigned-work?page=1&perPage=50', token);

    return {
        userId: auth.userId,
        courseIds,
        contentRefs,
        workAssignmentIds,
        assignedWorkIds,
    };
}

export default function (ctx) {
    runIteration(ctx, ACTIONS);
}
