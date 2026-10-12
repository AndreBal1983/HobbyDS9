# Ship Design Infrastructure v1

Status: additive portable foundation implemented on `codex/ship-design-infrastructure-v1`, based on main `36ac251`. The [ship-design chapter](design/ship-design.md) remains the intended design; its balance examples, Engineering portfolio, refits and planner are proposals. This milestone does not alter campaign designs, production, saves or AI.

## Early frontend contract handoff

Portable namespace: `HobbyDS9.Contracts.ShipDesign`. Application entry: `ShipDesignApplication.Create(ShipDesignCatalogueDto)`, returning structured catalogue issues and an optional `ShipDesigner`. The designer exposes an immutable `Catalogue`, `Preview(ShipClassDraftDto, ShipDesignAccessDto)` and `Publish(draft, access, blueprintId, revision)`. Invalid drafts return issues, not ordinary-validation exceptions. Only valid previews publish. Publication freezes selected hull/component values, fit, all profiles, costs and compiled performance; host-owned storage remains outside campaign saves.

Input DTOs are detached editing/content carriers, never campaign state. Catalogue versions have canonical stable version IDs; hull families are separate IDs. Installations have unique authored IDs plus immutable component-version ID and quantity. Each draft explicitly supplies Cruise, Combat and Survey allocations, with requested and allocated power per installation stack. All three profiles are required in this first foundation; no automatic allocation or shedding occurs. Essential minimums are hard constraints; reduced output is legal only with a declared Linear curve and minimum allocation. Fixed systems are enabled at full demand or disabled. Full installed peak demand may exceed available power; actual allocations may not.

Preview exposes `Issues` (stable code, Error/Warning severity, source path and explanation), `IsValid`, capacity/crew/mount usage, reactor and distribution limits, total costs, and ordered per-profile/per-installation requested/allocated/full power and actual outputs. Collections and nested values are readonly detached records. Warnings do not block publication: an unarmed surveyor can be valid. No frontend rule calculation or campaign mutation is needed.

Supported aggregate adapter outputs are MaxHull, Combat's shield/Long/Medium/Close attacks/accuracy/evasion, and Cruise MovementBudget. Current fleet travel takes the slowest member's budget before existing node penalties; this compiler adds no fleet rule. Sensors, Science and operational-range ratings may appear as preview-only capability facts with explicit integration-gap warnings: current campaign has no survey/operational-supply contract. No fuel, shield regeneration, weapon coverage bonus or doctrine/faction/maturity modifier is inferred. Version values are already resolved Engineering outcomes; later combat doctrine remains in the existing resolver.

Fixture values are authored in `scenarios/ship-design-fixtures/fixture.json`, separately from the live DS9 package. A surveyor and escort share one hull; quantities consume shared capacity and package-level mounts once. All examples are provisional and franchise-neutral.


## Implemented contract and arithmetic

The standalone catalogue has `FormatVersion = 1`; its compiler identity is `ship-design-compiler-v1`. These are independent of campaign save 13/content 9/rules `science-engineering-v1` and combat resolver v1. IDs follow existing canonical `DefinitionId` grammar, including rejection of reserved spawn IDs. Version IDs and family/revision pairs are unique within a catalogue. An optional parent must be an earlier same-family version (and same component kind); version parameters are resolved flat values, not inherited bonuses. Factory validation precedes deep snapshot creation. No catalogue/draft DTO is retained.

`HullFamilyDto`, `HullVersionDto`, `ComponentVersionDto`, `ShipRoleDto`, `ShipClassDraftDto`, `ShipDesignAccessDto`, `ShipInstallationDto`, `ShipOperatingProfileDto` and `ProfilePowerAllocationDto` are editing/content inputs. Corresponding snapshots and all result records expose get-only properties and deep readonly lists. Components use declared compatible hull families/tags, package-level mount requirements and per-profile modes. Physical capacity, embarked crew, computing demand, structural design load, reactor/propulsion package counts and envelope sizes are separate budgets. Every installed component has positive physical capacity demand; mounts provide installation eligibility, never free internal space. No mass formula is inferred from capacity.

