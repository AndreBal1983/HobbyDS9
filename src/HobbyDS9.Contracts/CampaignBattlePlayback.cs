using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.Contracts
{
    public enum CampaignPlaybackEventKind
    {
        RoundStarted, OwnOrderCommitted, Maneuver, WeaponFired, IncomingTorpedo,
        InterceptionAttempt, TorpedoIntercepted, Hit, Miss, ShieldDamage, HullDamage,
        ShipDestroyed, FormationDestroyed, ShieldRecharged, Retreated, BrokenThrough,
        ObjectiveProgress, ObjectiveCompleted, EncounterCompleted, FocusFallback
    }
    public sealed class CampaignPlaybackEvent
    {
        public string EncounterId { get; }
        public int Round { get; }
        public int Sequence { get; }
        public CampaignPlaybackEventKind Kind { get; }
        public string? SourceShipId { get; }
        public string? SourceFleetId { get; }
        public string? TargetShipId { get; }
        public string? TargetFleetId { get; }
        public TacticalWeaponClass? WeaponClass { get; }
        public TacticalSector? Sector { get; }
        public TacticalRange? Range { get; }
        public long Amount { get; }
        public CampaignPlaybackEvent(string encounter, int round, int sequence, CampaignPlaybackEventKind kind,
            string? sourceShip, string? sourceFleet, string? targetShip, string? targetFleet,
            TacticalWeaponClass? weaponClass, TacticalSector? sector, TacticalRange? range, long amount)
        {
            EncounterId = encounter; Round = round; Sequence = sequence; Kind = kind;
            SourceShipId = sourceShip; SourceFleetId = sourceFleet; TargetShipId = targetShip; TargetFleetId = targetFleet;
            WeaponClass = weaponClass; Sector = sector; Range = range; Amount = amount;
        }
    }
    public sealed class CampaignPlaybackShipIdentity
    {
        public string ShipId { get; }
        public string CivilizationDefinitionId { get; }
        public string ShipDesignDefinitionId { get; }
        public CampaignPlaybackShipIdentity(string ship, string civilizationDefinition, string shipDesignDefinition)
        { ShipId = ship; CivilizationDefinitionId = civilizationDefinition; ShipDesignDefinitionId = shipDesignDefinition; }
    }
    public sealed class CampaignPlaybackBoundary
    {
        public IReadOnlyList<CampaignTacticalCondition> OwnShips { get; }
        public IReadOnlyList<CampaignTacticalCondition> VisibleEnemies { get; }
        public IReadOnlyList<CampaignTacticalRelation> Relations { get; }
        // Exactly this boundary's own + visible ships, never identities from future reveal boundaries.
        public IReadOnlyList<CampaignPlaybackShipIdentity> ShipIdentities { get; }
        public CampaignPlaybackBoundary(IEnumerable<CampaignTacticalCondition> own, IEnumerable<CampaignTacticalCondition> enemies,
            IEnumerable<CampaignTacticalRelation> relations)
            : this(own, enemies, relations, Enumerable.Empty<CampaignPlaybackShipIdentity>()) { }
        public CampaignPlaybackBoundary(IEnumerable<CampaignTacticalCondition> own, IEnumerable<CampaignTacticalCondition> enemies,
            IEnumerable<CampaignTacticalRelation> relations, IEnumerable<CampaignPlaybackShipIdentity> identities)
        {
            OwnShips = own.ToList().AsReadOnly(); VisibleEnemies = enemies.ToList().AsReadOnly();
            Relations = relations.ToList().AsReadOnly(); ShipIdentities = identities.ToList().AsReadOnly();
        }
    }
    public sealed class CampaignRoundPlayback
    {
        public int Round { get; }
        public CampaignPlaybackBoundary Before { get; }
        public CampaignPlaybackBoundary After { get; }
        public IReadOnlyList<CampaignPlaybackEvent> Events { get; }
        public CampaignRoundPlayback(int round, CampaignPlaybackBoundary before, CampaignPlaybackBoundary after,
            IEnumerable<CampaignPlaybackEvent> events)
        { Round = round; Before = before; After = after; Events = events.ToList().AsReadOnly(); }
    }
    public sealed class CampaignBattleShipStatistics
    {
        public string ShipId { get; }
        public string FleetId { get; }
        public bool Own { get; }
        public long ShotsObserved { get; }
        public long HitsObserved { get; }
        public long MissesObserved { get; }
        public decimal HullDamageObserved { get; }
        public decimal ShieldDamageObserved { get; }
        public bool DestroyedObserved { get; }
        public CampaignBattleShipStatistics(string ship, string fleet, bool own, IEnumerable<CampaignPlaybackEvent> events)
        {
            ShipId = ship; FleetId = fleet; Own = own;
            ShotsObserved = events.LongCount(e => e.Kind == CampaignPlaybackEventKind.WeaponFired && e.SourceShipId == ship);
            HitsObserved = events.LongCount(e => e.Kind == CampaignPlaybackEventKind.Hit && e.SourceShipId == ship);
            MissesObserved = events.LongCount(e => e.Kind == CampaignPlaybackEventKind.Miss && e.SourceShipId == ship);
            HullDamageObserved = events.Where(e => e.Kind == CampaignPlaybackEventKind.HullDamage && e.TargetShipId == ship).Sum(e => (decimal)e.Amount);
            ShieldDamageObserved = events.Where(e => e.Kind == CampaignPlaybackEventKind.ShieldDamage && e.TargetShipId == ship).Sum(e => (decimal)e.Amount);
            DestroyedObserved = events.Any(e => e.Kind == CampaignPlaybackEventKind.ShipDestroyed && e.TargetShipId == ship);
        }
    }
    public sealed class CampaignBattlePlayback
    {
        public string ActorId { get; }
        public string EncounterId { get; }
        public string LocationNodeId { get; }
        public int Turn { get; }
        public bool Complete { get; }
        public CampaignPlaybackBoundary Initial { get; }
        public IReadOnlyList<CampaignRoundPlayback> Rounds { get; }
        public IReadOnlyList<CampaignBattleShipStatistics> Statistics { get; }
        public IReadOnlyList<CampaignTacticalOwnShip> OwnFinalCondition { get; }
        public CampaignBattlePlayback(string actor, string encounter, string location, int turn, bool complete,
            CampaignPlaybackBoundary initial, IEnumerable<CampaignRoundPlayback> rounds,
            IEnumerable<CampaignBattleShipStatistics> statistics, IEnumerable<CampaignTacticalOwnShip> ownCondition)
        {
            ActorId = actor; EncounterId = encounter; LocationNodeId = location; Turn = turn; Complete = complete; Initial = initial;
            Rounds = rounds.ToList().AsReadOnly(); Statistics = statistics.ToList().AsReadOnly(); OwnFinalCondition = ownCondition.ToList().AsReadOnly();
        }
    }
}
