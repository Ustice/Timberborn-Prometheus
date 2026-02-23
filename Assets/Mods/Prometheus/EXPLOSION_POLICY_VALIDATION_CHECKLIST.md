# Explosion Policy Validation Checklist

Use this checklist to validate explosion ignition policy behavior with log-backed evidence.

## Goal

Confirm policy behavior matches expectations:

- `Off`: no explosion-triggered ignition attempts or applies.
- `HighOnly`: explosion-triggered ignition behavior appears only in `High` profile.
- `Always`: explosion-triggered ignition behavior appears in all profiles.

## Evidence to capture per run

For each run, capture:

1. Runtime context note (profile + policy + target building).
2. Panel snapshot after triggering explosion/fire behavior.
3. Relevant `[Prometheus/Fire]` lines containing:
   - `explosion_detonated`
   - `explosion_ignition_request`
   - `explosion_ignite_applied`

Log file path: `~/Library/Logs/Mechanistry/Timberborn/Player.log`

## Recommended run matrix

Primary target: Explosives Factory.

| Run | Profile | Explosion policy | Expected result |
| --- | --- | --- | --- |
| 1 | Standard | Off | `explosion_detonated` may appear; no `explosion_ignition_request`/`explosion_ignite_applied` |
| 2 | High | Off | Same as Run 1 |
| 3 | Standard | HighOnly | No `explosion_ignition_request`/`explosion_ignite_applied` |
| 4 | High | HighOnly | `explosion_ignition_request` expected; `explosion_ignite_applied` may occur depending on moisture/safety checks |
| 5 | Low | Always | `explosion_ignition_request` expected; `explosion_ignite_applied` may occur depending on moisture/safety checks |
| 6 | Standard | Always | Same expectation as Run 5 |
| 7 | High | Always | Same expectation as Run 5 |

## Pass criteria

Validation is complete when all are true:

1. `Off` mode shows no explosion ignition request/apply events.
2. `HighOnly` mode shows explosion ignition request/apply behavior only in `High` profile.
3. `Always` mode shows explosion ignition request behavior in `Low`, `Standard`, and `High` profiles.
4. Any missing `explosion_ignite_applied` events can be explained by logged moisture/safety gating rather than missing request events.

## Run hygiene

- Use a single deliberate ignition trigger per measurement window when possible.
- Wait for logs to settle before starting next run.
- Annotate each saved log snippet with profile/policy so later comparisons are unambiguous.