Costs are hull base plus installed quantity × component cost, including named resources, Industry, credits and informational upkeep. Hull construction workload is positive; costs cannot be negative. Aggregate sums/products use BigInteger and checked Int64 conversion; unrepresentable aggregates produce `ArithmeticOverflow`, never wrapping/clamping/free equipment. Errors/warnings use explicit numeric enums and source paths keyed by IDs/profile kinds; results are sorted ordinally by path, then severity/code/message. Installations, versions, resources, mounts and profiles have explicit canonical ordering. No RNG, timestamps, frame timing, thread scheduling or process hash codes enter compilation.

Mount assignment processes coverage-constrained packages before wildcards, then descending required size; choose the smallest compatible remaining mount, breaking ties by mount ID. Kind/coverage/size must match, with a larger mount accepting a smaller package. Counts multiply once by installation quantity, including unpowered weapons. Weapon versions must declare mounts. Reactor and propulsion have both maximum package counts and per-package size envelopes. `MountUsage` and `EnvelopeUsage` expose actual usage/limits.

Every draft supplies all three operating profiles and exactly one power entry per installation, including zero for disabled optional equipment. Allocations apply to the **whole installation stack**, not each package separately. Mode full/minimum demands multiply by quantity. Allocation must not exceed its request or installed full demand; total allocation cannot exceed min(installed reactor supply, hull distribution). Essential systems cannot be disabled or fall below their declared minimum. Actual powered crew support must cover embarked crew; command capacity must cover powered components' summed computer demand; propulsion support must cover the hull's independent design load. Powered components also require their numeric output dependencies and tags supplied by another powered installation. Disabled optional modes impose no active dependency; installed hardware still consumes space/mounts and its crew requirement.

Fixed modes operate at full power or zero. Linear modes explicitly permit reduced allocation at/above the declared minimum: floor(full output × quantity × allocated stack power / full stack demand). Accuracy, evasion, movement budget and preview range are intensive ratings, so quantity does not multiply their full output. Across installations, these four take the greatest powered rating; other outputs add. This is the v1 compilation formula, not a new fleet rule. Accuracy/evasion remain 0–10000; support systems cannot impersonate weapons, shields or mandatory crew/command/propulsion outputs. Reactor supply is declared net usable power and reactors grant no free powered outputs.

Electrically invalid profiles or profiles with unmet operating dependencies report their requested/allocated totals and issues but expose **no effective outputs**, including per-installation outputs. Any hard issue blocks `Compiled` and publication. Legal partial profiles show only scaled actual output. Role recommendations and unarmed/peak-demand/reduced-power/integration-gap warnings never become universal hard requirements. Essential/support constraints are independent of the selected role.

`CompiledShipPerformance` uses the existing immutable `ShipCombatProfileSnapshot`: Combat shield/attacks/accuracy/evasion, hull-version MaxHull and Cruise MovementBudget. Shields remain encounter-local and hull damage persistent in the existing engine. Existing combat doctrine applies later in the resolver, once. No maturity/faction modifier is invented or reapplied; engineered version values already include their resolved outcome. There is no live-design registration, automatic production replacement, runtime profile switching or fleet aggregation change. Existing travel still uses the slowest ship budget and node penalties.

## Application API example

A host reads the fixture JSON and obtains the catalogue, access and draft DTOs using its own serializer. Unity references Application/Contracts only:

