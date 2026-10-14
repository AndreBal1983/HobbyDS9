using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewShell;

// Explicit authored actor/routes and human choices. No automatic gameplay/opponent policy.
public static class ScriptedReviewDemo
{
    public static void Run(TrustedReviewShell shell,TextWriter output)
    {
        const string a="civ.federation",b="civ.cardassian",fa="fleet.federation-scout",fb="fleet.cardassian-scout";
        void Command(string line)
        {
            output.WriteLine("SCRIPTED HUMAN CHOICE: "+line);
            var reply=shell.Execute(line); output.Write(reply.Text);
            if(!reply.Succeeded) throw new InvalidOperationException("Scripted review command rejected.");
        }
        output.WriteLine("SCRIPTED HOTSEAT DEMO / PROPOSED POLICIES / NO HOST SECRECY OR SHOT FORECAST");
        Command("actor "+a); Command("show");
        Command("cloak "+fa+" true command.physical.cloak");
        Command("route "+fa+" nav.bajor");
        Command("travel "+fa+" Follow nav.bajor "+fb); Command("send-travel command.physical."+a+".1");
        Command("actor "+b); Command("travel "+fb+" Leave nav.bajoran-wormhole");
        Command("send-travel command.physical."+b+".1"); Command("actor "+a); Command("advance");
        if(shell.Current.Frame.Battle != null)
        {
            for(int battle=1;battle<=2;battle++)
            {
                while(!shell.Current.Frame.Battle!.Complete)
                {
                    foreach(var actor in new[]{a,b})
                    {
                        Command("actor "+actor);
                        var view=shell.Current.Frame.Battle!;
                        if(view.OwnCommitmentAccepted) continue;
                        foreach(var order in view.StandingOrders.Where(o=>o.CanOrder))
                            Command("fleet "+order.FleetId+" "+(view.Round == 2 ? "Flank" : view.Round == 3 ? "Screen" : "Hold")+" "+(order.FocusFleetId ?? "-"));
                        Command("round command.physical.round."+view.EncounterId+"."+actor+"."+view.Round);
                        if(battle == 1 && actor == a && view.Round == 1)
                        { Command("save demo-waiting"); Command("open demo-waiting"); }
                    }
                    Command("actor "+a); Command("refresh");
                }
                Command("report"); Command("finalize");
                if(battle == 1)
                {
                    foreach(var actor in new[]{a,b})
                    {
                        Command("actor "+actor);
                        var fleet=shell.Current.Frame.OwnStrategicFleets.Single().FleetId.Value;
                        Command("travel "+fleet+" StayEngage"); Command("send-travel command.physical."+actor+".2");
                    }
                    Command("actor "+a); Command("advance");
                }
            }
        }
        Command("save demo-final"); Command("show"); Command("exit");
    }
}
