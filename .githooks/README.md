Local Git hooks

These hooks block commits when the project is misformatted, doesn't build, or when unit or integration tests fail, and when a commit message does not follow the conventional-commit format the changelog is generated from.

Enable once per clone:

1. Point git to this hooks folder
   git config core.hooksPath .githooks

2. Make scripts executable (Linux/macOS/WSL/Git Bash)
   chmod +x .githooks/pre-commit .githooks/commit-msg

The hooks

- pre-commit: `dotnet format --verify-no-changes`, then the build, then the unit and integration tests. The counterpart of the frontend's `pnpm check` in .husky/pre-commit — keep the two in step. Takes a couple of minutes, most of it the format check, which loads the whole MSBuild workspace.
- commit-msg: enforces the conventional-commit subject that scripts/generate-changelog.cs reads. Keep it in sync with the frontend's .husky/commit-msg.

Notes

- `NOO_SKIP_FORMAT=1 git commit …` skips the format check alone; `git commit --no-verify` skips the hook entirely.
- Hooks run locally only and can be bypassed. Use CI + branch protection to enforce on the server.
