---
ticket: TKT-008
status: verify
agent_level: High
requires_qa: true
doc_only: false
dependencies:
   - TKT-007
write_scope:
   - Assets/Mods/Prometheus/Scripts/Fire/Recovery/**
   - Assets/Mods/Prometheus/Scripts/Fire/Damage/**
   - Assets/Mods/Prometheus/Scripts/Fire/Visuals/**
   - Assets/Mods/Prometheus/Models/**
   - Assets/Mods/Prometheus/Materials/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
---

# TKT-008: Create Unified Burned Ground Ash Recovery

## Goal

Burned aftermath should leave a visible ground-level burned or ashy state that can also be the common Fertile Ash recovery object for trees, crops, buildings, and future field application.

## Requirements

- Investigate whether the best implementation is a native recovered-good stack, a custom ash deposit, a natural-resource-like yielder, or a lightweight visual marker plus existing recovery stack.
- Prefer a Timberborn-owned harvest or pickup path over fragile direct worker command injection.
- Make the burned ground visual spatially local to the burned source, not a wave or broad overlay.
- Keep the ash recovery object resettable by `Reset Fire State`.
- Avoid making trees, crops, or buildings appear alive after aftermath is created.
- Add tests for source classification, queueing policy, and reset behavior where dependency-light coverage is practical.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --test --launch`.
- Use Computer Use to capture at least one visible ash or burned-ground aftermath object in-game.
- Prove Fertile Ash can be collected or queued for collection without Prometheus exceptions.

## Notes

- The current recovered-good stack path already proved visible Rubble containing `Fertile ash 1`, but user live QA rejected that shape for burned trees. Tree ash should be harvested from the tree remnant itself, not from a loose Rubble stack.
- Current implementation uses a Prometheus-owned local burned-ground ash deposit marker for readability, rewrites tree stump/remnant yielder state to `FertileAsh`, and keeps the existing Timberborn recovered-good stack queue only for non-tree aftermath until TKT-009 resolves crop/building parity.
- Live QA on `Prometheus QA / beginning_safe` captured `burned_ground_ash_deposit_created`, `burned_ground_ash_deposit_marker_created`, `fertile_ash_recovered_good_stack_queued`, and `fertile_ash_spawn_queued` for charred-tree aftermath. Crop and building live proofs still belong to TKT-009/TKT-004.
- Follow-up live QA on `Prometheus QA / beginning cli-safe` repeated the same charred-tree burned-ground marker and recovered-good queue telemetry after a dead-tree reignition probe, which makes `beginning cli-safe` the preferred fixture while `beginning_safe` is suspect.
- Follow-up patch added `fertile_ash_tree_remnant_yield_applied/failed` telemetry and changed charred-tree aftermath to log `fertile_ash_spawn_queued reason=charred_tree_remnant_harvest` without calling the recovered-good stack spawner. `git diff --check`, `bash scripts/test.sh`, and `bash scripts/build.sh --test --launch` passed with `120` tests; live proof still needs a burned Pine remnant that is harvestable as Fertile Ash and does not create visible Rubble.
