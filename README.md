# Prometheus Mod (Standalone)

This repository contains the standalone Prometheus Timberborn mod assets, local deploy tooling, and internal project docs.

## Quick Commands

- `bash scripts/build.sh` - compile and deploy.
- `bash scripts/build.sh --test` - run fast tests, then compile and deploy.
- `bash scripts/build.sh --launch` - compile, deploy, clear Timberborn logs, and launch the game for QA.
- `bash scripts/build.sh --qa` - run tests, deploy, clear logs, launch Timberborn, and hand startup/menu QA to Computer Use.
- `bash scripts/test.sh` - run the fast plain C# regression suite.

Source of truth: run `bash scripts/build.sh --help` for current build/deploy behavior.

`scripts/build.sh` uses a shared build/QA lock across worktrees so concurrent agents wait before deploying, clearing logs, stopping Timberborn, or launching QA. `--qa` keeps the lock after launch until QA is complete; release it with `bash scripts/build.sh --release-qa-lock`.

## Documentation

| Need | Read |
| --- | --- |
| Startup order and doc map | [docs/INDEX.md](docs/INDEX.md) |
| Current state and next action | [docs/HANDOFF.md](docs/HANDOFF.md) |
| Orchestration kickoff | [docs/ORCHESTRATION.md](docs/ORCHESTRATION.md) |
| Ticket board | [docs/tickets/README.md](docs/tickets/README.md) |
| Durable design and roadmap | [docs/DESIGN.md](docs/DESIGN.md) |
| Validation runbook | [docs/TEST_PLAN.md](docs/TEST_PLAN.md) |

## Current Focus

Prometheus is in a 3D grid fire rewrite. Phase 2 stabilization is closed for the integrated scope, and active work now tracks through the permanent file board in [docs/tickets/README.md](docs/tickets/README.md). The current validation focus is the field-first resource model: heat/ember/smoke transfer, stochastic entity ignition, moisture loss, fuel depletion, charred remnants, and Fertile Ash recovery reset evidence.

Use [docs/HANDOFF.md](docs/HANDOFF.md) for the latest verified result, blocker list, and resume checklist.

The in-game QA channel reads instructions from `~/Library/Application Support/Timberborn/PrometheusQA/instructions.md` and appends button results to `~/Library/Application Support/Timberborn/PrometheusQA/results.md`.

## Repository Shape

- `Assets/Mods/Prometheus/` is shippable mod content. The deploy script symlinks non-`Scripts` items from this directory into the local Timberborn mod folder, so internal docs do not live there.
- `docs/` is the human/developer documentation home.
- `scripts/` owns local build, deploy, and test entrypoints.
- `tests/Prometheus.Tests/` owns dependency-light regression coverage.
