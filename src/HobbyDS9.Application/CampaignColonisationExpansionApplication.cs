using System;
using System.Collections.Generic;
using System.Diagnostics;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
namespace HobbyDS9.Application
{
    public enum ColonisationLoadPhase { ExpansionPreflight, CorePreflight, FrozenMap, InitialState, Journal, CoreEquality, ExpansionEquality }
    public enum ColonisationLoadOperationKind { Unclassified, Colonisation, Activity, Organization, Travel, Strategic, Cloak, Advance, Round, Finalize }
    public enum ColonisationLoadPhaseState { NotReached, Active, Returned, Failed }
    public sealed class ColonisationLoadPhaseTiming
    {
        public ColonisationLoadPhase Phase { get; }
        public ColonisationLoadPhaseState State { get; }
        public long ElapsedTicks { get; }
        internal ColonisationLoadPhaseTiming(int phase,byte state,long ticks)
        {Phase=(ColonisationLoadPhase)phase;State=(ColonisationLoadPhaseState)state;ElapsedTicks=ticks;}
    }
    public sealed class ColonisationLoadOperationCount
    {
        public ColonisationLoadOperationKind Kind { get; }
        public int Attempted { get; }
        public int Accepted { get; }
        public int Refused { get; }
        public int EqualityVerified { get; }
        internal ColonisationLoadOperationCount(int kind,int attempted,int accepted,int refused,int verified)
        {Kind=(ColonisationLoadOperationKind)kind;Attempted=attempted;Accepted=accepted;Refused=refused;EqualityVerified=verified;}
    }
    // Observation only, outside persisted Contracts. Timings are inclusive and non-additive.
    public sealed class ColonisationLoadTimingReport
    {
        public long StopwatchFrequency { get; }
        public IReadOnlyList<ColonisationLoadPhaseTiming> Phases { get; }
        public IReadOnlyList<ColonisationLoadOperationCount> Operations { get; }
        internal ColonisationLoadTimingReport(ColonisationReplayTiming timing)
        {
            StopwatchFrequency=Stopwatch.Frequency;
            var phases=new ColonisationLoadPhaseTiming[7];
            for(var i=0;i<phases.Length;i++)phases[i]=new ColonisationLoadPhaseTiming(i,timing.State[i],timing.Ticks[i]);
            var operations=new ColonisationLoadOperationCount[10];
            for(var i=0;i<operations.Length;i++)operations[i]=new ColonisationLoadOperationCount(i,timing.Counts[i,0],timing.Counts[i,1],timing.Counts[i,2],timing.Counts[i,3]);
            Phases=Array.AsReadOnly(phases);Operations=Array.AsReadOnly(operations);
        }
    }
    public interface ICampaignColonisationExpansionSession:ICampaignColonisationOperations
    {
        CampaignColonisationExpansionSaveDto ExportColonisationExpansionSave();
        ICampaignColonisationExpansionSession ForkInteraction(int turn,long revision);
    }
    public sealed class CampaignColonisationExpansionApplication:CampaignIntegratedPlaytestApplication,ICampaignColonisationExpansionSession
    {
        private CampaignColonisationExpansionApplication(CampaignTacticalReviewSession backend):base(backend){}
        public static CampaignColonisationExpansionPackageDto CreatePackage(CampaignColonisationRosterPackageDto roster)=>CampaignTacticalReviewSession.CreateColonisationExpansionPackage(roster);
        public static ICampaignColonisationExpansionSession StartGame(CampaignColonisationExpansionPackageDto package,NaturalGalaxySettingsDto settings)
            =>new CampaignColonisationExpansionApplication(CampaignTacticalReviewSession.StartColonisationExpansion(package,settings));
        public static ICampaignColonisationExpansionSession LoadSave(CampaignColonisationExpansionPackageDto package,CampaignColonisationExpansionSaveDto root)
            =>new CampaignColonisationExpansionApplication(CampaignTacticalReviewSession.LoadColonisationExpansion(package,root));
        public static ICampaignColonisationExpansionSession LoadSave(CampaignColonisationExpansionPackageDto package,CampaignColonisationExpansionSaveDto root,out ColonisationLoadTimingReport? report)
        {
            report=null;ColonisationReplayTiming? timing=null;
            try { timing=new ColonisationReplayTiming(); } catch(Exception) { }
            try { return new CampaignColonisationExpansionApplication(CampaignTacticalReviewSession.LoadColonisationExpansion(package,root,timing)); }
            finally
            {
                // Only diagnostics are shielded; load exceptions and their messages escape unchanged.
                if(timing!=null)try { report=new ColonisationLoadTimingReport(timing); } catch(Exception) { }
            }
        }
        public CampaignGeneratedMapDto CaptureGeneratedMap(string actor)=>session.CaptureGeneratedMap(actor);
        public CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor)=>session.CaptureKnownAstronomy(actor);
        public CampaignColonisationExpansionSaveDto ExportColonisationExpansionSave()=>session.ExportColonisationExpansionSave();
        public ColonisationCampaignSnapshot CaptureColonisation(string actor)=>session.CaptureColonisation(actor);
        public IReadOnlyList<ColonisationPlanetObservationDto> CaptureColonisationPlanets(string actor)=>session.CaptureColonisationPlanets(actor);
        public CampaignActivityObservationDto CaptureActivities(string actor)=>session.CaptureActivities(actor);
        public FrontierCommissionSnapshot CaptureFrontierCommission(string actor)=>session.CaptureFrontierCommission(actor);
        public ColonisationEligibilitySnapshot EvaluateColonisation(ColonisationCommandDto command)=>session.EvaluateColonisation(command);
        public CampaignTacticalMutationResult SubmitColonisation(ColonisationCommandDto? command)=>session.SubmitColonisation(command);
        public CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command)=>session.EvaluateActivity(command);
        public CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command)=>session.SubmitActivity(command);
        public ICampaignColonisationExpansionSession ForkInteraction(int turn,long revision)=>new CampaignColonisationExpansionApplication(session.ForkColonisationInteraction(turn,revision));
        public ICampaignColonisationOperations ForkOperations(int turn,long revision)=>ForkInteraction(turn,revision);
        public int RemainingPlayableOperations=>session.RemainingPlayableOperations;
        public CampaignPlayableFleetSaveDto ExportPlayableSave()=>session.ExportPlayableSave();
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor)=>session.CaptureFleetOrganization(actor);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command)=>session.SubmitFleetOrganization(command);
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter,string actor)=>session.CaptureBattlePlayback(encounter,actor);
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor)=>session.GetBattleHistory(actor);
    }
}
