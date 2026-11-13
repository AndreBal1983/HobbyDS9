using HobbyDS9.Contracts;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class ColonisationExpansionInventoryTests
{
    [Fact]public void LegacyGeneratedVariantAndMinorUnitsDoNotMatchActualMajorSpecies()
    {
        var p=ColonisationExpansionTests.Package();var old=CampaignTacticalReviewSession.StartColonisationRoster(p.BaseRosterV2!,PublishedConstructionProfileTests.Settings(250)).ExportColonisationSave();
        var majors=p.BaseRosterV2!.BaseV1!.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Species!.Where(s=>s.DefinitionId=="species.human"||s.DefinitionId=="species.cardassian").ToList();
        Assert.Equal(2,majors.Count);Assert.All(majors,s=>{Assert.True(s.MinimumGravity>=4000);Assert.True(s.MinimumPressure>=3000);Assert.DoesNotContain("atmosphere.oxygen",s.Atmospheres!);});
        var variants=old.FrozenPlanets!.Definitions!.Where(d=>new[]{"Temperate","Arid","Oceanic","Frozen","Vacuum"}.Contains(d.PlanetClass)&&d.Gravity<1000).ToList();
        Assert.Equal(5,variants.Select(d=>d.PlanetClass).Distinct().Count());
        Assert.All(variants,d=>Assert.All(majors,s=>Assert.All(d.Habitats!,h=>Assert.Equal(0,WorldEconomy.Suitability(s,d,h.Kind)))));
        var minorDefinitions=old.FrozenPlanets.Bodies!.Where(b=>b.MinorActorId!=null).Select(b=>old.FrozenPlanets.Definitions!.Single(d=>d.DefinitionId==old.FrozenPlanets.InitialPlanets!.Single(p=>p.PlanetId==b.PlanetId).DefinitionId));
        Assert.All(minorDefinitions,d=>{Assert.Equal(100,d.Gravity);Assert.Equal(100,d.Pressure);Assert.Equal("atmosphere.oxygen",d.Atmosphere);});
    }

    [Fact]public void NewTypedInventoryChangesOnlyDeclaredUnitsAndVocabularyWithExactExpectedPolicy()
    {
        var package=ColonisationExpansionTests.Package();var settings=PublishedConstructionProfileTests.Settings(250);
        var old=CampaignTacticalReviewSession.StartColonisationRoster(package.BaseRosterV2!,settings).ExportColonisationSave();
        var session=CampaignTacticalReviewSession.StartColonisationExpansion(package,settings);var root=session.ExportColonisationExpansionSave();var next=root.BaseCampaign!;
        var before=old.FrozenPlanets!;var after=next.FrozenPlanets!;Assert.Equal(CampaignColonisationIdentity.InventoryPolicy,before.PolicyId);Assert.Equal(CampaignColonisationExpansionIdentity.InventoryPolicy,after.PolicyId);
        Assert.Equal(ColonisationExpansionTests.Json(before.InitialPlanets!),ColonisationExpansionTests.Json(after.InitialPlanets!));
        Assert.Equal(ColonisationExpansionTests.Json(before.Bodies!),ColonisationExpansionTests.Json(after.Bodies!));Assert.Equal(ColonisationExpansionTests.Json(before.GenesisControls!),ColonisationExpansionTests.Json(after.GenesisControls!));
        Assert.NotEqual(before.ContentHash,after.ContentHash);var changed=0;
        foreach(var original in before.Definitions!)
        {
            var actual=after.Definitions!.Single(d=>d.DefinitionId==original.DefinitionId);var restored=PopulationData.Copy(actual);
            if(original.DefinitionId.StartsWith("planet-definition.colonisation.",StringComparison.Ordinal)&&original.Gravity<1000)
            {
                changed++;Assert.Equal(original.Gravity*100,actual.Gravity);Assert.Equal(original.Pressure*100,actual.Pressure);
                Assert.Equal(original.Atmosphere=="atmosphere.oxygen"?"atmosphere.oxygen-nitrogen":original.Atmosphere,actual.Atmosphere);
                restored.Gravity=original.Gravity;restored.Pressure=original.Pressure;restored.Atmosphere=original.Atmosphere;
            }
            Assert.Equal(ColonisationExpansionTests.Json(original),ColonisationExpansionTests.Json(restored));
        }
        Assert.True(changed>200);Assert.Throws<ArgumentException>(()=>ColonisationData.InventoryHash(after));
        Assert.Throws<ArgumentException>(()=>ColonisationData.InventoryHash(before,ColonisationInventoryVersion.Expansion));
        Assert.Equal(after.ContentHash,ColonisationData.InventoryHash(after,ColonisationInventoryVersion.Expansion));
        var species=package.BaseRosterV2!.BaseV1!.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Species!.Single(s=>s.DefinitionId=="species.cardassian");
        foreach(var kind in new[]{"Temperate","Arid","Oceanic"})Assert.Contains(after.Definitions!,d=>d.PlanetClass==kind&&d.Habitats!.Any(h=>h.NaturalCapacity>=100&&WorldEconomy.Suitability(species,d,h.Kind)>0));
        Assert.All(after.Definitions!.Where(d=>d.PlanetClass=="Vacuum"),d=>Assert.All(d.Habitats!,h=>Assert.Equal(0,h.NaturalCapacity)));
        Assert.Equal(ColonisationExpansionTests.Json(root),ColonisationExpansionTests.Json(CampaignTacticalReviewSession.LoadColonisationExpansion(package,root).ExportColonisationExpansionSave()));
        foreach(var edit in new Action<FrozenColonisationPlanetsDto>[] { f=>f.PolicyId=CampaignColonisationIdentity.InventoryPolicy,f=>f.Definitions![0].Gravity++,f=>f.Definitions!.Last().Deposits!.Add(new(){ResourceDefinitionId="resource.dilithium",Amount=1}) })
        {
            var forged=System.Text.Json.JsonSerializer.Deserialize<CampaignColonisationExpansionSaveDto>(ColonisationExpansionTests.Json(root))!;edit(forged.BaseCampaign!.FrozenPlanets!);
            if(forged.BaseCampaign.FrozenPlanets!.PolicyId==CampaignColonisationExpansionIdentity.InventoryPolicy)
                forged.BaseCampaign.FrozenPlanets.ContentHash=ColonisationData.InventoryHash(forged.BaseCampaign.FrozenPlanets,ColonisationInventoryVersion.Expansion);
            Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadColonisationExpansion(package,forged));
        }
        Assert.Equal(ColonisationExpansionTests.Json(old),ColonisationExpansionTests.Json(CampaignTacticalReviewSession.LoadColonisationRoster(package.BaseRosterV2!,old).ExportColonisationSave()));
    }
}
