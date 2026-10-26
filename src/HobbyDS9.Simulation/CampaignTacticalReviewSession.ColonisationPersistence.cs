using System;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public static CampaignTacticalReviewSession LoadColonisation(CampaignColonisationPackageDto package,CampaignColonisationSaveDto root)
            =>LoadColonisationCore(package,root,null);
        private static CampaignTacticalReviewSession LoadColonisationCore(CampaignColonisationPackageDto package,CampaignColonisationSaveDto root,ColonisationRosterSource? roster,ExpansionSource? expansionSource=null,ColonisationReplayTiming? timing=null)
        {
            timing?.Begin(ColonisationReplayPhase.CorePreflight);
            Require(package!=null&&package.PackageHash==ColonisationData.PackageHash(package),"Different approved colonisation package.");
            ColonisationRootBudget.Validate(root,package!.Settlement!);
            Require(root!=null&&root.FormatVersion==CampaignColonisationIdentity.Format&&root.ProfileId==CampaignColonisationIdentity.Profile&&root.PolicyId==CampaignColonisationIdentity.Policy
                &&root.TemplatePackageHash==(expansionSource?.Hash??roster?.Hash??package!.PackageHash)&&root.Map!=null&&root.FrozenPlanets!=null&&root.Campaign!=null&&root.Colonisation!=null&&root.Activities!=null
                &&root.Production!=null&&root.ProductionHistory!=null&&root.Discoveries!=null&&root.PublishedBuiltShips!=null&&root.Journal!=null&&root.Journal.Count<=package.Settlement!.JournalBudget,
                "Complete typed colonisation root required; no conversion.");
            Require(root!.Campaign!.Integrated?.Physical?.Core.ProfileId==CampaignColonisationIdentity.EmbeddedCoreProfile,"Colonisation owned core marker required.");
            var definitions=package!.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            timing?.End(ColonisationReplayPhase.CorePreflight);
            timing?.Begin(ColonisationReplayPhase.FrozenMap);
            var map=GeneratedCampaignFactory.Frozen(root.Map!,definitions);
            timing?.End(ColonisationReplayPhase.FrozenMap);
            timing?.Begin(ColonisationReplayPhase.InitialState);
            var replay=StartActivitiesFrozen(package.Activities,map,null,package,root.FrozenPlanets!,roster,expansionSource);
            Require(root.PackageHash==replay.playableHash&&root.SupportHash==replay.published!.SupportHash,"Different frozen colonisation source identity.");
            timing?.End(ColonisationReplayPhase.InitialState);
            timing?.Begin(ColonisationReplayPhase.Journal);
            foreach(var op in root.Journal!)
            {
                var kind=timing==null?default:ColonisationReplayTiming.Kind(op);
                timing?.Count(kind,0);
                Require(op!=null&&(op.Colonisation==null)!=(op.Existing==null),"Closed colonisation operation required.");
                if(op!.Existing!=null)Require((op.Existing.Activity==null)!=(op.Existing.Common==null),"Closed inherited colonisation operation required.");
                var accepted=op!.Colonisation!=null?replay.SubmitColonisation(op.Colonisation).Accepted:
                    op.Existing!.Activity!=null?replay.SubmitActivity(op.Existing.Activity).Accepted:ReplayActivityCommon(replay,op.Existing.Common!);
                timing?.Count(kind,accepted?1:2);
                Require(accepted&&FrontierSame(op,replay.colonisation!.Journal.Last()),"Illegal or reordered colonisation journal operation.");
                timing?.Count(kind,3);
            }
            timing?.End(ColonisationReplayPhase.Journal);
            timing?.Begin(ColonisationReplayPhase.CoreEquality);
            Require(FrontierSame(root,replay.ExportColonisationCore()),"Colonisation checkpoint differs from complete ordered operation replay.");
            timing?.End(ColonisationReplayPhase.CoreEquality);
            return replay;
        }
        // Private borrowed view under the session transaction lock. No DTO escapes,
        // no state is copied before the portable bound is checked, and the canonical
        // current core is used even while an ordinary command replaced state.
        private void ValidateColonisationRootBudget()
        {
            var c=colonisation!;var physical=travel!;
            var view=new CampaignColonisationSaveDto{TemplatePackageHash=c.PackageHash,PackageHash=playableHash!,SupportHash=published!.SupportHash,
                Map=generatedMap,FrozenPlanets=c.FrozenPlanets,Discoveries=discoveries,PublishedBuiltShips=published.Records,
                Activities=activities!.State,Colonisation=c.State,Production=c.Production,ProductionHistory=c.History,
                MovementOwnership=c.MovementOwnership,LastSettlementProof=c.SettlementProof,Journal=c.Journal,
                Campaign=new CampaignPlayableFleetSaveDto{FormatVersion=CampaignPlayableFleetIdentity.Format,ProfileId=CampaignPlayableFleetIdentity.Profile,PackageHash=playableHash!,
                    Names=playableNames.Select(n=>new CampaignFleetNameDto{FleetId=n.Key,DisplayName=n.Value}).ToList(),Transcript=playableTranscript,
                    Integrated=new CampaignIntegratedPlaytestSaveDto{FormatVersion=CampaignIntegratedPlaytestIdentity.SaveFormat,ProfileId=CampaignIntegratedPlaytestIdentity.Profile,PackageHash=integratedHash!,BuiltShips=builtShips,
                        Physical=new CampaignTacticalTravelSaveDto{FormatVersion=physical.FormatVersion,ProfileId=physical.ProfileId,PackageHash=physical.PackageHash,Core=state,
                            Phase=physical.Phase,Requests=physical.Requests,Orders=physical.Orders,Contacts=physical.Contacts,LastMovement=physical.LastMovement,MovementProof=physical.MovementProof}}}};
            if(expansion==null)ColonisationRootBudget.Validate(view,c.Policy);
            else ColonisationRootBudget.ValidateExpansion(new CampaignColonisationExpansionSaveDto{PackageHash=expansion.Source.Hash,BaseCampaign=view,AiPlans=expansion.Plans},c.Policy);
        }
        public CampaignColonisationSaveDto ExportColonisationSave()
        {lock(gate){Require(expansion==null,"Use complete expansion Format12 export.");return ExportColonisationCore();}}
        private CampaignColonisationSaveDto ExportColonisationCore()
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
