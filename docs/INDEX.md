# Prometheus Documentation Index

Read these in order when starting work.

| Need | Read | Why |
| --- | --- | --- |
| Repo instructions | [../AGENTS.md](../AGENTS.md) | Collaboration rules, markdown style, and QA preference |
| Quick commands | [../README.md](../README.md) | Fast build/test/deploy entrypoint |
| Current state | [HANDOFF.md](HANDOFF.md) | Latest verified result, blockers, and next action |
| Validation runbook | [TEST_PLAN.md](TEST_PLAN.md) | Smoke checks, QA matrices, and evidence templates |
| Product/design decisions | [DESIGN.md](DESIGN.md) | Durable mechanics, roadmap, and architecture choices |
| Stabilization sprint | [STABILIZATION_SPRINT.md](STABILIZATION_SPRINT.md) | Phase 2 to Phase 3 architecture risks and sprint plan |
| Stabilization tickets | [stabilization/README.md](stabilization/README.md) | File-based ticket board for multi-agent sprint execution |
| Stable repo conventions | [PROJECT_MEMORY.md](PROJECT_MEMORY.md) | Long-lived boundaries and source-of-truth pointers |

## Source Of Truth Shortcuts

| Question | Source |
| --- | --- |
| What do I run first? | [../README.md](../README.md) and `bash scripts/build.sh --help` |
| What is currently verified? | [HANDOFF.md](HANDOFF.md) |
| What is next? | [HANDOFF.md](HANDOFF.md) |
| Where is the validation matrix? | [TEST_PLAN.md](TEST_PLAN.md) |
| Where is the stabilization plan? | [STABILIZATION_SPRINT.md](STABILIZATION_SPRINT.md) |
| Where is the stabilization ticket board? | [stabilization/README.md](stabilization/README.md) |
| Where do design decisions live? | [DESIGN.md](DESIGN.md) |
| Where is history? | [ARCHIVE/](ARCHIVE/) |

## Documentation Ownership

- `README.md` stays compact and points here.
- `HANDOFF.md` is the only live status document.
- `TEST_PLAN.md` is the only active validation runbook.
- `DESIGN.md` is the durable product and architecture record.
- `STABILIZATION_SPRINT.md` is the active architecture-risk burn-down plan.
- `stabilization/` is the active file-based ticket board for sprint execution.
- `ARCHIVE/` keeps long chronological history out of startup docs.
