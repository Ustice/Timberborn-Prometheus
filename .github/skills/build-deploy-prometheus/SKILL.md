---
name: build-deploy-prometheus
description: Run automated deployment tests and then deploy Prometheus. Use this when asked to test and deploy Prometheus in one flow.
---

# Test + Deploy Prometheus skill

Use this skill when the user wants a gated flow: tests first, deployment second.

Use the shared checklist in `../_shared/prometheus-deploy-checklist.md` for canonical paths, preconditions, and failure handling.

## Workflow

1. Run automated tests:
	- `bash scripts/test_deploy_prometheus.sh`
2. If tests pass, run deployment:
	- `bash scripts/deploy_prometheus.sh`
3. Preferred one-command path:
	- `bash scripts/deploy_prometheus.sh --test`
   - Optional launch after deploy: `bash scripts/deploy_prometheus.sh --test --launch`
4. Validate deployment output:
	- Confirm script reported completion.
	- Confirm latest backup path is `.backups/Prometheus`.
	- Report manifest `Id` and `Version` from destination `manifest.json`.
	- Report copied runtime payload files in destination `Scripts` directory.
	- If launch flag is used, confirm launch request message is printed.

## Failure handling

- If tests fail, do not deploy.
- Follow common failure handling from `../_shared/prometheus-deploy-checklist.md`.

## Reporting format

After running, provide a concise summary with:

- Automated test status (pass/fail and counts)
- Deployment status (success/failure)
- Backup path (`.backups/Prometheus`)
- Manifest fields (`Id`, `Version`)
- Runtime payload list (`Timberborn.ModExamples.Prometheus.dll` and optional `.pdb`)
