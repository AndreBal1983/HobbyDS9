using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private sealed class ActivitiesContext
        {
            internal readonly ActivityTacticalSupportCatalogueDto Support;
            internal readonly CampaignActivityContentDto Content;
            internal CampaignActivityStateDto State=new CampaignActivityStateDto();
            internal List<CampaignActivityOperationDto> Journal=new List<CampaignActivityOperationDto>();
            internal Dictionary<string,string> AuthoredActivityBlueprints=new Dictionary<string,string>(StringComparer.Ordinal);
            internal StreamedActivitiesContext? Streamed;
            internal ActivitiesContext(ActivityTacticalSupportCatalogueDto support,CampaignActivityContentDto content)
            {Support=ActivitySupportCopies.Copy(support);Content=CampaignActivitiesData.Copy(content);}
            internal ActivitiesContext Copy()
            {var copy=new ActivitiesContext(Support,Content){State=CampaignActivitiesData.Copy(State),Journal=Journal.Select(CopyActivityOperation).ToList(),AuthoredActivityBlueprints=new Dictionary<string,string>(AuthoredActivityBlueprints,StringComparer.Ordinal),Streamed=Streamed?.Copy()};return copy;}
        }
        private ActivitiesContext? activities;
        private static CampaignActivityOperationDto CopyActivityOperation(CampaignActivityOperationDto o)=>new CampaignActivityOperationDto{
            Common=o.Common==null?null:CopyPlayableOperation(o.Common),Activity=o.Activity==null?null:CampaignActivitiesData.Copy(o.Activity)};
        private int UsedPlayableOperations=>colonisation?.Journal.Count??activities?.Journal.Count??playableTranscript.Count;
        public static CampaignActivitiesPackageDto CreateActivitiesPackage(CampaignPlayableFleetPackageDto template,ShipDesignCatalogueDto catalogue,
            IReadOnlyList<ShipClassDraftDto> ready,ActivityTacticalSupportCatalogueDto support,CampaignActivityContentDto content,int recordBudget)
        {
            var package=new CampaignActivitiesPackageDto{BaseTemplate=template,Catalogue=BlueprintCopies.Copy(catalogue),ReadyClasses=ready.Select(BlueprintCopies.Copy).ToList(),
                Support=ActivitySupportCopies.Copy(support),Content=CampaignActivitiesData.Copy(content),RecordBudget=recordBudget};
            package.PackageHash=CampaignActivitiesData.PackageHash(package);return package;
        }
        public static CampaignTacticalReviewSession StartActivities(CampaignActivitiesPackageDto package,NaturalGalaxySettingsDto settings)
        {
            RequireActivitiesPackage(package);var catalog=package.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            var map=NaturalGalaxyGenerator.Generate(settings,catalog);return StartActivitiesFrozen(package,GeneratedCampaignFactory.Frozen(map,catalog));
        }
        private static void RequireActivitiesPackage(CampaignActivitiesPackageDto p)
        {Require(p!=null&&p.PackageHash==CampaignActivitiesData.PackageHash(p),"Different or incomplete activity package.");}
        private sealed class ReadyShip
        {
            internal readonly SupportedPublishedTacticalFit Tactical;
            internal string SupportHash=>Tactical.SupportHash;
            internal ReadyShip(SupportedPublishedTacticalFit tactical){Tactical=tactical;}
        }
        private static CampaignTacticalReviewSession StartActivitiesFrozen(CampaignActivitiesPackageDto p,NaturalGalaxyDocumentDto map,CampaignStreamedActivitiesPackageDto? streamed=null,CampaignColonisationPackageDto? colonisationPackage=null,FrozenColonisationPlanetsDto? retainedPlanets=null,ColonisationRosterSource? startingRoster=null,ExpansionSource? expansionSource=null)
        {
            var ready=colonisationPackage==null?CampaignActivitiesData.Ready(p).ToDictionary(r=>r.Key,r=>new ReadyShip(r.Value.Tactical),StringComparer.Ordinal)
                :ColonisationData.Ready(colonisationPackage).ToDictionary(r=>r.Key,r=>new ReadyShip(r.Value.Tactical),StringComparer.Ordinal);
            var catalogue=colonisationPackage?.Catalogue??p.Catalogue!;var readyClasses=colonisationPackage?.ReadyClasses??p.ReadyClasses!;
            CampaignColonisationStateDto? inventory=null;FrozenColonisationPlanetsDto? frozenPlanets=null;CampaignActivityStateDto? seeded=null;CampaignPlayableFleetPackageDto built;
            if(colonisationPackage!=null)
            {seeded=SeedColonisationActivities(p,map);var facts=expansionSource==null?GeneratedCampaignFactory.BuildColonisation(p.BaseTemplate!,map,startingRoster?.Hash??colonisationPackage.PackageHash,seeded,colonisationPackage.Settlement!,retainedPlanets):GeneratedCampaignFactory.BuildExpansionColonisation(p.BaseTemplate!,map,expansionSource.Hash,seeded,colonisationPackage.Settlement!,retainedPlanets);built=facts.Package;inventory=facts.Inventory;frozenPlanets=facts.Frozen;}
            else built=streamed==null?GeneratedCampaignFactory.BuildActivities(p.BaseTemplate!,map,p.PackageHash)
                :GeneratedCampaignFactory.BuildStreamedActivities(p.BaseTemplate!,map,streamed.PackageHash);
            var physical=built.Integrated!.Physical!;var core=physical.CorePackage!;var scenario=core.Campaign!;var game=scenario.InitialState;var d=scenario.Definitions;
            var previous=d.ShipDesignCatalogue!;var oldVersions=previous.Hulls!.Select(h=>h.VersionId).Concat(previous.Components!.Select(c=>c.VersionId)).ToHashSet(StringComparer.Ordinal);
            d.ShipDesignCatalogue=BlueprintCopies.Copy(catalogue);
            var versions=catalogue.Hulls!.Select(h=>h.VersionId).Concat(catalogue.Components!.Select(c=>c.VersionId)).ToList();
            d.ShipDesignVersionAccess=versions.Select(id=>new ShipDesignVersionAccessDto{VersionId=id,RequiredEngineeringApplicationDefinitionIds=new List<string>()}).ToList();
            // This explicit new package authors the ready catalogue's baseline access. No old campaign is upgraded.
            d.DefinitionIds=d.DefinitionIds.Where(id=>!oldVersions.Contains(id)).Concat(versions).Distinct(StringComparer.Ordinal).OrderBy(id=>id,StringComparer.Ordinal).ToList();
            foreach(var s in p.Content!.Structures!)
            {d.Infrastructure!.Add(new InfrastructureDefinitionDto{DefinitionId=s.DefinitionId,DisplayName=s.DisplayName,Industry=s.Industry,Research=s.Research,FoodOutput=s.FoodOutput,
                PowerDemand=s.PowerDemand,Extraction=new List<ResourceAmountDto>(),AccessibleHabitats=new List<HabitatKind>()});d.DefinitionIds.Add(s.DefinitionId);}
            if(colonisationPackage!=null)
            {
                var policy=colonisationPackage.Settlement!;
                d.Resources!.Add(new ResourceDefinitionDto{DefinitionId=policy.EquipmentResourceId,DisplayName="Settlement equipment"});d.DefinitionIds.Add(policy.EquipmentResourceId);
                d.Infrastructure!.Add(PopulationData.Copy(policy.Camp!));d.DefinitionIds.Add(policy.Camp!.DefinitionId);
                var hub=PopulationData.Copy(policy.Camp);hub.DefinitionId="infrastructure.settlement-hub.v1";hub.DisplayName="Settlement hub";hub.FoodOutput=policy.EstablishedFoodOutput;
                d.Infrastructure.Add(hub);d.DefinitionIds.Add(hub.DefinitionId);
                d.Infrastructure.Add(PopulationData.Copy(policy.Mine!.Infrastructure!));d.DefinitionIds.Add(policy.Mine.Infrastructure!.DefinitionId);
            }
            var human=game.Civilizations!.Single(c=>c.ControllerKind==ControllerKind.Human).CivilizationId;
            var compatibility=new SaveCompatibility(scenario.ScenarioId,scenario.RulesVersion,scenario.ContentHash);
            var publisher=new PublishShipBlueprintHandler(d,compatibility);
            foreach(var actor in game.Civilizations.OrderBy(a=>a.CivilizationId,StringComparer.Ordinal))foreach(var draft in readyClasses)
            {
                var id=CampaignActivitiesData.ReadyId(draft)+(actor.CivilizationId==human?"":".ai");
                Require(publisher.Execute(game,new CommandEnvelope{ActorCivilizationId=actor.CivilizationId,Kind=CommandKind.PublishShipBlueprint,
                    PublishShipBlueprint=new PublishShipBlueprintPayload{BlueprintId=id,Revision=1,Draft=BlueprintCopies.Copy(draft)}})==CommandErrorCode.None,"Ready publication unavailable in actual campaign catalogue.");
            }
            Dictionary<string,string>? rosterBlueprints=null;
            if(startingRoster!=null)rosterBlueprints=InstallColonisationRoster(built,colonisationPackage!);
            else
            {
            // A truthful authored starting Explorer, not a fictional paid/completed queue.
            var explorerDraft=readyClasses.Single(draft=>draft.ClassId=="class.constitution-explorer.v1"&&draft.HullVersionId=="hull.constitution-explorer.v1");
            var explorer=ready[CampaignActivitiesData.ReadyId(explorerDraft)];
            var ship=game.Ships!.Where(s=>s.OwnerCivilizationId==human).OrderBy(s=>s.ShipId,StringComparer.Ordinal).First();
            var source=d.AuthoredTacticalEquipment!.InitialShipProvisions!.Single(s=>s.ShipId==ship.ShipId);
            var fit=PublishedBuiltShipValidation.ToFit(explorer.Tactical);d.AuthoredTacticalEquipment.Fits!.Add(fit);d.DefinitionIds.Add(fit.DefinitionId!);
            core.CloakSources!.Add(new CampaignTacticalCloakSourceDto{FitId=fit.DefinitionId,FitRevision=fit.Revision,Capable=false,ConcurrentPowerRequired=0});
            var design=CampaignBlueprints.Design(CampaignBlueprints.Restore(game.Blueprints!.Single(b=>b.BlueprintId==explorer.Tactical.BlueprintId)));
            design.DefinitionId="ship-design.constitution-explorer.v1";d.ShipDesigns!.Add(design);d.DefinitionIds.Add(design.DefinitionId);
            ship.ShipDesignDefinitionId=design.DefinitionId;ship.CurrentHull=design.MaxHull;ship.Blueprint=null;
            game.EntityIdentities.Single(e=>e.EntityId==ship.ShipId).DefinitionId=design.DefinitionId;
            source.ShipDesignDefinitionId=design.DefinitionId;source.FitDefinitionId=fit.DefinitionId;source.FitRevision=fit.Revision;
            source.InitialShields=fit.ShieldLimits!.Select(s=>new AuthoredTacticalShieldStockDto{Sector=s.Sector,Current=0}).ToList();source.InitialAmmunition=new List<AuthoredTacticalAmmoStockDto>();
            d.DefinitionIds=d.DefinitionIds.Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToList();
            // Existing source templates refer to a different initial vessel if they previously used this one.
            built.Integrated.ConstructionTemplates=built.Integrated.ConstructionTemplates!.Where(t=>t.SourceProvisionShipId!=ship.ShipId).ToList();
            if(built.Integrated.ConstructionTemplates.Count==0)
            {
                var ai=game.Ships.First(s=>s.OwnerCivilizationId!=human&&s.Blueprint==null);
                var aiProvision=d.AuthoredTacticalEquipment.InitialShipProvisions.Single(s=>s.ShipId==ai.ShipId);
                built.Integrated.ConstructionTemplates.Add(new CampaignIntegratedConstructionTemplateDto{ShipDesignDefinitionId=ai.ShipDesignDefinitionId,SourceProvisionShipId=aiProvision.ShipId});
            }
            rosterBlueprints=new Dictionary<string,string>(StringComparer.Ordinal){{ship.ShipId,explorer.Tactical.BlueprintId}};
            }
            if(startingRoster!=null)d.DefinitionIds=d.DefinitionIds.Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToList();
            var inputOwnership=colonisationPackage==null?null:PlanetaryOwnershipContext.CreateForColonisation(d,game,inventory!.PlanetControls!,seeded!.Minors!);
            ValidatePlayablePackage(built,true,inputOwnership);
            var wrapper=expansionSource?.Hash??startingRoster?.Hash??colonisationPackage?.PackageHash??streamed?.PackageHash??p.PackageHash;
            var context=new PublishedConstructionContext(built,CampaignPublishedConstructionData.CopySupport(p.Support!.Tactical!),wrapper,ready.First().Value.SupportHash,colonisationPackage?.Settlement!.RecordBudget??p.RecordBudget);
            var activity=new ActivitiesContext(p.Support,p.Content);if(seeded==null)SeedActivitySites(activity,map);else activity.State=seeded;
            if(streamed!=null)activity.Streamed=new StreamedActivitiesContext(streamed.PackageHash);
            foreach(var pair in rosterBlueprints!)activity.AuthoredActivityBlueprints.Add(pair.Key,pair.Value);
            var colony=colonisationPackage==null?null:new ColonisationContext(wrapper,colonisationPackage.Settlement!,colonisationPackage.Support!,inventory!,frozenPlanets!,startingRoster);
            var session=new CampaignTacticalReviewSession(core,null,physical,null,built.Integrated,null,null,null,built,map,wrapper,true,context,activity,colony);
            session.ObserveActivitySites(session.state.Campaign.GameState);
            if(colonisationPackage!=null)session.WarmColonisationViews(session.state.Campaign.GameState);
            if(expansionSource!=null)session.expansion=new ExpansionContext(expansionSource,session.state.Campaign.GameState.Civilizations!
                .Where(c=>c.ControllerKind==ControllerKind.AI).OrderBy(c=>c.CivilizationId,StringComparer.Ordinal).Select(c=>new ColonisationAiPlanDto{ActorId=c.CivilizationId}));
            if(session.RepeatEnabled)session.expansion!.RepeatGoals=session.expansion.Plans.Select(p=>new ColonisationRepeatGoalDto{ActorId=p.ActorId,
                Assignment=new ColonisationAiPlanDto{ActorId=p.ActorId,GoalId=CampaignColonisationRepeatIdentity.Goal}}).ToList();
            if(session.CycleEnabled)session.expansion!.CycleActors=session.expansion.Plans.Select(p=>new ColonisationCycleActorDto{ActorId=p.ActorId}).ToList();
            if(expansionSource!=null)session.ValidateActivityState();
            return session;
        }
        internal static CampaignActivityStateDto SeedColonisationActivities(CampaignActivitiesPackageDto package,NaturalGalaxyDocumentDto map)
        {
            var context=new ActivitiesContext(package.Support!,package.Content!);
            SeedActivitySites(context,map);return context.State;
        }
        private static void SeedActivitySites(ActivitiesContext context,NaturalGalaxyDocumentDto map)
        {
            var homes=map.Layout.HomeCandidates.Select(h=>h.NodeId).ToHashSet(StringComparer.Ordinal);
            var systems=map.Layout.Systems.Where(s=>!homes.Contains(s.NodeId)).OrderBy(s=>CampaignActivitiesData.Identity("activity-placement-frontier-v2",map.Settings.Map.Seed,s.SystemId),StringComparer.Ordinal).ToList();
            Require(systems.Count>=6,"Activity campaign requires at least six non-home systems.");
            // Authoring may inspect the frozen map, but this never charts a site or
            // supplies a route to an actor. Each empire gets a reachable nearby science
            // opportunity and minor world; the other two sites remain wider discoveries.
            var chosen=new Dictionary<int,string>();var used=new HashSet<string>(StringComparer.Ordinal);
            var homeList=map.Layout.HomeCandidates.OrderBy(h=>h.CivilizationDefinitionId,StringComparer.Ordinal).ToList();
            for(int h=0;h<Math.Min(2,homeList.Count);h++)
            {
                var distance=new Dictionary<string,int>(StringComparer.Ordinal){{homeList[h].NodeId,0}};
                var frontier=new Queue<string>();frontier.Enqueue(homeList[h].NodeId);
                while(frontier.Count>0)
                {var node=frontier.Dequeue();foreach(var edge in map.Layout.Connections.Where(e=>e.EndpointA==node||e.EndpointB==node).OrderBy(e=>e.ConnectionId,StringComparer.Ordinal))
                    {var target=edge.EndpointA==node?edge.EndpointB:edge.EndpointA;if(!distance.ContainsKey(target)){distance.Add(target,distance[node]+1);frontier.Enqueue(target);}}}
                foreach(var index in new[]{h,4+h})
                {
                    var selected=systems.Where(s=>!used.Contains(s.SystemId)&&distance.ContainsKey(s.NodeId))
                        .OrderBy(s=>distance[s.NodeId]<2?1:0).ThenBy(s=>Math.Abs(distance[s.NodeId]-3))
                        .ThenBy(s=>CampaignActivitiesData.Identity("activity-placement-frontier-v2",map.Settings.Map.Seed,index.ToString(CultureInfo.InvariantCulture),s.SystemId),StringComparer.Ordinal).First();
                    chosen.Add(index,selected.SystemId);used.Add(selected.SystemId);
                }
            }
            var templates=context.Content.Sites!.OrderBy(s=>s.DefinitionId,StringComparer.Ordinal).Concat(context.Content.Minors!.OrderBy(m=>m.DefinitionId,StringComparer.Ordinal).Select(m=>m.Encounter!)).ToList();
            for(int i=0;i<templates.Count;i++)
            {
                var minor=i<4?null:context.Content.Minors!.OrderBy(m=>m.DefinitionId,StringComparer.Ordinal).ElementAt(i-4);
                var system=chosen.TryGetValue(i,out var selectedId)?systems.Single(s=>s.SystemId==selectedId):systems.First(s=>!used.Contains(s.SystemId));
                used.Add(system.SystemId);var body="body.activity."+i.ToString(CultureInfo.InvariantCulture);var actor=minor==null?null:"minor.activity."+(i-4).ToString(CultureInfo.InvariantCulture);
                context.State.Sites!.Add(new ActivitySiteStateDto{SiteId="site.activity."+i.ToString(CultureInfo.InvariantCulture),DefinitionId=templates[i].DefinitionId,NodeId=system.NodeId,SystemId=system.SystemId,BodyId=body,MinorActorId=actor});
                context.State.Bodies!.Add(new ActivityBodyDto{BodyId=body,SystemId=system.SystemId,DisplayName=system.DisplayName+" · survey body",GovernmentActorId=actor});
                if(minor!=null)context.State.Minors!.Add(new ActivityMinorStateDto{ActorId=actor!,DefinitionId=minor.DefinitionId,BodyId=body,Population=1000000,SpeciesId=minor.SpeciesId});
            }
        }
    }
}
