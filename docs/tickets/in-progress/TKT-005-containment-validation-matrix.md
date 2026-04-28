---
ticket: TKT-005
status: in-progress
agent_level: Medium
requires_qa: true
doc_only: false
dependencies:
   - TKT-003
   - TKT-004
write_scope:
   - Assets/Mods/Prometheus/Scripts/Fire/**
   - Assets/Mods/Prometheus/Scripts/Debug/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
   - docs/TODO.md
---

# TKT-005: Validate Prepared Burn Containment

## Goal

Prove that prepared burns can be bounded by existing fire-system inputs and player preparation.

## Requirements

- Validate trees, crops, and buildings where fixtures allow.
- Cover moisture, water, barriers, exposed faces, spacing, firebreaks, and profile differences.
- Capture at least one prepared burn that stays bounded.
- Capture at least one unprepared/control burn that spreads more aggressively under comparable conditions.
- Record the smallest reproducible containment matrix in `docs/TEST_PLAN.md`.
- Move any fixture or Timberborn API blockers into `blocked/` with a concrete user-solvable ask.

## Dependencies

- TKT-003 must provide the selected-target ignition handle.
- TKT-004 should provide crop ash behavior before crop containment is accepted as Phase 3 economy proof.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Use Computer Use screenshots or video for prepared and control burns.
- Capture relevant `Fire.log` telemetry and clean `Player.log` / `Fire.log` scans.
- Release the QA lock with `bash scripts/build.sh --release-qa-lock` after evidence capture.

## Notes

- This is the core Phase 3 proof. Do not replace it with a new controlled-burn mechanic.
- 2026-04-27 dependency-light pass: `FireContainmentValidationTests` covers moisture, water firebreak planes, barriers, exposed faces, spacing, and profile thresholds against comparable control burns. Live QA remains required before this ticket can move to acceptance. TKT-003 is in `verify/`, but TKT-004 still needs crop ash visibility follow-up and no prepared/control burn screenshots or `Fire.log` evidence were captured in this pass.
