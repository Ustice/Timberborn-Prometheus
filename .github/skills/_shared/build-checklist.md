# Build checklist (shared)

Use this shared checklist for the build/deploy flow.

## Core paths

- Build script: `scripts/build.sh`
- Source mod directory: `Assets/Mods/Prometheus`
- Required build artifact: `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll`
- Optional debug artifact: `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.pdb`
- Deploy destination: `~/Documents/Timberborn/Mods/Prometheus`
- Runtime destination: `~/Documents/Timberborn/Mods/Prometheus/Scripts`
- Backup destination: `.backups/Prometheus` (project root, latest only)

## Preconditions

1. Confirm build script exists.
2. Confirm source mod directory exists.
3. Confirm source and destination paths are accessible (`Assets/Mods/Prometheus`, `~/Documents/Timberborn/Mods/Prometheus`).

## Build/deploy execution

1. Run from repository root: `bash scripts/build.sh`
2. Optional launch after deploy: `bash scripts/build.sh --launch`
3. If destination exists, ensure latest backup snapshot is saved to `.backups/Prometheus`.
4. Ensure script reports completion.

## Post-build verification

- Destination `manifest.json` is present and readable.
- Manifest summary includes `Id` and `Version`.
- Runtime payload links point to `Timberborn.ModExamples.Prometheus.dll` and optional `.pdb`.
- Backup path is in project root (`.backups/Prometheus`) rather than home Mods directory.

## Common failure messages

- Missing script: repository misconfiguration.
- Missing source mod dir: mod content missing.
- Missing DLL: Unity build required or compile step failed.
- Missing .NET SDK: install SDK or use `BUILD_SKIP_COMPILE=1` fallback mode.
- Stale build detected: source scripts are newer than DLL; rebuild scripts and rerun.
- Non-zero script exit: report stderr and stop.
