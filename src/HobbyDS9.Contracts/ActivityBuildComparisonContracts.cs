using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    // Detached construction comparison, never saved state, a queue token or a combat rating.
    public sealed class ActivityBuildComparisonDto
    {
        public int? StudyWork {get;set;}
        public int? ContactCapacity {get;set;}
        public long? MaxHull {get;set;}
        public long? Armor {get;set;}
        public long? ShieldCapacity {get;set;}
        public int? Speed {get;set;}
        public long? MovementBudget {get;set;}
        // Null is unavailable; an empty list is a supported unarmed fit.
        public List<ActivityBuildWeaponComparisonDto>? Weapons {get;set;}
    }
    public sealed class ActivityBuildWeaponComparisonDto
    {
        public TacticalWeaponClass Class {get;set;}
        public TacticalArc Coverage {get;set;}
        public long Damage {get;set;}
        public int OpportunitiesPerRound {get;set;}
        public int CadenceRounds {get;set;}
        public int AccuracyBps {get;set;}
        public int TrackingBps {get;set;}
        public long RequiredPower {get;set;}
        public long AllocatedPower {get;set;}
        public bool Powered {get;set;}
    }
}
