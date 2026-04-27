# P2S-019 Discover Native Ash Gatherable Path

Status: in-progress

Agent level: High

Dependencies: P2S-008, P2S-018

## Objective

Find and wrap the safest Timberborn-native path for visible collectable Fertile Ash.

## Requirements

- Inspect Timberborn gathering, recoverable good, yielding, inventory, and natural resource APIs.
- Prefer native gatherable or recoverable-good flow.
- Produce a wrapper if safe.
- Stop with evidence if native runtime spawning is unsafe.

## Unknowns

- Runtime-spawned gatherables may require templates or natural-resource factory support.
- Recoverable-good APIs may only support demolishable/building recovery.

## Write Scope

- Integration discovery notes in ticket handoff.
- Native ash gatherable wrapper if safe.
- No broad gameplay behavior unless safe path is confirmed.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- If code is added, run a local build check appropriate to the changed references.

## Integration Notes

High agent recommended. This is an API discovery ticket and must not guess.
