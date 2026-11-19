#nullable enable
using System;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    public sealed class ConstructionCompactTests
    {
        [Test]public void SupportedComparisonBindsToRowAndSelectedInspectorWithoutSubmitting()
        {
            var observed=new CampaignStreamedActivityObservationDto();observed.Activities.Colonies.Add(new(){ColonyId="colony.a",DisplayName="World A"});
            var option=new ActivityBuildOptionDto{Kind=ActivityProductionKind.PublishedShip,DefinitionId="blueprint.a",BlueprintRevision=2,DisplayName="Explorer",Supported=true,Cost=new(){Industry=123},Comparison=new(){StudyWork=3,ContactCapacity=1,MaxHull=30,Armor=0,ShieldCapacity=0,Speed=30,MovementBudget=6,Weapons=new()}};
            observed.Activities.BuildOptions.Add(option);var sent=0;
            var type=typeof(ReviewEntry).Assembly.GetType("HobbyDS9.UnityReview.ReviewConstructionCatalogue",true)!;
            var view=(VisualElement)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{observed,"colony.a",(Action<CampaignActivityCommandDto>)(_=>sent++),(Action<string>)(_=>sent++)},null)!;
            try
            {
                var row=view.Q<Label>("construction-capabilities-0");StringAssert.Contains("Study work 3",row.text);StringAssert.Contains("Shields 0",row.text);StringAssert.Contains("Unarmed",row.text);
                type.GetMethod("Inspect",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(view,new object[]{option});
                var detail=view.Q("construction-comparison-details");Assert.That(detail,Is.Not.Null);
                Assert.That(detail.Query<Label>().ToList().Any(l=>l.text=="Maximum hull · 30"),Is.True);Assert.That(detail.Query<Label>().ToList().Any(l=>l.text=="Weapons · Unarmed"),Is.True);
                Assert.That(sent,Is.Zero);
            }
            finally{((IDisposable)view).Dispose();}
        }
        [Test]public void ComparableRowsKeepRealCostsBuildingChangesAndUnavailableShipCapabilities()
        {
            var observation=new CampaignStreamedActivityObservationDto();
            observation.Activities.Colonies.Add(new ActivityOwnColonyDto{ColonyId="colony.a",DisplayName="World A"});
            observation.Activities.BuildOptions.Add(new ActivityBuildOptionDto{Kind=ActivityProductionKind.PublishedShip,DefinitionId="blueprint.a",BlueprintRevision=2,DisplayName="Explorer",Supported=true,Cost=new(){Industry=123,Resources=new(){new(){ResourceDefinitionId="resource.dilithium",Amount=2}}}});
            observation.Activities.BuildOptions.Add(new ActivityBuildOptionDto{Kind=ActivityProductionKind.Infrastructure,DefinitionId="infrastructure.a",DisplayName="Fabricator",Supported=true,Cost=new(){Industry=60}});
            observation.Activities.BuildOptions.Add(new ActivityBuildOptionDto{Kind=ActivityProductionKind.Infrastructure,DefinitionId="infrastructure.b",DisplayName="Existing laboratory",Supported=true,Cost=new(){Industry=75}});
            observation.Construction.Add(new(){ColonyId="colony.a",Current=new(){Industry=18,Research=8,Food=4},Buildings=new(){new(){DefinitionId="infrastructure.a",BuiltCount=0,QueuedCount=0,AfterOne=new(){Status=StreamedConstructionProjectionStatus.Available,Output=new(){Industry=23,Research=10,Food=3}}},new(){DefinitionId="infrastructure.b",BuiltCount=1,QueuedCount=0,Unavailable=StreamedBuildingUnavailable.AlreadyBuilt}}});
            var submitted=0;var cancelled=0;
            var type=typeof(ReviewEntry).Assembly.GetType("HobbyDS9.UnityReview.ReviewConstructionCatalogue",true)!;
            var view=(VisualElement)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{observation,"colony.a",(Action<CampaignActivityCommandDto>)(_=>submitted++),(Action<string>)(_=>cancelled++)},null)!;
            try
            {
                Assert.That(view.Q<Label>("construction-industry-0").text,Is.EqualTo("123 Industry"));
                Assert.That(view.Q<Label>("construction-materials-0").text,Is.EqualTo("Materials: Dilithium 2"));
                Assert.That(view.Q<Label>("construction-capabilities-0").text,Is.EqualTo("Exploration unavailable · Combat unavailable"));
                var building=view.Q("construction-tile-1");
                Assert.That(building.Query<Label>().ToList().Any(l=>l.text=="0 built · 0 queued"),Is.True);
                Assert.That(building.Query<Label>().ToList().Any(l=>l.text=="Gain / week: Industry +5 · Research +2 · Food -1"),Is.True);
                Assert.That(building.Q<Button>("construction-add-1").enabledSelf,Is.True);
                Assert.That(view.Q<Button>("construction-add-2").enabledSelf,Is.False);
                Assert.That(view.Q<Button>("construction-add-0").enabledSelf,Is.True);
                Assert.That(view.Q<ScrollView>("construction-catalogue-scroll"),Is.Not.SameAs(view.Q<ScrollView>("construction-queue")));
                Assert.That(submitted,Is.Zero);Assert.That(cancelled,Is.Zero);
            }
            finally{((IDisposable)view).Dispose();}
        }
    }
}
