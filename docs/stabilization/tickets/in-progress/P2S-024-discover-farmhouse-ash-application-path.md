# P2S-024 Discover Farmhouse Ash Application Path

Status: in-progress

Agent level: High

Dependencies: P2S-020, P2S-022

## Objective

Find the safest way for farmhouses or farmers to consume Fertile Ash and apply field amendments.

## Requirements

- Inspect farmhouse, planting, inventory, work behavior, and good-consuming APIs.
- Prefer decorating existing farmhouse or farmer behavior over building a parallel system.
- Confirm how stored `FertileAsh` can be consumed.
- Confirm how nearby eligible planting spots can be selected.
- Produce evidence and a chosen implementation path.

## Unknowns

- It is unknown whether existing farmhouse behavior can be decorated safely.
- It is unknown whether a new Prometheus-specific behavior is cleaner than using `GoodConsumingBuilding`.

## Write Scope

- Discovery notes in ticket handoff.
- Integration wrapper or small proof if safe.
- No full implementation unless the safe path is obvious and scoped.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- If code is added, run a build check appropriate to new Timberborn references.

## Integration Notes

High agent recommended because this touches Timberborn work and inventory systems.

