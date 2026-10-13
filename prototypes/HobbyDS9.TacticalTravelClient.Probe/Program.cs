using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.TacticalReviewStorage;

// Scripted controlled human acceptance host, never opponent AI or hidden-route inference.
try
{
    if(args.Length != 9 || args[0] != "inspect" && args[0] != "finish-two-battles")
    { Console.WriteLine("Physical probe arguments invalid."); return 2; }
    var mode=args[0]; var directory=args[1]; var slot=args[2]; var actor=args[3]; var otherActor=args[4];
    var codec=new JsonPhysicalReviewCodec();
    var package=PhysicalReviewPackageFiles.Load(args[5],args[6],args[7]);
    var store=new DiskPhysicalReviewCheckpointStore(directory,codec);
    var opened=PhysicalReviewHost.OpenFromSlot(package,codec,store,slot,actor);
    if(!opened.Succeeded) { Console.WriteLine("Checkpoint result: "+opened.Error); return 3; }
    var host=opened.Host!; var a=new ReviewController(host,actor);
    if(a.Frame.Encounters.Count > 0 && !a.SelectEncounter(a.Frame.Encounters.Single())) throw new InvalidOperationException();
    if(mode == "finish-two-battles")
    {
        var b=new ReviewController(host,otherActor);
        for(int battle=1;battle<=2;battle++)
        {
            if(a.Frame.Battle == null || !b.SelectEncounter(a.Frame.Battle.EncounterId)) throw new InvalidOperationException();
            while(!a.Frame.Battle.Complete)
            {
                foreach(var c in new[]{a,b})
                {
                    if(c.Frame.Battle!.OwnCommitmentAccepted) continue;
                    var round=c.Frame.Battle.Round;
                    foreach(var o in c.Frame.Battle.StandingOrders.Where(o=>o.CanOrder))
                        if(!c.EditFleet(o.FleetId,round == 2 ? CampaignFleetManeuver.Flank : round == 3 ? CampaignFleetManeuver.Screen : CampaignFleetManeuver.Hold,o.FocusFleetId))
                            throw new InvalidOperationException();
                    if(!c.ReviewRound()) throw new InvalidOperationException();
                    var work=c.ConfirmRound("command.physical.round."+c.Frame.Battle.EncounterId+"."+c.Frame.Actor+"."+round)!;
                    var receipt=host.Execute(work);
                    if(!receipt.Result.Accepted || !c.Receive(receipt)) throw new InvalidOperationException();
                }
                if(!a.Refresh() || !b.Refresh()) throw new InvalidOperationException();
            }
            Console.Write(ReviewTextRenderer.RenderReport(a));
            if(!a.ReviewCompletion()) throw new InvalidOperationException();
            var end=host.Execute(a.ConfirmCompletion()!);
            if(!end.Result.Accepted || !a.Receive(end) || !b.Refresh()) throw new InvalidOperationException();
            if(battle == 1)
            {
                foreach(var c in new[]{a,b})
                {
                    if(!c.Refresh()) throw new InvalidOperationException();
                    var fleet=c.Frame.OwnStrategicFleets.Single().FleetId.Value;
                    if(!c.EditTravel(fleet,CampaignTravelIntent.StayEngage) || !c.ReviewTravel()) throw new InvalidOperationException();
                    var receipt=host.Execute(c.ConfirmTravel("command.physical."+c.Frame.Actor+"."+c.Frame.Status.Turn)!);
                    if(!receipt.Result.Accepted || !c.Receive(receipt)) throw new InvalidOperationException();
                }
                if(!a.Refresh() || !host.AdvancePlanning(a.Frame).Accepted || !a.Refresh() || !b.Refresh()
                    || !a.SelectEncounter(a.Frame.Encounters.Single())) throw new InvalidOperationException();
            }
        }
        var result=a.SaveCommittedToSlot(args[8]);
        if(!result.Succeeded) { Console.WriteLine("Checkpoint result: "+result.Error); return 3; }
    }
    Console.Write(ReviewTextRenderer.Render(a));
    if(mode == "inspect") Console.Write(ReviewTextRenderer.RenderReport(a));
    return 0;
}
catch(Exception)
{
    Console.WriteLine("Trusted physical review probe failed.");
    return 3;
}
