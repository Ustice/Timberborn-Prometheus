# P2S-012 Add Source Attribution Model

Status: done

Agent level: Low

Dependencies: P2S-010

## Objective

Add shared source injection and attribution types.

## Requirements

- Represent debug ignition, configured source, burst source, and controlled-burn source kinds.
- Carry source identity through dependency-light grid tests.
- Keep telemetry naming stable unless tests are updated intentionally.
- Avoid UI wording changes.

## Unknowns

- Final player-facing attribution wording is not decided.

## Write Scope

- Grid/source model files.
- Fire telemetry constants if needed.
- Source attribution tests.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

This ticket prepares configured source implementation but should not wire profile fields yet.
