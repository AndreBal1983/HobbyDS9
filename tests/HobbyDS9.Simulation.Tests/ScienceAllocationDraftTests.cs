using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityClient;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class ScienceAllocationDraftTests
{
    private static readonly EntityId Human = new("civ.federation");
    private static ScienceAllocationDraft Draft() => new(ScienceCoreTests.Equal().Select(a => new ScienceAllocationSnapshot(a)).ToList());
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(70)][InlineData(100)][InlineData(130)][InlineData(420)][InlineData(599)][InlineData(600)]
    public void EditingAndEqualizationPreserveEveryUnitAndLeaveInputDetached(int weight)
    {
        var input = ScienceCoreTests.Equal().Select(a => new ScienceAllocationSnapshot(a)).ToList();
        var draft = new ScienceAllocationDraft(input); draft.SetWeight(ScienceDiscipline.Propulsion, weight);
        Assert.Equal(weight, draft.Weight(ScienceDiscipline.Propulsion)); Assert.Equal(600, draft.Export().Sum(a => a.Weight));
        Assert.All(draft.Export(), a => Assert.InRange(a.Weight, 0, 600)); Assert.All(input, a => Assert.Equal(100, a.Weight));
        Assert.Equal(weight != 100, draft.Changed); var exported = draft.Export(); exported[0].Weight = -1;
        Assert.Equal(600, draft.Export().Sum(a => a.Weight)); draft.Equalize(); Assert.False(draft.Changed); Assert.All(draft.Export(), a => Assert.Equal(100, a.Weight));
    }
    [Fact] public void TransfersAreEvenInEnumOrderAndIndependentOfInputOrder()
    {
        var left = Draft(); var right = new ScienceAllocationDraft(ScienceCoreTests.Equal().AsEnumerable().Reverse().Select(a => new ScienceAllocationSnapshot(a)).ToList());
        foreach (var target in new[]{150, 0, 600, 231, 100})
        { left.SetWeight(ScienceDiscipline.Weapons, target); right.SetWeight(ScienceDiscipline.Weapons, target); Assert.Equal(JsonSerializer.Serialize(left.Export()), JsonSerializer.Serialize(right.Export())); }
        left = Draft(); left.SetWeight(ScienceDiscipline.Weapons,150); Assert.Equal(new[]{90,90,90,90,90,150},left.Export().Select(a=>a.Weight));
    }
    [Theory][InlineData(-1)][InlineData(601)]
    public void InvalidEditsDoNotChangeDraft(int weight)
    {var draft=Draft();var before=JsonSerializer.Serialize(draft.Export());Assert.Throws<ArgumentOutOfRangeException>(()=>draft.SetWeight(ScienceDiscipline.Biotech,weight));Assert.Equal(before,JsonSerializer.Serialize(draft.Export()));}
    [Fact] public void BadInputIsNotSilentlyNormalized()
    { var input=ScienceCoreTests.Equal();input[0].Weight=99;Assert.Throws<ArgumentException>(()=>new ScienceAllocationDraft(input.Select(a=>new ScienceAllocationSnapshot(a)).ToList())); }
    [Fact] public void PresentationUsesConceptualAmountsAndExactEqualLabel()
    { Assert.Equal("4",ScienceAllocationDraft.Conceptual(2400));Assert.Equal("1.2",ScienceAllocationDraft.Conceptual(720));Assert.Equal("16⅔%",ScienceAllocationDraft.DisplayPercent(100m/6));Assert.Equal("70%",ScienceAllocationDraft.DisplayPercent(70)); }
    [Fact] public void LocalDraftOnlyCommitsThroughStampedClientAndReloadsExactly()
    {
        var scenario=VerticalSliceFixture.Scenario(false,weighted:true);var client=new StrategicClient(CampaignApplication.StartScenario(scenario),save=>CampaignApplication.LoadSave(scenario,save));
        string Save()=>JsonSerializer.Serialize(client.ExportSave());var original=client.Snapshot;var draft=new ScienceAllocationDraft(original.Campaign.Civilizations.Single(c=>c.CivilizationId==Human).Research!.ScienceAllocations);var before=Save();
        draft.SetWeight(ScienceDiscipline.Weapons,150);Assert.Equal(before,Save());Assert.True(client.AllocateScience(original,Human,draft.Export()).Accepted);Assert.Equal(original.Campaign.StateRevision+1,client.Snapshot.Campaign.StateRevision);
        var committed=Save();draft.Equalize();Assert.Equal(CommandErrorCode.StaleRevision,client.AllocateScience(original,Human,draft.Export()).ErrorCode);Assert.Equal(committed,Save());
        client.Load(JsonSerializer.Deserialize<SaveEnvelopeDto>(committed)!);Assert.Equal(committed,Save());Assert.True(client.AllocateScience(client.Snapshot,Human,draft.Export()).Accepted);Assert.All(client.Snapshot.Campaign.Civilizations.Single(c=>c.CivilizationId==Human).Research!.ScienceAllocations,a=>Assert.Equal(100,a.Weight));
    }
}
