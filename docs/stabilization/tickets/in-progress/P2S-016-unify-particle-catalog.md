# P2S-016 Unify Particle Catalog

Status: in-progress

Agent level: Low

Dependencies: P2S-005

## Objective

Use one native particle catalog and scoring path for preview and runtime.

## Requirements

- Remove duplicated source lists where possible.
- Preserve current preferred Timberborn-native particles.
- Preserve preview and runtime behavior unless the unified path reveals a bug.
- Keep native source logging intact.

## Unknowns

- None known.

## Write Scope

- Fire visual runtime files.
- Fire visual preview/authoring files.
- Tests for catalog selection if dependency-light.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

Keep this isolated from visual projection changes.
