---
ticket: TKT-007
status: verify
agent_level: High
requires_qa: true
doc_only: false
dependencies:
   - TKT-003
   - TKT-006
write_scope:
   - Assets/Mods/Prometheus/Scripts/Fire/Damage/**
   - Assets/Mods/Prometheus/Scripts/Fire/Visuals/**
   - Assets/Mods/Prometheus/Scripts/Fire/Recovery/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
---

# TKT-007: Stabilize Tree Dead And Stump Lifecycle

## Goal

Burned trees should progress one way through healthy, dried, dead, stump, and aftermath states without resurrecting, flickering smoke, or holding visible fire after the tree is dead.

## Requirements

- Prevent dead or stump-stage trees from returning to alive visuals or native alive state when they catch fire again.
- Keep flames off once a tree reaches the dead stage.
- Keep smoke visible at dead stage and fade stump smoke quickly.
- Preserve the charred pine texture effect, but delay it until the dead phase.
- Keep Fertile Ash recovery compatible with the chosen terminal tree representation.
- Add or update dependency-light tests for terminal state latching and non-regression where Timberborn runtime dependencies can be isolated.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --test --launch`.
- Use Computer Use and screenshots when needed to prove the tree stays dead/stump through reignition attempts.
- Scan `Player.log` and `Fire.log` for Prometheus exceptions and lifecycle telemetry during the QA window.

## Notes

- Overnight constraint: do not close the working turn before 9am America/New_York on 2026-04-28 unless the user explicitly interrupts or redirects.
- Current suspicion is that native natural-resource lifecycle state and Prometheus visual state can disagree unless dead and stump states are latched in both systems.
- Live QA on `Prometheus QA / beginning_safe` after `bash scripts/build.sh --test --launch` showed one-way dead/stump progression without the prior dead-to-alive resurrection loop. Logs captured charred-tree ash deposit and recovered-good stack queueing from burned Pine aftermath.
- Follow-up live QA on `Prometheus QA / beginning cli-safe` reignited a previously dead Pine. It stayed visually dead/stump-presented instead of resurrecting, and logs captured `visual_surface_material_texture_applied stage=stumpandcharred` plus charred-tree ash aftermath.
- User follow-up disproved the broad pass condition: pre-ash stumps can still flare back to alive trees on fire before final burnout. Patch now clamps raw tree stages through dead/stump latches before `FireDamageEffectApplier` can early-return, and reasserts `#Leftover` every tick once the stump stage is latched. `git diff --check`, `bash scripts/test.sh`, and `bash scripts/build.sh --test --launch` passed with `119` tests; live visual retest is still required.
