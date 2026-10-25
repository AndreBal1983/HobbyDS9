using System;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private bool ColonisationCapacity(int delta,CampaignTacticalTravelSaveDto? boundary=null,int unstagedAdvanceRevision=0)
        {
            bool Fail(string reason){LastPhysicalFailure=reason;return false;}
            if(delta<0||delta>1||unstagedAdvanceRevision<0||unstagedAdvanceRevision>1)return Fail("Colonisation operation delta.");
            var context=colonisation!;var a=context.State;var game=boundary?.Core.Campaign.GameState??state.Campaign.GameState;
            long m=a.ManifestRecords!.Count(x=>x.Stage==SettlerManifestStage.Loaded&&x.Kind==SettlerManifestKind.Settlement);
            long e=a.ManifestRecords.Count(x=>x.Stage==SettlerManifestStage.Loaded&&x.Kind==SettlerManifestKind.ReturnOnly);
            long v=a.ManifestRecords.Count(x=>x.Stage==SettlerManifestStage.Loaded&&x.Kind==SettlerManifestKind.SupplyOnly);
            long c=a.CampSources!.Count(x=>x.Stage==ColonyEstablishmentStage.Founding),i=a.DeploymentIntents!.Count;
            long s=a.Surveys!.Count(x=>x.Stage==PlanetSurveyStage.Active),q=context.Production.Count;
            long f=activities!.State.Findings!.Count(x=>x.Stage==ActivityProgressStage.Active||x.Stage==ActivityProgressStage.AwaitingClaim);
            BigInteger terminals=q+f+s+m+e+2*c+i+v;
            var physical=boundary??travel!;
            BigInteger combat=physical.Phase==SessionPhase.AwaitingCombat?PlayableAdvanceCapacity.RequiredSlots(physical,controllers)-1:0;
            if((BigInteger)UsedPlayableOperations+delta+terminals+combat>context.Policy.JournalBudget)return Fail("Colonisation terminal/combat journal capacity.");
            // Journal append and revision are different domains. The ordinary candidate
            // already contains its operation's increment; AI procurement alone is
            // certified before the enclosing Advance increments the staged revision.
            if((BigInteger)game.StateRevision+unstagedAdvanceRevision+terminals+combat>long.MaxValue)return Fail("Colonisation terminal/combat revision capacity.");
            BigInteger actual=a.ManifestRecords!.Count+a.ManifestOrigins!.Count+a.ManifestResolutions!.Count+a.ColonyOrigins!.Count+a.LandedCohortLineages!.Count+a.CampSources!.Count+a.CampTerminals!.Count;
            BigInteger r10=BuildRegistry.Count()+published!.Records.Count+context.History.Count+activities.State.Structures!.Count+activities.State.Findings!.Count+2*q;
            if(r10+actual+10*m+2*e+6*c+v>context.Policy.RecordBudget)return Fail("Colonisation retained terminal record capacity.");
            BigInteger futureIds=2*q+6*m+3*c+e;
            if((BigInteger)game.NextEntitySequence+futureIds>ulong.MaxValue)return Fail("Colonisation protected entity allocation capacity.");
            return true;
        }
    }
}
