# P2S-018 Add Aftermath Eligibility

Status: integration

Agent level: Low

Dependencies: P2S-015

## Objective

Define which burned things can produce Fertile Ash.

## Requirements

- Add tests for valid charred trees.
- Add tests for valid charred buildings.
- Add tests for excluded objects.
- Add placeholders for terrain and top-surface eligibility.
- Keep eligibility separate from ash spawning.

## Unknowns

- Final terrain eligibility waits on environment adapters from P2S-014.

## Write Scope

- Fire aftermath or recovery model files.
- Tests for eligibility.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

This ticket is the policy gate before native ash spawning.
