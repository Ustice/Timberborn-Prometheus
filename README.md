# Prometheus Mod (Standalone)

This repository contains the standalone Prometheus mod assets and local deploy tooling.

## Quick commands

* `bash scripts/deploy_prometheus.sh` — deploy only
* `bash scripts/deploy_prometheus.sh --test-only` — tests only
* `bash scripts/deploy_prometheus.sh --test` — test + deploy
* `bash scripts/deploy_prometheus.sh --launch` — deploy + launch Timberborn
* `bash scripts/deploy_prometheus.sh --test --launch` — test + deploy + launch

## Notes

* Latest backup is saved to `.backups/Prometheus`.
* `.backups/` is ignored by Git.
* `steam_appid.txt` is local-only and ignored.
