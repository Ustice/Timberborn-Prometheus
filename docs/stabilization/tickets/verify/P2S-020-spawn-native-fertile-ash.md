# P2S-020 Spawn Native Fertile Ash

Status: verify

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

## Blocker Notes

2026-04-27 orchestrator status:

- Worker became unresponsive after merging current `main`.
- Worktree contains uncommitted partial implementation edits, but no completed worker report or commit.
- No fresh `fertile_ash_*` telemetry was present in `Player.log` or `Fire.log` from the current QA run.
- The ticket cannot be integrated until visible Fertile Ash recovered-good stack spawn, builder collection, and normal storage flow are proven in-game.
- Smallest next action: reassign or resume P2S-020 from `/Users/jasonkleinberg/repos/Timberborn-Prometheus-P2S-020`, inspect the uncommitted partial edits, finish or replace them, run `git diff --check`, `bash scripts/test.sh`, and live QA showing visible stack collection/storage with clean logs.

2026-04-27 reassignment:

- Reopened for a replacement worker to recover or replace the partial implementation.
- Prior blocker still applies until live collection/storage proof exists.
