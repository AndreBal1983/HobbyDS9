using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private string? integratedHash;
        private List<CampaignIntegratedConstructionTemplateDto>? constructionTemplates;
        private List<CampaignIntegratedBuiltShipDto> builtShips = new List<CampaignIntegratedBuiltShipDto>();
        // Candidate provenance is visible only inside the locked staged advance.
        private List<CampaignIntegratedBuiltShipDto>? stagedBuiltShips;
        private IEnumerable<CampaignIntegratedBuiltShipDto> BuildRegistry => stagedBuiltShips ?? builtShips;

        public static CampaignTacticalReviewSession StartIntegrated(CampaignIntegratedPlaytestPackageDto package)
        {
            ValidateIntegratedPackage(package);
            return new CampaignTacticalReviewSession(package.Physical!.CorePackage!, null, package.Physical, null, package, null);
        }
        public static CampaignTacticalReviewSession LoadIntegrated(CampaignIntegratedPlaytestPackageDto package, CampaignIntegratedPlaytestSaveDto save)
        {
            ValidateIntegratedPackage(package);
            Require(save != null && save.FormatVersion == CampaignIntegratedPlaytestIdentity.SaveFormat
                && save.ProfileId == CampaignIntegratedPlaytestIdentity.Profile && save.PackageHash == package.PackageHash
                && save.Physical != null && save.BuiltShips != null && save.BuiltShips.All(b=>b!=null), "Unsupported integrated save format/profile/package; no migration.");
            RequireTravelShape(save.Physical!);
            return new CampaignTacticalReviewSession(package.Physical!.CorePackage!, save.Physical!.Core, package.Physical, save.Physical, package, save);
        }
        public CampaignIntegratedPlaytestSaveDto ExportIntegratedSave()
        {
            lock (gate)
            {
                Require(integratedHash != null, "Session is not an integrated playtest.");
                return new CampaignIntegratedPlaytestSaveDto { FormatVersion = CampaignIntegratedPlaytestIdentity.SaveFormat,
                    ProfileId = CampaignIntegratedPlaytestIdentity.Profile, PackageHash = integratedHash!, Physical = TravelCopy(),
                    BuiltShips = builtShips.OrderBy(b => b.ShipId, StringComparer.Ordinal).Select(CopyBuild).ToList() };
            }
        }
        private static CampaignIntegratedBuiltShipDto CopyBuild(CampaignIntegratedBuiltShipDto b) => new CampaignIntegratedBuiltShipDto {
            ShipId=b.ShipId,ShipDesignDefinitionId=b.ShipDesignDefinitionId,SourceProvisionShipId=b.SourceProvisionShipId,
            OwnerCivilizationId=b.OwnerCivilizationId,FleetIdAtConstruction=b.FleetIdAtConstruction,QueueItemId=b.QueueItemId,TurnBuilt=b.TurnBuilt };

        private static void ValidateIntegratedPackage(CampaignIntegratedPlaytestPackageDto p)
        {
            Require(p != null && p.FormatVersion == CampaignIntegratedPlaytestIdentity.PackageFormat
                && p.ProfileId == CampaignIntegratedPlaytestIdentity.Profile && Hash(p.PackageHash) && p.PolicyStatus == "proposed"
                && p.DefaultActivationAllowed == false && p.Physical != null && p.ConstructionTemplates != null
                && p.ConstructionTemplates.Count > 0, "Unsupported/ungated integrated package.");
            CampaignTacticalTravelData.ValidatePackage(p.Physical!);
            var campaign = p.Physical!.CorePackage!.Campaign!;
            Require(p.ConstructionTemplates!.All(t => t != null && DefinitionId.TryParse(t.ShipDesignDefinitionId, out _)
                && EntityId.TryParse(t.SourceProvisionShipId, out _)) && p.ConstructionTemplates.Select(t => t.ShipDesignDefinitionId)
                .Distinct(StringComparer.Ordinal).Count() == p.ConstructionTemplates.Count, "Missing/duplicate construction template.");
            foreach (var t in p.ConstructionTemplates)
            {
                var source = campaign.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!
                    .SingleOrDefault(s => s.ShipId == t.SourceProvisionShipId);
                var design = campaign.Definitions.ShipDesigns!.SingleOrDefault(d => d.DefinitionId == t.ShipDesignDefinitionId);
                Require(source != null && design != null && source.ShipDesignDefinitionId == t.ShipDesignDefinitionId,
                    "Construction template must bind an exact validated authored source; no blueprint conversion.");
                var fit = campaign.Definitions.AuthoredTacticalEquipment.Fits!.Single(f => f.DefinitionId == source!.FitDefinitionId && f.Revision == source.FitRevision);
                Require(design!.MaxHull == fit.MaxHull && campaign.InitialState.Ships!.Any(s => s.ShipId == source!.ShipId),
                    "Construction source hull/provision differs from its immutable design.");
            }
            Require(campaign.InitialState.Colonies!.SelectMany(c => c.ProductionQueue!).All(q => q.Blueprint == null
                && p.ConstructionTemplates.Any(t => t.ShipDesignDefinitionId == q.ShipDesignDefinitionId)), "Unadmitted initial construction queue.");
        }

        private AuthoredTacticalShipProvisionDto ResolveProvision(string id)
        {
            var source = definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.SingleOrDefault(p => p.ShipId == id);
            if (source != null) return source;
            var record = BuildRegistry.SingleOrDefault(b => b.ShipId == id);
            Require(constructionTemplates != null && record != null, "Unbound produced ship source.");
            var template = constructionTemplates!.Single(t => t.ShipDesignDefinitionId == record!.ShipDesignDefinitionId
                && t.SourceProvisionShipId == record.SourceProvisionShipId);
            var original = definitions.AuthoredTacticalEquipment.InitialShipProvisions.Single(p => p.ShipId == template.SourceProvisionShipId);
            return new AuthoredTacticalShipProvisionDto { ShipId = id, OwnerCivilizationId = record!.OwnerCivilizationId,
                ShipDesignDefinitionId = original.ShipDesignDefinitionId, FitDefinitionId = original.FitDefinitionId, FitRevision = original.FitRevision,
                InitialShields = original.InitialShields, InitialAmmunition = original.InitialAmmunition };
        }
        private bool KnownSource(string ship) => definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Any(p => p.ShipId == ship)
            || constructionTemplates != null && BuildRegistry.Any(b => b.ShipId == ship);
        private string? KnownFleetOwner(string fleet)
        {
            if (authoredFleetOwners != null && authoredFleetOwners.TryGetValue(fleet, out var owner)) return owner;
            return constructionTemplates == null ? null : BuildRegistry.FirstOrDefault(b => b.FleetIdAtConstruction == fleet)?.OwnerCivilizationId;
        }
        private bool AdmittedConstruction(CommandEnvelope command) => constructionTemplates != null
            && command.Kind == CommandKind.EnqueueProduction && command.EnqueueProduction != null
            && constructionTemplates.Any(t => t.ShipDesignDefinitionId == command.EnqueueProduction.DefinitionId);

        private void ValidateBuiltShips(GameStateDto game)
        {
            if (constructionTemplates == null) return;
            var records = BuildRegistry.ToList();
            Require(records.All(b => b != null && EntityId.TryParse(b.ShipId, out var id) && id.TryGetAllocationSequence(out var n)
                    && n < game.NextEntitySequence && !definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Any(p => p.ShipId == b.ShipId)
                    && EntityId.TryParse(b.QueueItemId, out var queue) && queue.TryGetAllocationSequence(out var q) && q < n
                    && EntityId.TryParse(b.FleetIdAtConstruction, out _) && controllers.ContainsKey(b.OwnerCivilizationId ?? "")
                    && b.TurnBuilt >= initialTurn && b.TurnBuilt <= game.TurnNumber
                    && constructionTemplates.Any(t => t.ShipDesignDefinitionId == b.ShipDesignDefinitionId && t.SourceProvisionShipId == b.SourceProvisionShipId))
                && records.Select(b => b.ShipId).Distinct(StringComparer.Ordinal).Count() == records.Count
                && records.Select(b => b.QueueItemId).Distinct(StringComparer.Ordinal).Count() == records.Count,
                "Invalid/missing/duplicate produced-source provenance.");
            foreach (var b in records)
            {
                var actual = game.Ships!.SingleOrDefault(s => s.ShipId == b.ShipId);
                Require(actual == null || actual.Blueprint == null && actual.ShipDesignDefinitionId == b.ShipDesignDefinitionId
                    && actual.OwnerCivilizationId == b.OwnerCivilizationId && actual.FleetId == b.FleetIdAtConstruction,
                    "Produced immutable source/owner/fleet substituted.");
                Require(!game.Colonies!.SelectMany(c => c.ProductionQueue!).Any(q => q.QueueItemId == b.QueueItemId), "Completed production is still queued.");
            }
            Require(records.GroupBy(b => b.FleetIdAtConstruction, StringComparer.Ordinal).All(g => g.Select(b => b.OwnerCivilizationId).Distinct().Count() == 1
                && (!authoredFleetOwners!.TryGetValue(g.Key!, out var owner) || g.All(b => b.OwnerCivilizationId == owner))), "Forged constructed fleet ownership.");
        }

        private void ProvisionCompletedShips(CampaignTacticalTravelSaveDto next, GameStateDto beforeProduction)
        {
            if (constructionTemplates == null) return;
            var game = next.Core.Campaign.GameState;
            var ships = game.Ships!.Where(s => !beforeProduction.Ships!.Any(old => old.ShipId == s.ShipId))
                .OrderBy(s => { Require(new EntityId(s.ShipId).TryGetAllocationSequence(out var n), "Unallocated construction ship."); return n; }).ToList();
            var completed = beforeProduction.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal).Where(c => c.ProductionQueue!.Count > 0
                && !game.Colonies!.Single(n => n.ColonyId == c.ColonyId).ProductionQueue!.Any(q => q.QueueItemId == c.ProductionQueue[0].QueueItemId)).ToList();
            Require(ships.Count == completed.Count, "Construction completion/provenance mismatch.");
            for (int i = 0; i < ships.Count; i++)
            {
                var ship=ships[i]; var colony=completed[i];
                Require(ship.Blueprint == null && constructionTemplates.Any(t => t.ShipDesignDefinitionId == ship.ShipDesignDefinitionId), "Produced source was not admitted.");
                var template = constructionTemplates.Single(t => t.ShipDesignDefinitionId == ship.ShipDesignDefinitionId);
                Require(colony.OwnerCivilizationId == ship.OwnerCivilizationId && colony.ProductionQueue![0].ShipDesignDefinitionId == ship.ShipDesignDefinitionId
                    && colony.ProductionQueue[0].Blueprint == null && new WeightedNavigationGraph(definitions).NodeForSystem(colony.SystemId)
                    == game.Fleets!.Single(f => f.FleetId == ship.FleetId).LocationNodeId, "Construction source/owner/location mismatch.");
                stagedBuiltShips!.Add(new CampaignIntegratedBuiltShipDto { ShipId = ship.ShipId,ShipDesignDefinitionId = ship.ShipDesignDefinitionId,
                    SourceProvisionShipId = template.SourceProvisionShipId,OwnerCivilizationId = ship.OwnerCivilizationId,
                    FleetIdAtConstruction = ship.FleetId,QueueItemId = colony.ProductionQueue![0].QueueItemId,TurnBuilt = game.TurnNumber });
                next.Core.Pools.Add(InitialPool(ship.ShipId));
            }
            foreach (var fleet in game.Fleets!.Where(f => !next.Core.FleetSettings.Any(s => s.FleetId == f.FleetId)))
            {
                next.Core.FleetSettings.Add(new CampaignTacticalFleetSettingDto { FleetId = fleet.FleetId });
                next.Orders.Add(new CampaignTravelOrderDto { FleetId = fleet.FleetId, Intent = CampaignTravelIntent.Stay, RouteNodeIds = new List<string>() });
            }
            ValidateBuiltShips(game);
        }
        internal (SessionSnapshot Campaign, DefinitionCatalogDto Definitions, NavigationViewSnapshot Navigation, CampaignTacticalReviewStatus Status)
            CaptureIntegratedProjection(string actor)
        {
            lock (gate)
            {
                Require(constructionTemplates != null && Actor(actor) == CampaignTacticalError.None, "Only controlled integrated actors may capture strategy.");
                var legacy = Legacy(state);
                return (legacy.CaptureSnapshot(), SimulationSession.CopyDefinitions(definitions), legacy.CaptureNavigation(actor), CaptureStatus());
            }
        }
        internal IReadOnlyList<CampaignIntegratedConstructionTemplateDto> CaptureConstructionTemplates()
        { lock(gate) return constructionTemplates!.Select(t => new CampaignIntegratedConstructionTemplateDto { ShipDesignDefinitionId=t.ShipDesignDefinitionId,SourceProvisionShipId=t.SourceProvisionShipId }).ToList().AsReadOnly(); }
    }
}
