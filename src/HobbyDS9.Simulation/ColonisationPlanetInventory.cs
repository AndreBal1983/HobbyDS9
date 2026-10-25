using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Frozen start authoring only. Survey/deployment never creates or improves a planet.
    internal static class ColonisationPlanetInventory
    {
        internal static CampaignColonisationStateDto Populate(DefinitionCatalogDto definitions,GameStateDto game,
            NaturalGalaxyDocumentDto map,CampaignActivityStateDto activities,ColonisationPolicyDto policy)
        {
            if(policy.PlanetInventoryPolicy!=CampaignColonisationIdentity.InventoryPolicy||policy.MaximumPlanets!=1040
                ||policy.MaximumStarterPlanets!=32||game.Planets==null||game.Planets.Count>policy.MaximumStarterPlanets
                ||map.Layout.Systems.Count>1000||activities.Bodies==null||activities.Minors==null||activities.Bodies.Count!=6)
                throw new ArgumentException("Unsupported frozen planet inventory policy or inputs.");
            var result=new CampaignColonisationStateDto();
            var originals=game.Planets.ToList();
            var homeEnvironments=game.Colonies!.GroupBy(c=>c.OwnerCivilizationId!,StringComparer.Ordinal)
                .ToDictionary(g=>g.Key,g=>PopulationData.Copy(definitions.Planets!.Single(d=>d.DefinitionId==originals.Single(p=>p.PlanetId==g.OrderBy(c=>c.ColonyId,StringComparer.Ordinal).First().PlanetId).DefinitionId)),StringComparer.Ordinal);
            var seed=map.Settings.Map.Seed;
            string Rank(string domain,string id)=>CampaignActivitiesData.Identity(CampaignColonisationIdentity.InventoryPolicy,domain,seed,id);
            List<ResourceAmountDto> Deposit(long amount)=>amount==0?new List<ResourceAmountDto>():new List<ResourceAmountDto>{new ResourceAmountDto{ResourceDefinitionId="resource.dilithium",Amount=amount}};
            void Capacity(PlanetDefinitionDto d)
            {
                // Bounded frontier habitat, not a copy of an established home world's capacity.
                var positive=d.Habitats!.Where(h=>h.ShareBps>0).OrderBy(h=>h.Kind).ToList();
                foreach(var h in d.Habitats!)h.NaturalCapacity=0;
                if(d.PlanetClass=="Vacuum")return;
                long remaining=1000;
                for(int i=0;i<positive.Count;i++){var value=i==positive.Count-1?remaining:1000L*positive[i].ShareBps/10000;positive[i].NaturalCapacity=value;remaining-=value;}
            }
            PlanetStateDto Add(string system,string suffix,PlanetDefinitionDto? environment=null)
            {
                var id="planet.colonisation."+suffix;var definition="planet-definition.colonisation."+suffix;
                if(game.Planets.Any(p=>p.PlanetId==id)||definitions.DefinitionIds.Contains(definition,StringComparer.Ordinal))
                    throw new ArgumentException("Duplicate frozen planet identity.");
                var variant=Convert.ToInt32(Rank("environment",id).Substring(0,2),16)%5;
                var d=environment==null?new PlanetDefinitionDto {
                    PlanetClass=new[]{"Temperate","Arid","Oceanic","Frozen","Vacuum"}[variant],
                    Gravity=new[]{100,120,90,80,30}[variant],Pressure=new[]{100,40,150,20,0}[variant],
                    Radiation=new[]{10,30,20,50,200}[variant],Atmosphere=variant==4?"atmosphere.vacuum":"atmosphere.oxygen",
                    Habitats=Enum.GetValues(typeof(HabitatKind)).Cast<HabitatKind>().Select(h=>new HabitatDefinitionDto {
                        Kind=h,ShareBps=h==new[]{HabitatKind.Temperate,HabitatKind.Arid,HabitatKind.Oceanic,HabitatKind.Polar,HabitatKind.Volcanic}[variant]?10000:0,
                        NaturalCapacity=h==new[]{HabitatKind.Temperate,HabitatKind.Arid,HabitatKind.Oceanic,HabitatKind.Polar,HabitatKind.Volcanic}[variant]&&variant!=4?1000:0}).ToList(),
                    Deposits=new List<ResourceAmountDto>()}:PopulationData.Copy(environment);
                d.DefinitionId=definition;d.SystemDefinitionId=game.StarSystems!.Single(s=>s.SystemId==system).DefinitionId;
                d.DisplayName=map.Layout.Systems.Single(s=>s.SystemId==system).DisplayName+" / "+suffix;
                Capacity(d);
                // Finite physical deposits, not delivered stock or a survey/deployment reward.
                d.Deposits=Deposit(new long[]{0,120,240}[Convert.ToInt32(Rank("deposits",id).Substring(0,2),16)%3]);
                var p=new PlanetStateDto{PlanetId=id,DefinitionId=definition,SystemId=system,RemainingDeposits=d.Deposits.Select(PopulationData.Copy).ToList()};
                definitions.Planets!.Add(d);definitions.DefinitionIds.Add(definition);game.Planets.Add(p);
                game.EntityIdentities!.Add(new EntityIdentityDto{EntityId=id,DefinitionId=definition});return p;
            }
            foreach(var system in map.Layout.Systems.OrderBy(s=>s.SystemId,StringComparer.Ordinal))
                if(!game.Planets.Any(p=>p.SystemId==system.SystemId))Add(system.SystemId,"primary."+system.SystemId);
            foreach(var body in activities.Bodies.OrderBy(b=>b.BodyId,StringComparer.Ordinal))
            {
                var planet=Add(body.SystemId,"body."+body.BodyId);
                var minor=activities.Minors.SingleOrDefault(m=>m.BodyId==body.BodyId);
                if(body.GovernmentActorId!=minor?.ActorId)throw new ArgumentException("Different frozen minor body association.");
                if(minor!=null)
                {
                    var habitat=minor.SpeciesId=="species.ilex.v1"?HabitatKind.Wetland:minor.SpeciesId=="species.namar.v1"?HabitatKind.Temperate
                        :throw new ArgumentException("Unsupported minor physical environment binding.");
                    var environment=definitions.Planets!.Single(d=>d.DefinitionId==planet.DefinitionId);
                    environment.PlanetClass=habitat==HabitatKind.Wetland?"Wetland":"Temperate";
                    environment.Gravity=100;environment.Pressure=100;environment.Radiation=10;environment.Atmosphere="atmosphere.oxygen";
                    foreach(var h in environment.Habitats!){h.ShareBps=h.Kind==habitat?10000:0;h.NaturalCapacity=h.Kind==habitat?1000:0;}
                }
                result.Bodies!.Add(new PlanetBodyBindingDto{BodyId=body.BodyId,PlanetId=planet.PlanetId,SystemId=body.SystemId,MinorActorId=minor?.ActorId});
            }
            var excluded=new HashSet<string>(activities.Bodies.Select(b=>b.SystemId).Concat(map.Layout.HomeCandidates.Select(h=>h.SystemId)),StringComparer.Ordinal);
            var neighbors=GeneratedGalaxyData.Neighbors(map.Layout.Regions,map.Layout.Connections);
            foreach(var actor in game.Civilizations!.OrderBy(c=>c.CivilizationId,StringComparer.Ordinal))
            {
                var home=map.Layout.HomeCandidates.Single(h=>h.CivilizationDefinitionId==actor.DefinitionId);
                var distances=GeneratedGalaxyData.Distances(neighbors,home.NodeId);
                var candidates=map.Layout.Systems.Where(s=>!excluded.Contains(s.SystemId)&&distances.TryGetValue(s.NodeId,out var hops)&&hops>=2&&hops<=4)
                    .OrderBy(s=>Rank("opportunity."+actor.CivilizationId,s.SystemId),StringComparer.Ordinal).Take(2).ToList();
                if(candidates.Count!=2)throw new ArgumentException("Frozen map has insufficient near-frontier settlement opportunities.");
                for(var i=0;i<candidates.Count;i++)
                {
                    var system=candidates[i];excluded.Add(system.SystemId);
                    var primary=game.Planets.Single(p=>p.SystemId==system.SystemId);
                    var suitable=PopulationData.Copy(homeEnvironments[actor.CivilizationId]);
                    suitable.DefinitionId=primary.DefinitionId;suitable.DisplayName=definitions.Planets!.Single(d=>d.DefinitionId==primary.DefinitionId).DisplayName;
                    suitable.SystemDefinitionId=game.StarSystems!.Single(s=>s.SystemId==system.SystemId).DefinitionId;suitable.Deposits=Deposit(240);Capacity(suitable);
                    primary.RemainingDeposits=suitable.Deposits.Select(PopulationData.Copy).ToList();
                    definitions.Planets![definitions.Planets.FindIndex(d=>d.DefinitionId==primary.DefinitionId)]=suitable;
                    if(i==0)Add(system.SystemId,"sibling."+system.SystemId);
                }
            }
            foreach(var planet in game.Planets.OrderBy(p=>p.PlanetId,StringComparer.Ordinal))
                if(!result.Bodies!.Any(b=>b.PlanetId==planet.PlanetId))result.Bodies!.Add(new PlanetBodyBindingDto{
                    PlanetId=planet.PlanetId,SystemId=planet.SystemId,BodyId="body.physical."+planet.PlanetId});
            foreach(var system in game.StarSystems!)system.OwnerCivilizationId=null;
            foreach(var planet in game.Planets.OrderBy(p=>p.PlanetId,StringComparer.Ordinal))
            {
                var colony=game.Colonies!.SingleOrDefault(c=>c.PlanetId==planet.PlanetId);
                var minor=result.Bodies!.SingleOrDefault(b=>b.PlanetId==planet.PlanetId)?.MinorActorId;
                result.PlanetControls!.Add(new PlanetControlDto{PlanetId=planet.PlanetId,Kind=colony!=null?PlanetControllerKind.Empire:minor!=null?PlanetControllerKind.IndependentGovernment:PlanetControllerKind.Unclaimed,ActorId=colony?.OwnerCivilizationId??minor});
            }
            if(game.Planets.Count>policy.MaximumPlanets||game.Planets.Count>map.Layout.Systems.Count+originals.Count+8)
                throw new ArgumentException("Frozen physical planet inventory exceeds its technical bound.");
            definitions.DefinitionIds=definitions.DefinitionIds.OrderBy(id=>id,StringComparer.Ordinal).ToList();
            game.Planets=game.Planets.OrderBy(p=>p.PlanetId,StringComparer.Ordinal).ToList();
            game.EntityIdentities=game.EntityIdentities!.OrderBy(e=>e.EntityId,StringComparer.Ordinal).ToList();
            return result;
        }
    }
}
