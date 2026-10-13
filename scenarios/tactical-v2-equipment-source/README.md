# Provisional authored Tactical v2 equipment source sandbox

This opt-in Content-11 package validates real scenario-owned equipment sources. It is **not a playable Tactical v2 campaign**: campaign combat remains phased v1, and the new source ships have explicitly inert v1 combat fallback profiles. Tactical v2 activation, rounds, mutable ammunition/shields, aftermath and v2 construction are absent.

All new equipment/capability/stock numbers are **provisional test-scenario values**. Two human-owned sources demonstrate armed and explicitly unarmed fits. A foreign authored source checks disclosure isolation. The original human Scout remains legacy and ineligible. Initial shields are deliberately non-full; torpedo stock is seven rounds in a twelve-round magazine. Inspection does not refill or heal them; it labels them as immutable initial content, not live stocks.

Run from the repository root:

```powershell
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- scenarios/tactical-v2-equipment-source/scenario.json --inspect-tactical-v2 civ.federation
python scripts/check-authored-tactical-equipment.py
```

The second command verifies real source inspection, repeated state/diagnostic equivalence, foreign isolation and rejection of an infeasible essential-power reservation. Existing scenario bytes, v1 blueprint compilation and Save 14 are unchanged. See the [equipment milestone report](../../docs/tactical-v2-authored-equipment-foundation.md) for schema, exact provisional values, version matrix, validation, power accounting and deferred lifecycle decisions.
