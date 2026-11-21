using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;
public sealed class TravelCopyParityTests
{
    static FieldInfo Field(string name)=>typeof(CampaignTacticalReviewSession).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!;
    static CampaignTacticalTravelSaveDto Current(CampaignTacticalReviewSession s)=>(CampaignTacticalTravelSaveDto)Field("travel").GetValue(s)!;
    static CampaignTacticalReviewSaveDto Core(CampaignTacticalReviewSession s)=>(CampaignTacticalReviewSaveDto)Field("state").GetValue(s)!;
    static CampaignTacticalTravelSaveDto Copy(CampaignTacticalReviewSession s)=>(CampaignTacticalTravelSaveDto)typeof(CampaignTacticalReviewSession).GetMethod("TravelCopy",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(s,null)!;
    // Pre-change algorithm, retained only in this test as an independent parity oracle.
    static CampaignTacticalTravelSaveDto OldCopy(CampaignTacticalReviewSession s)
    {var copy=CampaignTacticalTravelData.Copy(Current(s));copy.Core=CampaignTacticalReviewData.Copy(Core(s));return copy;}
    static string Json(object o)=>JsonSerializer.Serialize(o);
    static void Detached(object? a,object? b)
    {
        if(a==null){Assert.Null(b);return;}Assert.NotNull(b);var t=a.GetType();Assert.Equal(t,b!.GetType());
        if(t.IsValueType||a is string)return;
        Assert.NotSame(a,b);
        if(a is IEnumerable left&&b is IEnumerable right)
        {var x=left.Cast<object?>().ToArray();var y=right.Cast<object?>().ToArray();Assert.Equal(x.Length,y.Length);for(var i=0;i<x.Length;i++)Detached(x[i],y[i]);return;}
        foreach(var p in t.GetProperties().Where(p=>p.GetIndexParameters().Length==0))Detached(p.GetValue(a),p.GetValue(b));
    }
    static CampaignTacticalReviewSession Battle()
    {
        var p=CampaignPhysicalTravelTests.Package();p.InitialOrders!.ForEach(o=>o.Intent=CampaignTravelIntent.StayEngage);
        var s=CampaignTacticalReviewSession.StartTravel(p);var stamp=s.CaptureStatus();Assert.True(s.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);
        var id=Assert.Single(s.Encounters("civ.federation"));
        foreach(var actor in new[]{"civ.federation","civ.cardassian"})
        {var v=s.Observe(id,actor);Assert.True(s.CommitRound(new(){ActorId=actor,CommandId="command.copy."+actor,EncounterId=id,ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new()}).Accepted);}
        return CampaignTacticalReviewSession.LoadTravel(p,s.ExportTravelSave());
    }
    [Fact] public void AliasedCanonicalCoreMatchesOldBytesAndEveryMutableBranchIsDetached()
    {
        var s=Battle();var original=Current(s);Assert.Same(original.Core,Core(s));
        Assert.NotEmpty(original.Core.Pools);Assert.NotEmpty(original.Core.Battles);Assert.NotEmpty(original.Core.AcceptedRoundCommands);
        Assert.NotEmpty(original.Core.Battles[0].ResolvedRounds);Assert.NotNull(original.MovementProof);
        var before=Json(original);var copy=Copy(s);Assert.Equal(Json(OldCopy(s)),Json(copy));Detached(original,copy);
        copy.Core.Campaign.GameState.StateRevision++;copy.Core.Pools.Clear();copy.Core.Battles.Clear();copy.Core.AcceptedRoundCommands.Clear();copy.MovementProof!.Orders.Clear();
        Assert.Equal(before,Json(Current(s)));
    }
    [Fact] public void DistinctUpdatedStateWinsOverStaleTravelCoreAndKeepsOldBranchErrors()
    {
        var s=CampaignTacticalReviewSession.StartTravel(CampaignPhysicalTravelTests.Package());var stale=Json(Current(s));
        var updated=CampaignTacticalReviewData.Copy(Core(s));updated.Campaign.GameState.StateRevision++;
        Field("state").SetValue(s,updated);Assert.NotSame(Current(s).Core,Core(s));
        var copy=Copy(s);Assert.Equal(Json(OldCopy(s)),Json(copy));Assert.Equal(updated.Campaign.GameState.StateRevision,copy.Core.Campaign.GameState.StateRevision);
        Detached(updated,copy.Core);Assert.Equal(stale,Json(Current(s)));
        // Complete first copy must still traverse a malformed stale core before replacement.
        Current(s).Core.Pools=null!;
        var expected=Record.Exception(()=>OldCopy(s));var actual=Record.Exception(()=>Copy(s));
        Assert.NotNull(expected);Assert.IsType<TargetInvocationException>(actual);Assert.Equal(expected!.GetType(),actual!.InnerException!.GetType());Assert.Equal(expected.Message,actual.InnerException.Message);
    }
    [Fact] public void MalformedPhysicalIngressStillRejectsBeforeAnyCopyCanBePublished()
    {
        var p=CampaignPhysicalTravelTests.Package();var root=CampaignTacticalReviewSession.StartTravel(p).ExportTravelSave();root.Core.Pools=null!;
        var before=Json(root);Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadTravel(p,root));Assert.Equal(before,Json(root));
    }
}
