# Integrated campaign activities — provisional playtest content

This is the first content/compile gate of the approved integrated activity
milestone. It is not yet a selectable campaign, a player package or Unity/native
acceptance. Existing generated campaigns and their saves remain unchanged.

`catalogue.json`, `ready-classes.json` and `support.json` are authoritative named
playtest definitions, not mock UI statistics. The three hulls are distinct IDs,
families, capacities, mount limits, hull strength and costs. Every ready fit is
published through the ordinary design compiler and then independently derived
through the explicit activity-support v3 compiler. Artwork supplies no ratings.

| Ready class | Capacity used/limit | Hull | Credits / Industry | Beam mounts used | Study work / Contact capacity |
| --- | --- | --- | --- | --- | --- |
| Constitution Explorer | 94 / 100 | 30 | 74 / 108 | 1 | 3 / 1 |
| Galaxy Cruiser | 116 / 160 | 60 | 128 / 175 | 2 | 4 / 3 |
| Defiant Combat | 70 / 70 | 24 | 68 / 107 | 3 | 1 / 0 |

All numbers are owner-authorized reversible **provisional playtest tuning**, not
final balance. Defiant has explicitly authored compact reactor/life-support/
command/propulsion/targeting versions with their own capacities and costs; its
70 capacity was not met by weakening ordinary validation. Galaxy requires 40
crew, Explorer 30 and Defiant 20. Ready fits contain fully allocated legal power
profiles; no player power editing is required to use them.

The new support composition uses
`published-tactical-activity-support-v3` and
`published-tactical-activity-compiler-v3`. Its entire typed tactical and activity
metadata, including module powers/work/contact outputs, binds the support/fit
identity. Exact activity module kinds are laboratories and diplomatic suites;
Cruise/Combat power and outputs are zero, while Survey requires full actual
allocated power. At most one module of each activity kind is supported on a ship.
No activity module creates combat power, attack, command, sensors or equipment.
The v2 public compiler still rejects these modules. All ordinary capacity, access,
crew, computing, mounting, profile and source/cost recompilation checks remain.

Successful projections report the v3 compiler/catalogue/revision/provenance and
provide deeply detached complete metadata copies. Public snapshot constructors
are advisory, never permission: the future activity profile must rederive from
its externally pinned complete package at ingress/replay, including all source
and power facts. An old v2 inner catalogue paired with a claimed v3 hash is not a
source-admission route.

The class gate covers actual legal fits, genuine tradeoffs, disabled/underpowered
science rejection, combat-draw rejection, forged cost/access/identity, malformed
metadata, deterministic ordering and copy isolation. Existing v2 support/source
goldens are retained. The integrated mixed production queue, structures, finite
events, minor worlds, versioned save/host/UI and capacity revision are the next
approved implementation work; this first gate does not claim those exist.

Design context: [ship design](../../docs/design/ship-design.md),
[exploration](../../docs/design/exploration-missions.md),
[architecture](../../docs/architecture.md).
