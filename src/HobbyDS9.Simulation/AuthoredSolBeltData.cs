using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // New-edition authoring only. Existing richness-v1 genesis/replay must not call this.
    internal static class AuthoredSolBeltData
    {
        internal const string Version="authored-sol-main-belt-v1";
        internal const string FeatureId=CampaignAuthoredSolIdentity.FeatureId;
        internal const string SourceUrl="https://science.nasa.gov/mission/dawn/faq/";
        internal const string SourceQuestionDate="2010-06-13";
        internal const int InnerRadiusMilliAu=2200,OuterRadiusMilliAu=3200;

        // NASA's Dawn FAQ uses an approximate 2.2-3.2 AU band for an illustration,
        // not exact dynamical edges, a current ephemeris, density or mineable stock.
        internal static SystemAsteroidBeltDto? Create(FrozenColonisationPlanetsDto inventory,string mapSeed)
        {
            if(inventory==null)throw new ArgumentNullException(nameof(inventory));
            if(string.IsNullOrEmpty(mapSeed)||mapSeed.Length>128)throw new ArgumentException("A bounded map seed is required.",nameof(mapSeed));
            if(inventory.InitialPlanets==null||inventory.Definitions==null||inventory.Bodies==null)return null;
            if(inventory.InitialPlanets.Count>CampaignSystemRichnessIdentity.MaximumPlanets||inventory.Definitions.Count>CampaignSystemRichnessIdentity.MaximumPlanets||inventory.Bodies.Count>CampaignSystemRichnessIdentity.MaximumPlanets)return null;
            string? system=null,systemDefinition=null;
            var bodyIds=new HashSet<string>(StringComparer.Ordinal);
            foreach(var suffix in new[]{"sol","mars","jupiter"})
            {
                var id="planet."+suffix;var definition="planet-definition."+suffix;
                var rows=inventory.InitialPlanets.Where(p=>p!=null&&p.PlanetId==id).ToArray();
                var bindings=inventory.Bodies.Where(b=>b!=null&&b.PlanetId==id).ToArray();
                var definitions=inventory.Definitions.Where(d=>d!=null&&d.DefinitionId==definition).ToArray();
                if(rows.Length!=1||bindings.Length!=1||definitions.Length!=1)return null;
                var planet=rows[0];var body=bindings[0];var environment=definitions[0];
                if(planet.DefinitionId!=definition||!EntityId.TryParse(planet.SystemId,out _)
                    ||body.SystemId!=planet.SystemId||!EntityId.TryParse(body.BodyId,out _)||!bodyIds.Add(body.BodyId)
                    ||!EntityId.TryParse(environment.SystemDefinitionId,out _))return null;
                if(system!=null&&(planet.SystemId!=system||environment.SystemDefinitionId!=systemDefinition))return null;
                system=planet.SystemId;systemDefinition=environment.SystemDefinitionId;
            }
            return new SystemAsteroidBeltDto{FeatureId=FeatureId,SystemId=system!,InnerRadiusMilliAu=InnerRadiusMilliAu,OuterRadiusMilliAu=OuterRadiusMilliAu,
                VisualSeed=CampaignActivitiesData.Identity(Version,mapSeed,system!,FeatureId,"visual-scatter")};
        }
    }
}
