# P2S-006 Split Tests By Subsystem

Status: verify

Agent level: Low

Dependencies: P2S-001

## Objective

Split the monolithic test runner into subsystem files.

## Requirements

- Keep `bash scripts/test.sh` as the only required entrypoint.
- Preserve every existing test.
- Group tests by subsystem so gaps are visible from filenames.
- Keep dependency-light tests dependency-light.

## Unknowns

- None known.

## Write Scope

- `tests/Prometheus.Tests/`

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

Integrate before adding new tests from later tickets.
