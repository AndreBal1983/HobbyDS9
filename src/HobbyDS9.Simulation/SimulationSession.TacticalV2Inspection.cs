using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    public sealed partial class SimulationSession
    {
        // Deliberately enumerated legacy-source gaps, not a configurable whitelist.
        // An empty campaign cannot bypass them; authored equipment is validated separately.
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
                        var equipmentCatalogue = definitions.AuthoredTacticalEquipment;
                        var provision = equipmentCatalogue?.InitialShipProvisions!.SingleOrDefault(p => p.ShipId == ship.ShipId);
                        var fit = provision == null ? null : equipmentCatalogue!.Fits!.Single(f =>
                            f.DefinitionId == provision.FitDefinitionId && f.Revision == provision.FitRevision);
                        var equipment = fit == null ? null : new AuthoredTacticalEquipmentSnapshot(equipmentCatalogue!, fit, provision!);
                        ships.Add(new CampaignTacticalShipSourceSnapshot(new ShipSnapshot(ship,
                            CampaignShipDesignResolver.Ship(state, definitions, ship)), blueprint,
                            equipment == null ? TacticalV2FitGaps : Array.Empty<CampaignTacticalV2Blocker>(), equipment));
                    }
                    if (definitions.ShipDesignCatalogue != null)
                        catalogue = new CampaignShipDesignCatalogueSnapshot(new ShipDesignCatalogueSnapshot(definitions.ShipDesignCatalogue),
                            CampaignBlueprints.Access(definitions, owner!).AvailableVersionIds!);
                }
                return new CampaignTacticalV2EligibilitySnapshot(actor, error, state.TurnNumber, state.StateRevision,
                    state.Phase!.Value, compatibility.ScenarioId, compatibility.RulesVersion, compatibility.ContentHash,
                    (ships.Count == 0 || ships.Any(s => !s.CanProjectTacticalV2) || blueprints.Count > 0
                        ? TacticalV2FitGaps : new[] { CampaignTacticalV2Blocker.FormationCapabilityDerivationRequired })
                    .Concat(TacticalV2LifecycleGaps), catalogue, blueprints.Values, ships);
            }
        }
    }
}
