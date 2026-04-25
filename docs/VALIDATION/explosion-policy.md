# Explosion Policy Validation

Use this focused checklist only when explosion ignition policy is active or a request/apply gap reappears.

## Goal

Confirm policy behavior matches expectations.

| Policy | Expected Behavior |
| --- | --- |
| `Off` | No explosion-triggered ignition request or apply events. |
| `HighOnly` | Explosion ignition behavior appears only in the `High` profile. |
| `Always` | Explosion ignition request behavior appears in `Low`, `Standard`, and `High`. |

Source of truth: exact event names should come from `FireTelemetryEvents`.

## Evidence Per Run

| Evidence | Notes |
| --- | --- |
| Runtime context | Profile, policy, target building, save/window |
| Panel snapshot | After triggering explosion/fire behavior |
| `Fire.log` / `Player.log` excerpt | Include request/apply/detonation lines and moisture/safety gating when present |

## Run Matrix

Primary target: Explosives Factory.

| Run | Profile | Policy | Expected Result |
| --- | --- | --- | --- |
| 1 | Standard | Off | Detonation may appear; no ignition request/apply. |
| 2 | High | Off | Same as Run 1. |
| 3 | Standard | HighOnly | No ignition request/apply. |
| 4 | High | HighOnly | Ignition request expected; apply depends on moisture/safety checks. |
| 5 | Low | Always | Ignition request expected; apply depends on moisture/safety checks. |
| 6 | Standard | Always | Same as Run 5. |
| 7 | High | Always | Same as Run 5. |

## Pass Criteria

- [ ] `Off` mode shows no explosion ignition request/apply events.
- [ ] `HighOnly` mode shows explosion ignition request/apply behavior only in `High`.
- [ ] `Always` mode shows explosion ignition request behavior in all profiles.
- [ ] Missing apply events are explained by logged moisture/safety gating rather than missing request events.

## Run Hygiene

- Use one deliberate ignition trigger per measurement window when possible.
- Wait for logs to settle before starting the next run.
- Annotate each saved log snippet with profile and policy.
