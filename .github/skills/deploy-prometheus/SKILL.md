---
name: deploy-prometheus
description: Deploy the Prometheus mod from this repository to the local Timberborn Mods folder. Use this when asked to deploy Prometheus, run the deploy script, or verify deployment output.
---

# Deploy Prometheus skill

Use this skill when the user asks to deploy the `Prometheus` mod, run deployment, or verify what was copied.

Use the shared checklist in `../_shared/prometheus-deploy-checklist.md` for paths, preconditions, and failure handling.

## Preconditions

Follow the preconditions from `../_shared/prometheus-deploy-checklist.md`.

## Execution steps

1. Run the deployment script from the repository root:
	- `bash scripts/deploy_prometheus.sh`
	- Optional run tests first: `bash scripts/deploy_prometheus.sh --test`
	- Optional launch to main menu/startup: `bash scripts/deploy_prometheus.sh --launch`
	- Optional test+launch in one command: `bash scripts/deploy_prometheus.sh --test --launch`
2. If deployment fails because the DLL is missing, stop and tell the user to build scripts in Unity first.
3. If deployment succeeds, summarize:
	- backup location (`.backups/Prometheus`)
	- deployed manifest `Id` and `Version`
	- runtime payload files copied into `~/Documents/Timberborn/Mods/Prometheus/Scripts`

## Expected script behavior

The script should:

- Validate source mod directory and built DLL.
- Save latest backup to `.backups/Prometheus` (project root) when destination already exists.
- Sync mod files (excluding `*.meta` and `.DS_Store`).
- Copy `Timberborn.ModExamples.Prometheus.dll` and optional `.pdb`.
- Print manifest and runtime payload summary.
- Run automated tests first when `--test` is provided.
- Launch Timberborn via Steam when `--launch` is provided.

## Error handling

Follow common failure handling from `../_shared/prometheus-deploy-checklist.md`.
