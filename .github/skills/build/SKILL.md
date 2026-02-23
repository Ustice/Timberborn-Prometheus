---
name: build
description: Build and deploy this mod from the repository to the local Timberborn Mods folder. Use this when asked to build, deploy, launch after deploy, or verify build/deploy output.
---

# Build skill

Use this skill when the user asks to run the build/deploy loop, deploy the mod, or launch Timberborn after deploy.

Use the shared checklist in `../_shared/build-checklist.md` for paths, preconditions, and failure handling.

## Preconditions

Follow the preconditions from `../_shared/build-checklist.md`.

## Execution steps

1. Run the build script from the repository root:
	- `bash scripts/build.sh`
	- Optional launch flow: `bash scripts/build.sh --launch`
2. If build/deploy fails, report the error and stop before suggesting risky manual changes.
3. If build/deploy succeeds, summarize:
	- backup location (`.backups/Prometheus`)
	- deployed manifest `Id` and `Version`
	- runtime payload symlinks under `~/Documents/Timberborn/Mods/Prometheus/Scripts`
	- launch status (when `--launch` is used)

## Expected script behavior

The script should:

- Compile via `dotnet build` when `Timberborn.ModExamples.Prometheus.csproj` exists.
- Fall back to existing Unity DLL workflow when the project file is missing.
- Enforce stale-build protection (source scripts newer than DLL fails the run).
- Save latest backup to `.backups/Prometheus` (project root) when destination exists.
- Rebuild destination as symlinks (content + runtime DLL/PDB links).
- Print manifest and runtime payload summary.
- When `--launch` is provided: stop running Timberborn, wait for fresh/stable DLL, clear `Player.log` and `Fire.log`, wait 5 seconds, then launch via Steam.

## Useful environment variables

- `BUILD_CONFIGURATION` (default `Debug`)
- `BUILD_PROJECT_DIR` (default repo root, or sibling `../timberborn-modding` if present)
- `BUILD_SKIP_COMPILE=1` (skip `dotnet build`)
- `TIMBERBORN_PLAYER_LOG_PATH` / `TIMBERBORN_FIRE_LOG_PATH` (custom log paths)
- `TIMBERBORN_APP_ID` (override Steam app id)
- `TIMBERBORN_LAUNCH_DRY_RUN=1` (preview launch request without starting game)
- `UNITY_EDITOR_LOG_PATH` (override Unity Editor log path for wait diagnostics)

## Error handling

Follow common failure handling from `../_shared/build-checklist.md`.
