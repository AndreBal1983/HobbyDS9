using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class FrontierWalkthrough
{
    internal static CampaignFrontierSaveDto Run(CampaignFrontierPackageDto p,bool reload=false,bool reverse=false)
    {
        // Reordering is real content/input reordering, not a change to physical route order.
        if(reverse)
        {
            p=JsonSerializer.Deserialize<CampaignFrontierPackageDto>(JsonSerializer.Serialize(p))!;
            var s=p.Integrated!.Physical!.CorePackage!.Campaign!;
            s.Definitions.NavigationRegions!.Reverse();s.Definitions.NavigationConnections!.Reverse();
            s.InitialState.Civilizations!.Reverse();s.InitialState.Fleets!.Reverse();s.InitialState.NavigationKnowledge!.Reverse();
            foreach(var k in s.InitialState.NavigationKnowledge){k.Nodes!.Reverse();k.Connections!.Reverse();}
            p.PublicLaneIds!.Reverse();p.Integrated.Physical.InitialOrders!.Reverse();
        }
        var app=CampaignFrontierApplication.StartPackage(p);
        const string actor="civ.federation",fleet="fleet.federation-scout";
        foreach(var target in new[]{"nav.bajor","nav.bajoran-wormhole","nav.gamma-exit","nav.bajoran-wormhole","nav.bajor","nav.badlands"})
        {
            var position=app.GetOwnStrategicFleets(actor).Single(f=>f.FleetId.Value==fleet).LocationNodeId.Value;
            var route=app.FindKnownRoute(actor,position,target);
            if(!route.Success)throw new InvalidOperationException("Frontier target is not yet lawfully charted: "+target);
            var stamp=app.CaptureStatus();
            var command=new SubmitCampaignTravelOrderDto {ActorId=actor,CommandId="command.frontier.walk."+stamp.Turn,
                ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Order=new CampaignTravelOrderDto {
                    FleetId=fleet,Intent=CampaignTravelIntent.Leave,RouteNodeIds=route.Steps.ToList()}};
            if(!app.SubmitTravelOrder(command).Accepted)throw new InvalidOperationException("Frontier route rejected.");
            do
            {
                stamp=app.CaptureStatus();
                if(!app.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted)throw new InvalidOperationException("Frontier advance rejected.");
                if(app.CaptureStatus().Phase!=SessionPhase.Planning)throw new InvalidOperationException("Non-hostile frontier fixture unexpectedly stopped for combat.");
                if(reload)app=CampaignFrontierApplication.LoadSave(p,JsonSerializer.Deserialize<CampaignFrontierSaveDto>(JsonSerializer.Serialize(app.ExportFrontierSave()))!);
            }while(app.GetOwnTravelOrders(actor).Single(o=>o.FleetId==fleet).RouteNodeIds.Count>0);
        }
        return app.ExportFrontierSave();
    }
}
