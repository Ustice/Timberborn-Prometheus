# P2S-020 Spawn Native Fertile Ash

Status: in-progress

Agent level: High

Dependencies: P2S-019

## Objective

Spawn visible Fertile Ash from valid charred sources and prove normal stockpile flow.

## Requirements

- Spawn ash only from valid aftermath sources.
- Use the safe native path discovered in P2S-019.
- Make ash visible and collectable.
- Prove collected ash enters normal good storage.

## Unknowns

- Exact prefab or template strategy depends on P2S-019.

## Write Scope

- Ash spawning implementation.
- Content templates only if P2S-019 requires them.
- Telemetry and tests where possible.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run live QA sufficient to prove collection and stockpile flow.

## Integration Notes

Do not integrate if ash is only a debug/internal counter.
