# Stabilization Backlog

This backlog captures ticket-sized work implied by the stabilization sprint but not yet represented by active ticket files. These items should become normal ticket files only when the orchestrator is ready to schedule them into the board.

Source of truth: current ticket files own scheduled work. This backlog owns unscheduled follow-ups so broad closeout language does not hide gaps.

## Candidate Tickets

| Backlog ID | Title | Level | Likely Dependencies | Suggested Write Scope |
| --- | --- | --- | --- | --- |
| P2S-B001 | Runtime Visual Projection Migration | Medium | P2S-015, P2S-016, likely P2S-017 | Runtime visual files, projection consumers, visual tests, QA screenshot evidence hooks |
| P2S-B002 | Active Grid Telemetry And Bounds Evidence | Low/Medium | P2S-010, P2S-011, P2S-012 | Grid telemetry counters, telemetry constants/tests, debug/log display only if needed |
| P2S-B003 | Burst Explosion Source Injection Attribution | Medium | P2S-012, P2S-013 | Source injection path, burst/explosion callers, attribution tests, telemetry |
| P2S-B004 | Terrain And Top-Surface Aftermath Eligibility | Medium/High | P2S-014, P2S-018 | Aftermath eligibility model, environment adapter consumers, tests |
| P2S-B005 | Phase 2 Exit Evidence Matrix | Low | All implementation tickets, especially P2S-017, P2S-021, P2S-023, P2S-025 | `docs/TEST_PLAN.md`, `docs/HANDOFF.md`, optional evidence references |

## Notes

- P2S-B001 exists because P2S-015 defines the projection and P2S-016 unifies catalogs, but neither explicitly migrates runtime visual rules to consume projection data or proves visual telemetry matches runtime state.
- P2S-B002 exists because policy tests and stepping ownership do not explicitly provide active chunk/cell/source telemetry or live propagation-bound evidence.
- P2S-B003 exists because P2S-012 models burst source kinds and P2S-013 wires configured sources, but burst/explosion source injection still needs a concrete attribution path.
- P2S-B004 exists because P2S-018 covers initial aftermath eligibility while terrain and top-surface tagging may require environment adapter work after P2S-014.
- P2S-B005 may become a separate ticket or be folded into P2S-027 if closeout owns the full Phase 2 exit evidence matrix.

