using HobbyDS9.UnityClient;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using System.Text.Json;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class SfxPolicyTests
{
    private sealed class Store : IMusicSettingsStore
    {
        public Dictionary<string,float> Values = new();
        public float Read(string key,float fallback) => Values.TryGetValue(key,out var value) ? value : fallback;
        public void Write(string key,float value) => Values[key]=value;
        public void Flush() { }
    }
    [Theory]
    [InlineData(LcarsSfx.ButtonPress,"buttonPress")]
    [InlineData(LcarsSfx.Success,"success")]
    [InlineData(LcarsSfx.Error,"error")]
    [InlineData(LcarsSfx.Processing,"processing")]
    [InlineData(LcarsSfx.ScreenOpen,"screenOpen")]
    [InlineData(LcarsSfx.Search,"search")]
    public void AllSemanticIdsAreStable(LcarsSfx effect,string id) => Assert.Equal(id,SfxPolicy.SemanticId(effect));
    [Fact] public void StartupAndMissingClipsAreSilentWithoutConsumingSlots()
    {
        var p=new SfxPolicy(); Assert.Equal(-1,p.Reserve(LcarsSfx.Success,true,0,0,1));
        Assert.False(p.StartProcessing(true,0,1)); Assert.False(p.OpenSection("Audio"));
        p.Arm(); Assert.Equal(-1,p.Reserve(LcarsSfx.Success,false,0,0,1));
        Assert.Equal(0,p.Reserve(LcarsSfx.Success,true,0,0,1));
    }
    [Fact] public void DuplicateFrameAndButtonCooldownSuppressRepeats()
    {
        var p=new SfxPolicy();p.Arm(); Assert.Equal(0,p.Reserve(LcarsSfx.ButtonPress,true,1,1,.2));
        Assert.Equal(-1,p.Reserve(LcarsSfx.ButtonPress,true,1,1,.2));
        Assert.Equal(-1,p.Reserve(LcarsSfx.ButtonPress,true,2,1.01,.2));
        Assert.Equal(1,p.Reserve(LcarsSfx.ButtonPress,true,3,1.1,.2));
        Assert.Equal(2,p.Reserve(LcarsSfx.Success,true,3,1.1,.2));
    }
    [Fact] public void PoolIsBoundedAndAcknowledgementPreemptsOrdinaryCue()
    {
        var p=new SfxPolicy();p.Arm();
        foreach(var effect in new[]{LcarsSfx.ButtonPress,LcarsSfx.Search,LcarsSfx.ScreenOpen,LcarsSfx.Success}) Assert.InRange(p.Reserve(effect,true,1,1,10),0,3);
        Assert.Equal(-1,p.Reserve(LcarsSfx.Search,true,2,2,10));
        Assert.InRange(p.Reserve(LcarsSfx.Error,true,2,2,10),0,3);
        for(int i=3;i<100;i++) Assert.InRange(p.Reserve(LcarsSfx.Success,true,i,i,200),0,3);
    }
    [Fact] public void ProcessingCannotOverlapAndStopAllowsNextRequest()
    {
        var p=new SfxPolicy();p.Arm(); Assert.False(p.StartProcessing(false,0,2));
        Assert.True(p.StartProcessing(true,0,2));Assert.False(p.StartProcessing(true,1,2));
        Assert.True(p.StartProcessing(true,2,2));p.StopProcessing();Assert.True(p.StartProcessing(true,3,2));
    }
    [Fact] public void SameSectionRefreshAndResponsiveLayoutDoNotReplayOpen()
    {
        var p=new SfxPolicy();p.Arm(); Assert.False(p.OpenSection("Empire")); Assert.True(p.OpenSection("Audio"));
        foreach(var width in new[]{1280,1600,1920,2560,3440,5120}) { ResponsiveLayoutPolicy.ForLogicalSize(width,1080); Assert.False(p.OpenSection("Audio")); }
        Assert.True(p.OpenSection("Combat"));
    }
    [Fact] public void PassiveFocusChangesAreSilentButNextDeliberateOpeningStillCues()
    {
        var p=new SfxPolicy();p.Arm();Assert.True(p.OpenSection("Combat"));
        p.ObserveSection("Empire");Assert.False(p.OpenSection("Empire"));Assert.True(p.OpenSection("Combat"));
        Assert.False(p.OpenSection("Combat"));
    }
    [Fact] public void MasterAndIndependentMutesPersistWithoutTouchingMusicSettings()
    {
        var store=new Store();var music=new MusicPreferences(store);music.SetMaster(.5f);music.SetMusic(.8f);
        var sfx=new SfxPreferences(store);sfx.SetVolume(.4f);Assert.Equal(.2f,sfx.Gain(music.MasterVolume));
        music.SetMuted(true);Assert.Equal(.2f,sfx.Gain(music.MasterVolume));Assert.Equal(0,music.Gain);
        sfx.SetMuted(true);music.SetMuted(false);Assert.Equal(.4f,music.Gain);Assert.Equal(0,sfx.Gain(.5f));
        var reload=new SfxPreferences(store);Assert.True(reload.Muted);Assert.Equal(.4f,reload.Volume);
        reload.SetMuted(false);Assert.Equal(.2f,reload.Gain(.5f));
    }
    [Theory] [InlineData(CommandStatus.Accepted,LcarsSfx.Success)] [InlineData(CommandStatus.Rejected,LcarsSfx.Error)]
    public void CommandOutcomeIsTypedNotMessageBased(CommandStatus status,LcarsSfx cue)
    { Assert.Equal(cue,SfxActionFeedback.For(new CommandResult("command.test",status,status==CommandStatus.Accepted?CommandErrorCode.None:CommandErrorCode.StaleRevision,1,0))); }
    [Theory] [InlineData(TurnStatus.Completed,LcarsSfx.Success)] [InlineData(TurnStatus.AwaitingCombat,LcarsSfx.Success)] [InlineData(TurnStatus.Rejected,LcarsSfx.Error)]
    public void TurnOutcomeOnlyAcknowledgesAcceptedUserOperations(TurnStatus status,LcarsSfx cue)
    { Assert.Equal(cue,SfxActionFeedback.For(new TurnResult(status,TurnErrorCode.None,1,1,0,SessionPhase.Planning,Array.Empty<StrategicEvent>(),Array.Empty<CommandResult>()))); }
    [Theory] [InlineData(CombatSubmissionStatus.Buffered,LcarsSfx.Success)] [InlineData(CombatSubmissionStatus.Rejected,LcarsSfx.Error)] [InlineData(CombatSubmissionStatus.Deferred,LcarsSfx.Error)]
    public void OnlyBufferedCombatAcknowledgesSuccess(CombatSubmissionStatus status,LcarsSfx cue)
    { Assert.Equal(cue,SfxActionFeedback.For(new CombatSubmissionResult("encounter.test",status,CombatSubmissionError.None,1,0))); }
    [Fact] public void AudioSettingsAndPolicyNeverChangeAuthoritativeSaveOrRandomState()
    {
        var session=CampaignApplication.StartScenario(VerticalSliceFixture.Scenario(false));
        var before=JsonSerializer.Serialize(session.ExportSave());
        var settings=new SfxPreferences(new Store()); var policy=new SfxPolicy();policy.Arm();
        for(int i=0;i<100;i++) { settings.SetMuted(i%2==0);settings.SetVolume(i/100f);policy.Reserve(LcarsSfx.Success,true,i,i,.2);policy.OpenSection("Audio"); }
        Assert.Equal(before,JsonSerializer.Serialize(session.ExportSave()));
    }
    [Fact] public void TypedAcceptedAndStaleResearchResultsMapWithoutChangingCommandSemantics()
    {
        var session=CampaignApplication.StartScenario(VerticalSliceFixture.Scenario(false));
        var client=new StrategicClient(session,save=>CampaignApplication.LoadSave(VerticalSliceFixture.Scenario(false),save));
        var old=client.Snapshot;
        var accepted=client.Engineering(old,new EntityId("civ.federation"),new DefinitionId("engineering.advanced-starship-engineering"));
        Assert.True(accepted.Accepted);Assert.Equal(LcarsSfx.Success,SfxActionFeedback.For(accepted));
        var before=JsonSerializer.Serialize(client.ExportSave());
        var rejected=client.Engineering(old,new EntityId("civ.federation"),new DefinitionId("engineering.advanced-starship-engineering"));
        Assert.False(rejected.Accepted);Assert.Equal(LcarsSfx.Error,SfxActionFeedback.For(rejected));
        Assert.Equal(before,JsonSerializer.Serialize(client.ExportSave()));
    }
    [Theory] [InlineData(-1f,0f)] [InlineData(2f,1f)] [InlineData(float.NaN,0f)] [InlineData(float.PositiveInfinity,0f)]
    public void VolumeClampsInvalidInputs(float value,float expected)
    { var p=new SfxPreferences(new Store());p.SetVolume(value);Assert.Equal(expected,p.Volume); }
}
