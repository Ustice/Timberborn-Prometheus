# Prometheus deploy checklist (shared)

Use this shared checklist from Prometheus-related skills.

## Core paths

- Deploy script: `scripts/deploy_prometheus.sh`
- Test script: `scripts/test_deploy_prometheus.sh`
- Source mod directory: `Assets/Mods/Prometheus`
- Required build artifact: `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll`
- Optional debug artifact: `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.pdb`
- Deploy destination: `~/Documents/Timberborn/Mods/Prometheus`
- Runtime destination: `~/Documents/Timberborn/Mods/Prometheus/Scripts`
- Backup destination: `.backups/Prometheus` (project root, latest only)

## Preconditions

1. Confirm deploy script exists.
2. Confirm source mod directory exists.
3. Confirm required DLL exists. If missing, stop and ask user to build scripts in Unity first.

## Deployment execution

1. Run from repository root: `bash scripts/deploy_prometheus.sh`
2. Optional test-only check: `bash scripts/deploy_prometheus.sh --test-only`
3. Optional launch after deploy: `bash scripts/deploy_prometheus.sh --launch`
4. If destination exists, save latest backup snapshot to `.backups/Prometheus`.
5. Ensure deployment reports completion.

## Post-deploy verification

- Destination `manifest.json` is present and readable.
- Manifest summary includes `Id` and `Version`.
- Runtime payload contains `Timberborn.ModExamples.Prometheus.dll` and optional `.pdb`.
- Backup path is in project root (`.backups/Prometheus`) rather than home Mods directory.

## Common failure messages

- Missing script: repository misconfiguration.
- Missing source mod dir: mod content missing.
- Missing DLL: Unity build required before deploy.
- Missing `rsync`: install dependency and re-run.
- Non-zero script exit: report stderr and stop.
