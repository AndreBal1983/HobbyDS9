using System;
using System.Linq;

namespace HobbyDS9.TacticalUxPrototype.Tests.Prototype
{
    // A deliberately conservative TEST chronology contract, not production stamps.
    internal static class BoundaryChronology
    {
        internal static bool HasShape(Boundary b)
        {
            if (b.BoundaryRevision < 0 || string.IsNullOrWhiteSpace(b.Actor) || string.IsNullOrWhiteSpace(b.Encounter)) return false;
            return b.Status switch
            {
                BoundaryStatus.Ready or BoundaryStatus.Waiting => b.Round >= 1 && b.Round <= 3,
                BoundaryStatus.Report => b.Round >= 2 && b.Round <= 3,
                BoundaryStatus.EncounterComplete or BoundaryStatus.Finalized => b.Round >= 2 && b.Round <= 4,
                _ => false
            };
        }
        private static bool SameScope(Boundary a, Boundary b) => a.Actor == b.Actor && a.Encounter == b.Encounter;
        private static bool SameCursor(Boundary a, Boundary b) => a.Round == b.Round && a.BoundaryRevision == b.BoundaryRevision;
        private static bool Terminal(Boundary b) => b.Status == BoundaryStatus.EncounterComplete || b.Status == BoundaryStatus.Finalized;

        internal static bool CanRefresh(Boundary previous, Boundary next)
        {
            if (!HasShape(next) || !SameScope(previous, next)) return false;
            if (!HasShape(previous)) return true; // No watermark was established by malformed initial data.
            if (next.Round < previous.Round || next.BoundaryRevision < previous.BoundaryRevision) return false;
            if (SameCursor(previous, next))
                return (previous.Status == next.Status || previous.Status == BoundaryStatus.Ready && next.Status == BoundaryStatus.Waiting)
                    && SameFrozenFacts(previous, next);
            if (previous.Status == BoundaryStatus.Finalized) return false;
            if (previous.Status == BoundaryStatus.EncounterComplete) return CanFinalize(previous, next);
            // Refresh may catch up across missed rounds, unlike a single commit receipt.
            return next.Round > previous.Round && next.BoundaryRevision > previous.BoundaryRevision
                && next.Status != BoundaryStatus.Finalized;
        }
        internal static bool CanReceiveRound(Boundary sent, Boundary next)
        {
            if (!HasShape(sent) || Terminal(sent) || !HasShape(next) || !SameScope(sent, next)) return false;
            if (next.Status == BoundaryStatus.Waiting)
                return SameCursor(sent, next) && SameFrozenFacts(sent, next);
            return (next.Status == BoundaryStatus.Report || next.Status == BoundaryStatus.EncounterComplete)
                && next.Round == sent.Round + 1 && next.BoundaryRevision > sent.BoundaryRevision;
        }
        internal static bool CanFinalize(Boundary completed, Boundary next)
            => HasShape(completed) && completed.Status == BoundaryStatus.EncounterComplete && HasShape(next)
                && SameScope(completed, next) && next.Status == BoundaryStatus.Finalized
                && next.Round == completed.Round && next.BoundaryRevision > completed.BoundaryRevision;

        // Structural equality of supplied read facts; no legality, damage or target math.
        private static bool SameFrozenFacts(Boundary a, Boundary b)
            => a.GeometryText == b.GeometryText && a.ApproachCloak == b.ApproachCloak
                && a.BatchReady == b.BatchReady && a.TravelStopped == b.TravelStopped
                && a.LegalFocusChoices.SequenceEqual(b.LegalFocusChoices)
                && a.StandingOrders.Count == b.StandingOrders.Count
                && a.StandingOrders.Zip(b.StandingOrders).All(p => p.First.Fleet == p.Second.Fleet
                    && p.First.Maneuver == p.Second.Maneuver && p.First.FocusFleet == p.Second.FocusFleet)
                && a.OwnShips.Count == b.OwnShips.Count && a.OwnShips.Zip(b.OwnShips).All(p => SameOwn(p.First, p.Second))
                && a.VisibleShips.Count == b.VisibleShips.Count && a.VisibleShips.Zip(b.VisibleShips).All(p =>
                    p.First.Ship == p.Second.Ship && p.First.Fleet == p.Second.Fleet && p.First.Type == p.Second.Type
                    && p.First.Hull == p.Second.Hull && p.First.Shields.SequenceEqual(p.Second.Shields))
                && a.Reports.Count == b.Reports.Count && a.Reports.Zip(b.Reports).All(p =>
                    p.First.Sequence == p.Second.Sequence && p.First.Round == p.Second.Round && p.First.Message == p.Second.Message)
                && a.Encounters.Count == b.Encounters.Count && a.Encounters.Zip(b.Encounters).All(p =>
                    p.First.Encounter == p.Second.Encounter && p.First.Complete == p.Second.Complete);
        private static bool SameOwn(OwnShip a, OwnShip b)
            => a.Ship == b.Ship && a.Fleet == b.Fleet && a.Type == b.Type && a.Hull == b.Hull && a.MaxHull == b.MaxHull
                && a.Shields.SequenceEqual(b.Shields) && a.ShieldMaximum.SequenceEqual(b.ShieldMaximum)
                && SamePackages(a, b)
                && a.Hidden == b.Hidden && a.CloakCapable == b.CloakCapable;
        private static bool SamePackages(OwnShip a, OwnShip b)
        {
            if (a.Packages == null || b.Packages == null) return a.Packages == null && b.Packages == null;
            return a.Packages.Count == b.Packages.Count && a.Packages.Zip(b.Packages).All(p =>
                p.First.Package == p.Second.Package && p.First.WeaponClass == p.Second.WeaponClass
                && p.First.UsesPhysicalAmmunition == p.Second.UsesPhysicalAmmunition
                && p.First.Ammunition == p.Second.Ammunition && p.First.MagazineCapacity == p.Second.MagazineCapacity
                && p.First.ReadinessReason == p.Second.ReadinessReason);
        }
    }
}
