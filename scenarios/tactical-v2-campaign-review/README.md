# Authoritative tactical review package

**Proposed, headless, production activation disabled.** See [the Stage B core report](../../docs/campaign-tactical-v2-stage-b-core.md).

`scenario.json` is a separately authored Content-11 starting package with three complete equipped ships and two co-located fleets. It reuses provisional equipment values, removes unbound legacy scouts and uses human controllers for simultaneous-command proof. Existing scenario bytes remain unchanged.

`package.json` adds an explicitly versioned review profile and complete fit-bound cloak declarations. The host loads both exact LF files, supplies their hashes and validates the composite package. The profile cannot declare default activation true.

Run:

```powershell
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --review-campaign-tactical-v2
```

This performs two real owned encounters, saved waiting/round boundaries, changed later orders and retained depleted pools. Initial placement/legacy allocation is a controlled lifecycle experiment; **physical pursuit, contact intent and departure timing are not accepted by this package**. Do not present it as the live Unity slice.
