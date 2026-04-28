---
ticket: TKT-010
status: blocked
agent_level: High
requires_qa: true
doc_only: false
dependencies:
   - TKT-008
   - TKT-009
write_scope:
   - Assets/Mods/Prometheus/Scripts/Fire/Recovery/**
   - Assets/Mods/Prometheus/Scripts/Core/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
---

# TKT-010: Apply Fertile Ash Through Agriculture Buildings

## Goal

Fertile Ash should become useful from the farm first, then through the broader agriculture building family once the recovery loop is stable.

## Requirements

- Start from the archived farmhouse discovery notes, but reimplement from current source rather than resurrecting the discarded prototype.
- Apply ash only after selecting a valid crop target or field cell.
- Consume stored `FertileAsh` only after target selection succeeds.
- Extend the working pattern from farmhouse to other agriculture buildings only after the farmhouse path is proven.
- Keep trees and bushes excluded from the crop-growth amendment path unless a later design explicitly changes that.
- Log stable application, consumption, and target telemetry.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --test --launch`.
- Capture `fertile_ash_farmhouse_amendment_applied` or its successor event.
- Prove stored ash decreases and amended crop growth beats a nearby control crop in the same live save.

## Notes

- This ticket supersedes deferred TKT-001 only after a fresh loadable fixture and live evidence exist.
- A compile-clean first-slice scaffold now exists in `FarmHouseFertileAshAmendmentWorkplaceBehavior`: it selects an in-range planting coordinate, checks for an existing amendment, consumes one stored `FertileAsh`, applies `FireFieldAmendmentRuntimeState.SetAmendment`, and logs `fertile_ash_farmhouse_amendment_applied`.
- The `FarmHouse` workplace decorator registration is intentionally disabled in `PrometheusConfigurator` until live QA is reliable. During 2026-04-28 QA, `Prometheus QA / beginning_safe` reached `world_load_state_changed ready=true stage=post_load` but the rendered game stayed on Timberborn's `LOADING` screen, so no trustworthy farmhouse worker evidence was captured.
- `Prometheus QA / beginning cli-safe` loaded normally afterward, so the immediate blocker is no longer global save loading. This ticket still needs a fresh fixture with a finished farmhouse, eligible crop targets, and stored `FertileAsh` before enabling the decorator.
