using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Application
{
    // Immutable projections of validated, detached request/result data. No combat
    // re-simulation and no persisted presentation state. Held fleets/unchanged hull
    // are the result contract's explicit omission semantics.
    public sealed class BattleReportSnapshot
    {
        public string EncounterId { get; }
        public NavigationNodeId LocationNodeId { get; }
        public int TurnNumber { get; }
        public IReadOnlyList<BattleFleetReport> Fleets { get; }
        public IReadOnlyList<BattlePhaseReport> Phases { get; }
        public BigInteger ShieldDamage { get; }
        public BigInteger HullDamage { get; }
        public int DestroyedShips => Fleets.Sum(f => f.Ships.Count(s => s.RemainingHull == 0));
        public BattleReportSnapshot(ResolvedCombatDto combat)
        {
            var request = combat.Request; var result = combat.Result;
            EncounterId = request.EncounterId; LocationNodeId = new NavigationNodeId(request.LocationNodeId); TurnNumber = request.TurnNumber;
            Fleets = request.Participants.OrderBy(p => p.FleetId, System.StringComparer.Ordinal)
                .Select(p => new BattleFleetReport(p, result)).ToList().AsReadOnly();
            var events = result.Events.OrderBy(e => e.Sequence).Select(e => new BattleEventSnapshot(e)).ToList();
            Phases = new[] { CombatPhaseKind.LongRange, CombatPhaseKind.MediumRange, CombatPhaseKind.CloseRange }
                .Select(phase => new BattlePhaseReport(phase, events.Where(e => e.Phase == phase))).ToList().AsReadOnly();
            ShieldDamage = events.Where(e => e.Kind == CombatEventKind.ShieldDamaged).Aggregate(BigInteger.Zero, (sum, e) => sum + (e.Amount ?? 0));
            HullDamage = events.Where(e => e.Kind == CombatEventKind.ShipDamaged).Aggregate(BigInteger.Zero, (sum, e) => sum + (e.Amount ?? 0));
        }
    }
    public sealed class BattleFleetReport
    {
        public string FleetId { get; }
        public string CivilizationId { get; }
        public FleetCombatDisposition Disposition { get; }
        public string? RetreatNodeId { get; }
        public IReadOnlyList<BattleShipReport> Ships { get; }
        internal BattleFleetReport(CombatParticipantDto participant, CombatResultDto result)
        {
            FleetId = participant.FleetId; CivilizationId = participant.CivilizationId;
            var change = result.FleetChanges.SingleOrDefault(f => f.FleetId == FleetId);
            Disposition = change?.Disposition ?? FleetCombatDisposition.Held; RetreatNodeId = change?.RetreatNodeId;
            Ships = participant.Ships.OrderBy(s => s.ShipId, System.StringComparer.Ordinal)
                .Select(s => new BattleShipReport(s, result.ShipChanges.SingleOrDefault(c => c.ShipId == s.ShipId)?.RemainingHull ?? s.Hull)).ToList().AsReadOnly();
        }
    }
    public sealed class BattleShipReport
    {
        public string ShipId { get; }
        public string DesignId { get; }
        public long StartingHull { get; }
        public long RemainingHull { get; }
        internal BattleShipReport(CombatShipDto ship, long remaining)
        { ShipId = ship.ShipId; DesignId = ship.DesignId; StartingHull = ship.Hull; RemainingHull = remaining; }
    }
    public sealed class BattlePhaseReport
    {
        public CombatPhaseKind Phase { get; }
        public IReadOnlyList<BattleEventSnapshot> Events { get; }
        public int Hits => Events.Count(e => e.Kind == CombatEventKind.AttackHit);
        public int Misses => Events.Count(e => e.Kind == CombatEventKind.AttackMissed);
        public int Attacks => Hits + Misses;
        internal BattlePhaseReport(CombatPhaseKind phase, IEnumerable<BattleEventSnapshot> events)
        { Phase = phase; Events = events.ToList().AsReadOnly(); }
    }
    public sealed class BattleEventSnapshot
    {
        public int Sequence { get; }
        public CombatEventKind Kind { get; }
        public CombatPhaseKind? Phase { get; }
        public string? SourceFleetId { get; }
        public string? SourceShipId { get; }
        public long? Amount { get; }
        public string? FleetId { get; }
        public string? ShipId { get; }
        internal BattleEventSnapshot(CombatEventDto dto)
        {
            Sequence = dto.Sequence; Kind = dto.Kind; Phase = dto.Phase;
            SourceFleetId = dto.SourceFleetId; SourceShipId = dto.SourceShipId;
            Amount = dto.Amount; FleetId = dto.FleetId; ShipId = dto.ShipId;
        }
    }
}
