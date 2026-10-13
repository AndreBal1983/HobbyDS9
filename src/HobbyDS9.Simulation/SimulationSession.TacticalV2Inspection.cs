using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    public sealed partial class SimulationSession
    {
        // Deliberately enumerated current-schema gaps, not a configurable whitelist.
        // These remain blockers even for an unarmed ship or a campaign with no ships.
        private static readonly CampaignTacticalV2Blocker[] TacticalV2FitGaps =
        {
            CampaignTacticalV2Blocker.ExplicitEquipmentSchemaRequired,
            CampaignTacticalV2Blocker.WeaponClassesAndArcsRequired,
            CampaignTacticalV2Blocker.WeaponCadenceAndMagazineDefinitionsRequired,
            CampaignTacticalV2Blocker.DirectionalShieldLimitsAndRechargeRequired,
            CampaignTacticalV2Blocker.ArmorDefinitionRequired,
            CampaignTacticalV2Blocker.FormationCapabilityDerivationRequired
        };
        private static readonly CampaignTacticalV2Blocker[] TacticalV2LifecycleGaps =
        {
            CampaignTacticalV2Blocker.PersistentShipResourcesAndRecoveryPolicyRequired,
            CampaignTacticalV2Blocker.SessionRoundCheckpointAndOrderAuthorityRequired,
            CampaignTacticalV2Blocker.StrategicRetreatAndObjectivePolicyRequired,
            CampaignTacticalV2Blocker.AtomicV2BatchConsequencesRequired,
            CampaignTacticalV2Blocker.VersionedCampaignActivationAndSaveContractRequired
        };

        // One authoritative lock covers the stamps, sources and compatibility identity.
        // No input DTOs, equipment overrides, RNG draws, commands or state writes.
        public CampaignTacticalV2EligibilitySnapshot InspectCampaignTacticalV2(string? actor)
        {
            lock (gate)
            {
                var error = CampaignTacticalInspectionError.None;
                CivilizationStateDto? owner = null;
                if (!EntityId.TryParse(actor, out _)) error = CampaignTacticalInspectionError.InvalidActor;
                else
                {
                    owner = state.Civilizations!.SingleOrDefault(c => c.CivilizationId == actor);
                    if (owner == null) error = CampaignTacticalInspectionError.UnknownActor;
                    else if (owner.ControllerKind != ControllerKind.Human) error = CampaignTacticalInspectionError.ActorNotControlled;
                }

                var blueprints = new Dictionary<(string, int), CampaignBlueprintSnapshot>();
                var ships = new List<CampaignTacticalShipSourceSnapshot>();
                CampaignShipDesignCatalogueSnapshot? catalogue = null;
                if (error == CampaignTacticalInspectionError.None)
                {
                    // Compile each owned frozen revision once, regardless of ship count.
                    foreach (var record in state.Blueprints!.Where(b => b.OwnerCivilizationId == actor))
                        blueprints.Add((record.BlueprintId, record.Revision), ProjectBlueprint(record));
                    foreach (var ship in state.Ships!.Where(s => s.OwnerCivilizationId == actor))
                    {
                        var blueprint = ship.Blueprint == null ? null
                            : blueprints[(ship.Blueprint.BlueprintId, ship.Blueprint.Revision)];
                        ships.Add(new CampaignTacticalShipSourceSnapshot(new ShipSnapshot(ship,
                            CampaignShipDesignResolver.Ship(state, definitions, ship)), blueprint, TacticalV2FitGaps));
                    }
                    if (definitions.ShipDesignCatalogue != null)
                        catalogue = new CampaignShipDesignCatalogueSnapshot(new ShipDesignCatalogueSnapshot(definitions.ShipDesignCatalogue),
                            CampaignBlueprints.Access(definitions, owner!).AvailableVersionIds!);
                }
                return new CampaignTacticalV2EligibilitySnapshot(actor, error, state.TurnNumber, state.StateRevision,
                    state.Phase!.Value, compatibility.ScenarioId, compatibility.RulesVersion, compatibility.ContentHash,
                    TacticalV2FitGaps.Concat(TacticalV2LifecycleGaps), catalogue, blueprints.Values, ships);
            }
        }
    }
}
