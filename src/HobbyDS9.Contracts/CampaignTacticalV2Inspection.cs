using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Contracts
{
    // A read-only diagnostic projection, never a save, command or resolver input.
    public static class CampaignTacticalV2InspectionIdentity
    {
        public const int FormatVersion = 1;
        public const string ProjectionId = "campaign-tactical-v2-source-inspection";
    }

    public enum CampaignTacticalInspectionError
    {
        None = 0, InvalidActor = 1, UnknownActor = 2, ActorNotControlled = 3
    }

    public enum CampaignTacticalV2Blocker
    {
        ExplicitEquipmentSchemaRequired = 1,
        WeaponClassesAndArcsRequired = 2,
        WeaponCadenceAndMagazineDefinitionsRequired = 3,
        DirectionalShieldLimitsAndRechargeRequired = 4,
        ArmorDefinitionRequired = 5,
        FormationCapabilityDerivationRequired = 6,
        PersistentShipResourcesAndRecoveryPolicyRequired = 7,
        SessionRoundCheckpointAndOrderAuthorityRequired = 8,
        StrategicRetreatAndObjectivePolicyRequired = 9,
        AtomicV2BatchConsequencesRequired = 10,
        VersionedCampaignActivationAndSaveContractRequired = 11
    }

    public sealed class CampaignTacticalShipSourceSnapshot
    {
        public ShipSnapshot Ship { get; }
        // Null for static designs: absence is not an empty or fabricated fit.
        public CampaignBlueprintSnapshot? FrozenBlueprint { get; }
        public IReadOnlyList<CampaignTacticalV2Blocker> MissingFitSemantics { get; }
        public bool CanProjectTacticalV2 => MissingFitSemantics.Count == 0;

        public CampaignTacticalShipSourceSnapshot(ShipSnapshot ship, CampaignBlueprintSnapshot? blueprint,
            IEnumerable<CampaignTacticalV2Blocker> missingFitSemantics)
        {
            Ship = ship; FrozenBlueprint = blueprint;
            MissingFitSemantics = missingFitSemantics.Distinct().OrderBy(v => v).ToList().AsReadOnly();
        }
    }

    public sealed class CampaignTacticalV2EligibilitySnapshot
    {
        public int FormatVersion => CampaignTacticalV2InspectionIdentity.FormatVersion;
        public string ProjectionId => CampaignTacticalV2InspectionIdentity.ProjectionId;
        public string? ActorCivilizationId { get; }
        public CampaignTacticalInspectionError ErrorCode { get; }
        public int TurnNumber { get; }
        public long StateRevision { get; }
        public SessionPhase Phase { get; }
        public string ScenarioId { get; }
        public string CampaignRulesVersion { get; }
        public string CampaignContentHash { get; }
        public string CampaignResolverId => CampaignCombatRules.ResolverId;
        public int CampaignResolverVersion => CampaignCombatRules.ResolverVersion;
        public bool CanActivate => ErrorCode == CampaignTacticalInspectionError.None && Blockers.Count == 0;
        public IReadOnlyList<CampaignTacticalV2Blocker> Blockers { get; }
        public CampaignShipDesignCatalogueSnapshot? CurrentCatalogue { get; }
        public IReadOnlyList<CampaignBlueprintSnapshot> OwnedBlueprintRevisions { get; }
        public IReadOnlyList<CampaignTacticalShipSourceSnapshot> OwnedShips { get; }

        public CampaignTacticalV2EligibilitySnapshot(string? actor, CampaignTacticalInspectionError error,
            int turn, long revision, SessionPhase phase, string scenario, string rules, string hash,
            IEnumerable<CampaignTacticalV2Blocker> blockers, CampaignShipDesignCatalogueSnapshot? catalogue,
            IEnumerable<CampaignBlueprintSnapshot> blueprints, IEnumerable<CampaignTacticalShipSourceSnapshot> ships)
        {
            ActorCivilizationId = actor; ErrorCode = error; TurnNumber = turn; StateRevision = revision;
            Phase = phase; ScenarioId = scenario; CampaignRulesVersion = rules; CampaignContentHash = hash;
            Blockers = blockers.Distinct().OrderBy(v => v).ToList().AsReadOnly(); CurrentCatalogue = catalogue;
            OwnedBlueprintRevisions = blueprints.OrderBy(v => v.Blueprint.BlueprintId, System.StringComparer.Ordinal)
                .ThenBy(v => v.Blueprint.Revision).ToList().AsReadOnly();
            OwnedShips = ships.OrderBy(v => v.Ship.ShipId.Value, System.StringComparer.Ordinal).ToList().AsReadOnly();
        }
    }
}
