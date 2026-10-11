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
            Assert.That(rail.Sections.Count,Is.EqualTo(6));Assert.That(rail.Query<Button>().ToList().Count,Is.EqualTo(7));
            Assert.That(System.Linq.Enumerable.Count(rail.Sections,s=>s.Selected),Is.EqualTo(1));
            Assert.That(rail.Production.ClassListContains("production-open"),Is.True);
            Assert.That(rail.Production.ClassListContains("active-tab"),Is.False);Assert.That(production,Is.False);
            Assert.That(new LcarsTopBar(()=>{},()=>{}).EndTurn,Is.Not.Null);
            Assert.That(Resources.Load<StyleSheet>("LcarsTokens"),Is.Not.Null);
        }
        [TestCase(20,20)] [TestCase(104,64)] [TestCase(180,90)]
        public void VectorElbowAcceptsContentSizesWithoutFrameDrivenGeometry(float width,float height)
        {
            var elbow=new LcarsElbow();elbow.style.width=width;elbow.style.height=height;
            Assert.That(elbow.pickingMode,Is.EqualTo(PickingMode.Ignore));Assert.That(new ElbowMetrics(width,height).CanDraw,Is.True);
            // Construction/metrics only; actual Painter2D visual output needs local rendering.
            elbow.MarkDirtyRepaint();
        }
    }
}
