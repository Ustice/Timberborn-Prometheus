---
ticket: TKT-011
status: verify
agent_level: High
requires_qa: true
doc_only: false
dependencies:
   - TKT-007
   - TKT-008
write_scope:
   - Assets/Mods/Prometheus/Scripts/Fire/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
---

# TKT-011: Add First Fire Suppression Slice

## Goal

Add the first player-visible fire suppression behavior and slow the effective fire timeline enough that players can react before a prepared burn becomes a disaster.

## Requirements

- Prefer heartbeat or pacing-level slowing over retuning every combustible profile.
- Keep accepted tree and crop spread feel as the baseline before suppression is active.
- Add a minimal suppression action or suppression effect that can lower heat, ember pressure, flame sustainment, or fuel consumption in a targeted area.
- Keep suppression effects observable through logs and visuals.
- Avoid coupling suppression to the ash recovery object except where burned-ground aftermath clearly becomes a future cleanup or treatment surface.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --test --launch`.
- Capture one live comparison where unsuppressed fire continues and suppressed fire slows, shrinks, or goes out.
- Scan `Player.log` and `Fire.log` for Prometheus exceptions during the QA window.

## Notes

- The user expects suppression to be the place where fire slows down, not a permanent rollback of the current tuning.
- Implemented first as `Suppress Selected` in the Prometheus Selection panel. It queues a temporary suppression radius around the selected fire-profiled target, dampens local heat/ember/smoke/ignition pressure, and slows fuel consumption while active.
- Live QA after `bash scripts/build.sh --test --launch` captured `fire_suppression_area_queued`, repeated `fire_suppression_area_applied`, and `fire_suppression_area_expired`. A tracked Pine dropped from `intensity=1.000 heat=0.650 ember=0.350 smoke=0.250` to `intensity=0.450 heat=0.450 ember=0.235 smoke=0.165` after suppression.
