# P2S-027 Closeout Validation Docs

Status: todo

Agent level: Low

Dependencies: All implementation tickets

## Objective

Close the stabilization sprint with verified evidence and current docs.

## Requirements

- Run required tests and QA.
- Inspect `Player.log` and `Fire.log`.
- Record source-driven spread evidence.
- Record native ash gathering evidence.
- Record storage flow evidence.
- Record farmhouse amendment evidence.
- Record faster amended crop growth than a nearby control crop.
- Update `docs/HANDOFF.md`, `docs/DESIGN.md`, and `docs/TEST_PLAN.md`.

## Unknowns

- Final open issues depend on live QA results.

## Write Scope

- `docs/HANDOFF.md`
- `docs/DESIGN.md`
- `docs/TEST_PLAN.md`
- Optional evidence references if the repo already has an evidence convention.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Inspect logs for Prometheus exceptions.

## Integration Notes

This ticket moves the sprint from implemented to verified.

