# Load tests

Per-role request generators for finding resource-heavy routes. Built on [k6](https://k6.io); `run.sh` downloads the binary to `~/.local/bin` on first use if it is not installed.

Each scenario logs in as a real user of that role, verifies the role matches, discovers real ids in `setup()` (courses, material contents, work assignments, assigned works, tasks), then replays a weighted mix of that role's typical traffic. Every request is tagged with its route template (`GET /course/:id`), so latency is aggregated per route both in the k6 summary and in the OpenTelemetry dashboard.

## Usage

```bash
NOO_USER=somestudent NOO_PASSWORD=... ./loadtest/run.sh student
NOO_USER=somementor  NOO_PASSWORD=... ./loadtest/run.sh mentor stress
```

`./loadtest/run.sh <role> [profile]` — the account in `NOO_USER` must actually have the chosen role, otherwise setup aborts.

| Profile | Shape |
| --- | --- |
| `smoke` | 1 VU for 30s, sanity check |
| `load` | 10 VUs for 2m, steady load (default) |
| `stress` | ramp 0 → 60 VUs over 3m |

Environment variables: `NOO_USER` / `NOO_PASSWORD` (required), `BASE_URL` (default `http://localhost:5001`), `K6_VERSION` (used only when installing).

## Scenarios

| File | Focus |
| --- | --- |
| `student.js` | The production-heavy mix: answer autosave (`POST /assigned-work/:id/save-answer`, highest weight — the frontend calls it very often), course material content, course list, course tree (`GET /course/:id`), assigned-work list, assigned-work progress |
| `mentor.js` | Assigned-work list/detail/history/progress (checking flow), some course browsing |
| `teacher.js` | Course list/tree/content authoring views, work list and statistics, memberships, user list |
| `assistant.js` | Assigned works and statistics |
| `admin.js` | User management, courses, memberships, platform statistics |

`lib.js` holds the shared plumbing: login with per-VU token refresh on 401, weighted action runner, id-pool discovery helpers, per-route thresholds, and 401/403/429 counters.

## Notes

- **The student scenario writes to the database**: it autosaves generated draft answers into the student's unsolved assigned works (mimicking the frontend autosave). Use a throwaway student account on a dev database.
- **Rate limiting**: the API allows 300 requests/min per IP, which any real run will exceed. Start the API with the limit raised:

  ```bash
  RateLimiting__Global__PermitLimit=1000000 ./scripts.sh run dev
  ```

- Setup pools depend on the account's data: a student with no course memberships or assigned works will have the corresponding actions disabled (and the run fails if nothing is runnable at all).

## Reading the results

The k6 summary prints `http_req_duration` per route (avg, p90, p95, p99). Routes with a failing `p(95)<3000` threshold are marked. For the *why*, open the same time window in the OpenTelemetry dashboard: the slow route's traces show every EF Core query and Redis call with timings.
