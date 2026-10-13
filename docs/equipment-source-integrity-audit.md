# PR #19 equipment-source integrity audit

13 October 2026. **Bounded read-only source review; no defect established in the inspected scope.** Reviewed immutable delivered checkpoint **be02572fc3fc2cfdb90cdd9ad763bbdf3d79716b**. This is not a new full acceptance, review of evolving Stage B code, merge recommendation or playable-v2 claim.

An isolated worktree/branch codex/equipment-source-integrity-audit starts at that checkpoint. Production source, existing tests, scenarios, ci.yml, solution membership and Primary's active worktree are unchanged. Presenter source branches and draft PR #21 remain intact. This audit is a local report/probe delivery, not another published PR.

## Inspected paths and conclusions

| Scope | Source path inspected | Review result |
| --- | --- | --- |
| Session ownership | [SimulationSession constructor](../src/HobbyDS9.Simulation/SimulationSession.cs#L36), its definition copy at line 253, [equipment deep copy](../src/HobbyDS9.Simulation/AuthoredTacticalEquipmentCopies.cs#L10) | Save validation precedes own definitions/state copies. Catalogue, fits, capabilities, reservations, limits, mounts/allowed classes, weapons, provisions and stock rows are copied/canonically ordered; no caller list is retained |
| Immutable projection | [source snapshots](../src/HobbyDS9.Contracts/AuthoredTacticalEquipmentSnapshots.cs#L40) | Scalar values are copied; nested snapshots have get-only facts and newly allocated read-only collections. Public snapshot constructors are not campaign import endpoints |
| Scenario versus save binding | [scenario/save validators](../src/HobbyDS9.Simulation/FoundationValidator.cs#L38), [typed saved-state validation](../src/HobbyDS9.Simulation/GalaxyValidation.cs#L97), [source bindings](../src/HobbyDS9.Simulation/AuthoredTacticalEquipmentValidation.cs#L21) | Scenario requires each provisioned initial ship. Save/current-state validation permits missing destroyed ships; surviving bound ships must retain declared owner/static design, no blueprint substitution, matching fit/design hull limits |
| Exact fit/version | [provision validation](../src/HobbyDS9.Simulation/AuthoredTacticalEquipment.cs#L181), [locked inspection](../src/HobbyDS9.Simulation/SimulationSession.TacticalV2Inspection.cs#L33) | Provision fit ID/revision is checked against catalogue; inspection selects the exact pair for the actual owned ship ID, not a display name or latest fit. Definitions remain content-owned and save content identity remains checked |
| Destroyed sources / new construction | Same binding/inspection paths | Inspection iterates live ships, then looks up provisions by exact authored ShipId. Absence does not recreate a ship; a new ship.spawn identity cannot inherit provisions merely by static design equality |
| Initial versus current resources | [InitialCurrent / InitialAmmunition projection](../src/HobbyDS9.Contracts/AuthoredTacticalEquipmentSnapshots.cs#L89), all references searched through Contracts/Simulation/Application | Fields are explicitly initial content facts. PR #19 adds no mutable current v2 pools or provisioning writes. Inspection has no RNG/allocator/command/state write; repeated load validates/copies state rather than filling stocks |
| Compatibility | [content validation](../src/HobbyDS9.Simulation/AuthoredTacticalEquipment.cs#L46), [typed content guard](../src/HobbyDS9.Simulation/GalaxyValidation.cs#L13), [save version validation](../src/HobbyDS9.Simulation/FoundationValidator.cs#L62) | Legacy Content 10 rejects equipment payload; Content 11 requires valid equipment/schema. Save 14 remains the supported schema; incompatible content/save identities reject explicitly, without migration |
| Frozen v1 provenance | [blueprint restore / source resolver](../src/HobbyDS9.Simulation/CampaignBlueprints.cs#L39), [inspection exact revision dictionary](../src/HobbyDS9.Simulation/SimulationSession.TacticalV2Inspection.cs#L53) | Restore uses the publication's frozen catalogue/fit; ship resolution uses its exact blueprint ID/revision. Current catalogue/access and authored equipment names do not reinterpret a frozen v1 ship |

At this checkpoint, resource persistence/provisioning is intentionally absent, not completed functionality. A host is responsible for exact-byte content hashing; the portable factory validates the supplied content identity and structural bindings. This audit does not treat a caller-created source snapshot as authoritative or claim cryptographic save authentication.

## Existing-test evidence inspected, not rerun

[AuthoredTacticalEquipmentTests](../tests/HobbyDS9.Simulation.Tests/AuthoredTacticalEquipmentTests.cs) contains **149 existing cases**, including **68 required-field omission rows**. The probe independently counts Fact/InlineData plus the actual RequiredFields DTO-property inventory; it does not execute them.

Relevant existing cases were read:

- CallerContentAndReadOnlyProjectionCannotMutateOwnedSourceOrInitialStocks (line 292): caller mutation after StartScenario and read-only snapshot collections.
- ContentVersionsProvenanceAndScenarioOwnedShipBindingsReject (line 258): source owner/design/revision/provenance and 10/11/future schema rejection at scenario ingress.
- ScenarioContentAndSaveRoundTripsRetainSourcesWithoutAddingRuntimeResourceFields (line 325): Save 14 / Content 11, repeated observations, initial seven-round provision and content-hash rejection.
- DestroyedAuthoredShipDoesNotReappearOrReceiveNewStocksOnLoad (line 372): missing authored cruiser on save reload.
- NewLegacyProductionOfTheSameStaticDesignDoesNotInheritAnInitialShipsEquipment (line 382): actual v1 construction yields legacy/ineligible new identity.
- FrozenV1PublicationNeverAcquiresAuthoredEquipmentFromNamesOrTheLatestFit (line 339): v1 publication/restore and name-based conversion refusal.
- NonemptyCommandLedgerAndRejectedCommandsAreUnchangedByRepeatedInspection (line 358): ledger, rejection, save bytes and initial stock unchanged.

Existing [CampaignTacticalV2InspectionTests](../tests/HobbyDS9.Simulation.Tests/CampaignTacticalV2InspectionTests.cs#L126) also covers an actually constructed revision-1 ship after revision 2 is published, plus changed current catalogue/access after save load (line 142). These are retained foundation cases, not 149 additional equipment cases.

The delivered [equipment report](tactical-v2-authored-equipment-foundation.md) records **1,507 full-suite passes** and source/legacy/standalone evidence; the coordinator already verified green exact-source CI. Those executions were not rerun redundantly or claimed as independent audit acceptance.

## Independently executed focused probes

The concrete uncovered coverage question was **save-load ingress**, especially nested caller aliasing after LoadSave and an otherwise consistent changed saved owner/design. The existing equipment cases mutate scenario bindings or caller data after StartScenario; they do not directly exercise all of these load-boundary variants.

[Probe harness](../probes/EquipmentSourceIntegrity/Program.cs) uses actual Application/session factories at the pinned source. No fixture attaches a v2 battle or changes production rules.

| Probe | Trigger / checked behavior | Actual result |
| --- | --- | --- |
| P1 | Load an exported save; mutate original nested capability/reservation/shield/mount-class/weapon/provision data and fit revision, clear caller fit list; mutate input save hull and a later detached export. Compare fresh inspection, cached snapshot and authoritative exported save; attempt collection writes | All remain unchanged; collection writes throw NotSupportedException; labelled initial ammunition remains seven |
| P2 | Change saved authored cruiser owner to an existing foreign actor and update fleet membership consistently. Require binding-specific validation and rejected new-session load; compare original live session | InvalidTacticalEquipmentBinding present; load rejects; original session unchanged |
| P3 | Change saved cruiser static design to an existing Scout, update its entity definition header and set valid Scout hull. Require binding-specific validation rather than merely an identity mismatch | InvalidTacticalEquipmentBinding present; load rejects; original session unchanged |
| P4 | Remove every authored source ship and now-empty fleets/headers from a test-owned valid destruction-shaped save. Reload/inspect/export three times | No authored source ship/pool is recreated, no equipment is projected for survivors, activation remains false and each inspection leaves save bytes unchanged |

P4 represents a valid post-destruction save shape; it is not an independently executed battle or campaign-v2 aftermath. P2/P3 are corrupted detached save DTOs and verify validation, not capture/refit gameplay.

Executed locally: isolated probe locked restore; Release **zero warnings/errors**; **4/4 focused probes passed**; source inventory 149/68 confirmed. No existing test case was added or removed, no full 149/1,507-case rerun, no portable CI/Unity/native execution and no production fix.

Reproduce from this review worktree root:

```powershell
dotnet restore probes/EquipmentSourceIntegrity/EquipmentSourceIntegrity.csproj --locked-mode
dotnet build probes/EquipmentSourceIntegrity/EquipmentSourceIntegrity.csproj -c Release --no-restore
dotnet run --project probes/EquipmentSourceIntegrity/EquipmentSourceIntegrity.csproj -c Release --no-build --no-restore
```

The console probe is outside the normal solution/CI, uses no extra external packages and does not inflate the production test count.

## Outcome and remaining dependencies

**No actionable defect established.** No production patch/minimal-fix request is warranted by these results. The owned source/projection/load boundaries reviewed behave as intended, and there is no discovered inspection/load refill path in PR #19.

This scope does not accept later current-pool initialization, versioned round commitments, save/resume, cloak/visibility, travel/contact or atomic aftermath. The newly delivered gated review API at e117c95c7961bc424f190572f4e23a8bad7cfd08 was communicated while this audit ran; it was not inspected/bound as part of this PR #19 task. A later adapter assignment must review that immutable delivered checkpoint separately and retain default/physical-travel gates.

All probe/report work remains on this isolated local audit branch. Nothing was merged, activated, pushed, messaged to another agent or executed through desktop/Editor tools.
