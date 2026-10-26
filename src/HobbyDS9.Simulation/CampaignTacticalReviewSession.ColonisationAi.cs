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
        // Only reached inside the existing detached PublishedMutation Advance workspace.
        // Each attempted action has a further disposable workspace; rejected handlers
        // cannot leave payment, allocated IDs, plans or physical orders in the turn.
        private CampaignTacticalTravelSaveDto PlanExpansion(CampaignTacticalTravelSaveDto next)
        {
            var worker=ForkPublished();worker.travel=CampaignTacticalTravelData.Copy(next);worker.state=worker.travel.Core;
            foreach(var actor in worker.expansion!.Plans.Select(p=>p.ActorId).OrderBy(x=>x,StringComparer.Ordinal).ToList())
            {
                var p=worker.ExpansionPlan(actor);p.LatestActions!.Clear();p.LastPlannedTurn=worker.state.Campaign.GameState.TurnNumber;p.HoldReason="";
                var searches=new ExpansionSearch();var passes=0;
                while(passes++<worker.expansion.Source.Policy.MaximumAttempts&&p.LatestActions!.Count<worker.expansion.Source.Policy.MaximumAttempts&&p.LatestActions.Count(r=>r.Accepted)<worker.expansion.Source.Policy.MaximumAccepted)
                {
                    if(!worker.PlanExpansionAction(actor,searches))break;
                    p=worker.ExpansionPlan(actor);
                }
            }
            worker.ValidateActivityState();Require(worker.ColonisationCapacity(1,null,1),"Expansion turn capacity exhausted.");
            AdoptPublished(worker);return TravelCopy();
        }
        private ColonisationAiPlanDto ExpansionPlan(string actor)=>expansion!.Plans.Single(p=>p.ActorId==actor);
        private bool MissionFleet(GameStateDto game,string fleet)
        {
            if(expansion==null)return false;
            foreach(var p in expansion.Plans)
            {
                var assigned=game.Ships!.Where(s=>s.OwnerCivilizationId==p.ActorId&&s.FleetId==fleet).ToList();
                var surveying=colonisation!.State.Surveys!.Any(s=>s.ActorId==p.ActorId&&s.Stage==PlanetSurveyStage.Active&&assigned.Any(ship=>ship.ShipId==s.ShipId));
                if(surveying)return true;
                if(assigned.Any(s=>s.ShipId==p.ScoutShipId)&&p.EstablishedColonyId==null&&p.Stage!=ColonisationAiStage.Suspended&&p.Stage!=ColonisationAiStage.Supporting)return true;
                if(!assigned.Any(s=>s.ShipId==p.TransportShipId))continue;
                if(GrowthEnabled&&p.Stage==ColonisationAiStage.Immigrating)return true;
                if(colonisation.State.ManifestRecords!.Any(m=>m.OwnerId==p.ActorId&&m.ShipId==p.TransportShipId&&m.Stage==SettlerManifestStage.Loaded))return true;
                var camp=colonisation.State.CampSources!.SingleOrDefault(c=>c.CampId==p.CampId&&c.OwnerId==p.ActorId);
                if(camp?.Stage==ColonyEstablishmentStage.Founding)return true;
                if(camp?.Stage==ColonyEstablishmentStage.Established)
                {
                    var world=game.Colonies!.Single(c=>c.ColonyId==camp.ColonyId);var output=ProjectOwnedWorld(world,game);
                    if(output.FoodDemand>output.FoodOutput)return true;
                }
                if(p.EstablishedColonyId==null&&p.Stage!=ColonisationAiStage.Suspended)return true;
            }
            return false;
        }
        private bool MissionWorld(string actor,string colony)
        {
            if(expansion==null)return false;var p=ExpansionPlan(actor);if(p.Stage==ColonisationAiStage.Suspended)return false;
            var game=state.Campaign.GameState;
            return p.HomeColonyId==colony&&p.EstablishedColonyId==null&&PopulationRules.Stock(game.Colonies!.Single(c=>c.ColonyId==colony).Stockpiles!,colonisation!.Policy.EquipmentResourceId)<1
                ||p.YardColonyId==colony&&p.TransportShipId==null
                    &&(!GrowthEnabled||p.Stage!=ColonisationAiStage.GrowthClosed||GrowthSupplyNeeded(game,p));
        }
        private string ExpansionCommandId(string actor)
        {
            var game=state.Campaign.GameState;var ordinal=expansion!.Plans.OrderBy(p=>p.ActorId,StringComparer.Ordinal).Select(p=>p.ActorId).ToList().IndexOf(actor);
            return "ai-command.colonisation.a"+ordinal.ToString(CultureInfo.InvariantCulture)+".t"+game.TurnNumber.ToString(CultureInfo.InvariantCulture)+".n"+ExpansionPlan(actor).LatestActions!.Count.ToString(CultureInfo.InvariantCulture);
        }
        private bool ExpansionAttempt(string actor,ColonisationAiAction action,Func<CampaignTacticalReviewSession,string,CampaignTacticalError> execute)
        {
            var p=ExpansionPlan(actor);var policy=expansion!.Source.Policy;
            if(p.LatestActions!.Count>=policy.MaximumAttempts||p.LatestActions.Count(r=>r.Accepted)>=policy.MaximumAccepted)return false;
            var id=ExpansionCommandId(actor);var before=state.Campaign.GameState.StateRevision;var c=ForkPublished();
            CampaignTacticalError error;
            try
            {
                error=execute(c,id);
                if(error==CampaignTacticalError.None)
                {
                    c.travel=c.TravelCopy();c.ValidateTravelSave(c.travel);c.ValidateActivityState();
                    if(!c.ColonisationCapacity(1,null,1))error=CampaignTacticalError.Overflow;
                }
            }
            catch(OverflowException){error=CampaignTacticalError.Overflow;}
            if(error==CampaignTacticalError.None)AdoptPublished(c);
            p=ExpansionPlan(actor);p.LatestActions!.Add(new ColonisationAiActionReceiptDto{CommandId=id,Action=action,BeforeRevision=before,
                AfterRevision=state.Campaign.GameState.StateRevision,Accepted=error==CampaignTacticalError.None,Error=error});
            if(error!=CampaignTacticalError.None)p.HoldReason="Action unavailable; retained cargo and goal kept.";
            return error==CampaignTacticalError.None;
        }
        private bool ExpansionColonisation(string actor,ColonisationAiAction action,ColonisationCommandDto command)
            =>ExpansionAttempt(actor,action,(c,id)=>{var g=c.state.Campaign.GameState;command.ActorId=actor;command.CommandId=id;command.Turn=g.TurnNumber;command.Revision=g.StateRevision;
                return c.ApplyColonisationCommand(command,ControllerKind.AI).Error;});
        private bool ExpansionBuild(string actor,ColonyStateDto colony,BlueprintRevisionDto source)
            =>ExpansionAttempt(actor,ColonisationAiAction.Ship,(c,id)=>{
                var g=c.state.Campaign.GameState;var error=c.EnqueueActivity(g,new CampaignActivityCommandDto{ActorId=actor,Kind=CampaignActivityCommandKind.EnqueueShip,
                    TargetId=colony.ColonyId,DefinitionId=source.BlueprintId,BlueprintRevision=source.Revision},ControllerKind.AI);
                if(error==CampaignTacticalError.None){g.StateRevision=checked(g.StateRevision+1);g.AcceptedCommandIds!.Add(id);
                    c.ExpansionPlan(actor).TransportQueueId=c.colonisation!.Production.Last().QueueItemId;}
                return error;});
        // At most two bounded, actor-known graph traversals per actor/week. These
        // ephemeral paths never certify commands or survive a planning invocation.
        private sealed class ExpansionSearch
        {internal Dictionary<string,ExpansionPaths> Origins=new Dictionary<string,ExpansionPaths>(StringComparer.Ordinal);}
        private sealed class ExpansionPaths
        {
            internal string Origin="";
            internal Dictionary<string,System.Numerics.BigInteger> Distance=new Dictionary<string,System.Numerics.BigInteger>(StringComparer.Ordinal);
            internal Dictionary<string,string> Previous=new Dictionary<string,string>(StringComparer.Ordinal);
        }
        private ExpansionPaths? ExpansionRoutes(GameStateDto game,string actor,string origin,ExpansionSearch searches)
        {
            if(searches.Origins.TryGetValue(origin,out var prior))return prior;
            var policy=expansion!.Source.Policy;if(searches.Origins.Count>=policy.BfsTraversals)return null;
            var k=game.NavigationKnowledge!.Single(k=>k.CivilizationId==actor);
            var nodes=k.Nodes!.Where(n=>n.Level!=NavigationKnowledgeLevel.Unknown).Select(n=>n.Id).ToHashSet(StringComparer.Ordinal);
            var edges=k.Connections!.Where(n=>n.Level!=NavigationKnowledgeLevel.Unknown).Select(n=>n.Id).ToHashSet(StringComparer.Ordinal);
            if(nodes.Count>policy.KnownNodes||edges.Count>policy.KnownEdges||!nodes.Contains(origin))return null;
            var adjacent=nodes.ToDictionary(n=>n,_=>new List<string>(),StringComparer.Ordinal);
            foreach(var e in definitions.NavigationConnections!.Where(e=>edges.Contains(e.ConnectionId)&&nodes.Contains(e.EndpointA)&&nodes.Contains(e.EndpointB)))
            {adjacent[e.EndpointA].Add(e.EndpointB);adjacent[e.EndpointB].Add(e.EndpointA);}
            var paths=new ExpansionPaths{Origin=origin};paths.Distance[origin]=0;var settled=new HashSet<string>(StringComparer.Ordinal);
            while(true)
            {
                var current=paths.Distance.Where(d=>!settled.Contains(d.Key)).OrderBy(d=>d.Value).ThenBy(d=>d.Key,StringComparer.Ordinal).Select(d=>d.Key).FirstOrDefault();
                if(current==null)break;settled.Add(current);
                foreach(var next in adjacent[current].OrderBy(n=>n,StringComparer.Ordinal))
                {
                    if(settled.Contains(next))continue;var distance=paths.Distance[current]+physicalNavigation!.Cost(game,current,next).EffectiveCost;
                    if(!paths.Distance.TryGetValue(next,out var old)||distance<old){paths.Distance[next]=distance;paths.Previous[next]=current;}
                }
            }
            searches.Origins.Add(origin,paths);return paths;
        }
        private List<string>? ExpansionRoute(GameStateDto game,string actor,string origin,string destination,ExpansionSearch searches)
        {
            var paths=ExpansionRoutes(game,actor,origin,searches);
            if(paths==null||!paths.Distance.TryGetValue(destination,out var cost)||cost>long.MaxValue)return null;
            var route=new List<string>();var node=destination;while(node!=origin){route.Add(node);node=paths.Previous[node];}route.Reverse();return route;
        }
        private bool ExpansionMove(string actor,ShipStateDto ship,string destination,ExpansionSearch searches)
        {
            var g=state.Campaign.GameState;var fleet=g.Fleets!.Single(f=>f.FleetId==ship.FleetId);
            if(fleet.TransitTotalCost>0||fleet.RouteNodeIds!.Count>0||fleet.LocationNodeId==destination)return false;
            var route=ExpansionRoute(g,actor,fleet.LocationNodeId,destination,searches);
            if(route==null){ExpansionPlan(actor).HoldReason="No currently legal known route within this week's work bound; cargo retained.";return false;}
            return ExpansionAttempt(actor,ColonisationAiAction.Travel,(c,id)=>{
                var cg=c.state.Campaign.GameState;var command=new SubmitCampaignTravelOrderDto{ActorId=actor,CommandId=id,ExpectedTurn=cg.TurnNumber,ExpectedRevision=cg.StateRevision,
                    Order=new CampaignTravelOrderDto{FleetId=ship.FleetId,Intent=CampaignTravelIntent.Leave,RouteNodeIds=route}};
                var error=c.ValidateTravelCommand(command,c.travel!,true);if(error==CampaignTacticalError.None)ApplyTravelCommand(c.travel!,command);return error;});
        }
        private string? ExpansionExplore(GameStateDto game,string actor,string origin,ExpansionSearch searches)
        {
            var paths=ExpansionRoutes(game,actor,origin,searches);if(paths==null)return null;
            var visited=discoveries.Where(d=>d.ActorId==actor).Select(d=>d.ArrivalNodeId)
                .Concat(game.Colonies!.Where(c=>c.OwnerCivilizationId==actor).Select(c=>physicalNavigation!.NodeForSystem(c.SystemId)!)).ToHashSet(StringComparer.Ordinal);
            return paths.Distance.Where(d=>d.Value>0&&!visited.Contains(d.Key)).OrderBy(d=>d.Value).ThenBy(d=>d.Key,StringComparer.Ordinal)
                .Take(expansion!.Source.Policy.RouteCandidates).Select(d=>d.Key).FirstOrDefault();
        }
        private long ExpansionFoodAvailable(ColonyStateDto home,GameStateDto game)
        {
            var output=ProjectOwnedWorld(home,game);var reserve=(System.Numerics.BigInteger)Math.Max(0,output.FoodDemand-output.FoodOutput)*expansion!.Source.Policy.HomeDeficitWeeks;
            return (long)System.Numerics.BigInteger.Max(0,(System.Numerics.BigInteger)home.Food-reserve);
        }
        private ColonisationPlanetObservationDto? SelectExpansionBody(IEnumerable<ColonisationPlanetObservationDto> observations,ColonisationAiPlanDto p,
            PopulationCohortDto? cohort,IReadOnlyDictionary<string,System.Numerics.BigInteger> observedSystemCosts)
            =>observations.Where(o=>!p.OwnAttemptedTargets!.Contains(o.PlanetId)&&(!o.Surveyed||o.KnownControl?.Kind==PlanetControllerKind.Unclaimed))
                // Unknown environments remain survey candidates. Only lawfully completed
                // observations can be rejected for suitability before the candidate cap.
                .Where(o=>!o.Surveyed||cohort!=null&&o.Environment!.Habitats!.Any(h=>h.NaturalCapacity>=colonisation!.Policy.Settlers
                    &&WorldEconomy.Suitability(definitions.Species!.Single(s=>s.DefinitionId==cohort.SpeciesDefinitionId),o.Environment,h.Kind)>0))
                .Where(o=>observedSystemCosts.ContainsKey(o.SystemId)).OrderByDescending(o=>o.Surveyed)
                .ThenBy(o=>observedSystemCosts[o.SystemId]).ThenBy(o=>o.PlanetId,StringComparer.Ordinal).Take(expansion!.Source.Policy.BodyCandidates).FirstOrDefault();
        private bool PlanExpansionAction(string actor,ExpansionSearch searches)
        {
            var game=state.Campaign.GameState;var a=colonisation!.State;var p=ExpansionPlan(actor);var policy=expansion!.Source.Policy;
            var own=game.Colonies!.Where(c=>c.OwnerCivilizationId==actor&&EstablishedColony(c.ColonyId)&&PopulationRules.Population(c)>0).OrderBy(c=>c.ColonyId,StringComparer.Ordinal).ToList();
            var home=own.FirstOrDefault(c=>c.ColonyId==p.HomeColonyId)??own.FirstOrDefault(c=>PopulationRules.Population(c)-colonisation.Policy.Settlers>=colonisation.Policy.SourcePopulationFloor
                &&c.Cohorts!.Any(co=>(System.Numerics.BigInteger)co.Population*colonisation.Policy.MaximumSourceCohortBps>=(System.Numerics.BigInteger)colonisation.Policy.Settlers*10000))??own.FirstOrDefault();
            if(home==null){p.HoldReason="No sustainable owned source population.";return false;}
            p.HomeColonyId=home.ColonyId;
            var cohort=home.Cohorts!.Where(co=>(System.Numerics.BigInteger)co.Population*colonisation.Policy.MaximumSourceCohortBps>=(System.Numerics.BigInteger)colonisation.Policy.Settlers*10000)
                .OrderBy(co=>co.CohortId,StringComparer.Ordinal).FirstOrDefault();p.SourceCohortId=cohort?.CohortId;
            var yard=own.Where(c=>WorldEconomy.Infrastructure(c,definitions).Any(i=>i.definition.Shipbuilding)).OrderByDescending(c=>c.SystemId==home.SystemId).ThenByDescending(c=>ProjectOwnedWorld(c,game).Industry).ThenBy(c=>c.ColonyId,StringComparer.Ordinal).FirstOrDefault();
            p.YardColonyId=yard?.ColonyId;
            var scout=game.Ships!.Where(s=>s.OwnerCivilizationId==actor&&s.CurrentHull>0&&(ActivityCapability(game,s)?.StudyWork??0)>0).OrderBy(s=>s.ShipId,StringComparer.Ordinal).FirstOrDefault();
            p.ScoutShipId=scout?.ShipId;
            var transport=game.Ships!.Where(s=>s.OwnerCivilizationId==actor&&s.CurrentHull>0).OrderBy(s=>s.ShipId,StringComparer.Ordinal).FirstOrDefault(s=>TransportCapability(game,s)?.Passengers>=colonisation.Policy.Settlers);
            p.TransportShipId=transport?.ShipId;
            var manifest=transport==null?null:a.ManifestRecords!.SingleOrDefault(m=>m.ShipId==transport.ShipId&&m.OwnerId==actor&&m.Stage==SettlerManifestStage.Loaded);
            p.ManifestId=manifest?.ManifestId??p.ManifestId;
            var camp=a.CampSources!.Where(c=>c.OwnerId==actor).OrderByDescending(c=>c.DeployedTurn).ThenBy(c=>c.CampId,StringComparer.Ordinal).FirstOrDefault();
            if(camp!=null){p.CampId=camp.CampId;if(camp.Stage!=ColonyEstablishmentStage.EmptyOutpost&&p.Stage!=ColonisationAiStage.Recovering&&!(GrowthEnabled&&(p.Stage==ColonisationAiStage.Immigrating||p.Stage==ColonisationAiStage.GrowthClosed)))p.Stage=ColonisationAiStage.Supporting;if(camp.Stage==ColonyEstablishmentStage.Established)p.EstablishedColonyId=camp.ColonyId;}
            if(GrowthEnabled)
            {
                var carried=a.ManifestRecords!.Where(m=>m.OwnerId==actor&&m.Kind==SettlerManifestKind.ImmigrationOnly).OrderBy(m=>m.ManifestId,StringComparer.Ordinal).LastOrDefault();
                if(carried!=null&&carried.Stage!=SettlerManifestStage.Loaded)p.Stage=ColonisationAiStage.GrowthClosed;
                if(carried?.Stage==SettlerManifestStage.Loaded&&(transport==null||transport.ShipId!=carried.ShipId))
                {p.HoldReason="Immigration carrier unavailable; retained cargo awaits its survival boundary.";return false;}
                if(manifest?.Kind==SettlerManifestKind.ImmigrationOnly)
                    return PlanGrowthPassengers(actor,p,home,transport!,manifest,camp,searches);
            }
            // Urgent handling precedes every discretionary search/failed-target limit.
            if(manifest!=null&&(manifest.Kind==SettlerManifestKind.ReturnOnly||p.Stage==ColonisationAiStage.Recovering))
            {
                p.Stage=ColonisationAiStage.Recovering;
                var intent=a.DeploymentIntents!.SingleOrDefault(i=>i.ManifestId==manifest.ManifestId);
                if(intent!=null)return ExpansionColonisation(actor,ColonisationAiAction.Withdraw,new(){Action=ColonisationAction.WithdrawDeployment,Withdraw=new(){IntentId=intent.IntentId}});
                if(ShipAt(game,transport!,home.SystemId))
                    return ExpansionColonisation(actor,ColonisationAiAction.Unload,new(){Action=ColonisationAction.UnloadTransport,Unload=new(){ManifestId=manifest.ManifestId,ColonyId=home.ColonyId}});
                return ExpansionMove(actor,transport!,physicalNavigation!.NodeForSystem(home.SystemId)!,searches);
            }
            if(GrowthEnabled&&p.Stage==ColonisationAiStage.GrowthClosed&&(transport==null||camp?.Stage==ColonyEstablishmentStage.EmptyOutpost))
            {
                // Closing immigration does not retire ordinary food logistics. Only a
                // current populated receiver's real shortage can reserve a replacement.
                if(transport==null&&yard!=null&&GrowthSupplyNeeded(game,p))
                {
                    var source=game.Blueprints!.Where(b=>b.OwnerCivilizationId==actor&&b.Fit!.ClassId=="class.galaxy-colony-transport.v1")
                        .OrderBy(b=>b.BlueprintId,StringComparer.Ordinal).ThenByDescending(b=>b.Revision).FirstOrDefault();
                    if(source!=null&&!colonisation.Production.Any(q=>q.OwnerId==actor&&q.Kind==ColonisationProductionKind.PublishedShip&&q.Blueprint?.BlueprintId==source.BlueprintId))
                    {p.BlueprintId=source.BlueprintId;p.BlueprintRevision=source.Revision;return ExpansionBuild(actor,yard,source);}
                }
                p.HoldReason="Immigration closed; replacement transport waits for actual ordinary supply need.";return false;
            }
            if(p.Stage==ColonisationAiStage.Recovering&&manifest==null){p.TargetPlanetId=null;p.TargetNodeId=null;p.DisputedBoundaries=0;p.Stage=ColonisationAiStage.Preparing;}
            if(camp!=null&&camp.Stage!=ColonyEstablishmentStage.EmptyOutpost&&transport!=null)
            {
                var world=game.Colonies!.Single(c=>c.ColonyId==camp.ColonyId);var output=ProjectOwnedWorld(world,game);
                var demand=Math.Max(0,output.FoodDemand-output.FoodOutput);
                if(manifest!=null&&manifest.Kind==SettlerManifestKind.SupplyOnly)
                {
                    if(ShipAt(game,transport,world.SystemId))return ExpansionColonisation(actor,ColonisationAiAction.Deliver,new(){Action=ColonisationAction.DeliverCarriedSupplies,
                        Deliver=new(){ManifestId=manifest.ManifestId,ColonyId=world.ColonyId,Food=manifest.Food,Materials=manifest.Cargo!.Where(r=>r.Amount>0).Select(PopulationData.Copy).ToList()}});
                    return ExpansionMove(actor,transport,physicalNavigation!.NodeForSystem(world.SystemId)!,searches);
                }
                if(demand>0&&(System.Numerics.BigInteger)world.Food<(System.Numerics.BigInteger)demand*(colonisation.Policy.MaximumFood+policy.ContingencyWeeks))
                {
                    if(ShipAt(game,transport,home.SystemId)&&ExpansionFoodAvailable(home,game)>0)
                        return ExpansionColonisation(actor,ColonisationAiAction.SupplyLoad,new(){Action=ColonisationAction.LoadCarriedSupplies,LoadSupplies=new(){ShipId=transport.ShipId,SourceColonyId=home.ColonyId,Food=Math.Min(ExpansionFoodAvailable(home,game),colonisation.Policy.MaximumFood)}});
                    if(ShipAt(game,transport,world.SystemId)&&(System.Numerics.BigInteger)world.Food<=(System.Numerics.BigInteger)demand*policy.ContingencyWeeks&&camp.Stage==ColonyEstablishmentStage.Founding)
                        return ExpansionColonisation(actor,ColonisationAiAction.Evacuate,new(){Action=ColonisationAction.EvacuateColony,Evacuate=new(){CampId=camp.CampId,ShipId=transport.ShipId}});
                    return ExpansionMove(actor,transport,physicalNavigation!.NodeForSystem(home.SystemId)!,searches);
                }
                if(GrowthEnabled&&camp.Stage==ColonyEstablishmentStage.Established&&p.Stage!=ColonisationAiStage.GrowthClosed)
                    return PlanGrowthShipment(actor,p,home,world,cohort,transport,searches);
                p.HoldReason=GrowthEnabled&&p.Stage==ColonisationAiStage.GrowthClosed?"One immigration attempt closed; ordinary supply remains available.":"Established expedition supplied; retained transport available.";return false;
            }
            if(p.EstablishedColonyId!=null&&camp?.Stage==ColonyEstablishmentStage.EmptyOutpost)
            {p.Stage=ColonisationAiStage.Suspended;p.HoldReason="Completed frontier world is an empty outpost; no second-colony goal granted.";return false;}
            if(p.OwnAttemptedTargets!.Count>=policy.FailedTargets){p.Stage=ColonisationAiStage.Suspended;p.HoldReason="Observed target budget exhausted; no further discretionary expedition.";return false;}
            if(cohort==null||PopulationRules.Population(home)-colonisation.Policy.Settlers<colonisation.Policy.SourcePopulationFloor)
            {p.HoldReason="No sustainable source cohort for a discretionary expedition; retained return cargo remains recoverable.";return false;}
            // Reserve real transport/kit production ahead of ordinary warship procurement.
            if(transport==null&&yard!=null&&!colonisation.Production.Any(q=>q.OwnerId==actor&&q.Kind==ColonisationProductionKind.PublishedShip&&q.Blueprint?.BlueprintId==p.BlueprintId))
            {
                var source=game.Blueprints!.Where(b=>b.OwnerCivilizationId==actor&&b.Fit!.ClassId=="class.galaxy-colony-transport.v1").OrderBy(b=>b.BlueprintId,StringComparer.Ordinal).ThenByDescending(b=>b.Revision).FirstOrDefault();
                if(source!=null){p.BlueprintId=source.BlueprintId;p.BlueprintRevision=source.Revision;return ExpansionBuild(actor,yard,source);}
            }
            var homeOutput=ProjectOwnedWorld(home,game);
            if(ExpansionFoodAvailable(home,game)<colonisation.Policy.MaximumFood&&homeOutput.FoodDemand>=homeOutput.FoodOutput)
            {
                var food=activities!.Content.Structures!.Where(s=>s.FoodOutput>0&&!home.Infrastructure!.Any(i=>i.DefinitionId==s.DefinitionId&&i.Count>0)
                    &&!colonisation.Production.Any(q=>q.ColonyId==home.ColonyId&&q.DefinitionId==s.DefinitionId)).OrderBy(s=>s.DefinitionId,StringComparer.Ordinal).FirstOrDefault();
                if(food!=null)return ExpansionAttempt(actor,ColonisationAiAction.Infrastructure,(c,id)=>{
                    var g=c.state.Campaign.GameState;var error=c.EnqueueActivity(g,new(){ActorId=actor,Kind=CampaignActivityCommandKind.EnqueueStructure,TargetId=home.ColonyId,DefinitionId=food.DefinitionId},ControllerKind.AI);
                    if(error==CampaignTacticalError.None){g.StateRevision=checked(g.StateRevision+1);g.AcceptedCommandIds!.Add(id);}return error;});
            }
            if(PopulationRules.Stock(home.Stockpiles!,colonisation.Policy.EquipmentResourceId)<1&&!colonisation.Production.Any(q=>q.ColonyId==home.ColonyId&&q.Kind==ColonisationProductionKind.SettlementKit))
                return ExpansionAttempt(actor,ColonisationAiAction.Kit,(c,id)=>{var g=c.state.Campaign.GameState;var result=c.ApplyColonisationCommand(new(){ActorId=actor,CommandId=id,Turn=g.TurnNumber,Revision=g.StateRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId=home.ColonyId}},ControllerKind.AI);
                    if(result.Accepted)c.ExpansionPlan(actor).KitQueueId=c.colonisation!.Production.Last().QueueItemId;return result.Error;});
            var observations=ColonisationPlanetsForActor(game,actor);
            var target=observations.FirstOrDefault(o=>o.PlanetId==p.TargetPlanetId);
            if(target!=null&&target.Surveyed&&(target.KnownControl?.Kind!=PlanetControllerKind.Unclaimed||cohort!=null&&!target.Environment!.Habitats!.Any(h=>h.NaturalCapacity>=colonisation.Policy.Settlers&&WorldEconomy.Suitability(definitions.Species!.Single(s=>s.DefinitionId==cohort.SpeciesDefinitionId),target.Environment,h.Kind)>0)))
            {
                if(!p.OwnAttemptedTargets.Contains(target.PlanetId)&&p.OwnAttemptedTargets.Count<policy.FailedTargets)p.OwnAttemptedTargets.Add(target.PlanetId);
                p.TargetPlanetId=null;p.TargetNodeId=null;
                if(manifest!=null){p.Stage=ColonisationAiStage.Recovering;p.HoldReason="Own observed target is no longer suitable; return loaded settlers.";}
                return true;
            }
            if(target==null)
            {
                var origin=transport==null?scout==null?physicalNavigation!.NodeForSystem(home.SystemId)!:game.Fleets!.Single(f=>f.FleetId==scout.FleetId).LocationNodeId:physicalNavigation!.NodeForSystem(home.SystemId)!;
                var paths=ExpansionRoutes(game,actor,origin,searches);
                var costs=observations.Select(o=>o.SystemId).Distinct(StringComparer.Ordinal)
                    .Where(system=>paths!=null&&paths.Distance.ContainsKey(physicalNavigation!.NodeForSystem(system)!))
                    .ToDictionary(system=>system,system=>paths!.Distance[physicalNavigation!.NodeForSystem(system)!],StringComparer.Ordinal);
                target=SelectExpansionBody(observations,p,cohort,costs);
                p.TargetPlanetId=target?.PlanetId;p.TargetNodeId=target==null?null:physicalNavigation!.NodeForSystem(target.SystemId);
            }
            if(target==null||!target.Surveyed)
            {
                p.Stage=ColonisationAiStage.Scouting;if(scout==null){p.HoldReason="No living owned survey ship.";return false;}
                var fleet=game.Fleets!.Single(f=>f.FleetId==scout.FleetId);
                if(fleet.TransitTotalCost>0||fleet.RouteNodeIds!.Count>0)return false;
                if(target!=null&&ShipAt(game,scout,target.SystemId))
                {
                    if(target.ActiveSurveyId!=null)return false;
                    return ExpansionColonisation(actor,ColonisationAiAction.Survey,new(){Action=ColonisationAction.StartPlanetSurvey,StartSurvey=new(){ShipId=scout.ShipId,PlanetId=target.PlanetId}});
                }
                var destination=target==null?ExpansionExplore(game,actor,physicalNavigation!.NodeForSystem(home.SystemId)!,searches):p.TargetNodeId;
                return destination!=null&&ExpansionMove(actor,scout,destination,searches);
            }
            if(transport==null){p.Stage=ColonisationAiStage.Preparing;p.HoldReason="Transport construction progressing through ordinary local industry/material delivery.";return false;}
            if(manifest==null)
            {
                p.Stage=ColonisationAiStage.Assembling;
                if(!ShipAt(game,transport,home.SystemId))return ExpansionMove(actor,transport,physicalNavigation!.NodeForSystem(home.SystemId)!,searches);
                if(scout!=null&&!ShipAt(game,scout,home.SystemId))return ExpansionMove(actor,scout,physicalNavigation!.NodeForSystem(home.SystemId)!,searches);
                if(scout!=null&&scout.FleetId!=transport.FleetId)
                    return ExpansionAttempt(actor,ColonisationAiAction.Organise,(c,id)=>{
                        var g=c.state.Campaign.GameState;var result=c.ApplyFleetOrganization(new(){ActorId=actor,CommandId=id,ExpectedTurn=g.TurnNumber,ExpectedRevision=g.StateRevision,
                            Kind=CampaignFleetOrganizationKind.Transfer,ShipIds=new List<string>{transport.ShipId},TargetFleetId=scout.FleetId},ControllerKind.AI);
                        return result.Accepted?CampaignTacticalError.None:result.Error==CampaignFleetOrganizationError.Overflow?CampaignTacticalError.Overflow:CampaignTacticalError.InvalidOrder;});
                if(PopulationRules.Stock(home.Stockpiles!,colonisation.Policy.EquipmentResourceId)<1||cohort==null)return false;
                var route=ExpansionRoute(game,actor,physicalNavigation!.NodeForSystem(home.SystemId)!,p.TargetNodeId!,searches);
                if(route==null||ExpansionFoodAvailable(home,game)<colonisation.Policy.MaximumFood){p.HoldReason="Retain source-world food reserve before embarkation.";return false;}
                var fleet=game.Fleets!.Single(f=>f.FleetId==transport.FleetId);
                var duration=physicalNavigation!.Travel(game,fleet,definitions,route).EstimatedTurns;
                if((System.Numerics.BigInteger)duration+1+colonisation.Policy.MinimumFedWeeks+policy.ContingencyWeeks>colonisation.Policy.MaximumFood){p.HoldReason="Known voyage exceeds finite carried-food plan.";return false;}
                return ExpansionColonisation(actor,ColonisationAiAction.Embark,new(){Action=ColonisationAction.EmbarkSettlers,Embark=new(){ShipId=transport.ShipId,ColonyId=home.ColonyId,CohortId=cohort.CohortId,Population=colonisation.Policy.Settlers,Food=colonisation.Policy.MaximumFood}});
            }
            p.Stage=ColonisationAiStage.Loaded;
            var deployment=a.DeploymentIntents!.SingleOrDefault(i=>i.ManifestId==manifest.ManifestId);
            if(deployment!=null){p.Stage=ColonisationAiStage.Deploying;return false;}
            if(ShipAt(game,transport,target.SystemId))
            {
                var accepted=ExpansionColonisation(actor,ColonisationAiAction.Deploy,new(){Action=ColonisationAction.RequestDeployment,Deploy=new(){ManifestId=manifest.ManifestId,PlanetId=target.PlanetId}});
                if(!accepted){p=ExpansionPlan(actor);p.Stage=ColonisationAiStage.Recovering;if(!p.OwnAttemptedTargets!.Contains(target.PlanetId))p.OwnAttemptedTargets.Add(target.PlanetId);}
                return accepted;
            }
            if(manifest.Food<=policy.ContingencyWeeks){p.Stage=ColonisationAiStage.Recovering;return true;}
            return ExpansionMove(actor,transport,p.TargetNodeId!,searches);
        }
        private void ReconcileExpansion(GameStateDto game)
        {
            if(expansion==null)return;
            foreach(var p in expansion.Plans)
            {
                var camp=colonisation!.State.CampSources!.Where(c=>c.OwnerId==p.ActorId).OrderByDescending(c=>c.DeployedTurn).FirstOrDefault();
                if(camp!=null){p.CampId=camp.CampId;if(camp.Stage==ColonyEstablishmentStage.Established)p.EstablishedColonyId=camp.ColonyId;if(camp.Stage!=ColonyEstablishmentStage.EmptyOutpost&&p.Stage!=ColonisationAiStage.Recovering&&!(GrowthEnabled&&(p.Stage==ColonisationAiStage.Immigrating||p.Stage==ColonisationAiStage.GrowthClosed)))p.Stage=ColonisationAiStage.Supporting;}
                var receipt=colonisation.SettlementProof!.Receipts!.SingleOrDefault(r=>r.Intent!.ActorId==p.ActorId&&r.Intent.ManifestId==p.ManifestId);
                if(receipt==null||receipt.Outcome==DeploymentBoundaryOutcome.Deployed)continue;
                if(receipt.Outcome==DeploymentBoundaryOutcome.Disputed)p.DisputedBoundaries=Math.Min(expansion.Source.Policy.DisputedWeeks,p.DisputedBoundaries+1);
                if(receipt.Outcome==DeploymentBoundaryOutcome.Interrupted||p.DisputedBoundaries>=expansion.Source.Policy.DisputedWeeks)
                {p.Stage=ColonisationAiStage.Recovering;if(!p.OwnAttemptedTargets!.Contains(receipt.Intent!.PlanetId)&&p.OwnAttemptedTargets.Count<expansion.Source.Policy.FailedTargets)p.OwnAttemptedTargets.Add(receipt.Intent.PlanetId);}
            }
        }
    }
}

