using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private readonly NaturalGalaxyDocumentDto? generatedMap;
        private readonly string? generatedTemplateHash;
        public static CampaignTacticalReviewSession StartGenerated(CampaignPlayableFleetPackageDto template, NaturalGalaxySettingsDto settings)
        {
            ValidatePlayablePackage(template);
            var catalog = template.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            return StartGeneratedFrozen(template, GeneratedCampaignFactory.Frozen(NaturalGalaxyGenerator.Generate(settings, catalog), catalog));
        }
        private static CampaignTacticalReviewSession StartGeneratedFrozen(CampaignPlayableFleetPackageDto template, NaturalGalaxyDocumentDto map)
        {
            var package = GeneratedCampaignFactory.Build(template, map);
            ValidatePlayablePackage(package, generated: true);
            var i = package.Integrated!;
            return new CampaignTacticalReviewSession(i.Physical!.CorePackage!, null, i.Physical, null, i, null, null, null,
                package, map, template.PackageHash);
        }
        public static CampaignTacticalReviewSession LoadGenerated(CampaignPlayableFleetPackageDto template, CampaignGeneratedSaveDto save)
        {
            ValidatePlayablePackage(template);
            Require(save != null && save.FormatVersion == CampaignGeneratedIdentity.Format && save.ProfileId == CampaignGeneratedIdentity.Profile
                && save.PolicyId == CampaignGeneratedIdentity.Policy && save.TemplatePackageHash == template.PackageHash
                && save.Map != null && save.Campaign != null && save.Discoveries != null, "Unsupported/incomplete generated campaign save; no migration.");
            var map = GeneratedCampaignFactory.Frozen(save.Map, template.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!);
            var package = GeneratedCampaignFactory.Build(template, map);
            Require(save.PackageHash == package.PackageHash, "Different generated content/template identity.");
            var replay = ReplayPlayable(package, save.Campaign, StartGeneratedFrozen(template, map));
            Require(FrontierSame(replay.ExportGeneratedSave(), save), "Generated discovery/map checkpoint differs from accepted-operation replay.");
            return replay;
        }
        public CampaignGeneratedSaveDto ExportGeneratedSave()
        {
            lock (gate)
            {
                Require(generatedMap != null, "Not a generated campaign.");
                return new CampaignGeneratedSaveDto { FormatVersion = CampaignGeneratedIdentity.Format, ProfileId = CampaignGeneratedIdentity.Profile,
                    PolicyId = CampaignGeneratedIdentity.Policy, TemplatePackageHash = generatedTemplateHash!, PackageHash = playableHash!,
                    Map = NaturalGalaxyData.Copy(generatedMap), Campaign = ExportPlayableCore(), Discoveries = discoveries.Select(CopyDiscovery).ToList() };
            }
        }
        private List<string> GeneratedCoverage(GameStateDto game, string actor)
            => game.Fleets!.Where(f => f.OwnerCivilizationId == actor && f.TransitTotalCost == 0).Select(f => f.LocationNodeId)
                .Concat(game.Colonies!.Where(c => c.OwnerCivilizationId == actor).Select(c => definitions.NavigationRegions!.Single(n => n.AssociatedSystemId == c.SystemId).NodeId))
                .Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToList();
        private bool GeneratedContactFresh(GameStateDto game, CampaignContactKnowledgeDto contact)
            => (long)game.TurnNumber-contact.LastSeenTurn<=1&&GeneratedCoverage(game,contact.ActorId).Contains(contact.LastSeenNodeId,StringComparer.Ordinal);
        public CampaignGeneratedMapDto CaptureGeneratedMap(string actor)
        {
            lock (gate)
            {
                Require(generatedMap != null && Actor(actor) == CampaignTacticalError.None, "Only a controlled generated actor may capture its known map.");
                var knowledge = state.Campaign.GameState.NavigationKnowledge!.Single(k => k.CivilizationId == actor);
                var known = new HashSet<string>(knowledge.Nodes!.Where(n => n.Level != NavigationKnowledgeLevel.Unknown).Select(n => n.Id), StringComparer.Ordinal);
                var lanes = new HashSet<string>(knowledge.Connections!.Where(n => n.Level != NavigationKnowledgeLevel.Unknown).Select(n => n.Id), StringComparer.Ordinal);
                var copy = NaturalGalaxyData.Copy(generatedMap);
                return new CampaignGeneratedMapDto { ActorId = actor, MapContentHash = copy.ContentHash, Width = copy.Layout.Width, Height = copy.Layout.Height,
                    ObservationTurn = state.Campaign.GameState.TurnNumber, CampaignRevision = state.Campaign.GameState.StateRevision, ObservedNodeIds = GeneratedCoverage(state.Campaign.GameState, actor),
                    Regions = copy.Layout.Regions.Where(r => known.Contains(r.NodeId)).OrderBy(r => r.NodeId, StringComparer.Ordinal).ToList(),
                    Connections = copy.Layout.Connections.Where(e => lanes.Contains(e.ConnectionId) && known.Contains(e.EndpointA) && known.Contains(e.EndpointB)).OrderBy(e=>e.ConnectionId,StringComparer.Ordinal).ToList(),
                    Features = copy.Features.Where(f => f.NodeId != null && known.Contains(f.NodeId)).OrderBy(f=>f.FeatureId,StringComparer.Ordinal).ToList() };
            }
        }
        private void PlanGeneratedTravel(CampaignTacticalTravelSaveDto next)
        {
            var game = next.Core.Campaign.GameState; var graph = new WeightedNavigationGraph(definitions);
            var visited = new HashSet<string>(discoveries.Select(r => r.ActorId + "|" + r.ArrivalNodeId), StringComparer.Ordinal);
            foreach (var home in generatedMap!.Layout.HomeCandidates)
                foreach (var actor in game.Civilizations!.Where(c => c.DefinitionId == home.CivilizationDefinitionId)) visited.Add(actor.CivilizationId + "|" + home.NodeId);
            foreach (var fleet in game.Fleets!.Where(f => controllers[f.OwnerCivilizationId] == ControllerKind.AI).OrderBy(f => f.FleetId, StringComparer.Ordinal))
            {
                if (fleet.RouteNodeIds!.Count > 0 || fleet.TransitTotalCost > 0) continue;
                // Only owned knowledge and last observed contacts enter the candidate set.
                var contact = next.Contacts.Where(c => c.ActorId == fleet.OwnerCivilizationId && GeneratedContactFresh(game,c))
                    .OrderByDescending(c => c.LastSeenTurn).ThenBy(c => c.FleetId, StringComparer.Ordinal).FirstOrDefault();
                var route = contact == null ? null : graph.FindRoute(game, fleet.OwnerCivilizationId, fleet.LocationNodeId, contact.LastSeenNodeId);
                if (route == null || !route.Success)
                {
                    var knowledge=game.NavigationKnowledge!.Single(k=>k.CivilizationId==fleet.OwnerCivilizationId);
                    var knownNodes=new HashSet<string>(knowledge.Nodes!.Where(n=>n.Level!=NavigationKnowledgeLevel.Unknown).Select(n=>n.Id),StringComparer.Ordinal);
                    var knownEdges=new HashSet<string>(knowledge.Connections!.Where(n=>n.Level!=NavigationKnowledgeLevel.Unknown).Select(n=>n.Id),StringComparer.Ordinal);
                    // Generated v1 has unit ordinary crossings and no dynamic cost effects. One lawful BFS
                    // selects the same nearest/ordinal target; run the shared route solver only once.
                    var lawful=definitions.NavigationConnections!.Where(e=>knownEdges.Contains(e.ConnectionId)
                        &&knownNodes.Contains(e.EndpointA)&&knownNodes.Contains(e.EndpointB)).ToList();
                    var neighbors=knownNodes.ToDictionary(n=>n,_=>new List<string>(),StringComparer.Ordinal);
                    foreach(var edge in lawful){neighbors[edge.EndpointA].Add(edge.EndpointB);neighbors[edge.EndpointB].Add(edge.EndpointA);}
                    var distances=GeneratedGalaxyData.Distances(neighbors,fleet.LocationNodeId);
                    var destination=distances.Where(d=>d.Value>0&&!visited.Contains(fleet.OwnerCivilizationId+"|"+d.Key))
                        .OrderBy(d=>d.Value).ThenBy(d=>d.Key,StringComparer.Ordinal).Select(d=>d.Key).FirstOrDefault();
                    route=destination==null?null:graph.FindRoute(game,fleet.OwnerCivilizationId,fleet.LocationNodeId,destination);
                }
                if (route == null) continue;
                var command = new SubmitCampaignTravelOrderDto { ActorId = fleet.OwnerCivilizationId,
                    CommandId = "ai-command.generated.t" + game.TurnNumber.ToString("D10", CultureInfo.InvariantCulture) + "." + fleet.FleetId,
                    ExpectedTurn = game.TurnNumber, ExpectedRevision = game.StateRevision, Order = new CampaignTravelOrderDto {
                        FleetId = fleet.FleetId, Intent = route.Steps.Count == 0 ? CampaignTravelIntent.StayEngage : CampaignTravelIntent.Leave, RouteNodeIds = route.Steps.ToList() } };
                Require(ValidateTravelCommand(command, next, true) == CampaignTacticalError.None, "Invalid lawful generated AI route.");
                ApplyTravelCommand(next, command);
            }
        }
        private void RefreshGeneratedContacts(GameStateDto game, IReadOnlyList<CampaignTacticalFleetSettingDto> settings, List<CampaignContactKnowledgeDto> contacts, int turn)
        {
            foreach (var actor in game.Civilizations!.OrderBy(c => c.CivilizationId, StringComparer.Ordinal))
            {
                var covered = GeneratedCoverage(game, actor.CivilizationId);
                var detected = game.Fleets!.Where(f => CampaignPhysicalMovement.Hostile(declaredHostilities!, actor.CivilizationId, f.OwnerCivilizationId)
                    && f.TransitTotalCost == 0 && covered.Contains(f.LocationNodeId, StringComparer.Ordinal)
                    && (!settings.Single(s => s.FleetId == f.FleetId).ApproachCloak
                        || f.ShipIds!.Any(id => cloak.Single(c => c.FitId == Fit(id).DefinitionId).Capable != true)))
                    .OrderBy(f => f.FleetId, StringComparer.Ordinal).ToList();
                contacts.RemoveAll(c => c.ActorId == actor.CivilizationId && covered.Contains(c.LastSeenNodeId, StringComparer.Ordinal)
                    && !detected.Any(f => f.FleetId == c.FleetId)); // No detection clears an obsolete track; it does not disprove stealth.
                foreach (var fleet in detected)
                {
                    contacts.RemoveAll(c => c.ActorId == actor.CivilizationId && c.FleetId == fleet.FleetId);
                    contacts.Add(new CampaignContactKnowledgeDto { ActorId = actor.CivilizationId, FleetId = fleet.FleetId,
                        LastSeenNodeId = fleet.LocationNodeId, LastSeenTurn = turn, TrackingNodeIds = new List<string> { fleet.LocationNodeId } });
                }
            }
        }
        private void RevalidateGeneratedFollow(CampaignTacticalTravelSaveDto next)
        {
            foreach(var order in next.Orders.Where(o=>o.Intent==CampaignTravelIntent.Follow))
            {
                var actor=next.Core.Campaign.GameState.Fleets!.Single(f=>f.FleetId==order.FleetId).OwnerCivilizationId;
                var observed=next.Contacts.SingleOrDefault(c=>c.ActorId==actor&&c.FleetId==order.ContactFleetId);
                if(observed!=null&&observed.LastSeenTurn==order.ExpectedContactSeenTurn&&GeneratedContactFresh(next.Core.Campaign.GameState,observed))continue;
                order.Intent=order.RouteNodeIds.Count>0?CampaignTravelIntent.Leave:CampaignTravelIntent.Stay;
                order.ContactFleetId=null;order.ExpectedContactSeenTurn=null;
            }
        }
    }
}
