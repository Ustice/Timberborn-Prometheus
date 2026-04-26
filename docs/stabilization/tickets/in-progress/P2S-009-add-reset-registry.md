# P2S-009 Add Reset Registry

Status: blocked

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

## Blocker

Candidate branch: `codex/P2S-009-add-reset-registry` at `16bcc3d`.

What passed:

- `git diff --check`
- `bash scripts/test.sh` with 45 passed
- `bash scripts/build.sh --qa` reached readiness
- Fresh `Player.log` and `Fire.log` showed Prometheus startup with no reset exceptions

What is missing:

- Live evidence that the in-game `Reset Fire State` action runs without crashing after the registry change.

What was tried:

- Launched Timberborn through `bash scripts/build.sh --qa`.
- Confirmed Timberborn was running and Prometheus startup was detected.
- Inspected fresh `Player.log` and `Fire.log`.
- Attempted to drive the UI to the Prometheus debug actions panel, but did not safely reach the `Reset Fire State` button.

Smallest next action:

- In the loaded QA save, open the Prometheus debug Actions panel, click `Reset Fire State`, then check `Player.log` and `Fire.log` for `runtime_reset_registry_started`, `runtime_reset_registry_completed`, `runtime_reset_hook_failed`, exceptions, or crashes.
