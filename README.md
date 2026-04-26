# Prometheus Mod (Standalone)

This repository contains the standalone Prometheus Timberborn mod assets, local deploy tooling, and internal project docs.

## Quick Commands

- `bash scripts/build.sh` - compile and deploy.
- `bash scripts/build.sh --test` - run fast tests, then compile and deploy.
- `bash scripts/build.sh --launch` - compile, deploy, clear Timberborn logs, and launch the game for QA.
- `bash scripts/build.sh --qa` - run tests, launch Timberborn, try the normal menu continue flow with `cliclick`, and wait for Prometheus startup readiness.
- `bash scripts/test.sh` - run the fast plain C# regression suite.

Source of truth: run `bash scripts/build.sh --help` for current build/deploy behavior.

## Documentation

| Need | Read |
| --- | --- |
| Startup order and doc map | [docs/INDEX.md](docs/INDEX.md) |
| Current state and next action | [docs/HANDOFF.md](docs/HANDOFF.md) |
| Durable design and roadmap | [docs/DESIGN.md](docs/DESIGN.md) |
| Validation runbook | [docs/TEST_PLAN.md](docs/TEST_PLAN.md) |

## Current Focus

Prometheus is in a 3D grid fire rewrite. The old entity-neighbor spread and responder-first runtime model has been removed from active code, while the visual tuning tool remains available as the new sparse grid model is built.

Use [docs/HANDOFF.md](docs/HANDOFF.md) for the latest verified result, blocker list, and resume checklist.

The in-game QA channel reads instructions from `~/Library/Application Support/Timberborn/PrometheusQA/instructions.md` and appends button results to `~/Library/Application Support/Timberborn/PrometheusQA/results.md`.

## Repository Shape

- `Assets/Mods/Prometheus/` is shippable mod content. The deploy script symlinks non-`Scripts` items from this directory into the local Timberborn mod folder, so internal docs do not live there.
- `docs/` is the human/developer documentation home.
- `scripts/` owns local build, deploy, and test entrypoints.
- `tests/Prometheus.Tests/` owns dependency-light regression coverage.
