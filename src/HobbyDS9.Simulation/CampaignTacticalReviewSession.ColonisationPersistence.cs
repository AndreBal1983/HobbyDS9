using System;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public static CampaignTacticalReviewSession LoadColonisation(CampaignColonisationPackageDto package,CampaignColonisationSaveDto root)
        {
            Require(package!=null&&package.PackageHash==ColonisationData.PackageHash(package),"Different approved colonisation package.");
            Require(root!=null&&root.FormatVersion==CampaignColonisationIdentity.Format&&root.ProfileId==CampaignColonisationIdentity.Profile&&root.PolicyId==CampaignColonisationIdentity.Policy
                &&root.TemplatePackageHash==package!.PackageHash&&root.Map!=null&&root.FrozenPlanets!=null&&root.Campaign!=null&&root.Colonisation!=null&&root.Activities!=null
                &&root.Production!=null&&root.ProductionHistory!=null&&root.Discoveries!=null&&root.PublishedBuiltShips!=null&&root.Journal!=null&&root.Journal.Count<=package.Settlement!.JournalBudget,
                "Complete typed colonisation root required; no conversion.");
            Require(root!.Campaign!.Integrated?.Physical?.Core.ProfileId==CampaignColonisationIdentity.EmbeddedCoreProfile,"Colonisation owned core marker required.");
            var definitions=package!.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            var map=GeneratedCampaignFactory.Frozen(root.Map!,definitions);
            var replay=StartActivitiesFrozen(package.Activities,map,null,package,root.FrozenPlanets!);
            Require(root.PackageHash==replay.playableHash&&root.SupportHash==replay.published!.SupportHash,"Different frozen colonisation source identity.");
            foreach(var op in root.Journal!)
            {
                Require(op!=null&&(op.Colonisation==null)!=(op.Existing==null),"Closed colonisation operation required.");
                if(op!.Existing!=null)Require((op.Existing.Activity==null)!=(op.Existing.Common==null),"Closed inherited colonisation operation required.");
                var accepted=op!.Colonisation!=null?replay.SubmitColonisation(op.Colonisation).Accepted:
                    op.Existing!.Activity!=null?replay.SubmitActivity(op.Existing.Activity).Accepted:ReplayActivityCommon(replay,op.Existing.Common!);
                Require(accepted&&FrontierSame(op,replay.colonisation!.Journal.Last()),"Illegal or reordered colonisation journal operation.");
            }
            Require(FrontierSame(root,replay.ExportColonisationSave()),"Colonisation checkpoint differs from complete ordered operation replay.");
            return replay;
        }
        public CampaignColonisationSaveDto ExportColonisationSave()
        {
            lock(gate)
            {
                Require(colonisation!=null,"Explicit colonisation root required.");ValidateActivityState();
                return new CampaignColonisationSaveDto{TemplatePackageHash=colonisation!.PackageHash,PackageHash=playableHash!,SupportHash=published!.SupportHash,
                    Map=NaturalGalaxyData.Copy(generatedMap!),FrozenPlanets=ColonisationData.Copy(colonisation.FrozenPlanets),Campaign=ExportPlayableCore(),
                    Discoveries=discoveries.Select(CopyDiscovery).ToList(),PublishedBuiltShips=published.Records.OrderBy(r=>r.ShipId,StringComparer.Ordinal).Select(PublishedBuiltShipValidation.Copy).ToList(),
                    Activities=CampaignActivitiesData.Copy(activities!.State),Colonisation=ColonisationData.Copy(colonisation.State),Production=colonisation.Production.Select(ColonisationData.Copy).ToList(),
                    ProductionHistory=colonisation.History.Select(ColonisationData.Copy).ToList(),MovementOwnership=colonisation.MovementOwnership==null?null:ColonisationData.Copy(colonisation.MovementOwnership),
                    LastSettlementProof=colonisation.SettlementProof==null?null:ColonisationData.Copy(colonisation.SettlementProof),
                    Journal=colonisation.Journal.Select(o=>new ColonisationOperationDto{Existing=o.Existing==null?null:CopyActivityOperation(o.Existing),Colonisation=o.Colonisation==null?null:ColonisationData.Copy(o.Colonisation)}).ToList()};
            }
        }
    }
}
