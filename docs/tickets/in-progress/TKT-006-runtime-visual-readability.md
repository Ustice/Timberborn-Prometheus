---
ticket: TKT-006
status: in-progress
agent_level: Medium
requires_qa: true
doc_only: false
dependencies:
   - TKT-003
   - TKT-005
write_scope:
   - Assets/Mods/Prometheus/Scripts/Fire/**
   - Assets/Mods/Prometheus/Scripts/Debug/**
   - tests/Prometheus.Tests/**
   - docs/HANDOFF.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
---

# TKT-006: Improve Runtime Visual Readability

## Goal

Map enough runtime grid and exposure state into smoke, fire, steam, char, and ember visuals that prepared burns are understandable without reading debug logs.

## Requirements

- Preserve the existing selected-entity visual authoring tool.
- Connect runtime grid/exposure state to smoke, fire, steam, char, and ember feedback where the current architecture supports it.
- Keep local object sparks disabled when ember-field visuals are responsible for spread pressure.
- Avoid visual spam during prepared-burn QA.
- Add or update tests for visual projection policy where the rules can stay dependency-light.
- Document remaining visual gaps if fixture evidence shows a specific state is still unreadable.

## Dependencies

- TKT-003 must provide a repeatable ignition entrypoint.
- TKT-005 should identify the containment states that need to be readable.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Use Computer Use screenshots or video to prove prepared-burn readability.
- Scan `Player.log` and `Fire.log` for Prometheus exceptions during the QA window.
- Release the QA lock with `bash scripts/build.sh --release-qa-lock` after evidence capture.

## Notes

- The acceptance bar is practical readability during a burn, not perfect art tuning.
- Dependency-light visual policy is implemented for existing projection state: ember pressure shows restrained ember-field feedback on exposed non-burning targets, local sparks stay disabled on actively burning or dead targets, and low ember noise stays hidden. TKT-003 is in `verify/`; live prepared-burn QA still needs screenshots or video plus `Player.log` / `Fire.log` scans after TKT-005 provides the repeatable containment fixture.
