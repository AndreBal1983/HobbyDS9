#nullable enable
using System;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    // The actual generated-campaign view, not ReviewConstructionCatalogue.
    // Detached observation fixture; no native scene, host save or gameplay session.
    public sealed class GeneratedConstructionLedgerTests
    {
        private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        private static object? Call(object view,string method,params object[] args)=>typeof(ReviewColonisationPanel).GetMethod(method,Private)!.Invoke(view,args);
        private static string Labels(VisualElement view)=>string.Join("\n",view.Query<Label>().ToList().Select(l=>l.text));
        private static ReviewColonisationPanel Create(out CampaignActivityObservationDto activities,Action<CampaignActivityCommandDto> submit,bool shipyard=true)
        {
            var world=new ColonisationWorldSnapshot("colony.a","planet.a","system.a","Home",ColonyEstablishmentStage.Established,null,20,15,10,2,4,0,0,0,0,"",Array.Empty<ResourceAmountSnapshot>(),Array.Empty<ColonisationCohortSnapshot>(),shipyard);
            var queue=new[]{new ColonisationQueueSnapshot("order.first","colony.a",ColonisationProductionKind.PublishedShip,"ship.0","First order",1,100,12,"Working",Array.Empty<ResourceAmountSnapshot>(),Array.Empty<ResourceAmountSnapshot>())};
            var benefits=Enumerable.Range(0,4).Select(i=>new InfrastructureDefinitionSnapshot(new InfrastructureDefinitionDto{DefinitionId="building."+i,DisplayName="Installation "+i,FoodOutput=i,Industry=2,Research=0,Extraction=new(),AccessibleHabitats=new()}));
            var campaign=new ColonisationCampaignSnapshot("civ.a",7,19,"fixture",new[]{world},Array.Empty<ColonisationTransportSnapshot>(),queue,Array.Empty<ColonisationPresenceSnapshot>(),Array.Empty<ColonisationManifestReport>(),Array.Empty<ColonisationSurveySnapshot>(),Array.Empty<ColonisationIntentSnapshot>(),20,Array.Empty<ResourceAmountSnapshot>(),buildingBenefits:benefits);
            activities=new CampaignActivityObservationDto();
            foreach(var i in Enumerable.Range(0,8))activities.BuildOptions.Add(new(){Kind=i<4?ActivityProductionKind.PublishedShip:ActivityProductionKind.Infrastructure,DefinitionId=i<4?"ship."+i:"building."+(i-4),BlueprintRevision=i<4?1:0,DisplayName=(i<4?"Class ":"Installation ")+i,Supported=true,Cost=new(){Industry=100+i,Resources=new(){new(){ResourceDefinitionId="resource.dilithium",Amount=2}}},Comparison=i<4?new(){StudyWork=0,ContactCapacity=1,MaxHull=30,ShieldCapacity=0,Weapons=new()}:null});
            return new ReviewColonisationPanel(campaign,Array.Empty<ColonisationPlanetSnapshot>(),activities,(_,__)=>Assert.Fail("Unexpected review"),_=>Assert.Fail("Unexpected colony submission"),(_,__)=>Assert.Fail("Unexpected activity review"),submit);
        }
        [Test]public void BothCataloguesUseAlignedDenseRowsAndMoveExtendedFactsIntoSelectedInspector()
        {
            var sent=0;using var panel=Create(out var activities,_=>sent++);
            var rows=panel.Query(className:"dense-construction-row").ToList().Where(r=>r.name.StartsWith("construction-ledger-row-",StringComparison.Ordinal)).ToArray();
            Assert.That(rows.Length,Is.EqualTo(8));
            Assert.That(rows.Sum(r=>r.style.height.value.value+r.style.marginBottom.value.value),Is.LessThanOrEqualTo(432),"Eight choices fit in the source density budget; native font/layout proof is separate.");
            foreach(var row in rows)
            {
                Assert.That(row.Query<Foldout>().ToList(),Is.Empty);Assert.That(row.Q<Button>().style.minHeight.value.value,Is.GreaterThanOrEqualTo(44));
                Assert.That(row.Q(className:"dense-construction-industry").style.width.value.value,Is.EqualTo(64));
                Assert.That(row.Query<Button>().ToList().Count,Is.EqualTo(2),"Inspect name and Add remain distinct controls.");
            }
            foreach(var id in new[]{"construction-ledger-PublishedShip","construction-ledger-Infrastructure"})
            {var ledger=panel.Q(id);Assert.That(ledger.Q(className:"dense-construction-columns"),Is.Not.Null);Call(panel,"FitConstructionLedger",ledger,true);Assert.That(rows.First(r=>ledger.Contains(r)).style.minHeight.value.value,Is.GreaterThanOrEqualTo(100));Call(panel,"FitConstructionLedger",ledger,false);}
            Assert.That(Labels(rows[0]),Does.Contain("0").And.Contain("Unarmed").And.Not.Contain("Maximum hull"));
            Call(panel,"InspectHull",panel.Q("construction-selected-hull"),activities.BuildOptions[0]);
            Assert.That(Labels(panel.Q("construction-selected-hull")),Does.Contain("Maximum hull · 30").And.Contain("Shield capacity · 0"));
            Assert.That(panel.Q("colonisation-hull-image").style.height.value.value,Is.EqualTo(180));
            Call(panel,"InspectInstallation",panel.Q("construction-selected-hull"),activities.BuildOptions[5]);
            Assert.That(Labels(panel.Q("construction-selected-hull")),Does.Contain("Base additions / week: Food +1, Industry +2, Research 0."));
            Assert.That(panel.Q<Button>("colonisation-queue-cancel-order.first"),Is.Not.Null);Assert.That(sent,Is.Zero);
        }
        [Test]public void ShipyardAndUniqueBuildingGuardsSurviveDenseLayoutWithoutMaterialPrepaymentGate()
        {
            using var panel=Create(out var activities,_=>Assert.Fail("Unexpected order"),false);
            Assert.That(panel.Q<Button>("colonisation-build-0").enabledSelf,Is.False);
            Assert.That(panel.Q<Button>("colonisation-build-4").enabledSelf,Is.True,"No local dilithium does not prevent a streamed order.");
            activities.Structures.Add(new(){ColonyId="colony.a",DefinitionId="building.0"});Call(panel,"Show","Worlds");
            Assert.That(panel.Q<Button>("colonisation-build-4").enabledSelf,Is.False);
            Assert.That(panel.Q<Button>("colonisation-build-4").tooltip,Does.Contain("already built or queued"));
            Assert.That(panel.Q<Button>("colonisation-queue-cancel-order.first"),Is.Not.Null);
        }
    }
}
