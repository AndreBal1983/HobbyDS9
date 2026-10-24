# Published tactical support compiler — packet 1

24 October 2026. **Standalone source/CPU candidate only.** No live generated profile, campaign publication, construction admission, queue, provisioning, save schema, content package or balance table calls this compiler. Current immutable publication and `UnsupportedSource` construction behavior are unchanged.

Before implementation, the design index, ship-design/tactical chapters, architecture, v1 ship-design and blueprint contracts, integrated feature-parity boundary and generated V1/V2 identity/provenance paths were inspected. This packet implements only the proposed support-derivation boundary; agreed/proposed/open labels in design chapters remain unchanged.

## API and trust boundary

`PublishedTacticalSupportApplication.Evaluate(frozenCatalogue, publication, currentAccess, support)` delegates to a pure Simulation compiler. Inputs are the existing immutable v1 catalogue/publication, actual current access supplied by its eventual authoritative caller, and explicit support metadata. This first packet has **no authoritative caller in any live profile**. Passing a result, constructing a snapshot or supplying an access list cannot authorize a queue, campaign ship or publication.

The result is `PublishedTacticalSupportResult.IsSupportedByCompiler`, not Build permission. Its `Issues` and original `OrdinaryIssues` explain rejection. Successful `Fit` is a deeply detached read-only `SupportedPublishedTacticalFit`: blueprint/revision/ClassId, source/support fingerprints and support/compiler identities, exact ordinary cost/movement, typed tactical fields, explicit no-cloak policy, zero shield/recharge policy, and empty energy-weapon ammunition. It contains no owner authority, colony, queue, ship provision or mutable pool. Future session code must independently check actor ownership, stamps, yard/resources and exact source identity before using derivation.

`Publish` itself is untouched. There is no supplemental tactical-class registry and no live source admission. An eventual profile pins the support catalogue/compiler and privately derives from the exact immutable blueprint; existing exact blueprint/revision queue references can remain the primary publication reference under a separately reviewed contract.

## Metadata and supported subset

Schema `published-tactical-support-v1`, compiler `published-tactical-support-compiler-v1`. Metadata explicitly requires catalogue ID/revision, v1 source compiler identity, `provisional-test-only` balance status, `unsupported-no-cloak-v1`, complete hull/component mappings and a command-targeting partition value. Omission differs from zero. This packet authors no new real campaign metadata: all numeric values exist only inside tests.

Each installed component must have an exact version mapping and correct source kind. The subset accepts one unit per installation of Reactor, LifeSupport, Command, Propulsion, explicit Beam, and explicit TargetingSensors. Exactly one essential core of each kind is required; duplicate core/sensor roles reject. Separate single-unit beam installations are permitted only within both ordinary physical and explicit tactical aggregate mount limits. Unknown sensors/shields/support/torpedoes/components are not ignored or inferred.

All installed Combat requests and effective allocations must equal their actual source mode's full power. Normal v1 compilation remains authoritative for all profiles, current access, crew/capacity, dependencies, physical mounts, source costs and movement. The compiler re-runs publication and fingerprints the actual frozen source and performance, rejecting caller-created inconsistent `PublishedShipBlueprint` objects.

Hull metadata explicitly authors armor, tactical role and the five tactical ratings, lists supported v1 role IDs and maps named actual hull energy mounts to declared arcs/slots. It must bind the actual source mount ID, coverage tag and bounded physical count. Beam metadata explicitly authors damage, accuracy, tracking, opportunities, cadence and initial fire round, plus actual mount binding/slots/arcs. The source component's mount requirement kind/count/size/tag must match that actual hull mount; tactical slot sums are validated by the existing tactical validator. No aggregate Long/Medium/Close attack or scalar shield value is converted into a tactical weapon or sector.

### Actual sensor dependency and power accounting

The existing test beam component requires powered Sensors under the **unchanged ordinary compiler**. Its legal fixture therefore installs the existing escort-sensor component too. It is not stripped of its dependency or granted an implicit sensor capability.

`TargetingSensors` explicitly maps a Sensors-kind source version to a test-authored tactical sensor rating. The compiler uses the greater of declared hull baseline and installed targeting-sensor rating; it does not convert a v1 sensor output into tactical units or modify strategic coverage. The entire actual full Combat sensor allocation is reserved as Targeting power. When armed, a declared positive amount is also partitioned from the existing command allocation, leaving positive Command essential power. This is a named new derivation policy, not extra free power or an alteration of the original profile.

The test example allocates LifeSupport 6, Propulsion 12, Command 4, Sensors 6 and Beam 8 = **36**. Its tactical reservations are LifeSupport 6, Propulsion 12, Command 3 and Targeting 7, plus Beam 8 = **36**. Both profiles account for precisely the same power. The test-only beam has authored damage 7/tracking 4200/cadence 2; these are not inferred from old attack totals or copied from a cruiser. All those values remain provisional test inputs, not approved live balance.

## Validator reuse, identity and isolation

The derived fit passes the existing `AuthoredTacticalEquipment.Validate` using an explicit one-fit validation catalogue with no ammunition definitions and **empty initial ship provisions**. This exercises the actual shared field, essential power, arc, mount, weapon and aggregate validators without inventing a ship/provision or modifying the old validator. No new source is registered in campaign definitions.

The support and source fingerprints are deterministic semantic SHA-256 values, not new save/content/package identity or a trust signature. A private bounded canonical encoder reads only sealed Contracts metadata/snapshot getter graphs, invariant scalar values and order-normalized lists, with length prefixes and depth/work limits (32 / 100,000). These hashes include all declared support semantics and the independently recompiled source. Metadata order/culture changes preserve identity; a semantic metadata/source change changes it. This is not a general persistence serializer; any future schema/encoder meaning change requires an explicit identity review. Unity/IL2CPP reflection preservation/execution is **not claimed tested** by this portable packet.

Returned primitive values, weapon/mount/essential/capability snapshots and costs use read-only copied snapshot types. Mutating input metadata after evaluation cannot alter a returned result. Failure publishes no partial fit. The compiler performs no IO, RNG draws, host locking, session mutation, new entity allocation or source-content mutation.

## Acceptance scope

Focused cases cover the unarmed and real dependency-complete beam fits; exact costs/movement and total power; no cloak/ammo/shields/recharge; every unsupported installed component; missing/ambiguous schema/compiler/capability/role mappings; source access; full-vs-legal-partial Combat allocation; exact mount tag/size/count/arc/slots and aggregate oversubscription; unsupported quantities; original v1 diagnostics; targeting zero/whole-command/Int64-extreme rejection; forged public publication construction; changed exact source version; detached results; and deterministic identity under reorder/en-US/nl-NL/tr-TR.

Narrow existing v1 ship-design and authored-tactical-equipment suites check compatibility; broad campaign simulation/Unity/native runs are not needed or claimed for this uncalled pure compiler. New live metadata, construction/profile activation, actor-scoped eligibility calls, paid queue/completion/pool/provenance/replay, UI construction controls and physical receiving remain separate approved packets. The safe designer progress package stays immutable.
