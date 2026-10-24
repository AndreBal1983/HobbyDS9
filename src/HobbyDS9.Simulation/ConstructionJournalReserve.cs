using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal enum ConstructionJournalOperation { AlreadyStagedBoundary, PlayerOptional, InternalAiProcurement }
    internal sealed class ConstructionIndustryFloor
    {
        internal string ColonyId { get; }
        internal long Industry { get; }
        internal ConstructionIndustryFloor(string id,long industry) { ColonyId=id;Industry=industry; }
    }
    internal sealed class ConstructionJournalFacts
    {
        internal const string ClosedRules = "construction-journal-current-ship-only-v1";
        internal string PackageHash { get; }
        internal string ContentHash { get; }
        internal string RulesVersion { get; }
        internal int Turn { get; }
        internal long Revision { get; }
        internal SessionPhase Phase { get; }
        internal ConstructionJournalOperation Operation { get; }
        internal int ProposedDelta { get; }
        internal long Used { get; }
        internal long Limit { get; }
        internal long WorkUsed { get; }
        internal string? UncertifiableReason { get; }
        internal bool Certified => UncertifiableReason==null && RecordCapacity && JournalCapacity;
        internal bool RecordCapacity { get; }
        internal bool JournalCapacity { get; }
        internal long Pending { get; }
        internal long Retained { get; }
        internal long Horizon { get; }
        internal long PotentialShips { get; }
        internal long EncounterBound { get; }
        internal long BatchSlots { get; }
        internal long CurrentCombatDebt { get; }
        internal long ReservedSlots { get; }
        internal IReadOnlyList<ConstructionIndustryFloor> Floors { get; }
        internal ConstructionJournalFacts(string package,string content,string rules,int turn,long revision,SessionPhase phase,
            ConstructionJournalOperation operation,int delta,long used,long limit,long work,string? reason,bool records,bool journal,
            long pending,long retained,long horizon,long ships,long encounters,long batch,long debt,long reserve,List<ConstructionIndustryFloor> floors)
        { PackageHash=package;ContentHash=content;RulesVersion=rules;Turn=turn;Revision=revision;Phase=phase;Operation=operation;
          ProposedDelta=delta;Used=used;Limit=limit;WorkUsed=work;UncertifiableReason=reason;RecordCapacity=records;JournalCapacity=journal;
          Pending=pending;Retained=retained;Horizon=horizon;PotentialShips=ships;EncounterBound=encounters;BatchSlots=batch;
          CurrentCombatDebt=debt;ReservedSlots=reserve;Floors=Array.AsReadOnly(floors.ToArray()); }
    }

    // Unpublished scalar evidence for a future trusted opt-in profile. No live caller,
    // admission token, saved reservation, package authentication or new gameplay limit.
    internal static class ConstructionJournalReserve
    {
        private sealed class Exhausted : Exception { }
        private sealed class Work
        {
            internal long Used { get; private set; }
            private readonly long limit;
            internal Work(long value) { limit=value; }
            internal void Charge(BigInteger amount)
            { if(amount<0||amount>limit-Used)throw new Exhausted();Used+=(long)amount; }
        }

        // package is the actual frozen/rebuilt package, not its pre-generation template.
        // expectedPackageHash is a consistency assertion from the trusted future caller,
        // not authority supplied by a save. Candidate/source/provenance validation and
        // absence of concurrent caller mutation are prerequisites, as for existing DTO APIs.
        internal static ConstructionJournalFacts Evaluate(CampaignPlayableFleetPackageDto package,string expectedPackageHash,
            CampaignTacticalTravelSaveDto candidate,IReadOnlyList<CampaignIntegratedBuiltShipDto> staticRecords,
            IReadOnlyList<CampaignPublishedBuiltShipDto> publishedRecords,long journalUsed,ConstructionJournalOperation operation,
            long recordBudget,long workLimit=10000000)
        {
            var game=candidate.Core.Campaign.GameState;
            var work=new Work(workLimit);var floors=new List<ConstructionIndustryFloor>();
            long pending=0,retained=0,horizon=0,ships=0,encounters=0,batch=0,debt=0,reserve=0;
            bool records=false,journal=false;
            int delta=operation==ConstructionJournalOperation.PlayerOptional?1:0;
            long limit=package.ReplayOperationLimit??0;
            ConstructionJournalFacts Result(string? reason)=>new ConstructionJournalFacts(package.PackageHash??"",candidate.Core.Campaign.ContentHash,
                candidate.Core.Campaign.RulesVersion,game.TurnNumber,game.StateRevision,candidate.Phase,operation,delta,journalUsed,limit,work.Used,
                reason,records,journal,pending,retained,horizon,ships,encounters,batch,debt,reserve,floors);
            try
            {
                // 65536 is this detached calculator's supported technical journal domain,
                // not a new live package policy, fleet cap or approved balance value.
                if(workLimit<=0||workLimit>10000000||!Enum.IsDefined(typeof(ConstructionJournalOperation),operation)||journalUsed<0||limit<1||limit>65536
                    ||journalUsed>limit||recordBudget<0)return Result("Unsupported journal/operation domain");
                var physical=package.Integrated?.Physical;var core=physical?.CorePackage;var scenario=core?.Campaign;
                if(package.FormatVersion!=CampaignPlayableFleetIdentity.Format||package.ProfileId!=CampaignPlayableFleetIdentity.Profile
                    ||package.PackageHash!=expectedPackageHash||!CampaignTacticalReviewData.Hash(expectedPackageHash)
                    ||package.PolicyStatus!="proposed"||package.DefaultActivationAllowed!=false||scenario==null
                    ||core!.ProfileId!=CampaignTacticalReviewIdentity.Profile||core.FormatVersion!=CampaignTacticalReviewIdentity.PackageFormat
                    ||physical!.ProfileId!=CampaignTacticalTravelIdentity.Profile||physical.FormatVersion!=CampaignTacticalTravelIdentity.PackageFormat
                    ||candidate.PackageHash!=physical.PackageHash||candidate.Core.PackageHash!=core.PackageHash
                    ||candidate.Core.Campaign.ContentHash!=scenario.ContentHash||candidate.Core.Campaign.ScenarioId!=scenario.ScenarioId
                    ||candidate.Core.Campaign.RulesVersion!=scenario.RulesVersion)return Result("Frozen package mismatch");
                var definitions=scenario.Definitions;var initial=scenario.InitialState;
                work.Charge((BigInteger)game.Colonies!.Count+game.Ships!.Count+game.Fleets!.Count+game.Planets!.Count
                    +initial.Colonies!.Count+definitions.GovernancePolicies!.Count+staticRecords.Count+publishedRecords.Count);
                if(game.TurnNumber<initial.TurnNumber||game.StateRevision<initial.StateRevision
                    ||(candidate.Phase!=SessionPhase.Planning&&candidate.Phase!=SessionPhase.AwaitingCombat)
                    ||game.Civilizations!.Count(c=>c.ControllerKind==ControllerKind.Human)!=1
                    ||!game.Civilizations.Select(c=>(c.CivilizationId,c.ControllerKind)).OrderBy(c=>c.CivilizationId,StringComparer.Ordinal)
                        .SequenceEqual(initial.Civilizations!.Select(c=>(c.CivilizationId,c.ControllerKind)).OrderBy(c=>c.CivilizationId,StringComparer.Ordinal)))
                    return Result("Unsupported stamp/phase/controllers");
                if(game.Colonies.Count!=initial.Colonies.Count)return Result("Changed fixed worlds");
                var originals=initial.Colonies.ToDictionary(c=>c.ColonyId,StringComparer.Ordinal);
                var queueIds=new HashSet<string>(StringComparer.Ordinal);
                foreach(var colony in game.Colonies)
                {
                    work.Charge((BigInteger)colony.Infrastructure!.Count*colony.Infrastructure.Count+colony.ProductionQueue!.Count+1);
                    if(originals.TryGetValue(colony.ColonyId,out var originalWorld))
                        work.Charge((BigInteger)originalWorld.Infrastructure!.Count*originalWorld.Infrastructure.Count);
                    if(!originals.TryGetValue(colony.ColonyId,out var original)||colony.OwnerCivilizationId!=original.OwnerCivilizationId
                        ||colony.Automation!=original.Automation||colony.PlanetId!=original.PlanetId||colony.SystemId!=original.SystemId
                        ||!colony.Infrastructure.Select(i=>(i.DefinitionId,i.Count)).OrderBy(i=>i.DefinitionId,StringComparer.Ordinal)
                            .SequenceEqual(original.Infrastructure!.Select(i=>(i.DefinitionId,i.Count)).OrderBy(i=>i.DefinitionId,StringComparer.Ordinal)))
                        return Result("Changed fixed Industry determinants");
                    foreach(var q in colony.ProductionQueue)
                    {
                        work.Charge(q.PaidResources?.Count??0);
                        if(string.IsNullOrWhiteSpace(q.QueueItemId)||!queueIds.Add(q.QueueItemId)||q.RequiredIndustry<=0
                            ||q.CompletedIndustry<0||q.CompletedIndustry>=q.RequiredIndustry||q.PaidCredits<0
                            ||q.PaidResources==null||q.PaidResources.Any(r=>r==null||r.Amount<0))return Result("Invalid pending work");
                        pending++;
                    }
                }
                var retainedShips=new HashSet<string>(StringComparer.Ordinal);var retainedQueues=new HashSet<string>(StringComparer.Ordinal);
                foreach(var r in staticRecords.Select(r=>(r.ShipId,r.QueueItemId)).Concat(publishedRecords.Select(r=>(r.ShipId,r.QueueItemId))))
                {
                    if(string.IsNullOrWhiteSpace(r.ShipId)||string.IsNullOrWhiteSpace(r.QueueItemId)||!retainedShips.Add(r.ShipId!)
                        ||!retainedQueues.Add(r.QueueItemId!)||queueIds.Contains(r.QueueItemId!))return Result("Ambiguous retained/pending identities");
                    retained++;
                }
                records=(BigInteger)retained+pending<=recordBudget;
                // Project copies only the selected world: the full state/definitions are
                // read-only inputs. Project writes neither; no caller object is modified.
                foreach(var colony in game.Colonies.OrderBy(c=>c.ColonyId,StringComparer.Ordinal))
                {
                    if(colony.ProductionQueue!.Count==0)continue;
                    long minimum=long.MaxValue,food=0;
                    var planet=definitions.Planets!.Single(p=>p.DefinitionId==game.Planets!.Single(p=>p.PlanetId==colony.PlanetId).DefinitionId);
                    BigInteger projectionWork=64+(BigInteger)game.Planets!.Count+definitions.Planets!.Count
                        +(BigInteger)colony.Infrastructure!.Count*(definitions.Infrastructure!.Count+4)
                        +(BigInteger)(colony.Cohorts!.Count+1)*(planet.Habitats!.Count+1)*(definitions.Species!.Count+1)*16
                        +(BigInteger)(colony.Cohorts.Count+1)*(colony.Cohorts.Count+1)*(planet.Habitats.Count+1)*8;
                    foreach(var cohort in colony.Cohorts) { work.Charge(1);projectionWork+=cohort.Memories!.Count; }
                    foreach(var report in colony.Politics!.CohortReports!) { work.Charge(1);projectionWork+=report.Reasons!.Count; }
                    projectionWork+=colony.Stockpiles!.Count+colony.Politics.Events!.Count;
                    foreach(var q in colony.ProductionQueue) { work.Charge(1);projectionWork+=q.PaidResources!.Count+1; }
                    foreach(var policy in definitions.GovernancePolicies)
                    foreach(ResistanceState resistance in Enum.GetValues(typeof(ResistanceState)))
                    foreach(var disruption in new[]{0,1})
                    {
                        work.Charge(projectionWork);
                        var copy=PopulationData.Copy(colony);
                        copy.Politics!.GovernancePolicyDefinitionId=policy.DefinitionId;copy.Politics.Resistance=resistance;
                        copy.Politics.DisruptionRemainingTurns=disruption;
                        var projected=WorldEconomy.Project(copy,game,definitions);
                        minimum=Math.Min(minimum,projected.Industry);food=projected.FoodOutput;
                    }
                    if(minimum<=0||minimum==long.MaxValue)return Result("No positive exhaustive Industry floor");
                    floors.Add(new ConstructionIndustryFloor(colony.ColonyId,minimum));
                    BigInteger colonyTurns=0;
                    foreach(var q in colony.ProductionQueue)
                    {
                        work.Charge(1);long remaining=q.RequiredIndustry-q.CompletedIndustry;
                        colonyTurns+=Math.Max(1,remaining/minimum+(remaining%minimum==0?0:1));
                    }
                    if(colonyTurns>65536)return Result("Horizon exceeds supported journal domain");
                    horizon=Math.Max(horizon,(long)colonyTurns);
                    // A deliberately conservative directly-provable numeric precondition,
                    // not a claim to certify every unrelated economy counter.
                    if((BigInteger)colony.Food+colonyTurns*food>long.MaxValue)return Result("Food arithmetic headroom");
                }
                var controllers=game.Civilizations.ToDictionary(c=>c.CivilizationId,c=>c.ControllerKind!.Value,StringComparer.Ordinal);
                work.Charge(candidate.Core.Battles.Sum(b=>(long)b.State.Formations.Count+1));
                if(candidate.Phase==SessionPhase.AwaitingCombat&&candidate.Core.Battles.Count==0)return Result("Missing current combat batch");
                debt=candidate.Phase==SessionPhase.AwaitingCombat?PlayableAdvanceCapacity.RequiredSlots(candidate,controllers)-1:0;
                if(candidate.Phase==SessionPhase.Planning&&candidate.Core.Battles.Count!=0)return Result("Planning has combat debt");
                // Trusted full validation ensures nonempty fleets, disjoint requests and
                // exact source/payment/history. Check the cheap fleet premise here too.
                if(game.Fleets.Any(f=>f.ShipIds==null||f.ShipIds.Count==0))return Result("Empty fleet violates encounter bound");
                ships=checked((long)game.Ships.Count+pending);encounters=ships/2;batch=checked(2+3*encounters);
                BigInteger total=debt+(BigInteger)horizon*batch;
                if(total>long.MaxValue)return Result("Reserve arithmetic overflow");reserve=(long)total;
                // Conservative headroom for known player operations, internal AI orders,
                // round revisions, production and bounded cohort/encounter allocations.
                BigInteger futureRevisions=(limit-journalUsed)+(BigInteger)horizon*(game.Colonies.Count+ships+3*encounters+2);
                BigInteger futureIds=2*pending+(BigInteger)horizon*(encounters+1+PopulationRules.CohortCeiling*(BigInteger)game.Colonies.Count*game.Colonies.Count)
                    +(BigInteger)(limit-journalUsed)*(game.Colonies.Count+1);
                if((BigInteger)game.TurnNumber+horizon+(debt>0?1:0)>int.MaxValue
                    ||(BigInteger)game.StateRevision+futureRevisions>long.MaxValue
                    ||(BigInteger)game.NextEntitySequence+futureIds>ulong.MaxValue)return Result("Turn/revision/allocation headroom");
                journal=(BigInteger)journalUsed+delta+total<=limit;
                return Result(records&&journal?null:!records?"Retained record capacity":"Journal reserve does not fit");
            }
            catch(Exhausted) { return Result("Helper work budget exhausted"); }
            catch(OverflowException) { return Result("Checked arithmetic failed"); }
            catch(ArgumentException) { return Result("Invalid closed-rule input"); }
            catch(InvalidOperationException) { return Result("Invalid closed-rule association"); }
        }
    }
}
