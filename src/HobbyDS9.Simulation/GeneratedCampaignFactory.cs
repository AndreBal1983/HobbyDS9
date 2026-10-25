using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    internal static class GeneratedCampaignFactory
    {
        internal static string Identity(string domain, string template, string map)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.ASCII.GetBytes("generated-campaign-v1\n" + domain + "\n"
                    + CampaignGeneratedIdentity.Policy + "\n" + template + "\n" + map + "\n"))
                    .Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string Identity(string domain, string template, string map, bool staticAstronomy)
        {
            if (!staticAstronomy) return Identity(domain, template, map); // Retain the exact historical v1 hash domain.
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.ASCII.GetBytes(CampaignStaticAstronomyIdentity.Profile + "\n" + domain + "\n"
                    + CampaignStaticAstronomyIdentity.Policy + "\n" + template + "\n" + map + "\n"))
                    .Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        // Load validates and uses the persisted geometry. It never reruns generation from a seed.
        internal static NaturalGalaxyDocumentDto Frozen(NaturalGalaxyDocumentDto map, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            NaturalGalaxyData.Shape(map);
            NaturalGalaxyData.Settings(map.Settings, catalog);
            Require(NaturalGalaxyData.Hash(map) == map.ContentHash, "Frozen generated map hash mismatch.");
            var layout = map.Layout;
            Require(layout.Width == map.Settings.Map.Width && layout.Height == map.Settings.Map.Height
                && layout.Regions.Count == map.Settings.Map.SystemCount + map.Settings.Map.EmptyRegionCount
                && layout.Systems.Count == map.Settings.Map.SystemCount, "Frozen map counts/extents mismatch.");
            Require(layout.Regions.Select(r => r.NodeId).Distinct(StringComparer.Ordinal).Count() == layout.Regions.Count
                && layout.Systems.Select(s => s.SystemId).Distinct(StringComparer.Ordinal).Count() == layout.Systems.Count,
                "Duplicate frozen map identities.");
            List<NavigationConnectionDto> edges;
            try { edges = GeneratedGalaxyGeometry.Connect(layout.Regions, layout.Width, layout.Height); }
            catch(InvalidOperationException e) { throw new ArgumentException("Frozen map geometry is invalid.",nameof(map),e); }
            string Edge(NavigationConnectionDto e) => e.ConnectionId + "|" + e.EndpointA + "|" + e.EndpointB + "|" + (int)e.Kind + "|" + e.BaseTraversalCost;
            Require(edges.Select(Edge).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(layout.Connections.Select(Edge).OrderBy(x => x, StringComparer.Ordinal)),
                "Frozen map connections differ from positive shared boundaries.");
            Require(layout.Systems.All(s => layout.Regions.Count(r => r.NodeId == s.NodeId && r.SystemId == s.SystemId) == 1)
                && layout.Regions.Count(r => r.SystemId != null) == layout.Systems.Count, "Frozen system anchors mismatch.");
            Require(layout.Regions.All(r=>EntityId.TryParse(r.NodeId,out _))&&layout.Systems.All(s=>EntityId.TryParse(s.SystemId,out _))
                &&map.Features.Select(f=>f.FeatureId).Distinct(StringComparer.Ordinal).Count()==map.Features.Count
                &&map.Features.All(f=>EntityId.TryParse(f.FeatureId,out _)),"Invalid frozen node/system/feature identities.");
            var stars=map.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star).ToList();
            Require(stars.Count==layout.Systems.Count&&stars.All(f=>layout.Systems.Count(s=>s.SystemId==f.SystemId&&s.NodeId==f.NodeId)==1)
                &&stars.Select(f=>f.SystemId).Distinct(StringComparer.Ordinal).Count()==stars.Count
                &&map.Features.All(f=>f.NodeId==null?f.SystemId==null:layout.Regions.Any(r=>r.NodeId==f.NodeId&&r.SystemId==f.SystemId
                    &&r.Center.X==f.Position.X&&r.Center.Y==f.Position.Y)),"Frozen features differ from system/site anchors.");
            Require(map.StarGroups.SelectMany(g=>g.StarFeatureIds).OrderBy(id=>id,StringComparer.Ordinal)
                .SequenceEqual(stars.Select(f=>f.FeatureId).OrderBy(id=>id,StringComparer.Ordinal)),"Frozen group membership mismatch.");
            Require(layout.HomeCandidates.Count == 2 && layout.HomeCandidates.Select(h => h.NodeId).Distinct(StringComparer.Ordinal).Count() == 2
                && layout.HomeCandidates.All(h => layout.Systems.Any(s => s.SystemId == h.SystemId && s.NodeId == h.NodeId))
                && layout.HomeCandidates.Select(h => h.CivilizationDefinitionId).OrderBy(x => x, StringComparer.Ordinal)
                    .SequenceEqual(map.Settings.Map.HomeCivilizationDefinitionIds.OrderBy(x => x, StringComparer.Ordinal)), "Frozen home assignments mismatch.");
            Require(GeneratedGalaxyData.Distances(GeneratedGalaxyData.Neighbors(layout.Regions, layout.Connections), layout.Regions[0].NodeId).Count == layout.Regions.Count,
                "Disconnected frozen map.");
            return NaturalGalaxyData.Copy(map);
        }

        internal static CampaignPlayableFleetPackageDto Build(CampaignPlayableFleetPackageDto template, NaturalGalaxyDocumentDto map, bool staticAstronomy = false)
            => BuildFacts(template,map,staticAstronomy,null);
        internal static CampaignPlayableFleetPackageDto BuildPublished(CampaignPlayableFleetPackageDto template,NaturalGalaxyDocumentDto map,string wrapperHash)
            => BuildFacts(template,map,true,wrapperHash);
        internal static CampaignPlayableFleetPackageDto BuildActivities(CampaignPlayableFleetPackageDto template,NaturalGalaxyDocumentDto map,string wrapperHash)
            => BuildFacts(template,map,true,wrapperHash,true);
        internal static CampaignPlayableFleetPackageDto BuildStreamedActivities(CampaignPlayableFleetPackageDto template,NaturalGalaxyDocumentDto map,string wrapperHash)
            => BuildFacts(template,map,true,wrapperHash,true,true);
        internal static (CampaignPlayableFleetPackageDto Package,CampaignColonisationStateDto Inventory,FrozenColonisationPlanetsDto Frozen) BuildColonisation(
            CampaignPlayableFleetPackageDto template,NaturalGalaxyDocumentDto map,string wrapperHash,CampaignActivityStateDto seededActivities,ColonisationPolicyDto policy,FrozenColonisationPlanetsDto? retained=null)
        {
            var input=new ColonisationGenerationInput(seededActivities,policy,retained);
            var package=BuildFacts(template,map,true,wrapperHash,true,true,input);
            return(package,input.Inventory!,input.Frozen!);
        }
        private sealed class ColonisationGenerationInput
        {
            internal readonly CampaignActivityStateDto Activities;
            internal readonly ColonisationPolicyDto Policy;
            internal CampaignColonisationStateDto? Inventory;
            internal FrozenColonisationPlanetsDto? Frozen;
            internal readonly FrozenColonisationPlanetsDto? Retained;
            internal ColonisationGenerationInput(CampaignActivityStateDto activities,ColonisationPolicyDto policy,FrozenColonisationPlanetsDto? retained)
            {Activities=CampaignActivitiesData.Copy(activities);Policy=ColonisationData.Copy(policy);Retained=retained;}
        }
        private static CampaignPlayableFleetPackageDto BuildFacts(CampaignPlayableFleetPackageDto template,NaturalGalaxyDocumentDto map,bool staticAstronomy,string? wrapperHash,bool activities=false,bool streamed=false,ColonisationGenerationInput? colonisation=null)
        {
            // Existing template reader supplies the trusted exact raw-source identity.
            CampaignTacticalReviewSession.StartPlayable(template);
            string Domain(string domain)=>wrapperHash==null?Identity(domain,template.PackageHash!,map.ContentHash,staticAstronomy)
                :colonisation!=null?CampaignActivitiesData.Identity(CampaignColonisationIdentity.Profile,CampaignColonisationIdentity.InventoryAuthoring,colonisation.Frozen!.ContentHash,domain,wrapperHash,map.ContentHash)
                :activities?CampaignActivitiesData.Identity(streamed?CampaignStreamedActivitiesIdentity.Profile:CampaignActivitiesIdentity.Profile,domain,wrapperHash,map.ContentHash)
                :CampaignPublishedConstructionData.Identity(domain,wrapperHash,map.ContentHash);
            var source = template.Integrated!.Physical!.CorePackage!;
            var definitions = SimulationSession.CopyDefinitions(source.Campaign!.Definitions);
            var game = SimulationSession.CopyState(source.Campaign.InitialState);
            var selected = game.Civilizations!.Where(c => map.Settings.Map.HomeCivilizationDefinitionIds.Contains(c.DefinitionId, StringComparer.Ordinal))
                .OrderBy(c => c.CivilizationId, StringComparer.Ordinal).ToList();
            Require(selected.Count == 2 && selected.Count(c => c.ControllerKind == ControllerKind.Human) == 1
                && selected.Count(c => c.ControllerKind == ControllerKind.AI) == 1, "Generated starter requires one human and one equipped AI from the trusted template.");
            var actors = new HashSet<string>(selected.Select(c => c.CivilizationId), StringComparer.Ordinal);
            var omittedDefinitions=new HashSet<string>(definitions.Civilizations!.Where(c=>!selected.Any(s=>s.DefinitionId==c.DefinitionId)).Select(c=>c.DefinitionId),StringComparer.Ordinal);
            definitions.Civilizations=definitions.Civilizations!.Where(c=>!omittedDefinitions.Contains(c.DefinitionId)).ToList();
            Require(selected.All(c => game.Fleets!.Any(f => f.OwnerCivilizationId == c.CivilizationId)
                && game.Colonies!.Any(y => y.OwnerCivilizationId == c.CivilizationId)), "Each generated starter needs an existing fleet and colony.");
            var homes = selected.ToDictionary(c => c.CivilizationId,
                c => map.Layout.HomeCandidates.Single(h => h.CivilizationDefinitionId == c.DefinitionId), StringComparer.Ordinal);
            var oldSystemDefinitions = new HashSet<string>(definitions.StarSystems!.Select(s => s.DefinitionId), StringComparer.Ordinal);
            var oldPlanetDefinitions = new HashSet<string>(definitions.Planets!.Select(p => p.DefinitionId), StringComparer.Ordinal);
            string SystemDefinition(string id) => "star-system." + id;
            definitions.StarSystems = map.Layout.Systems.OrderBy(s => s.SystemId, StringComparer.Ordinal)
                .Select(s => new StarSystemDefinitionDto { DefinitionId = SystemDefinition(s.SystemId), DisplayName = s.DisplayName }).ToList();
            definitions.GalaxyEdges = new List<GalaxyEdgeDto>(); // Explicit region graph below is authoritative for travel.
            definitions.NavigationRegions = map.Layout.Regions.OrderBy(r => r.NodeId, StringComparer.Ordinal).Select(r => new NavigationRegionDefinitionDto {
                NodeId = r.NodeId, AssociatedSystemId = r.SystemId, DisplayName = r.DisplayName, PhenomenonDefinitionIds = new List<string>() }).ToList();
            definitions.NavigationConnections = map.Layout.Connections.OrderBy(e => e.ConnectionId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList();
            definitions.Phenomena = new List<PhenomenonDefinitionDto>(); // Visual generation features add no duplicate traversal surcharge.
            game.Civilizations = selected;
            game.StarSystems = map.Layout.Systems.OrderBy(s => s.SystemId, StringComparer.Ordinal).Select(s => new StarSystemStateDto {
                SystemId = s.SystemId, DefinitionId = SystemDefinition(s.SystemId), OwnerCivilizationId = homes.FirstOrDefault(h => h.Value.SystemId == s.SystemId).Key }).ToList();
            game.Colonies = game.Colonies!.Where(c => c.OwnerCivilizationId != null && actors.Contains(c.OwnerCivilizationId)).OrderBy(c => c.ColonyId, StringComparer.Ordinal).ToList();
            Require(game.Colonies.All(c => c.ProductionQueue!.Count == 0), "Generated starter template must not contain queued construction.");
            foreach (var colony in game.Colonies) colony.SystemId = homes[colony.OwnerCivilizationId!].SystemId;
            var planets = new HashSet<string>(game.Colonies.Select(c => c.PlanetId), StringComparer.Ordinal);
            game.Planets = game.Planets!.Where(p => planets.Contains(p.PlanetId)).OrderBy(p => p.PlanetId, StringComparer.Ordinal).ToList();
            definitions.Planets = definitions.Planets!.Where(p => game.Planets.Any(s => s.DefinitionId == p.DefinitionId)).ToList();
            foreach (var planet in game.Planets)
            {
                planet.SystemId = game.Colonies.Single(c => c.PlanetId == planet.PlanetId).SystemId;
                definitions.Planets.Single(p => p.DefinitionId == planet.DefinitionId).SystemDefinitionId = SystemDefinition(planet.SystemId);
            }
            definitions.DefinitionIds = definitions.DefinitionIds.Where(id => !oldSystemDefinitions.Contains(id) && !oldPlanetDefinitions.Contains(id)&&!omittedDefinitions.Contains(id))
                .Concat(definitions.StarSystems.Select(s => s.DefinitionId)).Concat(definitions.Planets.Select(p => p.DefinitionId)).OrderBy(id => id, StringComparer.Ordinal).ToList();
            game.Fleets = game.Fleets!.Where(f => actors.Contains(f.OwnerCivilizationId)).OrderBy(f => f.FleetId, StringComparer.Ordinal).ToList();
            foreach (var fleet in game.Fleets) { fleet.LocationNodeId = homes[fleet.OwnerCivilizationId].NodeId; fleet.RouteNodeIds = new List<string>(); fleet.TransitProgress = 0; fleet.TransitTotalCost = 0; }
            game.Ships = game.Ships!.Where(s => actors.Contains(s.OwnerCivilizationId)).OrderBy(s => s.ShipId, StringComparer.Ordinal).ToList();
            var liveIds = new HashSet<string>(actors.Concat(game.Colonies.Select(c => c.ColonyId)).Concat(game.Colonies.SelectMany(c => c.Cohorts!).Select(c => c.CohortId))
                .Concat(game.Planets.Select(p => p.PlanetId)).Concat(game.Ships.Select(s => s.ShipId)).Concat(game.Fleets.Select(f => f.FleetId)), StringComparer.Ordinal);
            game.EntityIdentities = game.EntityIdentities!.Where(e => liveIds.Contains(e.EntityId))
                .Concat(game.StarSystems.Select(s => new EntityIdentityDto { EntityId = s.SystemId, DefinitionId = s.DefinitionId })).OrderBy(e => e.EntityId, StringComparer.Ordinal).ToList();
            game.TraversalEffects = new List<TraversalEffectDto>();
            game.NavigationKnowledge = selected.Select(c => {
                var edges = definitions.NavigationConnections.Where(e => e.EndpointA == homes[c.CivilizationId].NodeId || e.EndpointB == homes[c.CivilizationId].NodeId).ToList();
                return new CivilizationNavigationKnowledgeDto { CivilizationId = c.CivilizationId,
                    Nodes = edges.SelectMany(e => new[] { e.EndpointA, e.EndpointB }).Append(homes[c.CivilizationId].NodeId).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)
                        .Select(id => new NavigationKnowledgeEntryDto { Id = id, Level = NavigationKnowledgeLevel.Charted }).ToList(),
                    Connections = edges.OrderBy(e => e.ConnectionId, StringComparer.Ordinal).Select(e => new NavigationKnowledgeEntryDto { Id = e.ConnectionId, Level = NavigationKnowledgeLevel.Charted }).ToList() };
            }).ToList();
            game.RngState.State = ulong.Parse(map.Settings.Map.Seed, CultureInfo.InvariantCulture) ^ 0x47454e43414d5031UL;
            if(colonisation!=null)
            {
                colonisation.Inventory=ColonisationPlanetInventory.Populate(definitions,game,map,colonisation.Activities,colonisation.Policy);
                colonisation.Frozen=ColonisationData.Freeze(definitions,game,colonisation.Inventory);
                if(colonisation.Retained!=null)
                {
                    Require(colonisation.Retained.ContentHash==ColonisationData.InventoryHash(colonisation.Retained)
                        &&colonisation.Retained.ContentHash==colonisation.Frozen.ContentHash,"Different frozen physical planet genesis.");
                    colonisation.Frozen=ColonisationData.Copy(colonisation.Retained);
                    // Consume the retained frozen definitions/facts after verifying their exact pinned policy genesis.
                    definitions.Planets=colonisation.Frozen.Definitions!.Select(PopulationData.Copy).ToList();
                    game.Planets=colonisation.Frozen.InitialPlanets!.Select(PopulationData.Copy).ToList();
                    colonisation.Inventory.Bodies=colonisation.Frozen.Bodies!.Select(ColonisationData.Copy).ToList();
                    colonisation.Inventory.PlanetControls=colonisation.Frozen.GenesisControls!.Select(ColonisationData.Copy).ToList();
                }
            }
            var scenario = new ScenarioDto { ScenarioId = colonisation!=null?"scenario.generated-colonisation-v1":streamed?"scenario.generated-activities-streamed-v1":activities?"scenario.generated-activities-v1":wrapperHash!=null?"scenario.generated-published-construction-v1":staticAstronomy ? "scenario.generated-campaign-v2" : "scenario.generated-campaign-v1", RulesVersion = colonisation!=null?CampaignColonisationIdentity.Rules:source.Campaign.RulesVersion,
                ContentHash = Domain("scenario"), Definitions = definitions, InitialState = game };
            if(colonisation==null)FoundationValidator.ValidateScenario(scenario).ThrowIfInvalid(nameof(scenario));
            else FoundationValidator.ValidatePlanetaryScenario(scenario,PlanetaryOwnershipContext.CreateForColonisation(definitions,game,
                colonisation.Inventory!.PlanetControls!,colonisation.Activities.Minors!)).ThrowIfInvalid(nameof(scenario));
            var core = new CampaignTacticalReviewPackageDto { FormatVersion = source.FormatVersion, ProfileId = source.ProfileId,
                PackageHash = Domain("core"), PolicyStatus = source.PolicyStatus, DefaultActivationAllowed = false,
                Campaign = scenario, Rules = CopyRules(source.Rules!), CloakSources = source.CloakSources!.Select(c => new CampaignTacticalCloakSourceDto {
                    FitId = c.FitId, FitRevision = c.FitRevision, Capable = c.Capable, ConcurrentPowerRequired = c.ConcurrentPowerRequired }).ToList() };
            var physicalSource = template.Integrated.Physical;
            var physical = new CampaignTacticalTravelPackageDto { FormatVersion = physicalSource.FormatVersion, ProfileId = physicalSource.ProfileId,
                PackageHash = Domain("physical"), PolicyStatus = physicalSource.PolicyStatus, DefaultActivationAllowed = false,
                BoundaryPolicy = physicalSource.BoundaryPolicy, SharedStartPolicy = physicalSource.SharedStartPolicy, LaterArrivalPolicy = physicalSource.LaterArrivalPolicy,
                ContactFreshnessTurns = physicalSource.ContactFreshnessTurns, CorePackage = core,
                Hostilities = physicalSource.Hostilities!.Where(h => actors.Contains(h.CivilizationA) && actors.Contains(h.CivilizationB))
                    .Select(h => new CampaignDeclaredHostilityDto { CivilizationA = h.CivilizationA, CivilizationB = h.CivilizationB }).ToList(),
                InitialContacts = new List<CampaignContactKnowledgeDto>(), InitialOrders = game.Fleets.Select(f => new CampaignTravelOrderDto {
                    FleetId = f.FleetId, Intent = CampaignTravelIntent.Stay, RouteNodeIds = new List<string>() }).ToList() };
            return new CampaignPlayableFleetPackageDto { FormatVersion = template.FormatVersion, ProfileId = template.ProfileId,
                PackageHash = Domain("package"), PolicyStatus = template.PolicyStatus, DefaultActivationAllowed = false,
                ReplayOperationLimit = template.ReplayOperationLimit, OpponentActorId = selected.Single(c => c.ControllerKind == ControllerKind.AI).CivilizationId,
                DefenseNodeId = homes[selected[0].CivilizationId].NodeId, // Not used by generated policy; legacy package metadata only.
                Integrated = new CampaignIntegratedPlaytestPackageDto { FormatVersion = template.Integrated.FormatVersion, ProfileId = template.Integrated.ProfileId,
                    PackageHash = Domain("integrated"), PolicyStatus = template.Integrated.PolicyStatus, DefaultActivationAllowed = false,
                    Physical = physical, ConstructionTemplates = template.Integrated.ConstructionTemplates!.Select(t => new CampaignIntegratedConstructionTemplateDto {
                        ShipDesignDefinitionId = t.ShipDesignDefinitionId, SourceProvisionShipId = t.SourceProvisionShipId }).ToList() } };
        }
    }
}