```csharp
using HobbyDS9.Application;
using HobbyDS9.Contracts.ShipDesign;

var created = ShipDesignApplication.Create(catalogueDto);
if (created.Designer == null)
{
    // Render created.Issues; preserve the user's invalid draft/content for editing.
    return;
}
var designer = created.Designer;
var preview = designer.Preview(draftDto, accessDto);
// Render preview.Issues, CapacityUsed/Limit, Mounts, Envelopes, Cost and Profiles.
// Each profile exposes AvailablePower, RequestedPower, AllocatedPower,
// installations and actual outputs; preview.Compiled is null on hard invalidity.
var release = designer.Publish(draftDto, accessDto, "blueprint.utility-escort.v1", 1);
if (release.Published)
{
    var frozen = release.Blueprint;
    // Host may retain this immutable record outside campaign persistence.
    // It pins the full hull/components, fit, profiles, costs and compiled result.
}
```

`Preview` and `Publish` are pure relative to both campaign and designer; invalid inputs return structured results. Publication uses caller-supplied ID/revision, does not allocate campaign entities and does not charge an Engineering tax. There is no mutable blueprint registry yet: an append-only host store must enforce identity uniqueness and retain old records. Do not reuse a version ID with changed values in another catalogue package. This foundation snapshots each catalogue; it does not police a cross-package version repository or define a serialized blueprint reload format.

`ShipDesignAccessDto.AvailableVersionIds` is an explicit deployment whitelist, not permission to queue a live ship. Missing selected versions reject. Science/Engineering-to-version availability derivation and the future authoritative publishing/production command are deferred; a frontend-provided whitelist cannot grant campaign unlocks because the designer has no campaign mutation path. Existing Science requirements, Engineering targets, 600-unit allocation, completion gating and production validation remain unchanged.

## Proposed versus implemented / gaps

Implemented: immutable version snapshots/lineage, editable class DTOs, immutable published records, shared capacity, crew/load/computer support, mount/envelope limits, declared dependencies, three explicit power profiles, fixed/linear scaling, deterministic aggregate compilation, role warnings, detached Application previews and authored surveyor/escort fixtures.

Still proposed: Engineering-generated versions/refinement tracks, diminishing returns/overmaturity, concurrent Engineering portfolio, yards and construction integration, refits/salvage/repair, ship installed-fit provenance in saves, blueprint persistence/registry, AI design/planner, templates/auto-complete UI, faction/maturity modifiers, mass/reliability, coverage-dependent weapon performance, supply-band aggregation and per-phase combat power switching. Nothing here relabels those roadmap proposals as implemented.

Explicit output gaps: Sensors/Science/OperationalRange are powered **preview-only ratings**, with `IntegrationGap` warnings wherever positive. Current campaigns have no surveying/mission evaluation, ship operational supply range or maintenance consumer; upkeep is an informational cost. No supply range becomes fuel units or graph movement cost. No tactical propulsion output implies new combat movement; only already-supported evasion is mapped. Maintenance/resource procurement/yard eligibility remain future host/command integration. No shield regeneration, batteries, ammunition or subsystem damage are introduced.

## Verification

The baseline main had 945 tests; the candidate passes 1,043 tests, including 98 new ship-design cases. New tests cover authored numeric examples, warnings versus errors, required systems, capacity/mount/envelope failure, active dependencies, essential/partial/fixed power, overflow, catalogue/draft validation, version access/lineage, immutable historical records, stable reorder/repeat/grid compilation, Application type boundary and zero campaign mutation. Existing long-run acceptance pins complete saves across repeated, reversed/reordered and reload/checkpoint paths. Portable libraries remain netstandard2.1.

Required commands (repository root, pinned SDK):

```sh
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25 --combat-order reverse
```

Expected unchanged save hash: `83bdb9013db7ac7b2b33ef77c4b591a4bca065544a01912eb48c48161688f5c4`; content hash `d2dc4ec92b535cdcb5b4eab79f46b99617b19e1b048272d46b63721d28438653`; turn 26/Planning/revision 185, AI commands 131, campaign RNG `d8919406d6e01cf1`, next entity 97. All existing production/combat/movement/economy results stay unchanged. No Unity code/assets change and no cloud Unity execution is claimed.
