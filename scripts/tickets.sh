#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BOARD_DIR="$ROOT_DIR/docs/tickets"
STATUSES=("todo" "ready" "in-progress" "verify" "integration" "done" "blocked" "deferred")

usage() {
  cat <<'USAGE'
Usage:
  scripts/tickets.sh list
  scripts/tickets.sh move <ticket-id-or-path> <status>
  scripts/tickets.sh ready|start|verify|integrate|done|block|defer <ticket-id-or-path>
  scripts/tickets.sh worktree <ticket-id> [branch]
  scripts/tickets.sh cleanup-worktrees [--apply]

Status aliases:
  start      -> in-progress
  integrate -> integration
  block      -> blocked
  defer      -> deferred

cleanup-worktrees is dry-run by default. Pass --apply to remove merged sibling worktrees.
USAGE
}

die() {
  echo "tickets.sh: $*" >&2
  exit 1
}

is_status() {
  local value="$1"
  printf '%s\n' "${STATUSES[@]}" | grep -qx "$value"
}

resolve_ticket() {
  local query="$1"

  if [[ -f "$query" ]]; then
    printf '%s\n' "$query"
    return
  fi

  if [[ -f "$BOARD_DIR/$query" ]]; then
    printf '%s\n' "$BOARD_DIR/$query"
    return
  fi

  local matches
  matches="$(find "$BOARD_DIR" -mindepth 2 -maxdepth 2 -type f -name "*$query*.md" | sort)"

  [[ -n "$matches" ]] || die "no ticket found for '$query'"

  local count
  count="$(printf '%s\n' "$matches" | wc -l | tr -d ' ')"
  [[ "$count" == "1" ]] || die "multiple tickets match '$query':"$'\n'"$matches"

  printf '%s\n' "$matches"
}

move_ticket() {
  local query="$1"
  local status="$2"

  is_status "$status" || die "unknown status '$status'"

  local source
  source="$(resolve_ticket "$query")"

  local dest="$BOARD_DIR/$status/$(basename "$source")"
  [[ "$source" != "$dest" ]] || die "ticket is already in $status"

  git -C "$ROOT_DIR" mv "$source" "$dest"
  echo "$dest"
}

list_tickets() {
  local status
  for status in "${STATUSES[@]}"; do
    echo "[$status]"
    find "$BOARD_DIR/$status" -maxdepth 1 -type f -name '*.md' -print | sort | sed "s#^$ROOT_DIR/##"
  done
}

create_worktree() {
  local ticket="$1"
  local branch="${2:-codex/$ticket}"
  local parent
  parent="$(dirname "$ROOT_DIR")"
  local worktree="$parent/$(basename "$ROOT_DIR")-$ticket"

  [[ ! -e "$worktree" ]] || die "worktree path already exists: $worktree"

  git -C "$ROOT_DIR" worktree add -b "$branch" "$worktree" main
  echo "$worktree"
}

cleanup_worktrees() {
  local apply="false"
  if [[ "${1:-}" == "--apply" ]]; then
    apply="true"
  elif [[ $# -gt 0 ]]; then
    die "unknown cleanup option '$1'"
  fi

  git -C "$ROOT_DIR" worktree list | tail -n +2 | while read -r path _ branch; do
    [[ -n "${path:-}" ]] || continue

    branch="${branch#[}"
    branch="${branch%]}"
    [[ -n "$branch" ]] || continue

    if git -C "$ROOT_DIR" merge-base --is-ancestor "$branch" main 2>/dev/null; then
      if [[ "$apply" == "true" ]]; then
        git -C "$ROOT_DIR" worktree remove "$path"
        echo "removed $path [$branch]"
      else
        echo "would remove $path [$branch]"
      fi
    fi
  done

  if [[ "$apply" == "true" ]]; then
    git -C "$ROOT_DIR" worktree prune
  fi
}

main() {
  local command="${1:-}"
  shift || true

  case "$command" in
    list)
      list_tickets
      ;;
    move)
      [[ $# -eq 2 ]] || die "move requires <ticket-id-or-path> <status>"
      move_ticket "$1" "$2"
      ;;
    ready)
      [[ $# -eq 1 ]] || die "ready requires <ticket-id-or-path>"
      move_ticket "$1" "ready"
      ;;
    start)
      [[ $# -eq 1 ]] || die "start requires <ticket-id-or-path>"
      move_ticket "$1" "in-progress"
      ;;
    verify)
      [[ $# -eq 1 ]] || die "verify requires <ticket-id-or-path>"
      move_ticket "$1" "verify"
      ;;
    integrate)
      [[ $# -eq 1 ]] || die "integrate requires <ticket-id-or-path>"
      move_ticket "$1" "integration"
      ;;
    done)
      [[ $# -eq 1 ]] || die "done requires <ticket-id-or-path>"
      move_ticket "$1" "done"
      ;;
    block)
      [[ $# -eq 1 ]] || die "block requires <ticket-id-or-path>"
      move_ticket "$1" "blocked"
      ;;
    defer)
      [[ $# -eq 1 ]] || die "defer requires <ticket-id-or-path>"
      move_ticket "$1" "deferred"
      ;;
    worktree)
      [[ $# -ge 1 && $# -le 2 ]] || die "worktree requires <ticket-id> [branch]"
      create_worktree "$@"
      ;;
    cleanup-worktrees)
      cleanup_worktrees "$@"
      ;;
    -h|--help|help|"")
      usage
      ;;
    *)
      die "unknown command '$command'"
      ;;
  esac
}

main "$@"
