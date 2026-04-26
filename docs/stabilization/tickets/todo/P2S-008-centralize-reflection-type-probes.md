# P2S-008 Centralize Reflection Type Probes

Status: todo

Agent level: Medium

Dependencies: P2S-007

## Objective

Centralize fragile Timberborn reflection and string type policies.

## Requirements

- Add one compatibility summary for damage, recovery, beaver, workplace, cache, focus, and operation APIs.
- Move type-name classifiers behind one integration-facing API.
- Keep logs clear when a Timberborn API is missing.
- Add dependency-light tests for normalization and classifiers where possible.

## Unknowns

- Exact probe list may expand during DLL inspection.
- Operation-state probes may not be fully known until configured sources are wired.

## Write Scope

- Integration facade or compatibility module.
- Classifier call sites.
- Tests for classifiers and probe result normalization.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

Medium agent should review this ticket because it sets compatibility boundaries for later High tickets.

