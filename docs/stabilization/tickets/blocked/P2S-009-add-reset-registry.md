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

Candidate branch: `codex/P2S-009-add-reset-registry` at `2576cf1`.

What passed:

- `git diff --check`
- `bash scripts/test.sh` with 56 passed after merging current `main`
- `bash scripts/build.sh --qa` reached Prometheus startup readiness on the candidate branch
- Fresh `Player.log` and `Fire.log` showed Prometheus startup with no managed exception before save load
- Candidate commits now preserve the reset registry while adding failure isolation, stale Unity-reference pruning, visual reset null-safety, and deferred per-entity reset hook registration until `Awake`

What is missing:

- Live evidence that the in-game `Reset Fire State` action runs without crashing after the registry change.
- Reliable evidence that the QA save finishes loading on the candidate branch.
- A reliable UI automation path to the Timberborn main menu, in-game toolbar, and Prometheus `Actions` panel.

What was tried:

- Launched Timberborn through `bash scripts/build.sh --qa`.
- Confirmed Timberborn was running and Prometheus startup was detected.
- Inspected fresh `Player.log` and `Fire.log`.
- Observed the candidate branch reach a persistent `LOADING` screen after the save load started; after 75 seconds logs still stopped after `Good group Juice has no goods`.
- Sampled the hung process at `/tmp/timberborn-p2s009-sample.txt`; symbols were mostly unknown and did not expose a Prometheus stack.
- Merged current `main` into the candidate branch and retried after deferring reset hook registration until `Awake`; the save still remained on the `LOADING` screen after the same bounded wait.
- Ran `bash scripts/build.sh --qa` on current `main`; the readiness gate passed but the game remained on the main menu, proving the current `--qa` readiness check does not prove save-load completion.
- Tried manual `cliclick` and Computer Use clicks/Return on the visible main menu `Continue` button; input did not activate the button in the observed run.

Smallest next action:

1. First fix or bypass the UI automation problem so `Continue` can be activated reliably from the main menu.
2. Prove current `main` can load the QA save with that path.
3. Re-test candidate branch `codex/P2S-009-add-reset-registry` at `2576cf1`.
4. If the save loads, open the Prometheus debug `Actions` panel, click `Reset Fire State`, then check `Player.log` and `Fire.log` for `runtime_reset_registry_started`, `runtime_reset_registry_completed`, `runtime_reset_hook_failed`, exceptions, or crashes.
