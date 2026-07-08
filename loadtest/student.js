import {
    makeOptions,
    pick,
    login,
    requireRole,
    apiGet,
    apiPost,
    getJson,
    fetchIds,
    collectContentRefs,
    randomDelta,
    runIteration,
} from './lib.js';

const ACTIONS = [
    {
        name: 'POST /assigned-work/:id/save-answer',
        weight: 25,
        enabled: (ctx) => ctx.answerTargets.length > 0,
        run: (ctx) => {
            const target = pick(ctx.answerTargets);
            apiPost(
                `/assigned-work/${target.assignedWorkId}/save-answer`,
                'POST /assigned-work/:id/save-answer',
                {
                    taskId: target.taskId,
                    richTextContent: randomDelta(),
                    status: 'not-submitted',
                    maxScore: target.maxScore,
                }
            );
        },
    },
    {
        name: 'GET /course/:courseId/content/:contentId',
        weight: 15,
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
        name: 'GET /course',
        weight: 12,
        run: () => apiGet('/course?page=1&perPage=20', 'GET /course'),
    },
    {
        name: 'GET /course/:id',
        weight: 12,
        enabled: (ctx) => ctx.courseIds.length > 0,
        run: (ctx) => apiGet(`/course/${pick(ctx.courseIds)}`, 'GET /course/:id'),
    },
    {
        name: 'GET /assigned-work',
        weight: 10,
        run: () => apiGet('/assigned-work?page=1&perPage=20', 'GET /assigned-work'),
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
        name: 'GET /assigned-work/:id',
        weight: 4,
        enabled: (ctx) => ctx.assignedWorkIds.length > 0,
        run: (ctx) => apiGet(`/assigned-work/${pick(ctx.assignedWorkIds)}`, 'GET /assigned-work/:id'),
    },
    {
        name: 'GET /course/membership',
        weight: 4,
        run: () => apiGet('/course/membership?page=1&perPage=20', 'GET /course/membership'),
    },
    {
        name: 'GET /notification',
        weight: 4,
        run: () => apiGet('/notification?page=1&perPage=20', 'GET /notification'),
    },
    {
        name: 'GET /assigned-work/:userId/metadata',
        weight: 2,
        run: (ctx) => apiGet(`/assigned-work/${ctx.userId}/metadata`, 'GET /assigned-work/:userId/metadata'),
    },
    {
        name: 'GET /calendar/:userId/:year/:month',
        weight: 2,
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
        weight: 2,
        run: (ctx) => apiGet(`/user/${ctx.userId}`, 'GET /user/:id'),
    },
];

export const options = makeOptions(ACTIONS);

export function setup() {
    const auth = requireRole(login(), 'student');
    const token = auth.accessToken;

    const courseIds = fetchIds('/course?page=1&perPage=50', token);

    const contentRefs = [];
    for (const courseId of courseIds.slice(0, 5)) {
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

    const assignedWorks = getJson('/assigned-work?page=1&perPage=50', token) || [];
    const assignedWorkIds = assignedWorks.map((work) => work.id).filter(Boolean);

    const answerTargets = [];
    const unsolved = assignedWorks.filter((work) => work.solveStatus !== 'solved').slice(0, 5);
    for (const work of unsolved) {
        const detail = getJson(`/assigned-work/${work.id}`, token);
        for (const task of (detail && detail.work && detail.work.tasks) || []) {
            answerTargets.push({
                assignedWorkId: work.id,
                taskId: task.id,
                maxScore: task.maxScore || 1,
            });
        }
    }

    return {
        userId: auth.userId,
        courseIds,
        contentRefs,
        workAssignmentIds,
        assignedWorkIds,
        answerTargets,
    };
}

export default function (ctx) {
    runIteration(ctx, ACTIONS);
}
