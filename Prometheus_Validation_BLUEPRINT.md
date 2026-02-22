# Prometheus_Validation Blueprint

Purpose: define a repeatable, pre-built validation map for Prometheus fire gameplay tuning.

## 1) Map intent

This map is a deterministic test harness for:

- Folktails emergency mass-response behavior
- Ironteeth firehouse/sprayer logistics behavior
- Building ignition-risk tiers
- Explosive detonation behavior
- Controlled burn prep/containment workflow
- Ash/recovery outcome checks

Use this map for all balancing A/B runs before changing defaults.

---

## 2) Global setup rules

- Keep weather/modifier settings fixed across runs.
- Keep game speed and pause points consistent for measurements.
- Use the same initial population and stockpile values per run.
- Only one tuning variable is changed per pass.

### Standard test profile defaults

- Difficulty profile: `Standard`
- Explosion ignition mode: `Off` (default baseline)
- Controlled burn emergency override: disabled
- Debug panel telemetry capture: enabled

---

## 3) Required pre-built zones

## Zone A — Folktails local-response district

Design goal: validate near-core emergency response and bucket-style suppression.

Include:

- Water source close to housing/work cluster
- Early/mid utility buildings
- Alarm point (watch + bell)
- One high-risk ignition candidate nearby

Expected behavior:

- Fast first response
- Strong containment near water
- Performance drops as front extends away from center

## Zone B — Ironteeth industrial-response district

Design goal: validate specialist response in sustained high-heat incidents.

Include:

- Firehouse candidate area
- Engine/smelter-heavy industrial strip
- Refill/logistics route corridor
- Alarm point with larger radius than Zone A

Expected behavior:

- Slower initial response than mass swarm
- Better sustained suppression in high-intensity windows
- Clear logistics dependency

## Zone C — Distance stress front

Design goal: compare response degradation over long travel distance.

Include:

- Ignition area far from both Zone A and Zone B cores
- At least one route bottleneck and one alternate route

Expected behavior:

- Folktails degrade more by distance
- Ironteeth degrade more when refill chain degrades

## Zone D — Hazard block

Design goal: test ignition risk tiers and explosive behavior.

Include at minimum:

- Campfire (low risk)
- Grill (medium risk)
- Smelter (high risk)
- Ironteeth engine (high risk)
- Explosives production/storage cluster
- A placed explosive test strip

Expected behavior:

- Risk ordering should be visible over repeated runs
- Explosives detonate when burning
- No blast-caused fires in baseline (`ExplosionIgnitionMode = Off`)

## Zone E — Controlled burn test field

Design goal: validate planned-burn workflow and post-burn value.

Include:

- Markable burn area adjacent to farm plots
- Perimeter corridor for manual firebreak prep
- Nearby suppression support point

Expected behavior:

- Planned burns are manageable with prep
- Controlled burns should not auto-trigger full emergency mobilization by default
- Ash/recovery value is measurable and temporary

---

## 4) Alarm and danger-level test wiring

For each alert point, verify modes:

- `Off`
- `Auto`
- `Forced On`

Danger-level policy under test:

- Guarded / High / Critical bands
- Hysteresis (avoid alert flapping)
- Recruitment radius expansion on bell escalation

---

## 5) Scenario scripts (manual runbook)

## Scenario 1 — Folktails close-front emergency

1. Trigger uncontrolled fire in Zone A
2. Set alarms to `Auto`
3. Capture first 5 minutes telemetry

Pass cues:

- Fast first suppression action
- Stable initial containment near core

## Scenario 2 — Ironteeth sustained industrial fire

1. Trigger high-intensity fire in Zone B
2. Keep supply chain intact for baseline
3. Re-run with supply disruption

Pass cues:

- Strong sustained suppression in intact run
- Clear drop when refill chain fails

## Scenario 3 — Long-distance dual-front

1. Trigger one front in Zone C and one in Zone B edge
2. Observe dispatch stability and retarget churn

Pass cues:

- No severe assignment thrashing
- Faction-specific performance differences remain readable

## Scenario 4 — Hazard and explosive validation

1. Trigger ignition sequence in Zone D
2. Record ignition order/frequency over repeated runs
3. Trigger explosive detonation case

Pass cues:

- Risk tier behavior aligns with design intent
- Explosion chain behavior does not ignite new fires in baseline mode

## Scenario 5 — Controlled burn economics

1. Mark controlled burn in Zone E
2. Run with prep complete
3. Run with incomplete prep

Pass cues:

- Prepared run yields manageable containment + post-burn benefit
- Unprepared run shows higher risk and reduced payoff

---

## 6) Telemetry fields to capture each run

- `DangerLevel` and band transitions
- `response_state` transitions (overwhelmed/contained/stabilized)
- `spread_pressure`
- `quenching`
- `intensity`
- dispatch lock/hysteresis fields
- retarget suppression indicator
- responder injury pressure (if available)
- explosive event markers (detonation count, blast impact)
- controlled-burn status progression (planned/preparing/ready/active/contained)

---

## 7) Result logging template

| Date | Scenario | Faction | Profile | Key outcome | Pass/Fail | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| YYYY-MM-DD | S1..S5 | Folktails/Ironteeth | Low/Standard/High | ... | Pass/Fail | ... |

---

## 8) Exit criteria for map validity

The map is ready as canonical validation infrastructure when:

- All 5 scenarios can be run without manual rebuilding.
- Baseline runs are reproducible enough to compare tuning deltas.
- Faction differences are observable in at least 3 scenarios.
- Explosive and controlled-burn checks produce consistent expected outcomes.

---

## 9) Naming and versioning

- Canonical map name: `Prometheus_Validation`
- Version suffix on structural changes: `Prometheus_Validation_v2`, `v3`, ...
- Record version used in every balancing note/handoff.
