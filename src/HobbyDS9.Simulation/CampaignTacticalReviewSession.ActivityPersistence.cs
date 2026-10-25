using System;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public CampaignActivitiesSaveDto ExportActivitiesSave()
        {
            lock(gate)
            {
                Require(activities!=null&&published!=null,"Explicit activity campaign required.");
                ValidateActivityState();
                return new CampaignActivitiesSaveDto{TemplatePackageHash=published!.WrapperHash,PackageHash=playableHash!,SupportHash=published.SupportHash,
                    Map=NaturalGalaxyData.Copy(generatedMap!),Campaign=ExportPlayableCore(),Discoveries=discoveries.Select(CopyDiscovery).ToList(),
                    PublishedBuiltShips=published.Records.OrderBy(r=>r.ShipId,StringComparer.Ordinal).Select(PublishedBuiltShipValidation.Copy).ToList(),
                    Activities=CampaignActivitiesData.Copy(activities!.State),Journal=activities.Journal.Select(CopyActivityOperation).ToList()};
            }
        }
        public static CampaignTacticalReviewSession LoadActivities(CampaignActivitiesPackageDto package,CampaignActivitiesSaveDto save)
        {
            RequireActivitiesPackage(package);
            Require(save!=null&&save.FormatVersion==CampaignActivitiesIdentity.Format&&save.ProfileId==CampaignActivitiesIdentity.Profile&&save.PolicyId==CampaignActivitiesIdentity.Policy
                &&save.TemplatePackageHash==package.PackageHash&&save.Map!=null&&save.Campaign!=null&&save.Activities!=null&&save.Discoveries!=null&&save.PublishedBuiltShips!=null
                &&save.Journal!=null&&save.Journal.Count<=package.BaseTemplate!.ReplayOperationLimit,"Complete typed activity root required; no conversion.");
            Require(save!.Campaign!.Integrated?.Physical?.Core.ProfileId==CampaignActivitiesIdentity.EmbeddedCoreProfile,"Activity embedded identity required.");
            var map=GeneratedCampaignFactory.Frozen(save.Map!,package.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!);
            var replay=StartActivitiesFrozen(package,map);
            Require(save.PackageHash==replay.playableHash&&save.SupportHash==replay.published!.SupportHash,"Different generated activity/support identity.");
            foreach(var entry in save.Journal!)
            {
                Require(entry!=null&&(entry.Activity==null)!=(entry.Common==null),"Closed activity journal operation required.");
                var expected=CopyActivityOperation(entry!);
                var accepted=entry!.Activity!=null?replay.SubmitActivity(entry.Activity).Accepted:ReplayActivityCommon(replay,entry.Common!);
                Require(accepted&&FrontierSame(expected,replay.activities!.Journal.Last()),"Illegal/reordered activity operation.");
            }
            Require(FrontierSame(replay.ExportActivitiesSave(),save),"Activity checkpoint differs from full ordered-operation replay.");
            return replay;
        }
        private static bool ReplayActivityCommon(CampaignTacticalReviewSession session,CampaignPlayableOperationDto operation)
        {
            Require((operation.Common==null)!=(operation.Organization==null),"Closed common activity operation.");
            if(operation.Organization!=null)return session.SubmitFleetOrganization(operation.Organization).Accepted;
            var op=operation.Common!;CampaignTacticalMutationResult? result=null;
            switch(op.Kind)
            {
                case FrontierOperationKind.Strategic:result=session.SubmitStrategicCommand(op.Strategic);break;
                case FrontierOperationKind.Travel:result=session.SubmitTravelOrder(op.Travel);break;
                case FrontierOperationKind.Round:result=session.CommitRound(op.Round);break;
                case FrontierOperationKind.Cloak:
                    Require(op.ActorId!=null&&op.CommandId!=null&&op.FleetId!=null&&op.Enabled.HasValue&&op.Turn.HasValue&&op.Revision.HasValue,"Complete cloak operation.");
                    result=session.SetApproachCloak(op.ActorId!,op.CommandId!,op.FleetId!,op.Enabled!.Value,op.Turn!.Value,op.Revision!.Value);break;
                case FrontierOperationKind.Advance:
                case FrontierOperationKind.Finalize:
                    Require(op.Turn.HasValue&&op.Revision.HasValue,"Complete activity boundary.");
                    result=op.Kind==FrontierOperationKind.Advance?session.AdvanceTurn(op.Turn!.Value,op.Revision!.Value):session.FinalizeBatch(op.Turn!.Value,op.Revision!.Value);break;
            }
            return result!=null&&result.Accepted;
        }
    }
}
