using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
internal static class SingleplayerDemoWalkthrough
{
    internal static CampaignSingleplayerDemoSaveDto Run(CampaignSingleplayerDemoPackageDto p,bool reload=false,bool reverse=false)
    {
        var app=CampaignSingleplayerDemoApplication.StartPackage(p);string human=p.HumanActorId!;int battles=0,steps=0;
        void Require(bool ok,string label){if(!ok)throw new InvalidOperationException(label);}
        CommandEnvelope Command(CommandKind kind)
        {var s=app.CaptureStatus();return new CommandEnvelope {ActorCivilizationId=human,Kind=kind,CommandId="command.demo.walk."+s.CampaignRevision,ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision};}
        for(int i=0;i<2;i++){var c=Command(CommandKind.EnqueueProduction);c.EnqueueProduction=new EnqueueProductionPayload {ColonyId="colony.sol",DefinitionId="ship-design.source-cruiser"};Require(app.SubmitStrategicCommand(c).Accepted,"Human paid enqueue");}
        var research=Command(CommandKind.SetResearchAllocation);research.SetResearchAllocation=new SetResearchAllocationPayload {ScienceAllocationUnits=450};Require(app.SubmitStrategicCommand(research).Accepted,"Human R&D");
        while(app.CaptureDemo().Outcome==CampaignDemoOutcome.Active)
        {
            Require(++steps<=128,"Demo stalled beyond deterministic work budget.");
            if(reload)app=CampaignSingleplayerDemoApplication.LoadSave(p,JsonSerializer.Deserialize<CampaignSingleplayerDemoSaveDto>(JsonSerializer.Serialize(app.ExportDemoSave()))!);
            var status=app.CaptureStatus();Require(status.Turn<=13,"Bounded deadline");
            if(status.Phase==SessionPhase.Planning)
            {
                var view=app.CaptureStrategy(human);
                var target=view.Research.Engineering.FirstOrDefault(t=>t.CanTarget);
                if(target!=null&&!view.Research.Target.HasValue){var c=Command(CommandKind.SetEngineeringTarget);c.SetEngineeringTarget=new SetEngineeringTargetPayload {TargetDefinitionId=target.Definition.DefinitionId.Value};Require(app.SubmitStrategicCommand(c).Accepted,"Earned Engineering");}
                if(status.Turn>=4)
                foreach(var fleet in app.GetOwnStrategicFleets(human).OrderBy(f=>f.FleetId.Value,StringComparer.Ordinal))
                {
                    var route=app.FindKnownRoute(human,fleet.LocationNodeId.Value,p.ObjectiveNodeId!);Require(route.Success,"Known route");
                    status=app.CaptureStatus();Require(app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto {ActorId=human,CommandId="command.demo.travel."+status.CampaignRevision,
                        ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Order=new CampaignTravelOrderDto {FleetId=fleet.FleetId.Value,
                            Intent=fleet.LocationNodeId.Value==p.ObjectiveNodeId?CampaignTravelIntent.StayEngage:CampaignTravelIntent.Leave,
                            RouteNodeIds=fleet.LocationNodeId.Value==p.ObjectiveNodeId?new():route.Steps.ToList()}}).Accepted,"Human travel");
                }
                status=app.CaptureStatus();var result=app.AdvanceTurn(status.Turn,status.CampaignRevision);Require(result.Accepted,"Advance failed: "+result.Error);
            }
            else
            {
                var encounters=app.GetEncounters(human).OrderBy(e=>e,StringComparer.Ordinal).ToList();if(reverse)encounters.Reverse();
                foreach(var id in encounters)
                {
                    var o=app.Observe(id,human);if(o.Complete)continue;
                    battles++;
                    var orders=o.StandingOrders.Select(s=>new CampaignTacticalFleetOrderDto {FleetId=s.FleetId,Maneuver=CampaignFleetManeuver.Charge,FocusFleetId=o.LegalFocusFleetIds.FirstOrDefault()}).ToList();if(reverse)orders.Reverse();
                    Require(app.CommitRound(new CommitCampaignTacticalRoundDto {ActorId=human,CommandId="command.demo.round."+id+"."+o.Round,EncounterId=id,
                        ExpectedTurn=o.Turn,ExpectedRound=o.Round,ExpectedBoundaryRevision=o.BoundaryRevision,Replacements=orders}).Accepted,"Human joint round");
                }
                status=app.CaptureStatus();if(status.CanFinalizeBatch)Require(app.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted,"Explicit aftermath");
            }
        }
        var save=app.ExportDemoSave();Require(battles>0,"No real battle occurred");Require(save.Integrated!.BuiltShips!.Any(b=>b.OwnerCivilizationId==human),"No paid human ship completed");
        Require(save.Integrated.Physical!.Core.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId==human).MatureEngineeringApplicationDefinitionIds!.Count>0,"No genuinely earned human Engineering maturity.");
        return save;
    }
}
