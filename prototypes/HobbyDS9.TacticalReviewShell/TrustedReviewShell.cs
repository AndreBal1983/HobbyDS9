using System.Globalization;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.TacticalReviewStorage;

namespace HobbyDS9.TacticalReviewShell;

public sealed class ShellReply
{
    public bool Succeeded { get; }
    public bool ExitRequested { get; }
    public string Text { get; }
    internal ShellReply(bool success,string text,bool exit=false)
    { Succeeded=success; Text=text; ExitRequested=exit; }
}
// Local trusted human review host. Actor switching is not multiplayer authorization.
public sealed class TrustedReviewShell
{
    private readonly CampaignTacticalTravelPackageDto package;
    private readonly IPhysicalReviewHostCodec codec;
    private readonly IPhysicalReviewCheckpointStore store;
    private PhysicalReviewHost host;
    private ReviewController current;
    public ReviewController Current => current;
    public TrustedReviewShell(CampaignTacticalTravelPackageDto package,IPhysicalReviewHostCodec codec,IPhysicalReviewCheckpointStore store,string actor)
    {
        this.package=codec.CopyPackage(package); this.codec=codec; this.store=store;
        host=PhysicalReviewHost.Start(this.package,codec,store);
        current=Controller(host,actor);
    }
    private static ReviewController Controller(PhysicalReviewHost host,string actor)
    {
        var c=new ReviewController(host,actor);
        if(c.Frame.Encounters.Count == 1) c.SelectEncounter(c.Frame.Encounters[0]);
        return c;
    }
    private string Label => "ACTOR VIEW: "+current.Frame.Actor+" | LOCAL TRUSTED HOTSEAT REVIEW | human choices are not secret to this host\n";
    private ShellReply Reply(bool success,string text,bool exit=false) => new ShellReply(success,Label+text+Environment.NewLine,exit);
    private ShellReply Guard(string reason="Command unavailable in the current observation.") => Reply(false,reason);
    private bool MayReplace(string[] words) => !current.HasPendingWork && (!current.Dirty || words.LastOrDefault() == "discard");
    public ShellReply Execute(string line)
    {
        var words=line.Split(' ',StringSplitOptions.RemoveEmptyEntries);
        if(words.Length == 0 || words[0].StartsWith("#",StringComparison.Ordinal)) return Reply(true,"");
        try
        {
            switch(words[0])
            {
                case "help" when words.Length == 1:
                    return Reply(true,"show | refresh | inspect <ship> | report [round] | actor <id> [discard] | encounter <id> [discard]\n"
                        +"route <own-fleet> <known-destination> | travel <own-fleet> <Stay|StayEngage|Leave|Follow> [destination|-] [known-contact|-]\n"
                        +"send-travel <command-id> | fleet <own-fleet> <maneuver> <visible-focus|-> | round <command-id> | advance | finalize\n"
                        +"cloak <own-fleet> <true|false> <command-id> | save <slot> [overwrite] | load <slot> [discard] | open <slot> [discard] | reset | exit\n"
                        +"Commands use real observations/validation. Policies proposed; production activation disabled.");
                case "show" when words.Length == 1: return Reply(true,ReviewTextRenderer.Render(current));
                case "refresh" when words.Length == 1:
                    return current.Refresh() ? Reply(true,ReviewTextRenderer.Render(current)) : Guard("Refresh unavailable; host generation or pending work changed.");
                case "inspect" when words.Length == 2:
                    return current.InspectShip(words[1]) ? Reply(true,ReviewTextRenderer.RenderInspection(current,words[1])) : Guard("No lawful ship observation available.");
                case "report" when words.Length <= 2:
                    if(words.Length == 2 && (!int.TryParse(words[1],NumberStyles.None,CultureInfo.InvariantCulture,out var round) || round<1 || round>3)) return Guard("Report round must be 1, 2 or 3.");
                    return Reply(true,ReviewTextRenderer.RenderReport(current,words.Length == 2 ? int.Parse(words[1],CultureInfo.InvariantCulture) : null));
                case "actor" when words.Length == 2 || words.Length == 3 && words[2] == "discard":
                    if(!MayReplace(words)) return Guard("Local drafts or pending work remain. Explicit discard is required to switch actor.");
                    var candidate=Controller(host,words[1]); // Validate lawful actor first; failure keeps old controller/draft.
                    current=candidate; return Reply(true,"Actor switched; prior inspection and local drafts cleared.\n"+ReviewTextRenderer.Render(current));
                case "encounter" when words.Length == 2 || words.Length == 3 && words[2] == "discard":
                    return current.SelectEncounter(words[1],words.Last() == "discard") ? Reply(true,ReviewTextRenderer.Render(current)) : Guard("Encounter selection unavailable; local drafts require explicit discard.");
                case "route" when words.Length == 3:
                    var offer=current.FindKnownRoute(words[1],words[2]);
                    if(offer == null) return Guard();
                    return Reply(offer.Route.Success,FormattableString.Invariant($"Known-route result {offer.Route.ErrorCode}; turn {offer.Turn}; revision {offer.CampaignRevision}; steps {string.Join(", ",offer.Route.Steps)}; supplied cost {offer.Route.TotalCost}. No ETA/pursuit forecast."));
                case "travel" when words.Length >= 3 && words.Length <= 5:
                    if(!Enum.TryParse<CampaignTravelIntent>(words[2],out var intent) || !Enum.IsDefined(intent)) return Guard("Unknown travel intent.");
                    var destination=words.Length >= 4 && words[3] != "-" ? words[3] : null;
                    var contact=words.Length == 5 && words[4] != "-" ? words[4] : null;
                    var route=destination == null ? null : current.FindKnownRoute(words[1],destination);
                    if(destination != null && route == null) return Guard();
                    return current.EditTravel(words[1],intent,route,contact) ? Reply(true,"Local travel draft recorded; send-travel requires an explicit command ID.") : Guard();
                case "send-travel" when words.Length == 2:
                    if(!current.ReviewTravel()) return Guard();
                    var travel=current.ConfirmTravel(words[1]);
                    return travel == null ? Guard() : Apply(travel);
                case "fleet" when words.Length == 4:
                    if(!Enum.TryParse<CampaignFleetManeuver>(words[2],out var maneuver) || !Enum.IsDefined(maneuver)) return Guard("Unknown maneuver.");
                    return current.EditFleet(words[1],maneuver,words[3] == "-" ? null : words[3]) ? Reply(true,"Local fleet draft recorded; round requires an explicit command ID.") : Guard();
                case "round" when words.Length == 2:
                    if(!current.ReviewRound()) return Guard();
                    var work=current.ConfirmRound(words[1]); return work == null ? Guard() : Apply(work);
                case "advance" when words.Length == 1:
                    if(current.Dirty || current.HasPendingWork) return Guard("Submit or explicitly reset local drafts before advancing.");
                    var advanced=host.AdvancePlanning(current.Frame);
                    if(!advanced.Accepted) return Reply(false,"Authoritative error: "+advanced.Error);
                    current.Refresh();
                    if(current.Frame.Encounters.Count == 1) current.SelectEncounter(current.Frame.Encounters[0]);
                    return Reply(true,ReviewTextRenderer.Render(current));
                case "finalize" when words.Length == 1:
                    if(!current.ReviewCompletion()) return Guard("Authoritative batch is not ready for finalization.");
                    var end=current.ConfirmCompletion(); return end == null ? Guard() : Apply(end);
                case "cloak" when words.Length == 4:
                    if(current.Dirty || current.HasPendingWork || !bool.TryParse(words[2],out var enabled)) return Guard();
                    var cloak=host.SetApproachCloak(current.Frame,words[3],words[1],enabled);
                    if(cloak.Accepted) current.Refresh();
                    return Reply(cloak.Accepted,"Authoritative result: "+cloak.Error);
                case "save" when words.Length == 2 || words.Length == 3 && words[2] == "overwrite":
                    var saved=current.SaveCommittedToSlot(words[1],words.Last() == "overwrite");
                    return Reply(saved.Succeeded,saved.Succeeded ? "Committed slot saved; unsent edits remain local." : "Checkpoint result: "+saved.Error);
                case "load" when words.Length == 2 || words.Length == 3 && words[2] == "discard":
                    var loaded=current.LoadCommittedFromSlot(words[1],words.Last() == "discard");
                    return Reply(loaded.Succeeded,loaded.Succeeded ? ReviewTextRenderer.Render(current) : "Checkpoint result: "+loaded.Error);
                case "open" when words.Length == 2 || words.Length == 3 && words[2] == "discard":
                    if(!MayReplace(words)) return Guard("Local drafts or pending work remain. Explicit discard is required to open a slot.");
                    var opened=PhysicalReviewHost.OpenFromSlot(package,codec,store,words[1],current.Frame.Actor,current.Frame.Battle?.EncounterId);
                    if(!opened.Succeeded) return Reply(false,"Checkpoint result: "+opened.Error);
                    var replacement=Controller(opened.Host!,current.Frame.Actor);
                    host=opened.Host!; current=replacement; return Reply(true,ReviewTextRenderer.Render(current));
                case "reset" when words.Length == 1:
                    if(!current.Dirty) return Reply(true,"No local draft.");
                    return (current.TravelDraft != null ? current.ResetTravelDraft() : current.ResetDraft()) ? Reply(true,"Local draft cleared; no command submitted.") : Guard();
                case "exit" when words.Length == 1: return Reply(true,"Review shell closed.",true);
                default: return Guard("Unknown command or argument count. Use help.");
            }
        }
        catch(Exception)
        {
            // No private parse/validation/IO/actor diagnostics enter presentation.
            return Guard("Review operation unavailable; current view retained.");
        }
    }
    private ShellReply Apply(ReviewWork work)
    {
        var receipt=host.Execute(work); var received=current.Receive(receipt);
        return Reply(received && receipt.Result.Accepted,"Authoritative result: "+receipt.Result.Error+"\n"+ReviewTextRenderer.Render(current));
    }
    public void Run(TextReader input,TextWriter output)
    {
        output.Write(Execute("help").Text);
        string? line;
        while((line=input.ReadLine()) != null)
        { var reply=Execute(line); output.Write(reply.Text); if(reply.ExitRequested) break; }
    }
}
