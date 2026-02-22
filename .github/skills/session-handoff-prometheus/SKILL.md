---
name: session-handoff-prometheus
description: Create or refresh a Prometheus session handoff note capturing current work, reasons, results, blockers, next steps, and key references when context is running low.
---

# Prometheus Session Handoff skill

Use this skill when:

- conversation context is getting long,
- a testing/debugging session is ending,
- the team needs a resumable checkpoint,
- the user asks for a summary + next steps + links.

## Primary outputs

Update (or create) the canonical handoff file:

- `Assets/Mods/Prometheus/SESSION_HANDOFF.md`

Also update the short status section in:

- `README.md` -> `## Latest session status`

This is a mandatory dual-write behavior for this skill.

## Required sections

Always include these sections in the handoff note:

1. **Why we are doing this**
   - current objective and reason.
2. **What we are actively working on**
   - concrete in-flight tasks.
3. **Confirmed results so far**
   - verified outcomes and stability/deploy state.
4. **Open issues / hypotheses**
   - unresolved problems and likely causes.
5. **Next steps (priority order)**
   - specific, ordered, actionable steps.
6. **How to quickly resume**
   - shortest path to continue testing/debugging.
7. **Important files / references**
   - markdown links to docs, scripts, key runtime files, log paths.

## Prometheus-specific references to include

At minimum, ensure links/paths include:

- `Assets/Mods/Prometheus/DESIGN.md`
- `Assets/Mods/Prometheus/TEST_PLAN.md`
- `scripts/deploy_prometheus.sh`
- `scripts/test_deploy_prometheus.sh`
- `README.md`
- key runtime scripts currently being modified (e.g. simulation/debug/effect controllers)
- log file path:
  - `~/Library/Logs/Mechanistry/Timberborn/Player.log`

## Update rules

- Keep facts concise and evidence-based.
- Prefer “verified” statements tied to observed results (build/deploy/log outputs).
- Separate **done** from **next** clearly.
- Include known caveats (e.g. stale-build risk, one-ignite-per-scenario guidance).
- Avoid speculative tuning changes unless marked as hypotheses.
- Keep `README.md` status concise (3-6 bullets max) and point to `SESSION_HANDOFF.md` for full detail.

## README status section contract

When running this skill, ensure `README.md` contains a `## Latest session status` section with:

1. Last updated date
2. Current focus (1-2 bullets)
3. Latest verified result(s) (1-2 bullets)
4. Next step(s) (1-3 bullets)
5. Link/reference to `Assets/Mods/Prometheus/SESSION_HANDOFF.md`

If the section does not exist, create it near the end of `README.md`.

## Recommended workflow

1. Gather current state from recent edits, test output, and logs.
2. Refresh `SESSION_HANDOFF.md` with required sections.
3. Refresh `README.md` -> `## Latest session status` from the same source of truth.
4. Ensure links are relative and valid for this repository.
5. End with a short “resume checklist”.

## Reporting format back to user

After updating handoff:

- confirm file path updated,
- summarize top 3 takeaways,
- summarize top 3 next actions.
