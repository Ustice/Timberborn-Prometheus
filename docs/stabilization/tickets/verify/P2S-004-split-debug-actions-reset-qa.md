# P2S-004 Split Debug Actions Reset QA

Status: verify

Agent level: Low

Dependencies: P2S-003

## Objective

Extract debug actions, reset commands, and QA tab logic from the debug panel.

## Requirements

- Preserve button names and visible layout.
- Preserve QA result display.
- Preserve telemetry and log messages.
- Preserve current admin command behavior.
- Do not attempt to redesign reset safety in this ticket.

## Unknowns

- Reset safety gaps remain until P2S-009 creates the reset registry.

## Write Scope

- `Assets/Mods/Prometheus/Scripts/Debug/`

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

This ticket prepares reset logic for registry extraction but should stay behavior-preserving.
