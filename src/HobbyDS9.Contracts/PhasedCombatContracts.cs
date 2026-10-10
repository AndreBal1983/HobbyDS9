using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public sealed class ShipCombatProfileDto
    {
        public long ShieldCapacity { get; set; }
        public long LongRangeAttack { get; set; }
        public long MediumRangeAttack { get; set; }
        public long CloseRangeAttack { get; set; }
        public int AccuracyBps { get; set; }
        public int EvasionBps { get; set; }
    }
    public sealed class ShipCombatProfileSnapshot
    {
        public long ShieldCapacity { get; }
        public long LongRangeAttack { get; }
        public long MediumRangeAttack { get; }
        public long CloseRangeAttack { get; }
        public int AccuracyBps { get; }
        public int EvasionBps { get; }
        public ShipCombatProfileSnapshot(ShipCombatProfileDto profile)
        {
            ShieldCapacity = profile.ShieldCapacity; LongRangeAttack = profile.LongRangeAttack;
            MediumRangeAttack = profile.MediumRangeAttack; CloseRangeAttack = profile.CloseRangeAttack;
            AccuracyBps = profile.AccuracyBps; EvasionBps = profile.EvasionBps;
        }
    }
    public enum CombatPhaseKind { LongRange = 1, MediumRange = 2, CloseRange = 3 }
    public enum CombatTactic { Standard = 1, FocusFire = 2, Evasive = 3, Overcharge = 4 }
    public enum CombatTargetPriority { LowestHullRatio = 1, HighestPhaseAttack = 2, LowestShipId = 3 }
    public sealed class FleetCombatPlanDto
    {
        public string FleetId { get; set; } = string.Empty;
        public CombatTactic LongRangeTactic { get; set; }
        public CombatTactic MediumRangeTactic { get; set; }
        public CombatTactic CloseRangeTactic { get; set; }
        public CombatTargetPriority TargetPriority { get; set; }
        public int RetreatHullPercent { get; set; }
        public string? RetreatSystemId { get; set; }
    }
    public interface IPlannedCombatResolver : ICombatResolver
    {
        CombatResultDto Resolve(CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto> plans);
    }
}
