using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.TacticalReviewStorage;

// Isolated scripted human host for cross-process acceptance, not AI or a player UI.
// Exactly supplied package files/directory/slots; no fallback, lookup or user-save discovery.
try
{
    if(args.Length != 8 || args[0] != "inspect" && args[0] != "finish")
    { Console.WriteLine("Review probe arguments invalid."); return 2; }
    var mode=args[0]; var directory=args[1]; var slot=args[2]; var actor=args[3];
    var otherActor=args[4]; var profile=args[5]; var scenario=args[6]; var output=args[7];
    var package=ReviewPackageFiles.Load(profile,scenario);
    var codec=new JsonReviewHostCodec(); var store=new DiskReviewCheckpointStore(directory,codec);
    var opened=ReviewHost.OpenFromSlot(package,codec,store,slot,actor);
    if(!opened.Succeeded) { Console.WriteLine("Checkpoint result: "+opened.Error); return 3; }
    var host=opened.Host!; var own=new ReviewController(host,actor);
    if(own.Frame.Encounters.Count > 0 && !own.SelectEncounter(own.Frame.Encounters.Single()))
    { Console.WriteLine("Review probe encounter unavailable."); return 3; }
    if(mode == "finish")
    {
        var other=new ReviewController(host,otherActor);
        if(own.Frame.Battle == null || !other.SelectEncounter(own.Frame.Battle.EncounterId))
        { Console.WriteLine("Review probe scripted actors unavailable."); return 3; }
        while(!own.Frame.Battle!.Complete)
        {
            foreach(var c in new[]{own,other})
            {
                if(c.Frame.Battle!.OwnCommitmentAccepted) continue;
                if(c.Frame.Battle.Round == 2)
                foreach(var o in c.Frame.Battle.StandingOrders.Where(o=>o.CanOrder))
                    if(!c.EditFleet(o.FleetId,CampaignFleetManeuver.Flank,o.FocusFleetId))
                        throw new InvalidOperationException();
                if(!c.ReviewRound()) throw new InvalidOperationException();
                var b=c.Frame.Battle;
                var work=c.ConfirmRound("command.client."+c.Frame.Actor+"."+b.EncounterId+"."+b.Round)!;
                var receipt=host.Execute(work);
                if(!receipt.Result.Accepted || !c.Receive(receipt)) throw new InvalidOperationException();
            }
            if(!own.Refresh() || !other.Refresh()) throw new InvalidOperationException();
        }
        Console.Write(ReviewTextRenderer.RenderReport(own));
        if(!own.ReviewCompletion()) throw new InvalidOperationException();
        var completion=host.Execute(own.ConfirmCompletion()!);
        if(!completion.Result.Accepted || !own.Receive(completion)) throw new InvalidOperationException();
        var result=own.SaveCommittedToSlot(output);
        if(!result.Succeeded) { Console.WriteLine("Checkpoint result: "+result.Error); return 3; }
    }
    Console.Write(ReviewTextRenderer.Render(own));
    if(mode == "inspect") Console.Write(ReviewTextRenderer.RenderReport(own));
    return 0;
}
catch(Exception)
{
    // File paths, malformed contents and exception details are not presentation.
    Console.WriteLine("Trusted review probe failed.");
    return 3;
}
