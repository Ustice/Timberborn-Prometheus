# P2S-007 Centralize Component Cache Lookup

Status: integration

Agent level: Low

Dependencies: P2S-004

## Objective

Create one home for cached-component traversal and loaded Prometheus entity lookup.

## Requirements

- Replace duplicated component-cache traversal in debug and beaver paths.
- Provide one helper for loaded Prometheus fire entity lookup.
- Keep failure behavior explicit and logged where current code logs.
- Preserve existing runtime behavior.

## Unknowns

- Timberborn component-cache internals may drift between game versions.
- Keep the helper defensive and searchable.

## Write Scope

- `Assets/Mods/Prometheus/Scripts/Core/` or a new integration folder.
- Existing debug and beaver files that currently duplicate cache traversal.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

This is the foundation for reflection probes, reset registry, and farmhouse integration.
