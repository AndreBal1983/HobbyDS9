using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        // Projection facts only. Command validation always recompiles the actual
        // frozen source through its existing authoritative path.
        private sealed class ColonisationViewCapacity
        {
            internal readonly bool Supported;internal readonly int Study,Contact,People,Food,Materials;
            internal ColonisationViewCapacity(ColonisationSupportedShip? ship)
            {Supported=ship!=null;Study=ship?.StudyWork??0;Contact=ship?.ContactCapacity??0;People=ship?.Passengers??0;Food=ship?.Food??0;Materials=ship?.Materials??0;}
        }
        private void WarmColonisationViews(GameStateDto game)
        {
            foreach(var source in game.Blueprints!)
            {
                var key=(source.OwnerCivilizationId,source.BlueprintId,source.Revision);
                if(colonisation!.ViewCapacities.ContainsKey(key))continue;
                var facts=PublishedTacticalSupportCompiler.EvaluateColonisation(new ShipDesignCatalogueSnapshot(source.FrozenCatalogue!),CampaignBlueprints.Restore(source),
                    CampaignBlueprints.FrozenAccess(source.FrozenCatalogue!),colonisation.Support).Ship;
                colonisation.ViewCapacities.Add(key,new ColonisationViewCapacity(facts));
            }
        }
        private ColonisationViewCapacity ColonisationView(GameStateDto game,ShipStateDto ship)
        {
            var blueprint=ship.Blueprint;
            if(blueprint==null&&activities!.AuthoredActivityBlueprints.TryGetValue(ship.ShipId,out var authored))blueprint=new BlueprintReferenceDto{BlueprintId=authored,Revision=1};
            return blueprint!=null&&colonisation!.ViewCapacities.TryGetValue((ship.OwnerCivilizationId,blueprint.BlueprintId,blueprint.Revision),out var facts)?facts:new ColonisationViewCapacity(null);
        }
        private bool ColonisationViewSupported(GameStateDto game,BlueprintRevisionDto record)
        {
            if(!colonisation!.ViewCapacities.TryGetValue((record.OwnerCivilizationId,record.BlueprintId,record.Revision),out var view)||!view.Supported)return false;
            var access=CampaignBlueprints.Access(definitions,game.Civilizations!.Single(c=>c.CivilizationId==record.OwnerCivilizationId)).AvailableVersionIds!;
            return access.Contains(record.Fit!.HullVersionId,StringComparer.Ordinal)&&record.Fit.Installations!.All(i=>access.Contains(i.ComponentVersionId,StringComparer.Ordinal));
        }
        private CampaignActivityStateDto CopyColonisationOwnActivities(string actor)
        {
            var a=activities!.State;var observed=a.Observations!.Where(o=>o.ActorId==actor).ToList();var known=observed.Select(o=>o.SiteId).ToHashSet(StringComparer.Ordinal);
            var sites=a.Sites!.Where(s=>known.Contains(s.SiteId)).ToList();var bodies=sites.Select(s=>s.BodyId).ToHashSet(StringComparer.Ordinal);var minors=sites.Select(s=>s.MinorActorId).ToHashSet(StringComparer.Ordinal);
            return CampaignActivitiesData.Copy(new CampaignActivityStateDto{Production=new List<ActivityProductionItemDto>(),Structures=a.Structures!.Where(s=>s.OwnerId==actor).ToList(),
                Findings=a.Findings!.Where(f=>f.ActorId==actor).ToList(),Observations=observed,Sites=sites,Bodies=a.Bodies!.Where(b=>bodies.Contains(b.BodyId)).ToList(),
                Minors=a.Minors!.Where(m=>minors.Contains(m.ActorId)).ToList(),Relations=a.Relations!.Where(r=>r.ActorId==actor).ToList()});
        }
        public ColonisationCampaignSnapshot CaptureColonisation(string actor)
        {
            lock(gate)
            {
                Require(colonisation!=null&&Actor(actor)==CampaignTacticalError.None,"Controlled colonisation observation required.");
                var game=state.Campaign.GameState;var a=colonisation!.State;var owner=PlanetaryOwner(game);
                var worlds=new List<ColonisationWorldSnapshot>();
                foreach(var colony in game.Colonies!.Where(c=>c.OwnerCivilizationId==actor).OrderBy(c=>c.ColonyId,StringComparer.Ordinal))
                {
                    var output=WorldEconomy.Project(colony,game,definitions,owner);var camp=CurrentCamp(a,colony.ColonyId);
                    var planet=game.Planets!.Single(p=>p.PlanetId==colony.PlanetId);var name=definitions.Planets!.Single(d=>d.DefinitionId==planet.DefinitionId).DisplayName;
                    worlds.Add(new ColonisationWorldSnapshot(colony.ColonyId,colony.PlanetId,colony.SystemId,name,camp?.Stage??ColonyEstablishmentStage.Established,camp?.CampId,
                        PopulationRules.Population(colony),colony.Food,output.Industry,output.ResearchOutput,output.FoodOutput,camp?.CompletedIndustry??0,camp?.FedWeeks??0,colony.FoodShortageTurns,camp?.Deaths??0,camp?.LatestReport??"Established world.",
                        ResourceAmountSnapshot.Copy(colony.Stockpiles!),colony.Cohorts!.OrderBy(c=>c.CohortId,StringComparer.Ordinal).Select(c=>new ColonisationCohortSnapshot(c.CohortId,c.SpeciesDefinitionId,c.Population,definitions.Species!.Single(s=>s.DefinitionId==c.SpeciesDefinitionId).DisplayName))));
                }
                var ships=new List<ColonisationTransportSnapshot>();
                foreach(var ship in game.Ships!.Where(s=>s.OwnerCivilizationId==actor).OrderBy(s=>s.ShipId,StringComparer.Ordinal))
                {
                    var capacity=ColonisationView(game,ship);if(capacity.People==0)continue;
                    var source=CampaignBlueprints.Find(game,ship.Blueprint!)!;var fleet=game.Fleets!.Single(f=>f.FleetId==ship.FleetId);
                    var manifest=a.ManifestRecords!.SingleOrDefault(m=>m.ShipId==ship.ShipId&&m.OwnerId==actor&&m.Stage==SettlerManifestStage.Loaded);
                    ships.Add(new ColonisationTransportSnapshot(ship.ShipId,ship.FleetId,fleet.LocationNodeId,source.Fit!.DisplayName,source.Fit.HullVersionId,source.BlueprintId,source.Revision,
                        fleet.TransitTotalCost==0&&fleet.RouteNodeIds!.Count==0,ship.CurrentHull>0,capacity.People,capacity.Food,capacity.Materials,manifest?.ManifestId,manifest?.Kind,manifest?.Passengers?.Population??0,
                        manifest?.Food??0,manifest?.PassengerDeaths??0,manifest?.FoodShortageWeeks??0,manifest?.LatestReport??"Empty reusable transport.",ResourceAmountSnapshot.Copy(manifest?.Cargo??new List<ResourceAmountDto>())));
                }
                var queue=new List<ColonisationQueueSnapshot>();
                foreach(var group in colonisation.Production.Where(q=>q.OwnerId==actor).GroupBy(q=>q.ColonyId,StringComparer.Ordinal))
                {
                    foreach(var order in group)
                    {
                        var first=ReferenceEquals(group.First(),order);var world=worlds.Single(c=>c.Id==order.ColonyId);var colony=game.Colonies!.Single(c=>c.ColonyId==order.ColonyId);
                        var work=first?StreamedMaterialWork.Plan(order.RequiredResources!,colony.Stockpiles!,order.RequiredIndustry,order.CompletedIndustry,world.Stage==ColonyEstablishmentStage.Established?world.Industry:0).CompletedWork-order.CompletedIndustry:0;
                        var status=!first?"Waiting behind earlier order":world.Stage!=ColonyEstablishmentStage.Established?"Requires an active established world":world.Industry<=0?"Awaiting Industry":work==0?"Awaiting materials":"Ready with current stocks";
                        var name=order.Kind==ColonisationProductionKind.PublishedShip?CampaignBlueprints.Find(game,order.Blueprint!)!.Fit!.DisplayName:order.Kind==ColonisationProductionKind.SettlementKit?"Settlement kit":definitions.Infrastructure!.Single(i=>i.DefinitionId==order.DefinitionId).DisplayName;
                        queue.Add(new ColonisationQueueSnapshot(order.QueueItemId,order.ColonyId,order.Kind,order.DefinitionId,name,order.Blueprint?.Revision??0,order.RequiredIndustry,order.CompletedIndustry,status,
                            ResourceAmountSnapshot.Copy(order.ConsumedResources!),ResourceAmountSnapshot.Copy(order.RequiredResources!)));
                    }
                }
                var disclosed=ColonisationPlanetsForActor(game,actor);var presence=disclosed.GroupBy(p=>p.SystemId,StringComparer.Ordinal).Select(g=>new ColonisationPresenceSnapshot(g.Key,
                    g.Where(p=>p.ControlObservedTurn.HasValue).Select(p=>p.ControlObservedTurn!.Value).DefaultIfEmpty(0).Min(),g.Where(p=>p.KnownControl?.ActorId!=null).Select(p=>p.KnownControl!.ActorId!).Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal)));
                var sources=new List<ColonisationOwnShipSourceSnapshot>();
                foreach(var own in game.Ships!.Where(s=>s.OwnerCivilizationId==actor))
                {
                    var reference=own.Blueprint;if(reference==null&&activities!.AuthoredActivityBlueprints.TryGetValue(own.ShipId,out var authored))reference=new BlueprintReferenceDto{BlueprintId=authored,Revision=1};
                    if(reference!=null)sources.Add(new ColonisationOwnShipSourceSnapshot(own.ShipId,reference.BlueprintId,reference.Revision));
                }
                var latest=colonisation.SettlementProof;var deployments=new List<ColonisationDeploymentReport>();
                if(latest!=null)foreach(var receipt in latest.Receipts!.Where(r=>r.Intent!.ActorId==actor&&r.ManifestBefore?.OwnerId==actor).OrderBy(r=>r.Intent!.IntentId,StringComparer.Ordinal))
                {
                    var intent=receipt.Intent!;var prior=receipt.ManifestBefore!;
                    var aboard=a.ManifestRecords!.Any(m=>m.ManifestId==intent.ManifestId&&m.ShipId==prior.ShipId&&m.OwnerId==actor&&m.Stage==SettlerManifestStage.Loaded&&m.Passengers?.Population>0)
                        &&game.Ships!.Any(s=>s.ShipId==prior.ShipId&&s.OwnerCivilizationId==actor&&s.CurrentHull>0);
                    deployments.Add(new ColonisationDeploymentReport(intent.IntentId,intent.ManifestId,prior.ShipId,intent.PlanetId,latest.Turn,receipt.Outcome,aboard));
                }
                return new ColonisationCampaignSnapshot(actor,game.TurnNumber,game.StateRevision,colonisation.PackageHash,worlds,ships,queue,presence,
                    a.ManifestRecords!.Where(m=>m.OwnerId==actor).OrderBy(m=>m.ManifestId,StringComparer.Ordinal).Select(m=>new ColonisationManifestReport(m.ManifestId,m.ShipId,m.Kind,m.Stage,m.PopulationTransferred,m.PassengerDeaths,m.FoodConsumed,m.FoodLost,m.LatestReport)),
                    a.Surveys!.Where(s=>s.ActorId==actor).Select(s=>new ColonisationSurveySnapshot(s.SurveyId,s.ShipId,s.PlanetId,s.Stage,s.CompletedWeeks)),
                    a.DeploymentIntents!.Where(i=>i.ActorId==actor).Select(i=>new ColonisationIntentSnapshot(i.IntentId,i.ManifestId,i.PlanetId,i.Disputed)),colonisation.Policy.KitIndustry,ResourceAmountSnapshot.Copy(colonisation.Policy.KitResources!),sources,deployments);
            }
        }
        public ColonisationEligibilitySnapshot EvaluateColonisation(ColonisationCommandDto command)
        {
            lock(gate){Require(colonisation!=null&&command!=null,"Colonisation preview required.");
                if(command!.Embark?.Materials?.Any(r=>r==null)==true||command.Deliver?.Materials?.Any(r=>r==null)==true||command.LoadSupplies?.Materials?.Any(r=>r==null)==true)
                    return new ColonisationEligibilitySnapshot(command,CampaignTacticalError.InvalidOrder);
                var copy=ColonisationData.Copy(command);var candidate=ForkPublished();candidate.publishedExecuting=false;
                return new ColonisationEligibilitySnapshot(copy,candidate.SubmitColonisation(copy).Error);}
        }
        public CampaignTacticalReviewSession ForkColonisationInteraction(int turn,long revision)
        {
            lock(gate){Require(colonisation!=null&&state.Campaign.GameState.TurnNumber==turn&&state.Campaign.GameState.StateRevision==revision,"Obsolete colonisation interaction.");
                var candidate=ForkPublished();candidate.publishedExecuting=false;return candidate;}
        }
    }
}
