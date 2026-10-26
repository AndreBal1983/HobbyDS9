using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;
public sealed class ColonisationReplayTimingTests
{
    private static (CampaignColonisationExpansionPackageDto Package,CampaignColonisationExpansionSaveDto Root) Fixture()
    {
        var package=ColonisationExpansionTests.Package();
        var session=CampaignColonisationExpansionApplication.StartGame(package,PublishedConstructionProfileTests.Settings(30));
        for(var i=0;i<2;i++)
        {var status=session.CaptureStatus();Assert.True(session.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);}
        return (package,session.ExportColonisationExpansionSave());
    }
    [Fact] public void MeasuredLoadRetainsExactWholeRootAndInputsWithClosedImmutableReport()
    {
        var (package,root)=Fixture();var input=ColonisationExpansionTests.Json(root);var content=ColonisationExpansionTests.Json(package);
        var normal=CampaignColonisationExpansionApplication.LoadSave(package,root);
        var measured=CampaignColonisationExpansionApplication.LoadSave(package,root,out var report);
        Assert.Equal(input,ColonisationExpansionTests.Json(normal.ExportColonisationExpansionSave()));
        Assert.Equal(input,ColonisationExpansionTests.Json(measured.ExportColonisationExpansionSave()));
        Assert.Equal(input,ColonisationExpansionTests.Json(root));Assert.Equal(content,ColonisationExpansionTests.Json(package));
        Assert.NotNull(report);Assert.Equal(7,report!.Phases.Count);Assert.Equal(10,report.Operations.Count);
        Assert.All(report.Phases,p=>{Assert.Equal(ColonisationLoadPhaseState.Returned,p.State);Assert.True(p.ElapsedTicks>=0);});
        Assert.True(report.StopwatchFrequency>0);
        Assert.Equal(root.BaseCampaign!.Journal!.Count,report.Operations.Sum(o=>o.Attempted));
        Assert.All(report.Operations,o=>{Assert.Equal(o.Attempted,o.Accepted);Assert.Equal(0,o.Refused);Assert.Equal(o.Accepted,o.EqualityVerified);});
        Assert.Equal(2,report.Operations.Single(o=>o.Kind==ColonisationLoadOperationKind.Advance).EqualityVerified);
        Assert.All(typeof(ColonisationLoadTimingReport).GetProperties(),p=>Assert.Null(p.SetMethod));
        Assert.Throws<NotSupportedException>(()=>((System.Collections.Generic.IList<ColonisationLoadPhaseTiming>)report.Phases).Clear());
    }
    private static ColonisationLoadTimingReport CompareRejection(CampaignColonisationExpansionPackageDto package,CampaignColonisationExpansionSaveDto root)
    {
        var before=ColonisationExpansionTests.Json(root);var content=ColonisationExpansionTests.Json(package);
        var normal=Record.Exception(()=>CampaignColonisationExpansionApplication.LoadSave(package,root));
        ColonisationLoadTimingReport? report=null;
        var measured=Record.Exception(()=>CampaignColonisationExpansionApplication.LoadSave(package,root,out report));
        Assert.NotNull(normal);Assert.NotNull(measured);Assert.Equal(normal!.GetType(),measured!.GetType());Assert.Equal(normal.Message,measured.Message);
        Assert.Equal(before,ColonisationExpansionTests.Json(root));Assert.Equal(content,ColonisationExpansionTests.Json(package));
        Assert.NotNull(report);Assert.Contains(report!.Phases,p=>p.State==ColonisationLoadPhaseState.Failed);
        Assert.DoesNotContain(report.Phases,p=>p.State==ColonisationLoadPhaseState.Active);return report;
    }
    [Fact] public void AiTamperAndReorderedJournalKeepOriginalExceptionsAndHonestFailureCounts()
    {
        var (package,root)=Fixture();root.AiPlans![0].HomeColonyId="forged";CompareRejection(package,root);
        (package,root)=Fixture();root.BaseCampaign!.Journal!.Reverse();
        var report=CompareRejection(package,root);
        var advance=report.Operations.Single(o=>o.Kind==ColonisationLoadOperationKind.Advance);
        Assert.Equal(1,advance.Attempted);Assert.Equal(0,advance.Accepted);Assert.Equal(1,advance.Refused);Assert.Equal(0,advance.EqualityVerified);
        (package,root)=Fixture();root.BaseCampaign!.Journal![0]=null!;CompareRejection(package,root);
    }
    [Fact] public async Task ConcurrentReportsAreIndependentAndNoTimingObjectIsRetainedBySession()
    {
        var (package,root)=Fixture();
        var results=await Task.WhenAll(Enumerable.Range(0,2).Select(_=>Task.Run(()=>
        {var session=CampaignColonisationExpansionApplication.LoadSave(package,root,out var report);return (session,report);})));
        Assert.NotSame(results[0].report,results[1].report);Assert.NotSame(results[0].report!.Phases,results[1].report!.Phases);
        Assert.Equal(results[0].report!.Operations.Sum(x=>x.EqualityVerified),results[1].report!.Operations.Sum(x=>x.EqualityVerified));
        Assert.All(typeof(CampaignTacticalReviewSession).GetFields(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static),f=>Assert.NotEqual(typeof(ColonisationReplayTiming),f.FieldType));
        Assert.All(typeof(CampaignColonisationExpansionApplication).GetFields(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static),f=>Assert.NotEqual(typeof(ColonisationReplayTiming),f.FieldType));
        Assert.Equal(ColonisationExpansionTests.Json(root),ColonisationExpansionTests.Json(results[0].session.ExportColonisationExpansionSave()));
    }
    [Fact] public void ClosedKindClassifierDoesNotInventAnAcceptedOrVerifiedOperation()
    {
        Assert.Equal(ColonisationReplayKind.Unclassified,ColonisationReplayTiming.Kind(null));
        Assert.Equal(ColonisationReplayKind.Unclassified,ColonisationReplayTiming.Kind(new()));
        var timing=new ColonisationReplayTiming();timing.Begin(ColonisationReplayPhase.Journal);
        timing.Count(ColonisationReplayKind.Unclassified,0);timing.CloseFailed();
        Assert.Equal(3,timing.State[(int)ColonisationReplayPhase.Journal]);Assert.Equal(1,timing.Counts[0,0]);
        Assert.Equal(0,timing.Counts[0,1]);Assert.Equal(0,timing.Counts[0,2]);Assert.Equal(0,timing.Counts[0,3]);
        Assert.Empty(typeof(ColonisationReplayTiming).GetFields(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public));
    }
    [Fact] public void DefaultLoadEntryPointsDoNotConstructTimingAndClosedEnumMappingsAgree()
    {
        Assert.Equal(Enum.GetNames(typeof(ColonisationReplayPhase)),Enum.GetNames(typeof(ColonisationLoadPhase)));
        Assert.Equal(Enum.GetNames(typeof(ColonisationReplayKind)),Enum.GetNames(typeof(ColonisationLoadOperationKind)));
        foreach(var type in new[]{typeof(CampaignColonisationExpansionApplication),typeof(CampaignTacticalReviewSession)})
        {
            var name=type==typeof(CampaignTacticalReviewSession)?"LoadColonisationExpansion":"LoadSave";
            var method=type.GetMethod(name,new[]{typeof(CampaignColonisationExpansionPackageDto),typeof(CampaignColonisationExpansionSaveDto)})!;
            var bytes=method.GetMethodBody()!.GetILAsByteArray()!;
            var codes=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(c=>unchecked((ushort)c.Value));
            for(var i=0;i<bytes.Length;)
            {
                ushort key=bytes[i++];if(key==0xfe)key=(ushort)(0xfe00|bytes[i++]);var code=codes[key];
                if(code==OpCodes.Newobj)Assert.NotEqual(typeof(ColonisationReplayTiming),method.Module.ResolveMethod(BitConverter.ToInt32(bytes,i))!.DeclaringType);
                i+=code.OperandType switch
                {
                    OperandType.InlineNone=>0,
                    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,
                    OperandType.InlineVar=>2,
                    OperandType.InlineI8 or OperandType.InlineR=>8,
                    OperandType.InlineSwitch=>4+4*BitConverter.ToInt32(bytes,i),
                    _=>4
                };
            }
        }
    }
}
