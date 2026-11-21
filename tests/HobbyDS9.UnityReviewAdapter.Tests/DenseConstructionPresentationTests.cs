using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class DenseConstructionPresentationTests
{
    [Fact]public void MissingFactsZeroAndSupportedUnarmedRemainDistinct()
    {
        Assert.Equal("?",DenseConstructionPresentation.Number(null));Assert.Equal("0",DenseConstructionPresentation.Number(0));
        Assert.Equal("?",DenseConstructionPresentation.Arms(null));Assert.Equal("?",DenseConstructionPresentation.Arms(new()));
        Assert.Equal("Unarmed",DenseConstructionPresentation.Arms(new(){Weapons=new()}));
        Assert.Equal("2",DenseConstructionPresentation.Arms(new(){Weapons=new(){new(){Powered=false},new(){Powered=true}}}));
        Assert.Equal("+3",DenseConstructionPresentation.Delta(3));Assert.Equal("0",DenseConstructionPresentation.Delta(0));Assert.Equal("-2",DenseConstructionPresentation.Delta(-2));
    }
    [Fact]public void MaterialTooltipKeepsFullRequirementsAndCurrentLocalStocksWithoutQueuePolicy()
    {
        var required=new[]{new ResourceAmountDto{ResourceDefinitionId="resource.dilithium",Amount=12}};
        var local=new[]{new ResourceAmountSnapshot(new(){ResourceDefinitionId="resource.dilithium",Amount=2})};
        Assert.Equal("DIL 12",DenseConstructionPresentation.Materials(required));
        var detail=DenseConstructionPresentation.MaterialDetails(required,local);
        Assert.Contains("Dilithium: required 12 / local 2",detail);Assert.Contains("shortage pauses progress",detail);
        Assert.Equal(12,required[0].Amount);Assert.Equal(2,local[0].Amount);
        Assert.DoesNotContain("afford",detail,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("cannot",detail,StringComparison.OrdinalIgnoreCase);
    }
    [Fact]public void NoMaterialCostDoesNotInventAStockAndLargeIndustryStaysExact()
    {
        Assert.Equal("None",DenseConstructionPresentation.Materials(null));Assert.Equal("None",DenseConstructionPresentation.Materials(Array.Empty<ResourceAmountDto>()));
        Assert.Equal("No construction materials required.",DenseConstructionPresentation.MaterialDetails(null,Array.Empty<ResourceAmountSnapshot>()));
        Assert.Equal("1,234,567",DenseConstructionPresentation.Number(1234567));
    }
}
