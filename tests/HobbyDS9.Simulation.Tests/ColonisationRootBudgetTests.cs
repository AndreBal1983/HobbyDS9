using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationRootBudgetTests
{
    static (CampaignColonisationSaveDto Root,ColonisationPolicyDto Policy) Initial()
    {
        var package=ColonisationInventoryTests.Package();
        return(CampaignTacticalReviewSession.StartColonisation(package,PublishedConstructionProfileTests.Settings()).ExportColonisationSave(),package.Settlement!);
    }
    static string Json(object? value)=>JsonSerializer.Serialize(value);
    static T Copy<T>(T value)=>JsonSerializer.Deserialize<T>(Json(value))!;

    [Fact] public void RealInitialRootPassesAndPreflightNeverMutatesCallerData()
    {
        var (root,policy)=Initial();var before=Json(root);var policyBefore=Json(policy);
        ColonisationRootBudget.Validate(root,policy);
        Assert.Equal(before,Json(root));Assert.Equal(policyBefore,Json(policy));
        root.PackageHash=new string('x',11200000);before=Json(root);
        Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.Validate(root,policy));
        Assert.Equal(before,Json(root));Assert.Equal(policyBefore,Json(policy));
    }
    [Theory][InlineData("frozen")][InlineData("manifest-list")][InlineData("game-list")][InlineData("null-item")][InlineData("wrong-profile")]
    public void RequiredStructureRejectsBeforeReplay(string change)
    {
        var (root,policy)=Initial();
        if(change=="frozen")root.FrozenPlanets!.Definitions=null;
        if(change=="manifest-list")root.Colonisation!.ManifestRecords=null;
        if(change=="game-list")root.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Ships=null;
        if(change=="null-item")root.FrozenPlanets!.Bodies!.Add(null!);
        if(change=="wrong-profile")root.FormatVersion=10;
        var before=Json(root);
        Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.Validate(root,policy));Assert.Equal(before,Json(root));
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)]
    public void EveryJournalCarrierAndColonisationActionIsClosed(int malformed)
    {
        var (root,policy)=Initial();
        var command=new ColonisationCommandDto{Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}};
        var op=new ColonisationOperationDto{Colonisation=command};
        if(malformed==0)op.Existing=new();
        if(malformed==1){op.Colonisation=null;op.Existing=new();}
        if(malformed==2){op.Colonisation=null;op.Existing=new(){Activity=new(),Common=new()};}
        if(malformed==3){op.Colonisation=null;op.Existing=new(){Common=new(){Common=new(){Kind=FrontierOperationKind.Advance,Turn=1,Revision=0},Organization=new()}};}
        if(malformed==4)command.Action=ColonisationAction.EmbarkSettlers;
        if(malformed==5)command.LoadSupplies=new();
        if(malformed==6){op.Colonisation=null;op.Existing=new(){Common=new(){Common=new(){Kind=FrontierOperationKind.Advance,Turn=1,Revision=0,Travel=new()}}};}
        root.Journal!.Add(op);var before=Json(root);
        Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.Validate(root,policy));Assert.Equal(before,Json(root));
    }
    static long MinimumPassing(CampaignColonisationSaveDto root,ColonisationPolicyDto source,bool work)
    {
        long low=1,high=work?source.MaximumValidationVisits:source.MaximumCanonicalRootBytes;
        while(low<high)
        {
            long mid=low+(high-low)/2;var policy=Copy(source);
            if(work)policy.MaximumValidationVisits=mid;else policy.MaximumCanonicalRootBytes=mid;
            try{ColonisationRootBudget.Validate(root,policy);high=mid;}catch(ArgumentException){low=mid+1;}
        }
        return low;
    }
    [Fact] public void ExactWorkAndByteEdgesRejectOneUnitLessAndUnicodeBoundCoversActualJson()
    {
        var (root,source)=Initial();root.PackageHash="Quotes\"slashes\\controls\n\u0001 é 漢字 😀";
        foreach(bool work in new[]{true,false})
        {
            var edge=MinimumPassing(root,source,work);Assert.True(edge>1);
            var policy=Copy(source);if(work)policy.MaximumValidationVisits=edge;else policy.MaximumCanonicalRootBytes=edge;
            ColonisationRootBudget.Validate(root,policy);
            if(!work)Assert.True(JsonSerializer.SerializeToUtf8Bytes(root).LongLength<=edge);
            if(work)policy.MaximumValidationVisits--;else policy.MaximumCanonicalRootBytes--;
            Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.Validate(root,policy));
        }
    }
    [Fact] public void PolicyCannotDisableOrRaiseTechnicalCeilings()
    {
        var (root,source)=Initial();
        ColonisationRootBudget.Validate(root,source);
        var p=Copy(source);p.MaximumValidationVisits=0;Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.Validate(root,p));
        p=Copy(source);p.MaximumValidationVisits=2000001;Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.Validate(root,p));
        p=Copy(source);p.MaximumCanonicalRootBytes=0;Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.Validate(root,p));
        p=Copy(source);p.MaximumCanonicalRootBytes=67108865;Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.Validate(root,p));
    }
    [Fact] public void GeneratedVisitorMatchesEntireCurrentClosedAcyclicContractSurface()
    {
        // Reflection is test/offline authoring only, never part of the portable guard.
        var classes=new HashSet<Type>();var pending=new Queue<Type>();pending.Enqueue(typeof(CampaignColonisationSaveDto));
        while(pending.Count>0)
        {
            var type=pending.Dequeue();
            if(Nullable.GetUnderlyingType(type) is Type n){pending.Enqueue(n);continue;}
            if(type.IsEnum){Assert.Equal(typeof(int),Enum.GetUnderlyingType(type));continue;}
            if(type==typeof(string)||type.IsPrimitive)continue;
            if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(List<>)){pending.Enqueue(type.GenericTypeArguments[0]);continue;}
            Assert.Equal(typeof(CampaignColonisationSaveDto).Assembly,type.Assembly);Assert.True(type.IsClass&&type.IsSealed);
            if(!classes.Add(type))continue;
            foreach(var property in type.GetProperties(BindingFlags.Instance|BindingFlags.Public)){Assert.NotNull(property.SetMethod);pending.Enqueue(property.PropertyType);}
        }
        var canonical=string.Join("\n",classes.OrderBy(t=>t.FullName,StringComparer.Ordinal).SelectMany(t=>t.GetProperties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>t.FullName+"|"+p.Name+"|"+p.PropertyType)));
        Assert.Equal(ColonisationRootBudget.SchemaFingerprint,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant());
        var visiting=new HashSet<Type>();var complete=new HashSet<Type>();
        void Acyclic(Type type)
        {
            if(complete.Contains(type))return;Assert.True(visiting.Add(type),"Recursive contract needs a reviewed bounded visitor.");
            if(Nullable.GetUnderlyingType(type) is Type n)Acyclic(n);
            else if(type.IsGenericType)Acyclic(type.GenericTypeArguments[0]);
            else if(classes.Contains(type))foreach(var property in type.GetProperties())Acyclic(property.PropertyType);
            visiting.Remove(type);complete.Add(type);
        }
        Acyclic(typeof(CampaignColonisationSaveDto));
    }
}
