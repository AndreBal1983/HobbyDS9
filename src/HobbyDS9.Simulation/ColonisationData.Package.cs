using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    internal static partial class ColonisationData
    {
        internal static Dictionary<string,ColonisationSupportedShip> Ready(CampaignColonisationPackageDto p)
        {
            Require(p.Catalogue!=null&&ShipDesignCatalogueValidation.Validate(p.Catalogue).Count==0&&p.ReadyClasses!=null&&p.ReadyClasses.Count==4
                &&p.ReadyClasses.All(d=>d!=null)&&p.ReadyClasses.Select(d=>d.ClassId).Distinct(StringComparer.Ordinal).Count()==4&&p.Support!=null,"Four legal colonisation ready classes required.");
            var catalogue=new ShipDesignCatalogueSnapshot(p.Catalogue!);var compiler=new ShipDesignCompiler(catalogue);var access=CampaignBlueprints.FrozenAccess(p.Catalogue!);
            var results=new Dictionary<string,ColonisationSupportedShip>(StringComparer.Ordinal);
            foreach(var draft in p.ReadyClasses!)
            {
                Require(draft.ClassId.StartsWith("class.",StringComparison.Ordinal)&&draft.ClassId.EndsWith(".v1",StringComparison.Ordinal),"Invalid ready class identity.");
                var publication=compiler.Publish(draft,access,CampaignActivitiesData.ReadyId(draft),1);Require(publication.Published,"Illegal colonisation ready class.");
                var support=PublishedTacticalSupportCompiler.EvaluateColonisation(catalogue,publication.Blueprint!,access,p.Support!);
                Require(support.Supported,"Unsupported colonisation ready class.");results.Add(CampaignActivitiesData.ReadyId(draft),support.Ship!);
            }
            Require(results.Count(r=>r.Value.Passengers>0)==1&&results["blueprint.ready.galaxy-colony-transport"].Passengers==100,"One actual ready colony transport required.");
            return results;
        }
        internal static string PackageHash(CampaignColonisationPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==1&&p.ProfileId==CampaignColonisationIdentity.PackageProfile&&p.Activities!=null
                &&p.Activities.PackageHash==CampaignActivitiesData.PackageHash(p.Activities),"Different colonisation base package.");
            var ready=Ready(p!);var old=CampaignActivitiesData.Ready(p!.Activities!);
            var common=BlueprintCopies.Copy(p.Catalogue!);common.Components!.RemoveAll(c=>c.VersionId=="component.settler-module.v1");
            common.Roles!.RemoveAll(r=>r.RoleId=="role.colony-transport.v1");
            Require(PublishedTacticalSupportEncoding.ActivityCatalogue(new ShipDesignCatalogueSnapshot(common))
                ==PublishedTacticalSupportEncoding.ActivityCatalogue(new ShipDesignCatalogueSnapshot(p.Activities!.Catalogue!)),"Conflicting retained and new catalogue facts.");
            foreach(var pair in old)Require(ready.TryGetValue(pair.Key,out var exact)&&exact.Tactical.SourceHash==pair.Value.Tactical.SourceHash,"Conflicting retained ready source.");
            var policy=p.Settlement;
            Require(policy!=null&&policy.Identity=="colonisation-playtest-values-v1"&&policy.PlanetInventoryPolicy==CampaignColonisationIdentity.InventoryPolicy
                &&policy.MaximumPlanets==1040&&policy.MaximumStarterPlanets==32&&policy.MaximumValidationVisits==2000000&&policy.MaximumCanonicalRootBytes==64L*1024*1024
                &&policy.RecordBudget==8192&&policy.JournalBudget==65536&&policy.Settlers==100&&policy.SourcePopulationFloor==500&&policy.MaximumSourceCohortBps==2500
                &&policy.MinimumFood==8&&policy.MaximumFood==20&&policy.EquipmentResourceId=="resource.settlement-kit"&&policy.KitIndustry==60
                &&policy.KitResources!=null&&policy.KitResources.Count==1&&policy.KitResources[0]!=null&&policy.KitResources[0].ResourceDefinitionId=="resource.dilithium"&&policy.KitResources[0].Amount==6
                &&policy.EstablishmentIndustry==16&&policy.MinimumFedWeeks==4&&policy.EstablishedFoodOutput==2&&policy.Camp!=null,"Unsupported provisional settlement policy.");
            var camp=policy!.Camp!;
            var mine=policy.Mine?.Infrastructure;var mineCost=policy.Mine?.Cost;
            Require(mine!=null&&mine.DefinitionId=="infrastructure.frontier-mine.v1"&&mine.DisplayName!=null&&mine.DisplayName.Length<=128&&mine.PowerDemand==3
                &&mine.FoodOutput==0&&mine.Industry==0&&mine.PowerSupply==0&&mine.Research==0&&mine.LogisticsCapacity==0&&!mine.Shipbuilding
                &&mine.ArtificialCapacity==0&&mine.ArtificialHabitability==0&&mine.AccessibleHabitats!=null&&mine.AccessibleHabitats.Count==0
                &&mine.Extraction!=null&&mine.Extraction.Count==1&&mine.Extraction[0]!=null&&mine.Extraction[0].ResourceDefinitionId=="resource.dilithium"&&mine.Extraction[0].Amount==3
                &&mineCost!=null&&mineCost.Credits==0&&mineCost.Industry==60&&mineCost.Maintenance==0&&mineCost.Resources!=null&&mineCost.Resources.Count==0,"Different provisional frontier mine recipe.");
            Require(camp.DefinitionId=="infrastructure.settlement-camp.v1"&&camp.DisplayName!=null&&camp.DisplayName.Length<=128&&camp.ArtificialCapacity==100&&camp.ArtificialHabitability==10000
                &&camp.Industry==4&&camp.PowerSupply==4&&camp.PowerDemand==2&&camp.LogisticsCapacity==10&&camp.FoodOutput==0&&camp.Research==0&&!camp.Shipbuilding
                &&camp.Extraction!=null&&camp.Extraction.Count==0&&camp.AccessibleHabitats!=null&&camp.AccessibleHabitats.Count==9
                &&camp.AccessibleHabitats.OrderBy(h=>h).SequenceEqual(Enum.GetValues(typeof(HabitatKind)).Cast<HabitatKind>()),"Different settlement camp authoring.");
            using(var bytes=new MemoryStream())using(var writer=new BinaryWriter(bytes,new UTF8Encoding(false,true),true))
            {
                writer.Write(policy.Identity);writer.Write(policy.PlanetInventoryPolicy);writer.Write(CampaignColonisationIdentity.InventoryAuthoring);
                writer.Write(ColonisationRootBudget.AlgorithmIdentity);writer.Write(ColonisationRootBudget.SchemaFingerprint);
                writer.Write(policy.MaximumPlanets);writer.Write(policy.MaximumStarterPlanets);writer.Write(policy.MaximumValidationVisits);writer.Write(policy.MaximumCanonicalRootBytes);
                writer.Write(policy.RecordBudget);writer.Write(policy.JournalBudget);writer.Write(policy.Settlers);writer.Write(policy.SourcePopulationFloor);writer.Write(policy.MaximumSourceCohortBps);
                writer.Write(policy.MinimumFood);writer.Write(policy.MaximumFood);writer.Write(policy.EquipmentResourceId);writer.Write(policy.KitIndustry);
                writer.Write(policy.KitResources![0].ResourceDefinitionId);writer.Write(policy.KitResources[0].Amount);writer.Write(policy.EstablishmentIndustry);writer.Write(policy.MinimumFedWeeks);writer.Write(policy.EstablishedFoodOutput);
                writer.Write(camp.DefinitionId);writer.Write(camp.DisplayName!);writer.Write(camp.FoodOutput);writer.Write(camp.Industry);writer.Write(camp.PowerSupply);writer.Write(camp.PowerDemand);writer.Write(camp.Research);writer.Write(camp.LogisticsCapacity);writer.Write(camp.Shipbuilding);
                writer.Write(camp.ArtificialCapacity);writer.Write(camp.ArtificialHabitability);foreach(var h in camp.AccessibleHabitats!.OrderBy(h=>h))writer.Write((int)h);
                writer.Write(mine!.DefinitionId);writer.Write(mine.DisplayName!);writer.Write(mine.FoodOutput);writer.Write(mine.Industry);writer.Write(mine.PowerSupply);writer.Write(mine.PowerDemand);
                writer.Write(mine.Research);writer.Write(mine.LogisticsCapacity);writer.Write(mine.Shipbuilding);writer.Write(mine.ArtificialCapacity);writer.Write(mine.ArtificialHabitability);
                writer.Write(mine.Extraction![0].ResourceDefinitionId);writer.Write(mine.Extraction[0].Amount);writer.Write(mineCost!.Credits);writer.Write(mineCost.Industry);writer.Write(mineCost.Maintenance);
                writer.Flush();
                var parts=new List<string>{CampaignColonisationIdentity.Profile,CampaignColonisationIdentity.Policy,CampaignColonisationIdentity.Rules,p.Activities.PackageHash,
                    PublishedTacticalSupportEncoding.ActivityCatalogue(new ShipDesignCatalogueSnapshot(p.Catalogue!)),Convert.ToBase64String(bytes.ToArray())};
                foreach(var pair in ready.OrderBy(r=>r.Key,StringComparer.Ordinal)){parts.Add(pair.Key);parts.Add(pair.Value.Tactical.SourceHash);parts.Add(pair.Value.Tactical.SupportHash);}
                return CampaignActivitiesData.Identity(parts.ToArray());
            }
        }
    }
}
