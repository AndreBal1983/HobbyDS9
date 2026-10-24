using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal enum ConstructionBoundaryCertificate { Uncertifiable, AllPaidQueuesCompleteNextStagedAdvance }

    internal sealed class ConstructionBoundaryFacts
    {
        internal long RetainedRecords { get; }
        internal long PendingReservations { get; }
        internal bool RecordCapacity { get; }
        internal long NextBatchSlots { get; }
        // One cancellation operation per remaining queue. This is a slot count,
        // NOT a promise that cancellation is legal in the current combat phase.
        internal long CancellationSlots { get; }
        internal bool NextBatchFitsJournal { get; }
        internal bool CancellationReserveFitsAfterBatch { get; }
        internal bool JournalCapacity => NextBatchFitsJournal && CancellationReserveFitsAfterBatch;
        internal bool AllPaidQueuesCompleteNextAdvance { get; }
        internal bool CertifiedNextBoundary => RecordCapacity && JournalCapacity && AllPaidQueuesCompleteNextAdvance;
        internal ConstructionBoundaryCertificate Certificate => CertifiedNextBoundary
            ? ConstructionBoundaryCertificate.AllPaidQueuesCompleteNextStagedAdvance : ConstructionBoundaryCertificate.Uncertifiable;
        internal ConstructionBoundaryFacts(long retained, long pending, bool records, long batch, long cancel, bool batchFits, bool cancelFits, bool complete)
        { RetainedRecords=retained; PendingReservations=pending; RecordCapacity=records; NextBatchSlots=batch;
          CancellationSlots=cancel; NextBatchFitsJournal=batchFits; CancellationReserveFitsAfterBatch=cancelFits; AllPaidQueuesCompleteNextAdvance=complete; }
    }

    // Unpublished diagnostics only. Neither a queue permission nor a transaction validator.
    internal static class ConstructionBoundaryCapacity
    {
        private readonly struct Record
        {
            internal readonly string Ship, Queue, Owner, Fleet, Design;
            internal readonly string? Blueprint;
            internal readonly int? Revision, Turn;
            internal Record(string? ship,string? queue,string? owner,string? fleet,int? turn,string? design,string? blueprint,int? revision)
            { Ship=ship??"";Queue=queue??"";Owner=owner??"";Fleet=fleet??"";Turn=turn;Design=design??"";Blueprint=blueprint;Revision=revision; }
            internal bool Same(Record r)=> (Ship,Queue,Owner,Fleet,Turn,Design,Blueprint,Revision)==(r.Ship,r.Queue,r.Owner,r.Fleet,r.Turn,r.Design,r.Blueprint,r.Revision);
        }
        private static Record[] Records(IReadOnlyList<CampaignIntegratedBuiltShipDto> statics,IReadOnlyList<CampaignPublishedBuiltShipDto> published)
            => statics.Select(r=>new Record(r.ShipId,r.QueueItemId,r.OwnerCivilizationId,r.FleetIdAtConstruction,r.TurnBuilt,r.ShipDesignDefinitionId,null,null))
                .Concat(published.Select(r=>new Record(r.ShipId,r.QueueItemId,r.OwnerCivilizationId,r.FleetIdAtConstruction,r.TurnBuilt,null,r.BlueprintId,r.BlueprintRevision))).ToArray();

        // beforeProduction MUST be the actual post-Economy/post-Research, pre-Production
        // staged state. It is not today's Planning state or an industry forecast.
        // The future trusted caller supplies a validated final staged boundary and its
        // actual production association. These facts cannot authenticate a fabricated
        // history, authorize admission, or promise completion beyond this one boundary.
        internal static ConstructionBoundaryFacts Evaluate(GameStateDto beforeProduction, GameStateDto afterProduction,
            DefinitionCatalogDto definitions, CampaignTacticalTravelSaveDto next, IReadOnlyDictionary<string,ControllerKind> controllers,
            IReadOnlyList<CampaignIntegratedBuiltShipDto> staticBefore, IReadOnlyList<CampaignIntegratedBuiltShipDto> staticAfter,
            IReadOnlyList<CampaignPublishedBuiltShipDto> publishedBefore, IReadOnlyList<CampaignPublishedBuiltShipDto> publishedAfter,
            long recordBudget, long journalUsed, long journalLimit, int expectedTurn, long expectedRevision)
        {
            if(recordBudget<0 || journalUsed<0 || journalLimit<journalUsed) throw new ArgumentException("Invalid technical capacity.");
            if(beforeProduction.TurnNumber!=expectedTurn || beforeProduction.StateRevision!=expectedRevision)
                throw new ArgumentException("Stale production boundary.");
            var before=SimulationSession.CopyState(beforeProduction);
            var after=SimulationSession.CopyState(afterProduction);
            var old=Records(staticBefore,publishedBefore); var retained=Records(staticAfter,publishedAfter);
            var pending=before.Colonies!.SelectMany(c=>c.ProductionQueue!).ToArray();
            // A bounded helper workload, NOT a permanent fleet or campaign cap.
            if((long)old.Length+retained.Length+pending.Length>32768) throw new ArgumentException("Detached capacity work bound exceeded.");
            Require(old.All(r=>r.Ship.Length>0 && r.Queue.Length>0) && retained.All(r=>r.Ship.Length>0 && r.Queue.Length>0),"Missing retained identity.");
            Require(old.Select(r=>r.Ship).Distinct(StringComparer.Ordinal).Count()==old.Length
                && old.Select(r=>r.Queue).Distinct(StringComparer.Ordinal).Count()==old.Length
                && retained.Select(r=>r.Ship).Distinct(StringComparer.Ordinal).Count()==retained.Length
                && retained.Select(r=>r.Queue).Distinct(StringComparer.Ordinal).Count()==retained.Length,"Ambiguous retained identity.");
            var oldByShip=old.ToDictionary(r=>r.Ship,StringComparer.Ordinal);
            var retainedByShip=retained.ToDictionary(r=>r.Ship,StringComparer.Ordinal);
            Require(old.All(r=>retainedByShip.TryGetValue(r.Ship,out var n)&&r.Same(n)),"Retained source association changed.");
            var oldQueues=new HashSet<string>(old.Select(r=>r.Queue),StringComparer.Ordinal);
            Require(pending.All(q=>!oldQueues.Contains(q.QueueItemId)) && pending.Select(q=>q.QueueItemId).Distinct(StringComparer.Ordinal).Count()==pending.Length,"Ambiguous pending identity.");

            // Execute the REAL production phase on a private copy, including all owners.
            // Exact phase guards compare the supplied actual association against it;
            // no caller boolean, forecast, or projected multi-turn horizon is accepted.
            var replay=SimulationSession.CopyState(before);
            ProductionPhase.Execute(replay,definitions);
            SimulationSession.RequirePhaseChanges(SimulationSession.CopyState(replay),after,allowEconomy:false);
            Require(next.MovementProof!=null,"Missing production/movement association.");
            SimulationSession.RequirePhaseChanges(after,SimulationSession.CopyState(next.MovementProof!.Start),allowEconomy:false);
            var final=next.Core.Campaign.GameState;
            Require(final.StateRevision>expectedRevision && (next.Phase==SessionPhase.Planning
                ? (long)final.TurnNumber==(long)expectedTurn+1 : next.Phase==SessionPhase.AwaitingCombat && final.TurnNumber==expectedTurn),"Stale final boundary.");

            var originalShips=new HashSet<string>(before.Ships!.Select(s=>s.ShipId),StringComparer.Ordinal);
            var born=after.Ships!.Where(s=>!originalShips.Contains(s.ShipId)).ToArray();
            var removed=before.Colonies!.OrderBy(c=>c.ColonyId,StringComparer.Ordinal)
                .Where(c=>c.ProductionQueue!.Count>after.Colonies!.Single(n=>n.ColonyId==c.ColonyId).ProductionQueue!.Count)
                .Select(c=>(Owner:c.OwnerCivilizationId,Queue:c.ProductionQueue![0])).ToArray();
            var added=retained.Where(r=>!oldByShip.ContainsKey(r.Ship)).ToArray();
            Require(born.Length==removed.Length && added.Length==born.Length
                && after.PopulationStatistics!.ProductionCompleted-before.PopulationStatistics!.ProductionCompleted==born.Length,"Incomplete production ledger.");
            for(int i=0;i<born.Length;i++)
            {
                var ship=born[i];var q=removed[i];
                Require(retainedByShip.TryGetValue(ship.ShipId,out var r) && !oldByShip.ContainsKey(ship.ShipId)
                    && r.Queue==q.Queue.QueueItemId && r.Owner==q.Owner && r.Owner==ship.OwnerCivilizationId
                    && r.Fleet==ship.FleetId && r.Turn==expectedTurn && r.Design==ship.ShipDesignDefinitionId
                    && r.Blueprint==ship.Blueprint?.BlueprintId && r.Revision==ship.Blueprint?.Revision,"Wrong production-born association.");
            }
            // Final combat can destroy a born ship. Retained records, not final live
            // ships, preserve its completed queue association.
            long remaining=after.Colonies!.Sum(c=>(long)c.ProductionQueue!.Count);
            long batch=PlayableAdvanceCapacity.RequiredSlots(next,controllers);
            long available=journalLimit-journalUsed;
            return new ConstructionBoundaryFacts(old.Length,pending.Length,(long)old.Length+pending.Length<=recordBudget,
                batch,remaining,batch<=available,batch<=available && remaining<=available-batch,remaining==0);
        }
        private static void Require(bool condition,string message) { if(!condition)throw new ArgumentException(message); }
    }
}
