# P2S-027 Closeout Validation Docs

Status: todo

Agent level: Low

Dependencies: All integrated implementation tickets; P2S-025 is explicitly pushed out of this sprint.

## Objective

Close the stabilization sprint with verified evidence and current docs.

## Requirements

- Run required tests and QA.
- Inspect `Player.log` and `Fire.log`.
- Record source-driven spread evidence.
- Record native ash gathering evidence.
- Record storage flow evidence.
- Record the P2S-025 farmhouse amendment deferral and evidence gap.
- Record that faster amended crop growth is dependency-light verified only, not live farmhouse-applied.
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
