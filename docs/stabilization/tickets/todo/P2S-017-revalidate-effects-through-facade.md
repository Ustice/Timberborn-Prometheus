# P2S-017 Revalidate Effects Through Facade

Status: todo

Agent level: Medium

Dependencies: P2S-008, P2S-009, P2S-015

## Objective

Route effect assumptions through the integration facade and reset registry.

## Requirements

- Revalidate workplace, beaver, damage, and recovery effects.
- Remove proven-unsafe fallbacks.
- Use centralized type and reflection policies.
- Ensure each effect has a registered reset path.

## Unknowns

- Live game may reveal additional Timberborn mutation paths.

## Write Scope

- Fire effect appliers.
- Integration facade call sites.
- Reset registry registrations.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Inspect `Player.log` and `Fire.log`.

## Integration Notes

Do not integrate without live QA because effect mutation is difficult to prove with dependency-light tests only.

