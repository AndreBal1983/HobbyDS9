using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.CampaignRepositoryHost.Tests;

public sealed class RepositoryFixture:IDisposable
{
    internal const string Human="civ.federation";
    internal readonly ReviewNewtonsoftCodec Codec=new();
    internal readonly string DirectoryPath=Path.Combine(AppContext.BaseDirectory,"repository-fixtures",Guid.NewGuid().ToString("N"));
    internal ICampaignRepositoryCodec Wire=>new DelegatedCampaignRepositoryCodec("review-newtonsoft-cycle-v1",Codec.EncodeCycleRoot,Codec.DecodeCycleRoot);
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    internal CampaignColonisationCyclePackageDto Package(int format=18)
    {
        var t=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),Read("sol-eight-planets-v1","scenario"),Codec);
        var a=ActivitiesPlaytestOptions.CreatePackage(t,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),Codec);
        var c=ColonisationPlaytestOptions.CreatePackage(a,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),Codec);
        var r=ColonisationPlaytestOptions.CreateRosterPackage(c,Read("colonisation-roster-v2","roster"),Codec);
        var g=ColonisationGrowthPlaytestOptions.CreatePackage(r,Read("colonisation-expansion-v1","planner"),Codec);
        return format switch {18=>CampaignColonisationCycleApplication.CreateBeltBodiesPackage(g),17=>CampaignColonisationCycleApplication.CreateEngineeringRefinementPackage(g),
            _=>ColonisationCyclePlaytestOptions.CreatePackage(g,Read("colonisation-cycle-v1","cycle-policy"),Codec)};
    }
    internal PhysicalReviewHost Host(int format=18)=>PhysicalReviewHost.StartColonisationCycle(Package(format),new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}},Codec);
    internal static ICampaignColonisationCycleSession Session(PhysicalReviewHost host)=>(ICampaignColonisationCycleSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(host)!;
    internal byte[] Bytes(PhysicalReviewHost host)=>Codec.EncodeCycleRoot(Session(host).ExportCycleSave());
    internal CampaignRepositoryImage Image(PhysicalReviewHost host)
    {var p=host.PrepareRepositoryCheckpoint(host.Read(Human),Wire);Assert.Equal(CampaignRepositoryError.None,p.Error);var r=p.Work!.Execute(()=>false);Assert.True(r.Succeeded);return r.Image!;}
    internal CycleCheckpointValidator Validator(CampaignRepositoryImage image,int format=18)=>new(new CycleCampaignRepositoryAdapter(Package(format),Codec,Wire),image.Edition.Identity);
    internal SqliteCampaignRepository Repository(CampaignRepositoryImage image,int format=18,string name="campaign")=>new(Path.Combine(DirectoryPath,name),name,image.Edition.Identity,Validator(image,format));
    internal static void Advance(PhysicalReviewHost host){var f=host.Read(Human);Assert.True(host.AdvancePlanning(f).Accepted);}
    internal static ReferenceUpdate Manual(string id="manual",long generation=0)=>new(id,generation,SaveReferenceKind.Manual,"Named save "+id,"");
    internal static SaveWorkResult Run(RepositorySaveQueue queue,PhysicalReviewHost host)
    {var p=queue.PrepareNext(host.Read(Human));Assert.Equal(SaveRequestStatus.Queued,p.Status);var work=Assert.IsType<RepositorySaveWork>(p.Work);var result=work.Execute();Assert.True(queue.Complete(work));return result;}
    public void Dispose()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"repository-fixtures"))+Path.DirectorySeparatorChar;
        var target=Path.GetFullPath(DirectoryPath);
        Assert.StartsWith(root,target,StringComparison.OrdinalIgnoreCase);
        if(Directory.Exists(target))Directory.Delete(target,true);
    }
}
