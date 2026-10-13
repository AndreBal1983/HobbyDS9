# Test-only Tactical v2 presenter prototype

**PROTOTYPE / TEST-OWNED DATA / CAMPAIGN V2 DISABLED**

This isolated project runs headless interaction tests. It is not a Unity assembly, view, campaign adapter, combat resolver or executable playable campaign. It is not included in the main solution/CI. No production dependencies or Stage A assembly are referenced. Accepted ResponsiveLayoutPolicy.cs is linked read-only for presentation reflow assertions.

Read [the package and pending real-campaign acceptance script](../../docs/unity-tactical-v2-implementation-acceptance-plan.md) and [preserved UX specification](../../docs/campaign-tactical-v2-playable-ux-spec.md).

From the repository root:

```powershell
dotnet restore tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj --locked-mode
dotnet build tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj -c Release --no-restore
dotnet test tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj -c Release --no-build --no-restore
```

Executed: 53 passed, zero failures/skips, Release zero warnings/errors, SDK 10.0.401. No runtime/Editor/native acceptance.

Models and Record* action seams are test-local presentation contracts, not invented Application APIs. Fixtures manually supply observed state and callbacks. There is no fake success connection to ICampaignSession or the live Unity client. RenderText always labels the prototype. Nullable extra fields remain explicit dependencies.

BoundaryRevision follows the Stage A toy notion, not global campaign stamps. Own toy ammunition is aggregate, not production per-package pools. Legal focus choices, standing/acknowledged orders, batch/encounter readiness, event sequence and travel/approach fields need actual safe backend projections before a real adapter can exist. A UI Boundary is never a valid campaign save; the save intent is only an observed-state anchor for a future host operation.

The tests establish presentation/control behavior and dependency separation, not backend privacy, legality, atomic save files, simultaneous combat, target math, cloak power, travel, batch rollback or real campaign completion. Those require the accepted authoritative lifecycle.
