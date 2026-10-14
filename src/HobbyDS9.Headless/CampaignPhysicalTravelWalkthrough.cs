using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class CampaignPhysicalTravelWalkthrough
{
    private const string A="civ.federation",B="civ.cardassian",FA="fleet.federation-scout",FB="fleet.cardassian-scout";
    private static JsonSerializerOptions Options=>CampaignTacticalReviewWalkthrough.Options;
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static CampaignTacticalTravelPackageDto Read(string folder)
    {
        var path=Path.Combine(AppContext.BaseDirectory,"scenarios","tactical-v2-physical-review",folder);
        var profile=File.ReadAllBytes(Path.Combine(path,"package.json"));
        var core=File.ReadAllBytes(Path.Combine(path,"core.json"));
        var source=File.ReadAllBytes(Path.Combine(path,"scenario.json"));
        var p=JsonSerializer.Deserialize<CampaignTacticalTravelPackageDto>(profile,Options)!;
        p.CorePackage=JsonSerializer.Deserialize<CampaignTacticalReviewPackageDto>(core,Options)!;
        p.CorePackage.Campaign=JsonSerializer.Deserialize<ScenarioDto>(source,Options)!;
        p.CorePackage.Campaign.ContentHash=Hash(source);
        p.CorePackage.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-tactical-review-package-v1\n"+Hash(core)+"\n"+Hash(source)+"\n"));
        p.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-physical-review-package-v2\n"+Hash(profile)+"\n"+p.CorePackage.PackageHash+"\n"));
        return p;
    }
    private static ICampaignTacticalTravelSession Reload(CampaignTacticalTravelPackageDto p,ICampaignTacticalTravelSession app)=>
        CampaignTacticalTravelApplication.LoadReviewSave(p,JsonSerializer.Deserialize<CampaignTacticalTravelSaveDto>(JsonSerializer.Serialize(app.ExportSave()),Options)!);
    private static void Order(ICampaignTacticalTravelSession app,string actor,CampaignTravelIntent intent,params string[] route)
    {
        var status=app.CaptureStatus();var follow=intent==CampaignTravelIntent.Follow;
        var target=actor==A?FB:FA;
        Require(app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto { ActorId=actor,CommandId="command.physical."+actor+"."+status.Turn,
            ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
            Order=new CampaignTravelOrderDto { FleetId=actor==A?FA:FB,Intent=intent,RouteNodeIds=route.ToList(),
                ContactFleetId=follow?target:null,ExpectedContactSeenTurn=follow?app.GetKnownContacts(actor).Single(k=>k.FleetId==target).LastSeenTurn:null } }).Accepted,"travel order");
    }
    private static void Advance(ICampaignTacticalTravelSession app)
    {var s=app.CaptureStatus();Require(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted,"physical advance");}
    private static CommitCampaignTacticalRoundDto Command(ICampaignTacticalTravelSession app,string id,string actor)
    {
        var v=app.Observe(id,actor);
        return new CommitCampaignTacticalRoundDto { ActorId=actor,CommandId="command.physical.round."+id+"."+actor+"."+v.Round,EncounterId=id,
            ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,
            Replacements=v.StandingOrders.Where(o=>v.OwnShips.Any(s=>s.Condition.FleetId==o.FleetId&&s.Condition.Hull>0)).Select(o=>
                new CampaignTacticalFleetOrderDto {FleetId=o.FleetId,Maneuver=v.Round==2?CampaignFleetManeuver.Flank:v.Round==3?CampaignFleetManeuver.Screen:CampaignFleetManeuver.Hold,
                    FocusFleetId=o.FocusFleetId}).ToList() };
    }
    internal static void Run()
    {
        Console.WriteLine("PHYSICAL AUTHORITATIVE REVIEW: actual weighted graph/compiled movement/Scenario/Application; production activation=False; policies proposed.");
        foreach(var example in new[]{"faster","slower"})
        {
            var p=Read(example);Console.WriteLine("physical-profile="+example+" package-sha256="+p.PackageHash);
            var vectors=new List<string>();
            foreach(var variant in new[]{"canonical","reverse","repeated","reload","reordered"})
            {
                // Reorder serialized authored lists; do not change any content contract or itinerary.
                var owned=JsonSerializer.Deserialize<CampaignTacticalTravelPackageDto>(JsonSerializer.Serialize(p),Options)!;
                if(variant=="reordered")
                {owned.InitialOrders!.Reverse();owned.InitialContacts!.Reverse();owned.Hostilities!.Reverse();owned.CorePackage!.Campaign!.InitialState.Fleets!.Reverse();}
                ICampaignTacticalTravelSession app=CampaignTacticalTravelApplication.StartReviewPackage(owned);
                var status=app.CaptureStatus();Require(app.SetApproachCloak(A,"command.physical.cloak",FA,true,status.Turn,status.CampaignRevision).Accepted,"cloak");
                Order(app,A,CampaignTravelIntent.Follow,"nav.bajor");Order(app,B,CampaignTravelIntent.Leave,"nav.bajor","nav.bajoran-wormhole");
                Advance(app);app=variant=="reload"?Reload(owned,app):app;
                var r=app.GetOwnTravelReceipts(A).Single();
                if(example=="slower")
                {
                    Require(app.GetEncounters(A).Count==0&&r.TransitTotalCost==3&&r.TransitProgress==2,"slower miss with weighted partial progress");
                    Require(app.GetOwnTravelReceipts(B).Single().TransitProgress==1,"target continues its actual weighted edge");
                    if(variant=="canonical")Console.WriteLine("physical slower missed: own-progress=2/3; target-progress=1/2; no final-position shortcut.");
                }
                else
                {
                    Require(r.EndNodeId=="nav.bajor"&&r.StoppedByEncounter&&r.StopTimeNumerator=="3"&&r.StopTimeDenominator=="4","intermediate physical stop");
                    Require(app.GetOwnStrategicFleets(A).Concat(app.GetOwnStrategicFleets(B)).All(f=>f.RouteNodeIds.Count==0),"stop clears executable routes");
                    if(variant=="canonical")Console.WriteLine("physical faster met at nav.bajor time=3/4; both participants stopped.");
                    long priorAmmo=7;
                    for(var battle=1;battle<=2;battle++)
                    {
                        var id=app.GetEncounters(A).Single();
                        if(battle==2)Require(app.GetOwnTravelReceipts(A).Single().StopTimeNumerator=="0","stationary reengagement");
                        while(!app.Observe(id,A).Complete)
                        {
                            var a=Command(app,id,A);var b=Command(app,id,B);var reverse=variant=="reverse";
                            Require(app.CommitRound(reverse?b:a).Accepted,"first simultaneous commitment");
                            if(variant=="reload")app=Reload(owned,app);
                            Require(app.CommitRound(reverse?a:b).Accepted,"joint round");
                            if(variant=="reload")app=Reload(owned,app);
                        }
                        status=app.CaptureStatus();Require(status.CanFinalizeBatch&&app.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted,"atomic aftermath");
                        app=Reload(owned,app);
                        var currentAmmo=app.InspectOwnShips(A).SelectMany(s=>s.Weapons).Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition;
                        Require(currentAmmo<=priorAmmo&&currentAmmo<7,"retained depleted stock");priorAmmo=currentAmmo;
                        if(variant=="canonical")Console.WriteLine("physical finalized battle="+battle+" turn="+app.CaptureStatus().Turn+" own-ammo="+currentAmmo);
                        if(battle==1){Order(app,A,CampaignTravelIntent.StayEngage);Order(app,B,CampaignTravelIntent.StayEngage);Advance(app);}
                    }
                }
                app=Reload(owned,app);
                var vector=Hash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(app.ExportSave())));vectors.Add(vector);
                Console.WriteLine("physical "+example+" "+variant+" save-sha256="+vector);
            }
            Require(vectors.Distinct(StringComparer.Ordinal).Count()==1,"canonical/reverse/repeated/reload/reordered equality");
        }
        Console.WriteLine("Physical travel/current pools/round commitments/two battles/save-reload equality passed; no prototype itinerary imported.");
        Console.WriteLine("Gated: owner policy acceptance, neutral connected owner/side contract, dynamic sensors/fog-of-war and production UI activation.");
    }
    private static void Require(bool value,string check){if(!value)throw new InvalidOperationException("Physical review failed: "+check);}
}
