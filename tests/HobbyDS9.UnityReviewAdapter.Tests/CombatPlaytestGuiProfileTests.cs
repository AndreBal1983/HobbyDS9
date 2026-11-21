using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class CombatPlaytestGuiProfileTests
{
    static byte[][] Inputs(string folder="combat-playtest-v1")=>CombatPlaytestOptions.InputNames().Select(n=>File.ReadAllBytes(
        Path.Combine(AppContext.BaseDirectory,"scenarios",folder,n+".json"))).ToArray();

    [Fact] public void GuiNewAndColdContinueUseExactFixtureAndIsolatedDirectoryAtPendingHumanDecision()
    {
        var root=Path.Combine(Path.GetTempPath(),"hobbyds9-combat-gui-"+Guid.NewGuid().ToString("N"));
        var first=ReviewUiModel.StartCombatPlaytest(Inputs(),root);
        ReviewUiModel? continued=null;
        try
        {
            Assert.True(first.CombatPlaytest);Assert.True(first.SingleplayerDemo);Assert.False(first.GeneratedCampaign);
            var start=first.Current.Frame.Status.Turn;
            for(int i=0;i<3;i++)Assert.True(first.Advance());
            Assert.Equal(SessionPhase.AwaitingCombat,first.Current.Frame.Status.Phase);
            Assert.Equal(start+2,first.Current.Frame.Status.Turn);
            Assert.NotNull(first.Current.Frame.Battle);Assert.False(first.Current.Frame.Battle!.OwnCommitmentAccepted);
            var slot=CombatPlaytestOptions.Slots()[0];Assert.True(first.Save(slot));
            var path=Path.Combine(root,CombatPlaytestOptions.SaveDirectory,"unity-singleplayer-demo-"+slot+".json");
            var before=File.ReadAllBytes(path);
            Assert.Single(Directory.GetDirectories(root));
            Assert.False(Directory.Exists(Path.Combine(root,"HobbyDS9SingleplayerDemoOnly-v4")));
            Assert.True(first.Close(true));
            continued=ReviewUiModel.StartCombatPlaytest(Inputs(),root);
            Assert.True(continued.Load(slot,true));Assert.True(continued.CombatPlaytest);
            Assert.False(continued.Current.Frame.Battle!.OwnCommitmentAccepted);
            Assert.True(continued.Save(slot,true));Assert.Equal(before,File.ReadAllBytes(path));
        }
        finally
        {
            continued?.Close(true);first.Close(true);
            if(Directory.Exists(root))Directory.Delete(root,true);
        }
    }

    [Fact] public void GuiFactoryRejectsNormalDemoBeforeCreatingAnyCombatStore()
    {
        var root=Path.Combine(Path.GetTempPath(),"hobbyds9-combat-reject-"+Guid.NewGuid().ToString("N"));
        Assert.Throws<ArgumentException>(()=>ReviewUiModel.StartCombatPlaytest(Inputs("singleplayer-demo-v1"),root));
        Assert.False(Directory.Exists(root));
    }
}
