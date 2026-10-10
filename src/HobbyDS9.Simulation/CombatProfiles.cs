using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class CombatProfiles
    {
        internal static bool IsValid(ShipCombatProfileDto? p) => p != null && p.ShieldCapacity >= 0 && p.LongRangeAttack >= 0
            && p.MediumRangeAttack >= 0 && p.CloseRangeAttack >= 0 && p.AccuracyBps >= 0 && p.AccuracyBps <= 10000 && p.EvasionBps >= 0 && p.EvasionBps <= 10000;
        internal static ShipCombatProfileDto Copy(ShipCombatProfileDto p) => new ShipCombatProfileDto
        { ShieldCapacity = p.ShieldCapacity, LongRangeAttack = p.LongRangeAttack, MediumRangeAttack = p.MediumRangeAttack, CloseRangeAttack = p.CloseRangeAttack, AccuracyBps = p.AccuracyBps, EvasionBps = p.EvasionBps };
        internal static bool Equal(ShipCombatProfileDto? a, ShipCombatProfileDto? b) => a != null && b != null && Values(a) == Values(b);
        internal static (long, long, long, long, int, int) Values(ShipCombatProfileDto p) => (p.ShieldCapacity, p.LongRangeAttack, p.MediumRangeAttack, p.CloseRangeAttack, p.AccuracyBps, p.EvasionBps);
    }
}
