# Prometheus Documentation Index

Read these in order when starting work.

| Need | Read | Why |
| --- | --- | --- |
| Repo instructions | [../AGENTS.md](../AGENTS.md) | Collaboration rules, markdown style, and QA preference |
| Quick commands | [../README.md](../README.md) | Fast build/test/deploy entrypoint |
| Current state | [HANDOFF.md](HANDOFF.md) | Latest verified result, blockers, and next action |
| Orchestration runs | [ORCHESTRATION.md](ORCHESTRATION.md) | Durable kickoff prompts and multi-agent run loop |
| Validation runbook | [TEST_PLAN.md](TEST_PLAN.md) | Smoke checks, QA matrices, and evidence templates |
| Product/design decisions | [DESIGN.md](DESIGN.md) | Durable mechanics, roadmap, and architecture choices |
| Architecture | [ARCHITECTURE.md](ARCHITECTURE.md) | Durable system boundaries, data flow, reset contracts, and integration boundaries |
| Ticket board | [tickets/README.md](tickets/README.md) | Permanent file-based ticket board for multi-agent execution |
| Stable repo conventions | [PROJECT_MEMORY.md](PROJECT_MEMORY.md) | Long-lived boundaries and source-of-truth pointers |

## Source Of Truth Shortcuts

| Question | Source |
| --- | --- |
| What do I run first? | [../README.md](../README.md) and `bash scripts/build.sh --help` |
| What is currently verified? | [HANDOFF.md](HANDOFF.md) |
| What is next? | [HANDOFF.md](HANDOFF.md) |
| How do I start orchestration? | [ORCHESTRATION.md](ORCHESTRATION.md) |
| Where is the validation matrix? | [TEST_PLAN.md](TEST_PLAN.md) |
| Where is the active ticket board? | [tickets/README.md](tickets/README.md) |
| Where is the Phase 2 stabilization archive? | [ARCHIVE/stabilization-sprint-2026-04/README.md](ARCHIVE/stabilization-sprint-2026-04/README.md) |
| Where is the architecture map? | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Where do design decisions live? | [DESIGN.md](DESIGN.md) |
| Where is history? | [ARCHIVE/](ARCHIVE/) |

## Documentation Ownership

- `README.md` stays compact and points here.
- `HANDOFF.md` is the only live status document.
- `ORCHESTRATION.md` owns the durable multi-agent kickoff and run loop.
- `TEST_PLAN.md` is the only active validation runbook.
- `DESIGN.md` is the durable product and architecture record.
- `ARCHITECTURE.md` is the durable system architecture and boundary contract.
- `tickets/` is the active file-based ticket board.
- `ARCHIVE/` keeps long chronological history out of startup docs.
