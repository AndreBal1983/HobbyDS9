using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static partial class ColonisationRootBudget
    {
        private static (long Visits,long Bytes) BeltBodiesCost(CampaignBeltBodiesSaveDto root,ColonisationPolicyDto policy)
        {
            var s=root.BeltBodies;
            if(s==null||s.PolicyId!=BeltBodiesEditionIdentity.Policy||s.Definitions==null||s.Deposits==null||s.Observations==null||s.Surveys==null||s.Mines==null||s.LastShipments==null||s.Operations==null
                ||s.Definitions.Count>BeltBodiesEditionIdentity.MaximumBodies||s.Deposits.Count>BeltBodiesEditionIdentity.MaximumBodies
                ||s.Observations.Count>BeltBodiesEditionIdentity.MaximumObservations||s.Surveys.Count>BeltBodiesEditionIdentity.MaximumSurveys
                ||s.Mines.Count>BeltBodiesEditionIdentity.MaximumMines||s.LastShipments.Count>BeltBodiesEditionIdentity.MaximumLastShipments||s.Operations.Count>BeltBodiesEditionIdentity.MaximumCommands
                ||root.BaseCampaign?.Journal==null||root.Refinement?.Operations==null||root.Refinement.Operations.Count>EngineeringRefinementIdentity.MaximumCommands
                ||root.Refinement.Projects==null||root.Refinement.Projects.Count>EngineeringRefinementIdentity.MaximumProjects)Fail("Complete bounded belt-body state required.");
            var stamps=new HashSet<(int,long)>();
            var priorOffset=0;long priorRevision=-1;
            foreach(var op in root.Refinement!.Operations!)
            {
                if(op==null||op.Command==null||!stamps.Add((op.BaseJournalOffset,op.Command.ExpectedRevision)))Fail("Unique extension revision order required.");
                if(op.BaseJournalOffset<priorOffset||op.BaseJournalOffset==priorOffset&&op.Command.ExpectedRevision<=priorRevision)Fail("Ordered refinement extensions required.");
                priorOffset=op.BaseJournalOffset;priorRevision=op.Command.ExpectedRevision;
            }
            priorOffset=0;priorRevision=-1;
            foreach(var op in s!.Operations!)
            {
                if(op==null||op.Command==null||op.BaseJournalOffset<priorOffset||op.BaseJournalOffset>root.BaseCampaign!.Journal!.Count
                    ||op.BaseJournalOffset==priorOffset&&op.Command.ExpectedRevision<=priorRevision||!stamps.Add((op.BaseJournalOffset,op.Command.ExpectedRevision)))
                    Fail("Ordered bounded belt-body operations required.");
                priorOffset=op.BaseJournalOffset;priorRevision=op.Command.ExpectedRevision;
            }
            return new Counter(policy.MaximumValidationVisits,policy.MaximumCanonicalRootBytes,ColonisationSourceEdition.Growth,true).BeltBodies(s);
        }
        private sealed partial class Counter
        {
            internal (long Visits,long Bytes) BeltBodies(BeltBodyStateDto s)
            {
                var initialVisits=remainingVisits;var initialBytes=remainingBytes;Count(128);Text(s.PolicyId);
                Count(2);foreach(var d in s.Definitions!)
                {Need(d);Count(256);Text(d.BodyId);Text(d.FeatureId);Text(d.SystemId);Text(d.DisplayName);V(d.HabitabilityBps);Text(d.ResourceDefinitionId);V(d.InitialDeposit);}
                Count(2);foreach(var d in s.Deposits!)
                {Need(d);Count(128);Text(d.BodyId);V(d.Remaining);V(d.LocalStock);}
                Count(2);foreach(var o in s.Observations!)
                {Need(o);Count(192);Text(o.ActorId);Text(o.BodyId);V(o.SurveyedTurn);V(o.ObservedTurn);V(o.ObservedRemaining);}
                Count(2);foreach(var q in s.Surveys!)
                {Need(q);Count(256);Text(q.SurveyId);Text(q.ActorId);Text(q.ShipId);Text(q.BodyId);V((int)q.Stage);V(q.CompletedWeeks);V(q.StartedTurn);V(q.ResolvedTurn);}
                Count(2);foreach(var m in s.Mines!)
                {Need(m);Count(320);Text(m.InstallationId);Text(m.QueueItemId);Text(m.ActorId);Text(m.BodyId);Text(m.ColonyId);V((int)m.Stage);V(m.CompletedTurn);V(m.TotalExtracted);V(m.TotalDelivered);V(m.LastExtractionTurn);V(m.LastDeliveryTurn);Text(m.LatestReport);}
                Count(2);foreach(var f in s.LastShipments!)
                {Need(f);Count(256);Text(f.InstallationId);Text(f.ActorId);Text(f.BodyId);Text(f.ColonyId);Text(f.ResourceDefinitionId);V(f.Amount);V(f.Turn);}
                Count(2);foreach(var op in s.Operations!)
                {Need(op);Count(96);V(op.BaseJournalOffset);var c=op.Command!;Count(256);Text(c.ActorId);Text(c.CommandId);V(c.ExpectedTurn);V(c.ExpectedRevision);V((int)c.Action);Text(c.BodyId);Text(c.ShipId);Text(c.ColonyId);Text(c.SurveyId);Text(c.MineId);}
                return(initialVisits-remainingVisits,initialBytes-remainingBytes);
            }
        }
    }
}
