using System.Collections.Concurrent;
using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ReviewLoadDiagnosticTests
{
    [Fact] public void DisabledSinkIsNullAndDefaultCodecRemainsSilent()
    {
        Assert.Null(ReviewLoadDiagnostic.Create(null));
        var codec=new ReviewNewtonsoftCodec();
        Assert.Null(typeof(ReviewNewtonsoftCodec).GetField("loadDiagnostic",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(codec));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeColonisationRoot(new byte[]{123}));
    }
    [Fact] public void EnabledRecordContainsOnlyClosedFieldsAndNoExceptionText()
    {
        var rows=new List<ReviewLoadDiagnosticRecord>();var diagnostic=ReviewLoadDiagnostic.Create(rows.Add)!;
        diagnostic.Record(ReviewLoadPhase.StoreRead,ReviewLoadEvent.Begin);
        diagnostic.Record(ReviewLoadPhase.HostOpen,ReviewLoadEvent.Error,new InvalidDataException("PRIVATE-root-path-actor"),ReviewCheckpointError.InvalidCheckpoint);
        Assert.Equal(2,rows.Count);Assert.Equal(ReviewLoadFailureType.InvalidData,rows[1].Failure);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,rows[1].Error);Assert.True(rows[1].ElapsedTicks>=rows[0].ElapsedTicks);
        Assert.All(typeof(ReviewLoadDiagnosticRecord).GetProperties(),p=>Assert.True(p.PropertyType.IsEnum||p.PropertyType==typeof(long)));
        diagnostic.Record((ReviewLoadPhase)999,ReviewLoadEvent.Begin);diagnostic.Record(ReviewLoadPhase.Publish,(ReviewLoadEvent)999);
        diagnostic.Record(ReviewLoadPhase.HostOpen,ReviewLoadEvent.Error,error:(ReviewCheckpointError)999);
        Assert.Equal(2,rows.Count);
    }
    [Fact] public void ThrowingSinkCannotEscapeAndStillHasFiniteBudget()
    {
        var count=0;var diagnostic=ReviewLoadDiagnostic.Create(_=>{count++;throw new InvalidOperationException("sink failure");})!;
        for(var i=0;i<200;i++)diagnostic.Record(ReviewLoadPhase.Publish,ReviewLoadEvent.NormalReturn);
        Assert.Equal(96,count);
    }
    [Fact] public void ClosedOperationCannotEmitFromRetainedCodecAndDoesNotCloseAnotherOperation()
    {
        var a=new List<ReviewLoadDiagnosticRecord>();var b=new List<ReviewLoadDiagnosticRecord>();
        var da=ReviewLoadDiagnostic.Create(a.Add)!;var db=ReviewLoadDiagnostic.Create(b.Add)!;
        var retainedCodec=new ReviewNewtonsoftCodec(da);da.Close();da.Close();
        Assert.Throws<InvalidDataException>(()=>retainedCodec.DecodeColonisationRoot(new byte[]{123}));
        db.Record(ReviewLoadPhase.StoreRead,ReviewLoadEvent.Begin);
        Assert.Empty(a);Assert.Single(b);
    }
    [Fact] public void ConcurrentOperationsHaveIndependentNinetySixRecordCaps()
    {
        var a=new ConcurrentBag<ReviewLoadDiagnosticRecord>();var b=new ConcurrentBag<ReviewLoadDiagnosticRecord>();
        var da=ReviewLoadDiagnostic.Create(a.Add)!;var db=ReviewLoadDiagnostic.Create(b.Add)!;
        Parallel.For(0,1000,i=>{da.Record(ReviewLoadPhase.StoreRead,ReviewLoadEvent.Begin);db.Record(ReviewLoadPhase.Publish,ReviewLoadEvent.NormalReturn);});
        Assert.Equal(96,a.Count);Assert.Equal(96,b.Count);
        Assert.All(a,r=>Assert.Equal(ReviewLoadPhase.StoreRead,r.Phase));Assert.All(b,r=>Assert.Equal(ReviewLoadPhase.Publish,r.Phase));
    }
    [Fact] public void ExceptionClassificationIsClosedAndDoesNotUnwrapOrReadText()
    {
        var cases=new (Exception? Error,ReviewLoadFailureType Expected)[]{
            (null,ReviewLoadFailureType.None),(new ArgumentNullException(),ReviewLoadFailureType.ArgumentNull),
            (new ArgumentException(),ReviewLoadFailureType.Argument),(new InvalidDataException(),ReviewLoadFailureType.InvalidData),
            (new IOException(),ReviewLoadFailureType.IO),(new InvalidOperationException(),ReviewLoadFailureType.InvalidOperation),
            (new UnauthorizedAccessException(),ReviewLoadFailureType.UnauthorizedAccess),(new MissingMethodException(),ReviewLoadFailureType.MissingMethod),
            (new TypeLoadException(),ReviewLoadFailureType.TypeLoad),(new NotSupportedException(),ReviewLoadFailureType.NotSupported),
            (new OverflowException(),ReviewLoadFailureType.Overflow),(new OutOfMemoryException(),ReviewLoadFailureType.OutOfMemory),
            (new TargetInvocationException(new ArgumentNullException()),ReviewLoadFailureType.Other),(new PoisonTextException(),ReviewLoadFailureType.Other)};
        foreach(var pair in cases)Assert.Equal(pair.Expected,ReviewLoadDiagnostic.Classify(pair.Error));
    }
    sealed class PoisonTextException:Exception
    {
        public override string Message=>throw new Exception("Must never inspect message");
        public override string ToString()=>throw new Exception("Must never stringify exception");
    }
    static CampaignColonisationRosterPackageDto Package(ReviewNewtonsoftCodec codec)
    {
        byte[] Read(string f,string n)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",f,n+".json"));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var baseV1=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        return ColonisationPlaytestOptions.CreateRosterPackage(baseV1,Read("colonisation-roster-v2","roster"),codec);
    }
    [Fact] public void SmallValidAndMalformedOpenHaveSameResultsWithNullEnabledOrThrowingSink()
    {
        var directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Load-Diagnostic-"+Guid.NewGuid().ToString("N"));
        try
        {
            var codec=new ReviewNewtonsoftCodec();var package=Package(codec);
            var initial=CampaignColonisationApplication.StartRosterGame(package,new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}}).ExportColonisationSave();
            var bytes=codec.EncodeColonisationRoot(initial);var store=ReviewColonisationSlotStore.CreateRosterV2(directory,codec);store.Write("valid",initial,false);
            File.WriteAllText(Path.Combine(directory,"unity-campaign-colonisation-v2-bad.json"),"{");
            foreach(var mode in new[]{0,1,2})
            {
                var rows=new List<ReviewLoadDiagnosticRecord>();
                var diagnostic=mode==0?null:ReviewLoadDiagnostic.Create(row=>{rows.Add(row);if(mode==2)throw new InvalidOperationException("observer only");});
                var observedCodec=mode==0?new ReviewNewtonsoftCodec():new ReviewNewtonsoftCodec(diagnostic);
                var observedStore=ReviewColonisationSlotStore.CreateRosterV2(directory,observedCodec);
                var opened=PhysicalReviewHost.OpenColonisationRosterFromSlot(package,observedCodec,observedStore,"valid","civ.federation",diagnostic:diagnostic);
                Assert.True(opened.Succeeded);Assert.Equal(ReviewCheckpointError.None,opened.Error);
                var session=(ICampaignColonisationSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(opened.Host)!;
                Assert.Equal(bytes,codec.EncodeColonisationRoot(session.ExportColonisationSave()));
                if(mode!=0){Assert.Contains(rows,x=>x.Phase==ReviewLoadPhase.FullReplay&&x.Event==ReviewLoadEvent.NormalReturn);Assert.Contains(rows,x=>x.Phase==ReviewLoadPhase.Presentation&&x.Event==ReviewLoadEvent.NormalReturn);}
                rows.Clear();
                var bad=PhysicalReviewHost.OpenColonisationRosterFromSlot(package,observedCodec,observedStore,"bad","civ.federation",diagnostic:diagnostic);
                Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,bad.Error);Assert.Null(bad.Host);
                Assert.DoesNotContain(rows,x=>x.Phase==ReviewLoadPhase.FullReplay);
                Assert.Equal(ReviewCheckpointError.NotFound,PhysicalReviewHost.OpenColonisationRosterFromSlot(package,observedCodec,observedStore,"missing","civ.federation",diagnostic:diagnostic).Error);
                Assert.Equal(bytes,File.ReadAllBytes(Path.Combine(directory,"unity-campaign-colonisation-v2-valid.json")));
            }
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
}
