---
ticket: TKT-009
status: verify
agent_level: Medium
requires_qa: true
doc_only: false
dependencies:
   - TKT-008
write_scope:
   - Assets/Mods/Prometheus/Scripts/Fire/Damage/**
   - Assets/Mods/Prometheus/Scripts/Fire/Recovery/**
   - Assets/Mods/Prometheus/Scripts/Fire/Visuals/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
---

# TKT-009: Extend Aftermath Parity To Buildings And Crops

## Goal

Buildings and crops should use the same aftermath concepts as trees: clear visual progression, local burned aftermath, Fertile Ash recovery, and reset-safe terminal state.

## Requirements

- Align building, crop, and tree aftermath classification under one ash policy.
- Keep crop fires local enough for field QA while still allowing crop-to-crop spread.
- Keep building terminal state from recovering while still allowing future repair or suppression design to be added deliberately.
- Ensure Fertile Ash source kind and amount telemetry is stable for trees, crops, and buildings.
- Add or update tests for building and crop aftermath eligibility.

## Verification

- [x] Run `git diff --check`.
- [x] Run `bash scripts/test.sh`.
- [x] Run `bash scripts/build.sh --launch`.
- [x] Use logs and screenshots to prove one crop source reaches aftermath without Prometheus exceptions.
- [x] Use logs to prove one building source reaches aftermath without Prometheus exceptions.

## Notes

- This ticket should not invent a separate building-only or crop-only ash system unless TKT-008 proves a shared object is impossible.
- Live crop proof used `ignite-first-crop` on `Carrot(Clone)` and logged `sourceKind=charredcrop`, `damageCategory=crop`, `cropContext=burned_crop`, burned-ground marker creation, `fertile_ash_recovered_good_stack_queued`, and `fertile_ash_spawn_queued reason=charred_crop`.
- Live building proof used `ignite-first-building` on a newly placed unfinished `Bakery.Folktails(Clone)` construction site and logged `sourceKind=charredbuilding`, `damageCategory=building`, burned-ground marker creation, `fertile_ash_recovered_good_stack_queued amount=4`, and `fertile_ash_spawn_queued reason=charred_building`.
- For repeat QA, an unfinished Bakery construction site is enough for the building burn; it does not need to be completed first.
