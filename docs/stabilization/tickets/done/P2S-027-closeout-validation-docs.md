# P2S-027 Closeout Validation Docs

Status: in-progress

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

Closeout validation on 2026-04-27 passed `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, and post-QA log inspection. `Player.log` showed Prometheus v0.2 loaded and initialized; `Fire.log` recorded the compatibility summary; no scanned Prometheus exceptions were present.

P2S-025 remains outside this sprint. Do not claim live farmhouse amendment proof until a fresh loadable farmhouse/crop/ash fixture captures ash consumption, `fertile_ash_farmhouse_amendment_applied`, and faster amended crop growth than a nearby control.
