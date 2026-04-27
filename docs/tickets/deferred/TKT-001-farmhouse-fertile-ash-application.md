---
ticket: TKT-001
status: deferred
agent_level: Medium
requires_qa: true
doc_only: false
dependencies:
   - archived P2S-024 discovery notes
write_scope:
   - Assets/Mods/Prometheus/Scripts/Fire/**
   - Assets/Mods/Prometheus/Scripts/Core/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
---

# TKT-001: Implement Farmhouse Fertile Ash Application

## Goal

Redo farmhouse-driven Fertile Ash application from current source after a fresh live fixture exists. Do not resurrect the discarded P2S-025 prototype as the starting point.

## Requirements

- Use the archived P2S-024 discovery notes as evidence for the intended integration path.
- Apply `FertileAsh` through a farmhouse-scoped path that can select eligible crop planting spots.
- Consume stored District Center `FertileAsh` only after a valid target is selected.
- Apply field amendment state through the existing `FireFieldAmendmentRuntimeState` contract.
- Log a stable `fertile_ash_farmhouse_amendment_applied` event with enough context to prove source, target, and amount.
- Keep trees and bushes excluded from the crop-growth amendment path.

## Required Evidence

- A fresh loadable fixture with a finished farmhouse, eligible crop planting spots, and stored `FertileAsh`.
- `Fire.log` shows `fertile_ash_farmhouse_amendment_applied`.
- Game UI or log evidence shows stored ash decreased after application.
- Amended crop growth is faster than a nearby control crop in the same live save.
- `Player.log` and `Fire.log` have no scanned Prometheus exceptions for the QA window.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Use Computer Use to load the fixture through the normal menu path and capture the live evidence above.

## Notes

- The Phase 2 P2S-025 prototype was intentionally discarded even though it compiled and passed its dependency-light tests.
- The copied `Prometheus P2S-025 QA` fixture was not viable for acceptance evidence because it crashed or hung during load after local repair attempts.
