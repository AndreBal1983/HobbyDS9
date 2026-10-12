using HobbyDS9.UnityClient;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient.Tests
{
    public sealed class LcarsTests
    {
        [Test]
        public void ComponentsConstructWithSingleSelectedSectionAndIndependentProductionToggle()
        {
            var production=false;var rail=new LcarsNavigationRail(_=>{},()=>production=!production);
            rail.Select("Combat");rail.ShowProduction(true);
            Assert.That(rail.Sections.Count,Is.EqualTo(7));Assert.That(rail.Query<Button>().ToList().Count,Is.EqualTo(8));
            Assert.That(System.Linq.Enumerable.Count(rail.Sections,s=>s.Selected),Is.EqualTo(1));
            Assert.That(rail.Production.ClassListContains("production-open"),Is.True);
            Assert.That(rail.Production.ClassListContains("active-tab"),Is.False);Assert.That(production,Is.False);
            Assert.That(new LcarsTopBar(()=>{},()=>{}).EndTurn,Is.Not.Null);
            Assert.That(Resources.Load<StyleSheet>("LcarsTokens"),Is.Not.Null);
        }
        [Test]
        public void ShellKeepsHeaderScrollableContextAndExactlyOneActiveNavigationSegment()
        {
            var shell=new LcarsShell(new VisualElement(),new ProductionOverviewView(),_=>{},()=>{},()=>{});
            shell.SelectSection("Combat");
            Assert.That(shell.Header.EndTurn,Is.Not.Null);Assert.That(shell.Details,Is.InstanceOf<ScrollView>());
            Assert.That(System.Linq.Enumerable.Count(shell.Navigation.Sections,s=>s.Selected),Is.EqualTo(1));
            Assert.That(System.Linq.Enumerable.Single(shell.Navigation.Sections,s=>s.Section=="Combat").Selected,Is.True);
            shell.ApplyLogicalSize(1422,800);Assert.That(shell.ClassListContains("layout-compact"),Is.True);
            Assert.That(shell.Q<VisualElement>("lcars-header"),Is.SameAs(shell.Header));
        }
        [Test]
        public void ResizeDoesNotRecreateControlsOrLoseExplicitProductionChoice()
        {
            var shell=new LcarsShell(new VisualElement(),new ProductionOverviewView(),_=>{},()=>{},()=>{});
            var endTurn=shell.Header.EndTurn;var count=shell.Query<Button>().ToList().Count;
            shell.ApplyLogicalSize(2580,1080);Assert.That(shell.ProductionOpen,Is.True);
            shell.ToggleProduction();Assert.That(shell.ProductionOpen,Is.False);
            for(var i=0;i<10;i++)foreach(var width in new[]{1422,1920,2580,3840})shell.ApplyLogicalSize(width,width==1422?800:1080);
            Assert.That(shell.ProductionOpen,Is.False);Assert.That(shell.Header.EndTurn,Is.SameAs(endTurn));
            Assert.That(shell.Query<Button>().ToList().Count,Is.EqualTo(count));
            shell.OpenProduction();shell.ApplyLogicalSize(1422,800);
            Assert.That(shell.ProductionFrame.style.position.value,Is.EqualTo(Position.Absolute));
            Assert.That(shell.DetailsFrame.style.display.value,Is.EqualTo(DisplayStyle.None));
            shell.ToggleDetails();Assert.That(shell.ProductionOpen,Is.False);Assert.That(shell.DetailsOpen,Is.True);
        }
        [Test]
        public void ShellModeChangesDoNotTouchCampaignOrExistingBufferedCombatReport()
        {
            var host=new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);
            for(var turn=0;turn<25&&host.Snapshot.Campaign.Phase==HobbyDS9.Contracts.SessionPhase.Planning;turn++)host.EndTurn();
            Assert.That(host.Snapshot.Campaign.Phase,Is.EqualTo(HobbyDS9.Contracts.SessionPhase.AwaitingCombat));
            var request=host.Pending()[0];host.Resolve(request);
            var before=host.SaveJson();var shell=new LcarsShell(new GalaxyMap(),new ProductionOverviewView(),_=>{},()=>{},()=>{});
            var view=new CombatPanelView();view.Present(host.Snapshot,host.Client,new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<HobbyDS9.Contracts.FleetCombatPlanDto>>(),operation=>operation());
            shell.Details.Add(view);shell.SelectSection("Combat");
            foreach(var width in new[]{1422,1920,2580,3840})shell.ApplyLogicalSize(width,width==1422?800:1080);
            Assert.That(view.Q<Foldout>("battle-report-"+request.EncounterId),Is.Not.Null);Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [TestCase(1422,800,"layout-compact")]
        [TestCase(1920,1080,"layout-standard")]
        [TestCase(2580,1080,"layout-wide")]
        public void ExactlyOneResponsiveClassApplies(float width,float height,string expected)
        {
            var shell=new LcarsShell(new VisualElement(),new ProductionOverviewView(),_=>{},()=>{},()=>{});shell.ApplyLogicalSize(width,height);
            Assert.That(shell.ClassListContains(expected),Is.True);
            Assert.That(System.Linq.Enumerable.Count(new[]{"layout-compact","layout-standard","layout-wide"},shell.ClassListContains),Is.EqualTo(1));
            Assert.That(shell.DetailsFrame.Heading,Is.Not.Null);
        }
        [Test]
        public void ShellResizeLeavesAudioDirectorSingleAndItsSourcesUnchanged()
        {
            AudioDirector director=null;
            try
            {
                director=AudioDirector.TryGetOrCreate();Assert.That(director,Is.Not.Null);
                var sources=director.GetComponents<AudioSource>();var shell=new LcarsShell(new VisualElement(),new ProductionOverviewView(),_=>{},()=>{},()=>{});
                foreach(var width in new[]{1422,1920,2580,3840,1422})shell.ApplyLogicalSize(width,width==1422?800:1080);
                Assert.That(AudioDirector.TryGetOrCreate(),Is.SameAs(director));
                Assert.That(director.GetComponents<AudioSource>(),Is.EqualTo(sources));Assert.That(sources.Length,Is.EqualTo(2));
            }
            finally{if(director!=null)Object.DestroyImmediate(director.gameObject);}
        }
        [Test]
        public void ProductionWorldsAndRealQueueRemainInSameViewAcrossDrawerAndDockModes()
        {
            var host=new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);
            Assert.That(host.Client.Enqueue(host.Snapshot,new HobbyDS9.Contracts.EntityId("civ.federation"),new HobbyDS9.Contracts.EntityId("colony.sol"),new HobbyDS9.Contracts.DefinitionId("ship-design.scout")).Accepted,Is.True);
            var model=new ProductionOverview(host.Client,host.Snapshot);var production=new ProductionOverviewView();production.Present(model,operation=>operation());
            var shell=new LcarsShell(new GalaxyMap(),production,_=>{},()=>{},()=>{});var before=host.SaveJson();shell.OpenProduction();
            foreach(var width in new[]{1422,1920,2580,3840})
            {
                shell.ApplyLogicalSize(width,width==1422?800:1080);
                foreach(var world in model.Worlds)Assert.That(production.Q<Foldout>(world.ColonyId.Value),Is.Not.Null);
                foreach(var item in model.Worlds[1].ProductionQueue)Assert.That(production.Q<VisualElement>(item.QueueItemId.Value),Is.Not.Null);
            }
            Assert.That(host.SaveJson(),Is.EqualTo(before));Assert.That(shell.ProductionFrame.Body,Is.SameAs(production));
        }
        [Test]
        public void RepeatedLogicalLayoutChangesDoNotDuplicateControls()
        {
            var shell = new LcarsShell(
                new VisualElement(),
                new ProductionOverviewView(),
                _ => { },
                () => { },
                () => { });

            var count = shell.Query<Button>().ToList().Count;

            for (var i = 0; i < 5; i++)
            {
                foreach (var width in new[] { 1422, 1920, 2580 })
                {
                    var height = width == 1422 ? 800 : 1080;

                    shell.ApplyLogicalSize(width, height);

                    Assert.That(
                        shell.Plan.Mode,
                        Is.EqualTo(
                            ResponsiveLayoutPolicy.ForLogicalSize(width, height).Mode));
                }
            }

            Assert.That(shell.Query<Button>().ToList().Count, Is.EqualTo(count));
            Assert.That(shell.Details, Is.InstanceOf<ScrollView>());
        }
        [TestCase(20,20)] [TestCase(104,64)] [TestCase(180,90)]
        public void VectorElbowAcceptsContentSizesWithoutFrameDrivenGeometry(float width,float height)
        {
            var elbow=new LcarsElbow();elbow.style.width=width;elbow.style.height=height;
            Assert.That(elbow.pickingMode,Is.EqualTo(PickingMode.Ignore));
            Assert.That(new ElbowMetrics(width,height).CanDraw,Is.True);
            Assert.DoesNotThrow(() => elbow.MarkDirtyRepaint());
        }
    }

}
