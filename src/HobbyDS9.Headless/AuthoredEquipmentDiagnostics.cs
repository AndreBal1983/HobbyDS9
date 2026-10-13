using HobbyDS9.Contracts;

internal static class AuthoredEquipmentDiagnostics
{
    internal static void Write(AuthoredTacticalEquipmentSnapshot source)
    {
        Console.WriteLine(FormattableString.Invariant($"tactical-equipment={source.ShipId}, projection={source.ProjectionId}, fit={source.FitDefinitionId}/{source.FitRevision}, source={source.CatalogueProvenanceId}, balance={source.BalanceStatus}"));
        Console.WriteLine(FormattableString.Invariant($"tactical-power={source.ShipId}, reactor={source.ReactorOutput}, distribution={source.DistributionLimit}, essentials={source.EssentialPowerReserved}, equipment-budget={source.EquipmentPowerBudget}, equipment-allocated={source.EquipmentPowerAllocated}, recharge={source.ShieldRechargePerRound}, recharge-power={source.ShieldRechargeAllocatedPower}/{source.ShieldRechargeRequiredPower}"));
        var c = source.Capabilities;
        Console.WriteLine(FormattableString.Invariant($"tactical-capabilities={source.ShipId}, speed={c.Speed}, agility={c.Agility}, sensors={c.Sensors}, command={c.Command}, cohesion={c.Cohesion}, role={source.Role}, armor={source.Armor}, evasion={source.EvasionBps}; per-ship source only; formation derivation unavailable"));
        foreach (var s in source.InitialShields)
            Console.WriteLine(FormattableString.Invariant($"tactical-initial-shield={source.ShipId}/{s.Sector}, authored-initial={s.InitialCurrent}/{s.Maximum}; not live campaign shield state"));
        foreach (var w in source.Weapons)
            Console.WriteLine(FormattableString.Invariant($"tactical-weapon={source.ShipId}/{w.WeaponId}, package={w.PackageVersionId}, class={w.Class}, mount={w.MountId}, slots={w.SlotsRequired}, coverage={w.Coverage}, damage={w.Damage}, accuracy={w.AccuracyBps}, tracking={w.TrackingBps}, opportunities={w.OpportunitiesPerRound}, cadence={w.CadenceRounds}, initial-next-fire={w.InitialNextFireRound}, power={w.AllocatedPower}/{w.RequiredPower}, powered={w.Powered}, ammo={w.AmmunitionDefinitionId}, authored-initial={w.InitialAmmunition}/{w.MagazineCapacity}; not live ammunition"));
    }
}
