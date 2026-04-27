# P2S-009 Add Reset Registry

Status: verify

Agent level: Medium

Dependencies: P2S-007

## Objective

Create a reset registry so debug and QA reset paths cannot miss runtime effects.

## Requirements

- Register reset hooks for grid state, source state, damage, workplace, beaver, recovery, visuals, preview state, and ash state.
- Emit reset telemetry.
- Replace broad duplicated reset scans where the registry can safely do so.
- Keep admin reset behavior safe for live Timberborn entities.

## Unknowns

- Hidden Timberborn state mutations may only be discoverable through live QA.
- Keep unknown reset gaps documented in the ticket handoff.

## Write Scope

- Runtime reset registry module.
- Debug reset command call sites.
- Runtime appliers that need reset hook registration.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Inspect `Player.log` and `Fire.log` for reset errors.

## Integration Notes

Do not integrate without QA evidence because reset paths have caused crashes before.

## Verification Evidence

Candidate branch: `codex/P2S-009-add-reset-registry`.

What passed:

- `git diff --check`
- `bash scripts/test.sh` with 56 passed
- `bash scripts/build.sh --qa` with 56 passed, successful compile/deploy, cleared logs, and fresh Timberborn launch
- Computer Use accepted the mod list and experimental-mode dialog, clicked `Continue`, and loaded `Prometheus QA - 2026-04-26 23h01m, Day 3-2.autosave`
- `Player.log` reached `Load time: 12000ms (scene index: 2)`
- Computer Use opened `Prometheus` -> `Actions` and clicked `Reset Fire State`
- The in-game panel reported `Reset fire state for 989 entities`
- `Player.log` and `Fire.log` recorded `runtime_reset_registry_started reason=debug_reset_fire_state globalHooks=7 entityHooks=6923 entities=989`
- `Player.log` and `Fire.log` recorded `runtime_reset_registry_completed reason=debug_reset_fire_state globalHooks=7 entityHooks=6923 entities=989 failures=0`
- `Player.log` and `Fire.log` recorded `debug_reset_fire_exposure result=success globalHooks=7 entityHooks=6923 entities=989 failures=0`

Unknowns resolved:

- The save-load hang was caused by singleton-held reset delegates to transient entity components. The fix keeps global hooks in the singleton registry, but discovers loaded entity hooks at reset time through the component cache.
- The stale-DLL false pass was caused by the external sibling build project compiling its own `Assets/Mods/Prometheus/Scripts` tree. `scripts/build.sh` now points external-project compile items at the active worktree source.

Remaining verification:

- Orchestrator should review the diff, then move through `integration` and `done` after merge-order checks pass on `main`.
