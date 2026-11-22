using System.Reflection;
using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class CampaignRepositoryCaptureTests
{
    const string Human="civ.federation";
    readonly ReviewNewtonsoftCodec codec=new();
    ICampaignRepositoryCodec WireCodec=>new DelegatedCampaignRepositoryCodec("review-newtonsoft-cycle-v1",codec.EncodeCycleRoot,codec.DecodeCycleRoot);
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    CampaignColonisationCyclePackageDto Package(int format=18,bool originalScenario=false)
    {
        var t=originalScenario?PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios","playable-fleet-v1")):ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),Read("sol-eight-planets-v1","scenario"),codec);
        var a=ActivitiesPlaytestOptions.CreatePackage(t,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var c=ColonisationPlaytestOptions.CreatePackage(a,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var r=ColonisationPlaytestOptions.CreateRosterPackage(c,Read("colonisation-roster-v2","roster"),codec);
        var g=ColonisationGrowthPlaytestOptions.CreatePackage(r,Read("colonisation-expansion-v1","planner"),codec);
        return format switch {18=>CampaignColonisationCycleApplication.CreateBeltBodiesPackage(g),17=>CampaignColonisationCycleApplication.CreateEngineeringRefinementPackage(g),
            _=>ColonisationCyclePlaytestOptions.CreatePackage(g,Read("colonisation-cycle-v1","cycle-policy"),codec)};
    }
    static NaturalGalaxySettingsDto Settings(int size=30)=>new(){Map=new(){Seed="42",SystemCount=size,EmptyRegionCount=size,Width=size==30?750000:1500000,Height=size==30?600000:1200000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    PhysicalReviewHost Host(int format=18,int size=30)=>PhysicalReviewHost.StartColonisationCycle(Package(format),Settings(size),codec);
    static ICampaignColonisationCycleSession Session(PhysicalReviewHost host)=>(ICampaignColonisationCycleSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(host)!;
    byte[] Bytes(PhysicalReviewHost h)=>codec.EncodeCycleRoot(Session(h).ExportCycleSave());
    CampaignRepositoryImage Image(PhysicalReviewHost h)
    {
        var p=h.PrepareRepositoryCheckpoint(h.Read(Human),WireCodec);Assert.Equal(CampaignRepositoryError.None,p.Error);
        var result=p.Work!.Execute(()=>false);Assert.True(result.Succeeded,result.Error.ToString());return result.Image!;
    }
    static void Advance(PhysicalReviewHost h)
    {var frame=h.Read(Human);Assert.True(h.AdvancePlanning(frame).Accepted);}
    static CommandEnvelope Allocation(ICampaignColonisationCycleSession s)
    {var f=s.CaptureStatus();return new(){ActorCivilizationId=Human,CommandId="command.repository.allocate."+f.Turn+"."+f.CampaignRevision,ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Kind=CommandKind.SetResearchAllocation,SetResearchAllocation=new(){ScienceAllocationUnits=0}};}

    [Theory][InlineData(14,30)][InlineData(17,30)][InlineData(18,30)][InlineData(18,100)]
    public void CompleteTypedCarrierAndDescriptorRoundTripWithoutConversion(int format,int size)
    {
        var host=Host(format,size);var before=Bytes(host);var image=Image(host);
        Assert.Equal(before,image.ReadPayload());Assert.Equal(format.ToString(),image.Edition.Fields.Single(p=>p.Key=="root-format").Value);
        Assert.Contains(image.Edition.Fields,p=>p.Key=="carrier"&&p.Value=="campaign-colonisation-cycle");
        Assert.Contains(image.Edition.Fields,p=>p.Key=="rng");Assert.Contains(image.Edition.Fields,p=>p.Key=="resolver");
        var adapter=new CycleCampaignRepositoryAdapter(Package(format),codec,WireCodec);
        var valid=adapter.Validate(image.ReadPayload(),image.Edition.Identity);Assert.True(valid.Succeeded,valid.Error.ToString());
        Assert.Equal(image.PayloadHash,valid.Image!.PayloadHash);Assert.Equal(before,Bytes(host));
        var callerCopy=image.ReadPayload();callerCopy[0]=0;Assert.Equal(before,image.ReadPayload());
    }

    [Fact] public void ValidatorPreservesExactInputWhitespaceAndRejectsMetadataCrossEditionAndTampering()
    {
        var host=Host();var image=Image(host);var adapter=new CycleCampaignRepositoryAdapter(Package(),codec,WireCodec);
        var exact=Encoding.UTF8.GetBytes(" \n"+Encoding.UTF8.GetString(image.ReadPayload())+"\n ");
        var valid=adapter.Validate(exact,image.Edition.Identity);Assert.True(valid.Succeeded);Assert.Equal(exact,valid.Image!.ReadPayload());
        exact[0]=0;Assert.NotEqual(exact,valid.Image.ReadPayload());
        Assert.Equal(CampaignRepositoryError.Incompatible,adapter.Validate(image.ReadPayload(),image.Edition.Identity+"x").Error);
        Assert.Equal(CampaignRepositoryError.Incompatible,new CycleCampaignRepositoryAdapter(Package(17),codec,WireCodec).Validate(image.ReadPayload()).Error);
        var changed=JObject.Parse(Encoding.UTF8.GetString(image.ReadPayload()));changed["BeltBodies"]!["Deposits"]![0]!["Remaining"]=0;
        Assert.Equal(CampaignRepositoryError.InvalidPayload,adapter.Validate(Encoding.UTF8.GetBytes(changed.ToString())).Error);
        changed=JObject.Parse(Encoding.UTF8.GetString(image.ReadPayload()));changed.Remove("Refinement");
        Assert.Equal(CampaignRepositoryError.InvalidPayload,adapter.Validate(Encoding.UTF8.GetBytes(changed.ToString())).Error);
        Assert.Equal(CampaignRepositoryError.InvalidPayload,adapter.Validate(Encoding.UTF8.GetBytes("{\"FormatVersion\":18,\"FormatVersion\":18}")).Error);
        Assert.Equal(CampaignRepositoryError.InvalidPayload,adapter.Validate(Array.Empty<byte>()).Error);
    }

    [Fact] public void CaptureOwnsOldCompleteRootWhileLiveSessionAdvancesAndWorkIsOneUse()
    {
        var host=Host();var old=Bytes(host);var preparation=host.PrepareRepositoryCheckpoint(host.Read(Human),WireCodec);var work=preparation.Work!;
        var command=Allocation(Session(host));Assert.True(Session(host).SubmitStrategicCommand(command).Accepted);
        Assert.True(host.IsRepositorySessionCurrent(work.Source));Assert.False(host.IsRepositoryStateCurrent(work.Source));
        var captured=work.Execute(()=>false);Assert.True(captured.Succeeded);Assert.Equal(old,captured.Image!.ReadPayload());
        Assert.NotEqual(old,Bytes(host));Assert.Throws<InvalidOperationException>(()=>work.Execute(()=>false));
    }

    [Fact] public void ValidatedRestorePublishesOnceChangesEpochAndPreservesNextActionsAndDedup()
    {
        var host=Host();var s=Session(host);var command=Allocation(s);Assert.True(s.SubmitStrategicCommand(command).Accepted);
        var image=Image(host);var expected=CampaignColonisationCycleApplication.LoadCycleSave(Package(),codec.DecodeCycleRoot(image.ReadPayload()));
        var oldWork=host.PrepareRepositoryCheckpoint(host.Read(Human),WireCodec).Work!;
        var restore=host.PrepareRepositoryRestore(host.Read(Human),image.ReadPayload(),image.Edition.Identity,WireCodec).Work!;
        var before=Bytes(host);var completion=restore.Execute(()=>false);Assert.Equal(CampaignRepositoryError.None,completion.Error);Assert.Equal(before,Bytes(host));
        Assert.Equal(CampaignRepositoryError.None,host.CompleteRepositoryRestore(restore,completion,out var frame));Assert.NotNull(frame);
        Assert.False(host.IsRepositorySessionCurrent(oldWork.Source));Assert.Equal(image.ReadPayload(),Bytes(host));
        Assert.False(Session(host).SubmitStrategicCommand(command).Accepted);Assert.Equal(image.ReadPayload(),Bytes(host));
        Assert.Equal(CampaignRepositoryError.Stale,host.CompleteRepositoryRestore(restore,completion,out _));completion.Abandon();
        for(var i=0;i<2;i++)
        {
            var stamp=expected.CaptureStatus();Assert.True(expected.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);Advance(host);
            Assert.Equal(codec.EncodeCycleRoot(expected.ExportCycleSave()),Bytes(host));
        }
    }

    [Fact] public void StaleCancelledForeignOrAbandonedRestoreCannotReplaceAuthority()
    {
        var host=Host();var image=Image(host);var frame=host.Read(Human);
        Assert.Equal(CampaignRepositoryError.Stale,host.PrepareRepositoryCheckpoint(Host().Read(Human),WireCodec).Error);
        var capture=host.PrepareRepositoryCheckpoint(frame,WireCodec).Work!;Assert.Equal(CampaignRepositoryError.Cancelled,capture.Execute(()=>true).Error);
        var restore=host.PrepareRepositoryRestore(frame,image.ReadPayload(),image.Edition.Identity,WireCodec).Work!;
        var done=restore.Execute(()=>false);Advance(host);var after=Bytes(host);
        Assert.Equal(CampaignRepositoryError.Stale,host.CompleteRepositoryRestore(restore,done,out _));Assert.Equal(after,Bytes(host));
        restore=host.PrepareRepositoryRestore(host.Read(Human),image.ReadPayload(),image.Edition.Identity,WireCodec).Work!;done=restore.Execute(()=>true);
        Assert.Equal(CampaignRepositoryError.Cancelled,host.CompleteRepositoryRestore(restore,done,out _));Assert.Equal(after,Bytes(host));
        restore=host.PrepareRepositoryRestore(host.Read(Human),image.ReadPayload(),image.Edition.Identity,WireCodec).Work!;done=restore.Execute(()=>false);done.Abandon();
        Assert.Equal(CampaignRepositoryError.Stale,host.CompleteRepositoryRestore(restore,done,out _));Assert.Equal(after,Bytes(host));
        Assert.Equal(CampaignRepositoryError.Stale,host.PrepareRepositoryCheckpoint(frame,WireCodec).Error);
    }

    [Fact] public void OutstandingOuterInteractionBlocksCaptureAndRestoreUntilAdoptedOrDrained()
    {
        var host=Host();var image=Image(host);var frame=host.Read(Human);var bodies=host.CaptureBeltBodies(frame)!;
        var body=bodies.Bodies.First();var c=new BeltBodyCommandDto{ActorId=Human,CommandId="command.repository.busy",ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,Action=BeltBodyAction.StartSurvey,BodyId=body.BodyId,ShipId=body.SurveyShipIds.First()};
        var work=host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Preview,c);
        Assert.Equal(CampaignRepositoryError.Busy,host.PrepareRepositoryCheckpoint(frame,WireCodec).Error);
        Assert.Equal(CampaignRepositoryError.Busy,host.PrepareRepositoryRestore(frame,image.ReadPayload(),image.Edition.Identity,WireCodec).Error);
        Assert.True(host.CompleteBeltBodyInteraction(work,work.Execute()));Assert.Equal(image.ReadPayload(),Image(host).ReadPayload());
    }

    PhysicalReviewHost Restore(CampaignRepositoryImage image,CampaignColonisationCyclePackageDto? package=null)
    {
        var host=package==null?Host():PhysicalReviewHost.StartColonisationCycle(package,Settings(),codec);
        var work=host.PrepareRepositoryRestore(host.Read(Human),image.ReadPayload(),image.Edition.Identity,WireCodec).Work!;
        var done=work.Execute(()=>false);Assert.Equal(CampaignRepositoryError.None,done.Error);
        Assert.Equal(CampaignRepositoryError.None,host.CompleteRepositoryRestore(work,done,out _));return host;
    }
    [Fact] public void ProgressedRefinementSurveyPaidMineAndNextRngAllocatorConsequencesSurviveWholeRestore()
    {
        var host=Host();var s=Session(host);var refinement=(ICampaignEngineeringRefinementSession)s;var belts=(ICampaignBeltBodiesSession)s;
        var status=s.CaptureStatus();Assert.True(refinement.SubmitEngineeringRefinement(new(){ActorId=Human,CommandId="command.repository.refinement",ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
            Action=EngineeringRefinementAction.Start,ParentVersionId="component.reactor.v1",Goal=EngineeringRefinementGoal.ReduceCapacity}).Accepted);
        Assert.True(s.SubmitStrategicCommand(Allocation(s)).Accepted);
        var body=belts.CaptureBeltBodies(Human)!.Bodies.Single(b=>b.FeatureId==CampaignAuthoredSolIdentity.FeatureId);
        status=s.CaptureStatus();Assert.True(belts.SubmitBeltCommand(new(){ActorId=Human,CommandId="command.repository.survey",ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
            Action=BeltBodyAction.StartSurvey,BodyId=body.BodyId,ShipId=body.SurveyShipIds.First()}).Accepted);
        Advance(host);Advance(host);status=s.CaptureStatus();Assert.True(belts.SubmitBeltCommand(new(){ActorId=Human,CommandId="command.repository.mine",ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
            Action=BeltBodyAction.EnqueueMine,BodyId=body.BodyId,ColonyId="colony.sol"}).Accepted);
        for(var i=0;i<64&&s.ExportCycleSave() is CampaignBeltBodiesSaveDto r&&r.BeltBodies!.Mines!.Single().Stage==BeltMineStage.Queued;i++)Advance(host);
        Advance(host);var image=Image(host);var root=Assert.IsType<CampaignBeltBodiesSaveDto>(codec.DecodeCycleRoot(image.ReadPayload()));
        Assert.True(root.BeltBodies!.Mines!.Single().TotalExtracted>0);Assert.NotEmpty(root.BeltBodies.LastShipments!);
        Assert.Equal(EngineeringRefinementStage.Pending,root.Refinement!.Projects!.Single().Stage);
        Assert.NotEmpty(root.Refinement.Operations!);Assert.NotEmpty(root.BeltBodies.Operations!);Assert.NotEmpty(root.BaseCampaign!.Journal!);
        var restored=Restore(image);Assert.Equal(Bytes(host),Bytes(restored));
        foreach(var current in new[]{host,restored})
        {
            var application=(ICampaignEngineeringRefinementSession)Session(current);var stamp=Session(current).CaptureStatus();
            Assert.True(application.SubmitEngineeringRefinement(new(){ActorId=Human,CommandId="command.repository.discard",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
                Action=EngineeringRefinementAction.Discard,ProjectId=root.Refinement.Projects!.Single().ProjectId}).Accepted);
            Advance(current);
        }
        Assert.Equal(Bytes(host),Bytes(restored));
    }

    [Fact] public void RealCombatRequestsRoundsResolvedHistoryAndAftermathRemainEquivalent()
    {
        // Match the established human/AI combat fixture and choose orders
        // from public contact observations.
        var package=Package(14,originalScenario:true);var host=PhysicalReviewHost.StartColonisationCycle(package,Settings(),codec);
        var s=Session(host);var frame=host.Read(Human);
        var fleet=Assert.Single(frame.OwnStrategicFleets).FleetId.Value;
        var stamp=s.CaptureStatus();Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.repository.combat.build",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.defiant-combat",BlueprintRevision=1}).Accepted);
        var advances=0;
        while(!s.CaptureColonisation(Human).OwnShipSources.Any(b=>b.BlueprintId=="blueprint.ready.defiant-combat"&&b.Revision==1)&&advances<80)
        {
            frame=host.Read(Human);
            Assert.True(host.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.repository.combat.prepare."+advances,ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
                Order=new(){FleetId=fleet,Intent=CampaignTravelIntent.StayEngage,RouteNodeIds=new()}}).Accepted);
            Advance(host);advances++;
        }
        var built=Assert.Single(s.CaptureColonisation(Human).OwnShipSources,b=>b.BlueprintId=="blueprint.ready.defiant-combat"&&b.Revision==1);
        Assert.Contains(host.Read(Human).OwnStrategicFleets.Single(f=>f.FleetId.Value==fleet).ShipIds,ship=>ship.Value==built.ShipId);
        Assert.Equal(SessionPhase.Planning,s.CaptureStatus().Phase);
        while(s.CaptureStatus().Phase==SessionPhase.Planning&&advances<80)
        {
            frame=host.Read(Human);
            var contact=frame.Travel!.KnownContacts.Where(c=>c.Fresh).OrderBy(c=>c.FleetId,StringComparer.Ordinal)
                .FirstOrDefault(c=>host.FindKnownRoute(frame,fleet,c.LastSeenNodeId,false).Route.Success);
            var route=contact==null?new List<string>():host.FindKnownRoute(frame,fleet,contact.LastSeenNodeId,false).Route.Steps.ToList();
            Assert.True(host.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.repository.combat.reach."+advances,ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
                Order=new(){FleetId=fleet,Intent=contact==null?CampaignTravelIntent.StayEngage:CampaignTravelIntent.Follow,ContactFleetId=contact?.FleetId,ExpectedContactSeenTurn=contact?.LastSeenTurn,RouteNodeIds=route}}).Accepted);
            Advance(host);advances++;
        }
        Assert.Equal(SessionPhase.AwaitingCombat,s.CaptureStatus().Phase);var waiting=Image(host);var restored=Restore(waiting,package);
        Assert.Equal(Bytes(host),Bytes(restored));Assert.NotEmpty(codec.DecodeCycleRoot(waiting.ReadPayload()).BaseCampaign!.Campaign!.Integrated!.Physical!.Requests);
        var capture=host.PrepareRepositoryCheckpoint(host.Read(Human),WireCodec).Work!;
        foreach(var encounter in s.GetEncounters(Human))
        {
            for(var round=0;!s.Observe(encounter,Human).Complete&&round<32;round++)
            {
                foreach(var current in new[]{host,restored})
                {
                    var session=Session(current);var b=session.Observe(encounter,Human);
                    Assert.True(session.CommitRound(new(){ActorId=Human,EncounterId=encounter,CommandId="command.repository.combat.round."+encounter+"."+b.Round,
                        ExpectedTurn=b.Turn,ExpectedRound=b.Round,ExpectedBoundaryRevision=b.BoundaryRevision,Replacements=new()}).Accepted);
                }
                Assert.Equal(Bytes(host),Bytes(restored));
                if(round==0){Assert.False(host.IsRepositoryStateCurrent(capture.Source));Assert.Equal(Bytes(host),Bytes(Restore(Image(host),package)));}
            }
            Assert.True(s.Observe(encounter,Human).Complete);
        }
        foreach(var current in new[]{host,restored}){stamp=Session(current).CaptureStatus();Assert.True(Session(current).FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted);}
        Assert.Equal(Bytes(host),Bytes(restored));Assert.Equal(Bytes(host),Bytes(Restore(Image(host),package)));
    }
}
