using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        // No caller DTO retained: only this validated closed source descriptor reaches genesis/forks.
        private sealed class ColonisationRosterSource
        {
            internal readonly string Hash;
            internal ColonisationRosterSource(CampaignColonisationRosterPackageDto p)
            {Require(p!=null&&p.PackageHash==ColonisationData.RosterHash(p),"Different trusted roster package.");Hash=p!.PackageHash;}
        }
        public static CampaignColonisationRosterPackageDto CreateColonisationRosterPackage(CampaignColonisationPackageDto baseV1)
        {
            var p=new CampaignColonisationRosterPackageDto{BaseV1=baseV1,StartingRoster=new ColonisationStartingRosterDto()};
            p.PackageHash=ColonisationData.RosterHash(p);return p;
        }
        public static CampaignTacticalReviewSession StartColonisationRoster(CampaignColonisationRosterPackageDto package,NaturalGalaxySettingsDto settings)
        {
            var source=new ColonisationRosterSource(package);var baseV1=package.BaseV1!;
            var civilizations=baseV1.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            var map=NaturalGalaxyGenerator.Generate(settings,civilizations);
            return StartActivitiesFrozen(baseV1.Activities,GeneratedCampaignFactory.Frozen(map,civilizations),null,baseV1,null,source);
        }
        public static CampaignTacticalReviewSession LoadColonisationRoster(CampaignColonisationRosterPackageDto package,CampaignColonisationSaveDto root)
        {var source=new ColonisationRosterSource(package);return LoadColonisationCore(package.BaseV1!,root,source);}
        private static Dictionary<string,string> InstallColonisationRoster(CampaignPlayableFleetPackageDto built,CampaignColonisationPackageDto package)
        {
            var core=built.Integrated!.Physical!.CorePackage!;var scenario=core.Campaign!;var game=scenario.InitialState;var d=scenario.Definitions;
            var human=game.Civilizations!.Single(c=>c.ControllerKind==ControllerKind.Human).CivilizationId;
            var map=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var actor in game.Civilizations.OrderBy(a=>a.CivilizationId,StringComparer.Ordinal))
            {
                var ships=game.Ships!.Where(s=>s.OwnerCivilizationId==actor.CivilizationId).ToList();Require(ships.Count==1,"One actual starter per roster actor required.");var ship=ships[0];
                var id="blueprint.ready.constitution-explorer"+(actor.CivilizationId==human?"":".ai");
                var record=game.Blueprints!.Single(b=>b.OwnerCivilizationId==actor.CivilizationId&&b.BlueprintId==id&&b.Revision==1);
                var support=PublishedTacticalSupportCompiler.EvaluateColonisation(new ShipDesignCatalogueSnapshot(record.FrozenCatalogue!),CampaignBlueprints.Restore(record),CampaignBlueprints.FrozenAccess(record.FrozenCatalogue!),package.Support!);
                Require(support.Supported&&support.Ship!.Tactical.BlueprintId==id,"Actual actor-owned starter support required.");
                var fit=PublishedBuiltShipValidation.ToFit(support.Ship!.Tactical);Require(!d.AuthoredTacticalEquipment!.Fits!.Any(f=>f.DefinitionId==fit.DefinitionId),"Duplicate roster fit identity.");
                d.AuthoredTacticalEquipment!.Fits!.Add(fit);d.DefinitionIds.Add(fit.DefinitionId!);
                core.CloakSources!.Add(new CampaignTacticalCloakSourceDto{FitId=fit.DefinitionId,FitRevision=fit.Revision,Capable=false,ConcurrentPowerRequired=0});
                var design=CampaignBlueprints.Design(CampaignBlueprints.Restore(record));design.DefinitionId="ship-design.colonisation-roster-v2."+actor.CivilizationId;
                d.ShipDesigns!.Add(design);d.DefinitionIds.Add(design.DefinitionId);ship.ShipDesignDefinitionId=design.DefinitionId;ship.CurrentHull=design.MaxHull;ship.Blueprint=null;
                game.EntityIdentities.Single(e=>e.EntityId==ship.ShipId).DefinitionId=design.DefinitionId;
                var provision=d.AuthoredTacticalEquipment.InitialShipProvisions!.Single(p=>p.ShipId==ship.ShipId);
                provision.ShipDesignDefinitionId=design.DefinitionId;provision.FitDefinitionId=fit.DefinitionId;provision.FitRevision=fit.Revision;
                provision.InitialShields=fit.ShieldLimits!.Select(s=>new AuthoredTacticalShieldStockDto{Sector=s.Sector,Current=0}).ToList();provision.InitialAmmunition=new List<AuthoredTacticalAmmoStockDto>();
                map.Add(ship.ShipId,id);
            }
            built.Integrated.ConstructionTemplates=built.Integrated.ConstructionTemplates!.Where(t=>!map.ContainsKey(t.SourceProvisionShipId!)).ToList();
            foreach(var ship in game.Ships!.Where(s=>map.ContainsKey(s.ShipId)))built.Integrated.ConstructionTemplates.Add(new CampaignIntegratedConstructionTemplateDto{ShipDesignDefinitionId=ship.ShipDesignDefinitionId,SourceProvisionShipId=ship.ShipId});
            return map;
        }
    }
}
