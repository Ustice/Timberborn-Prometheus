---
ticket: TKT-004
status: in-progress
agent_level: Medium
requires_qa: true
doc_only: false
dependencies:
   - TKT-002
write_scope:
   - Assets/Mods/Prometheus/Scripts/Fire/**
   - Assets/Mods/Prometheus/Scripts/Core/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
---

# TKT-004: Produce Fertile Ash From Burned Crops

## Goal

Extend aftermath eligibility so valid burned crops can produce visible, collectible Fertile Ash alongside trees and buildings.

## Requirements

- Add `CharredCrop` or an equivalent source classification for burned crop aftermath.
- Define a crop ash amount policy that is intentionally smaller than or otherwise distinct from large source types when appropriate.
- Queue `FertileAsh` through the existing recovered-good stack path.
- Include source kind and crop context in `fertile_ash_*` telemetry.
- Add dependency-light tests for crop eligibility, amount policy, and non-crop exclusions.
- Keep farmhouse ash application out of scope; TKT-001 remains deferred.

## Dependencies

- TKT-002 must be complete.
- TKT-003 is useful for live QA but should not be required for dependency-light implementation if the existing forced ignite path can create the fixture state.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Use Computer Use to prove a visible recovered-good stack from a burned crop.
- Prove District Center storage receives the Fertile Ash after pickup.
- Capture `fertile_ash_*` telemetry with crop/source-kind context.
- Scan `Player.log` and `Fire.log` for Prometheus exceptions during the QA window.
- Release the QA lock with `bash scripts/build.sh --release-qa-lock` after evidence capture.

## Notes

- If live crop burnout cannot be produced quickly, move the ticket to `blocked/` with the smallest fixture request needed.
- Dependency-light implementation is committed in `13d8ee36aacb9b51aa8c19688db49da88ce5ddce`: `CharredCrop` aftermath classification, crop ash amount policy, existing recovered-good stack queueing, crop telemetry context, reset telemetry context, and tests are in place. Live crop stack visibility and clean-log proof are still required before this ticket can move to verify.
- Live QA on 2026-04-27 proved carrot `Ignite Selected` now works after the carrot crop `FireProfileSpec` overlay. Logs captured carrot burn completion plus `fertile_ash_recovered_good_stack_queued` and `fertile_ash_spawn_queued` with `sourceKind=charredcrop`, `damageCategory=crop`, and `cropContext=burned_crop`. User live QA confirmed District 1 storage reached `Fertile ash 18` after the carrot burns, but the user did not see ash goods on the ground. Explicit visible recovered-good stack proof remains missing; investigate whether the stack is too brief, hidden by crop remnants, picked up immediately, spawned at an unexpected coordinate, or visually indistinguishable from other recovered goods.
- Live QA on 2026-04-28 repeated `ignite-first-crop` in the current `Prometheus QA` save after TKT-009 building proof. Logs again captured `qa_command_result command=ignite-first-crop result=success`, `burned_ground_ash_deposit_created amount=1 sourceKind=charredcrop damageCategory=crop cropContext=burned_crop coordinates=36,4,22`, `fertile_ash_recovered_good_stack_queued`, and `fertile_ash_spawn_queued reason=charred_crop`. Computer Use selected nearby carrots but did not capture a visible/selectable Fertile Ash stack, so the remaining blocker is still visual stack/pickup proof rather than crop aftermath telemetry.
