# Project Memory

Purpose: durable repo conventions that are stable across sessions and do not belong in live handoff notes.

## Repo Boundaries

- Primary working repository: `Timberborn-Prometheus`.
- Do not modify `../timberborn-modding` source as part of Prometheus feature, debug, or tuning work unless explicitly requested.
- Prometheus code, deploy tooling, tests, and human/developer docs stay in this repository.

## Source Of Truth

| Topic | Source |
| --- | --- |
| Build and launch behavior | `bash scripts/build.sh --help` and `scripts/build.sh` |
| Test behavior | `bash scripts/test.sh` and `tests/Prometheus.Tests` |
| Runtime telemetry names | `FireTelemetryEvents` |
| Current live project state | [HANDOFF.md](HANDOFF.md) |
| Validation workflow | [TEST_PLAN.md](TEST_PLAN.md) |
| Durable design decisions | [DESIGN.md](DESIGN.md) |

## Documentation Rules

- Keep `Assets/Mods/Prometheus/` limited to shippable mod content because the deploy script symlinks non-`Scripts` assets into the local Timberborn mod folder.
- Keep live status in [HANDOFF.md](HANDOFF.md), validation gates in [TEST_PLAN.md](TEST_PLAN.md), and durable design in [DESIGN.md](DESIGN.md).
- Prefer links to source, commands, tests, and logs over copied inventories that can drift.
