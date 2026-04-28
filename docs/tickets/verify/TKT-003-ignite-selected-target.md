---
ticket: TKT-003
status: verify
agent_level: Medium
requires_qa: true
doc_only: false
dependencies:
   - TKT-002
write_scope:
   - Assets/Mods/Prometheus/Scripts/Debug/**
   - Assets/Mods/Prometheus/Scripts/Fire/**
   - Assets/Mods/Prometheus/Scripts/Core/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
---

# TKT-003: Add Ignite Selected Target

## Goal

Add a player-facing `Ignite Selected` action under the existing Prometheus tool surface. It should intentionally ignite one selected valid target by reusing the existing forced-ignition/grid-seeding path.

## Requirements

- Reuse the current forced-ignition/grid-seeding behavior instead of adding a parallel ignition system.
- Require a selected target with a valid fire profile.
- Reject invalid selections with visible UI feedback.
- Log stable telemetry for successful ignition and invalid-target attempts.
- Keep the action narrow: no zone painting, scheduling, permits, controlled-burn source type, labor path, or new building mechanic.
- Keep `Reset Fire State` behavior compatible with the new entrypoint.

## Dependencies

- TKT-002 must be complete.
- Review the current forced/debug ignite implementation before choosing UI and telemetry names.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Use Computer Use to prove valid selected-target ignition and invalid-target feedback in game.
- Scan `Player.log` and `Fire.log` for Prometheus exceptions during the QA window.
- Release the QA lock with `bash scripts/build.sh --release-qa-lock` after evidence capture.

## Notes

- This ticket creates the user-facing ignition handle for prepared burns; it does not prove containment by itself.
- Code implementation is present in the shared worktree: the button is renamed to `Ignite Selected`, invalid selections log `ignite_selected_rejected`, valid requests log `ignite_selected_queued`, and the forced-ignition queue now reports whether a request was accepted. Live QA proves valid selected-target ignition for trees and carrots. Logs also captured invalid-target rejection for `WeatherStation.Folktails(Clone)` with `ignite_selected_rejected`, so this ticket is ready for verification review.
- Save-load QA for this ticket should use the `Prometheus QA` settlement and allow two minutes before treating the load as hung. The 2026-04-27 world-load gate fix loaded `Prometheus QA - 2026-04-27 15h28m, Day 4-11.autosave` in `12644ms`.
