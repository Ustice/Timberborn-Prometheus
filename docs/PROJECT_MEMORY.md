# Project Memory — Prometheus

Purpose: durable, human-readable memory for stable project conventions and decisions.

## Scope and repo boundaries

- Primary working repository: `Timberborn-Prometheus`.
- Do **not** modify `timberborn-modding` source as part of Prometheus feature/debug/tuning work.
- Unity compile output consumed by deploy is produced from sibling project `../timberborn-modding` (default), but Prometheus code/design changes stay in this repository.
- Keep code edits, deploy tooling changes, and handoff docs in this repository.

## Build and deploy workflow

- Build source of truth for deploy: `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll` in selected build project (`PROMETHEUS_BUILD_PROJECT_DIR`, default `../timberborn-modding` when present).
- Deploy is symlink-first by default:
  - non-`Scripts` mod content in `~/Documents/Timberborn/Mods/Prometheus` is symlinked to `Assets/Mods/Prometheus/*`
  - runtime `Scripts/Timberborn.ModExamples.Prometheus.(dll|pdb)` are symlinked to build output
- Standard gated deploy command:
  - `bash scripts/deploy_prometheus.sh --test`
- Launch-safe command for playtests:
  - `bash scripts/deploy_prometheus.sh --wait-for-build --test --stop-running --launch`
- Launch timing guard:
  - `--launch` waits 5s before opening Timberborn by default; override with `--launch-delay <seconds>`.
- Deploy script enforces stale-build protection (source `.cs` newer than DLL blocks deploy; no bypass flag).
- `--wait-for-build` now requires DLL freshness and stability across polls before deploy/launch continues, reducing Unity compile race risk.

## Runtime telemetry conventions

- Primary runtime log: `~/Library/Logs/Mechanistry/Timberborn/Player.log`
- Filter token: `[Prometheus/Fire]`
- Baseline capture convention: single `Ignite` press per measurement window.

## Tuning methodology

- Change one variable set at a time.
- Capture before/after from the same scenario window.
- Prefer small, conservative adjustments first.

## Living documents

- Session checkpoint: `Assets/Mods/Prometheus/SESSION_HANDOFF.md`
- Test runbook: `Assets/Mods/Prometheus/TEST_PLAN.md`
- Design and roadmap: `Assets/Mods/Prometheus/DESIGN.md`

## Update rules

- Add entries that are stable across sessions (conventions, workflows, known constraints).
- Keep temporary experiments and scratch notes out of this file.
- If a rule changes, update this file in the same commit as the change.

## Documentation style conventions

- Prefer concise, operational markdown in handoff docs (clear sections, short bullets, explicit next actions).
- Avoid deep nested bullets when possible; flatten to single-line bullets if markdown lint flags list indentation.
- Keep handoff/status updates evidence-based (test/deploy/log outputs) and aligned with repo runbooks.
