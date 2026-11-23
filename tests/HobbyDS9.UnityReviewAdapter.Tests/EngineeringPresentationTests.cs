using HobbyDS9.UnityReview;
using HobbyDS9.Contracts;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class EngineeringPresentationTests
{
    [Fact]public void EveryAuthoredProjectAppearsOnceInTheTwoSupportedSections()
    {
        var source=JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"scenarios/engineering-development-v1/scenario.json")));
        var rows=source.Descendants().OfType<JObject>().Where(o=>o["BaseEngineeringCost"]!=null&&o["DefinitionId"]!=null).ToList();
        Assert.Equal(20,rows.Count);Assert.Equal(20,rows.Select(o=>(string)o["DefinitionId"]!).Distinct().Count());
        Assert.Equal(12,rows.Count(o=>EngineeringPresentation.IsRefinement((EngineeringProjectKind)(int)o["Kind"]!)));
        Assert.Equal(8,rows.Count(o=>!EngineeringPresentation.IsRefinement((EngineeringProjectKind)(int)o["Kind"]!)));
        Assert.All(rows,o=>Assert.NotEqual("Other technologies",EngineeringPresentation.Category((string)o["DefinitionId"]!)));
        Assert.Equal("Other technologies",EngineeringPresentation.Category("engineering.future-project"));
        Assert.False(EngineeringPresentation.IsRefinement(EngineeringProjectKind.CivilApplication));
    }
    [Theory][InlineData(6000,0,0,600,10)][InlineData(6000,1200,3000,600,3)]
    [InlineData(6000,0,6000,0,1)][InlineData(6000,0,5999,600,1)][InlineData(6000,6000,0,0,0)]
    [InlineData(long.MaxValue,0,0,long.MaxValue-1,2)]
    public void ConditionalEstimateUsesAcceptedBankAndIncomeWithoutOverflow(long cost,long progress,long reserve,long income,long expected)
        =>Assert.Equal(expected,EngineeringPresentation.ResearchPhases(cost,progress,reserve,income));
    [Fact]public void NoFiniteEstimateWithoutIncomeOrSufficientReserve()
        =>Assert.Null(EngineeringPresentation.ResearchPhases(6000,1200,0,0));
}
