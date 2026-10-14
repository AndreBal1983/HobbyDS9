using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class IntegratedPlaytestWalkthrough
{
    private const string A="civ.federation",B="civ.cardassian",FA="fleet.federation-scout",FB="fleet.cardassian-scout";
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    private static void Advance(ICampaignIntegratedPlaytestSession app)
    {var s=app.CaptureStatus();Require(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted,"Integrated advance failed.");}
    private static CommandEnvelope Command(ICampaignIntegratedPlaytestSession app,CommandKind kind)
    {var s=app.CaptureStatus();return new CommandEnvelope {CommandId="command.walkthrough."+s.Turn+"."+s.CampaignRevision,
        ActorCivilizationId=A,ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,Kind=kind};}
    private static void Order(ICampaignIntegratedPlaytestSession app,string actor,string fleet,CampaignTravelIntent intent,IEnumerable<string> route)
    {
        var s=app.CaptureStatus();Require(app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto {ActorId=actor,
            CommandId="command.walkthrough.travel."+s.CampaignRevision,ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
            Order=new CampaignTravelOrderDto {FleetId=fleet,Intent=intent,RouteNodeIds=route.ToList()}}).Accepted,"Travel rejected.");
    }
    private static ICampaignIntegratedPlaytestSession Reload(CampaignIntegratedPlaytestPackageDto p,ICampaignIntegratedPlaytestSession app)
        =>CampaignIntegratedPlaytestApplication.LoadSave(p,JsonSerializer.Deserialize<CampaignIntegratedPlaytestSaveDto>(Json(app.ExportIntegratedSave()))!);
    private static string Execute(CampaignIntegratedPlaytestPackageDto p,int mode)
    {
        var app=CampaignIntegratedPlaytestApplication.StartPackage(p);
        var science=Command(app,CommandKind.SetScienceAllocation);science.SetScienceAllocation=new SetScienceAllocationPayload {
            Allocations=Enum.GetValues<ScienceDiscipline>().Select((d,i)=>new ScienceAllocationDto {Discipline=d,Weight=i==0?150:i==1?50:100}).ToList()};
        Require(app.SubmitStrategicCommand(science).Accepted,"Science allocation rejected.");
        var split=Command(app,CommandKind.SetResearchAllocation);split.SetResearchAllocation=new SetResearchAllocationPayload {ScienceAllocationUnits=600};
        Require(app.SubmitStrategicCommand(split).Accepted,"R&D split rejected.");
        var enqueue=Command(app,CommandKind.EnqueueProduction);enqueue.EnqueueProduction=new EnqueueProductionPayload {ColonyId="colony.sol",DefinitionId="ship-design.source-cruiser"};
        Require(app.SubmitStrategicCommand(enqueue).Accepted,"Paid construction rejected.");
        Require(app.CaptureStrategy(A).Civilization.Credits==50,"Construction not charged exactly200.");
        int limit=0;while(app.ExportIntegratedSave().BuiltShips!.Count==0&&limit++<100){Advance(app);if(mode==2)app=Reload(p,app);}
        Require(app.ExportIntegratedSave().BuiltShips!.Count==1,"No actual completed ship.");
        var built=app.ExportIntegratedSave().BuiltShips!.Single();var bornPool=app.ExportIntegratedSave().Physical!.Core.Pools.Single(s=>s.ShipId==built.ShipId);
        Require(bornPool.Ammunition.Single().Stock==7,"Construction stocks wrong.");
        Console.WriteLine("mode="+mode+" built-turn="+built.TurnBuilt+" ship="+built.ShipId+" fleet="+built.FleetIdAtConstruction+" source="+built.ShipDesignDefinitionId+" torpedoes=7");
        Order(app,A,FA,CampaignTravelIntent.Leave,app.FindKnownRoute(A,"nav.sol","nav.bajor").Steps);
        Order(app,B,FB,CampaignTravelIntent.Leave,app.FindKnownRoute(B,"nav.cardassia","nav.bajor").Steps);
        limit=0;while(app.CaptureStatus().Phase==SessionPhase.Planning&&app.GetOwnStrategicFleets(B).Single().LocationNodeId.Value!="nav.bajor"&&limit++<10)Advance(app);
        if(app.CaptureStatus().Phase==SessionPhase.Planning){Order(app,A,FA,CampaignTravelIntent.StayEngage,Array.Empty<string>());Advance(app);}
        Require(app.CaptureStatus().Phase==SessionPhase.AwaitingCombat,"No real admitted contact.");
        foreach(var id in app.GetEncounters(A))while(!app.Observe(id,A).Complete)
        {
            var commands=new[]{A,B}.Select(actor=>{var v=app.Observe(id,actor);return new CommitCampaignTacticalRoundDto {
                ActorId=actor,EncounterId=id,CommandId="command.walkthrough.round."+actor+"."+id+"."+v.Round,
                ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new List<CampaignTacticalFleetOrderDto>()};}).ToArray();
            if(mode==1)Array.Reverse(commands);
            foreach(var command in commands){Require(app.CommitRound(command).Accepted,"Joint round failed.");if(mode==2)app=Reload(p,app);}
        }
        var stamp=app.CaptureStatus();Require(app.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted,"Aftermath failed.");
        Require(!app.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted,"Double finalization accepted.");
        app=Reload(p,app);var pool=app.ExportIntegratedSave().Physical!.Core.Pools.Single(s=>s.ShipId==built.ShipId);
        Require(pool.Ammunition.Single().Stock<7,"Produced ship did not spend actual ordnance.");
        Console.WriteLine("mode="+mode+" retained-built-hull="+app.CaptureStrategy(A).Ships.Single(s=>s.ShipId.Value==built.ShipId).CurrentHull+" torpedoes="+pool.Ammunition.Single().Stock+" turn="+app.CaptureStatus().Turn);
        limit=0;while(!app.CaptureStrategy(A).Research.Engineering.Single().CanTarget&&limit++<300){Advance(app);if(mode==2)app=Reload(p,app);}
        var project=app.CaptureStrategy(A).Research.Engineering.Single();Require(project.CanTarget,"Science prerequisites not earned.");
        var target=Command(app,CommandKind.SetEngineeringTarget);target.SetEngineeringTarget=new SetEngineeringTargetPayload {TargetDefinitionId=project.Definition.DefinitionId.Value};
        Require(app.SubmitStrategicCommand(target).Accepted,"Engineering target rejected.");
        split=Command(app,CommandKind.SetResearchAllocation);split.SetResearchAllocation=new SetResearchAllocationPayload {ScienceAllocationUnits=0};
        Require(app.SubmitStrategicCommand(split).Accepted,"Engineering split rejected.");
        limit=0;while(app.CaptureStrategy(A).Research.Engineering.Single().Band!=EngineeringMaturityBand.Mature&&limit++<100){Advance(app);if(mode==2)app=Reload(p,app);}
        Require(app.CaptureStrategy(A).Research.Engineering.Single().Band==EngineeringMaturityBand.Mature,"Project failed to progress.");
        Console.WriteLine("mode="+mode+" earned-engineering="+project.Definition.DefinitionId+" complete-turn="+app.CaptureStatus().Turn);
        Advance(app);Require(app.GetEncounters(A).Count==0&&app.ExportIntegratedSave().BuiltShips!.Count==1,"Automatic rematch or duplicate completion.");
        return Json(app.ExportIntegratedSave());
    }
    internal static void Run()
    {
        var folder=Path.Combine(AppContext.BaseDirectory,"scenarios","integrated-playtest-v1");string? canonical=null;
        for(int mode=0;mode<4;mode++)
        {
            var p=IntegratedPlaytestPackageReader.Read(folder);
            if(mode==3){p.Physical!.CorePackage!.Campaign!.InitialState.Colonies!.Reverse();p.Physical.InitialOrders!.Reverse();p.Physical.InitialContacts!.Reverse();}
            var result=Execute(p,mode);if(canonical==null)canonical=result;else Require(result==canonical,"Integrated determinism mismatch.");
        }
        Console.WriteLine("integrated-package-sha256="+IntegratedPlaytestPackageReader.Read(folder).PackageHash);
        Console.WriteLine("integrated-save-sha256="+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical!))).ToLowerInvariant());
        Console.WriteLine("authoritative integrated research/paid production/produced-ship combat/reload/reverse/reordered checks passed; native UI/audio NOT executed.");
    }
}
