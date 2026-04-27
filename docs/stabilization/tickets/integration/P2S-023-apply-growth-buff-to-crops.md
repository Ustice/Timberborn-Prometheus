# P2S-023 Apply Growth Buff To Crops

Status: integration

Agent level: Medium

Dependencies: P2S-008, P2S-022

## Objective

Apply Fertile Ash field amendments as a crop growth-speed buff.

## Requirements

- Reduce eligible `Growable` growth time on amended tiles.
- Restore base growth time on expiry and reset.
- Avoid permanently mutating Timberborn template data.
- Distinguish eligible crops from unrelated growables where possible.
- Add a control-vs-amended test or live QA evidence.

## Unknowns

- Crop-versus-tree classification needs integration confirmation.
- Exact tile-to-entity lookup may depend on P2S-014 results.

## Write Scope

- Recovery or renewal effect applier.
- Integration facade growable helpers.
- Amendment state consumers.
- Tests where dependency-light.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.

## Integration Notes

Medium agent recommended because this mutates live `Growable` state.
