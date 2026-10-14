# Test-only Tactical v2 presenter prototype

**PROTOTYPE / TEST-OWNED DATA / CAMPAIGN V2 DISABLED**

This isolated project runs headless interaction tests. It is not a Unity assembly, view, campaign adapter, combat resolver or executable playable campaign. It stays outside the main solution and unchanged normal ci.yml; [dedicated isolated CI](../../.github/workflows/tactical-presenter-prototype.yml) runs it explicitly on Linux/Windows/macOS. No production dependencies or Stage A assembly are referenced. Accepted ResponsiveLayoutPolicy.cs is linked read-only for presentation reflow assertions.

Read [the package and pending real-campaign acceptance script](../../docs/unity-tactical-v2-implementation-acceptance-plan.md), [prototype chronology/package contract](../../docs/tactical-v2-presenter-hardening.md) and [preserved UX specification](../../docs/campaign-tactical-v2-playable-ux-spec.md).

From the repository root:

```powershell
dotnet restore tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj --locked-mode
dotnet build tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj -c Release --no-restore
dotnet test tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj -c Release --no-build --no-restore
```

Executed: **115 passed**, zero failures/skips, locked restore and Release zero warnings/errors, SDK 10.0.401. All original 53 named cases remain and pass; 50 chronology plus 12 package cases are added. The first 27 chronology regressions reproduced failures against prior commit 785bd46f4ead9872fc2bccbff6c0fd907a052ea0. No runtime/Editor/native acceptance.

Models and Record* action seams are test-local presentation contracts, not invented Application APIs. Fixtures manually supply observed state and callbacks. There is no fake success connection to ICampaignSession or the live Unity client. RenderText always labels the prototype. Nullable extra fields remain explicit dependencies.

BoundaryRevision follows a deliberately documented prototype chronology, not known final production stamps. Waiting keeps frozen round/read facts; resolved receipts advance exactly one round; finalization matches the encounter and terminal cursor with a newer revision. Refresh cannot regress or reopen terminal encounters. New validated generation loads intentionally reset watermarks. Receive* returning true means a matched receipt was processed, not that its observation was applied.

Own stocks/readiness are optional per-package supplied facts. No toy aggregate conversion, stock pooling, class-name inference or target/power/cadence calculation occurs. Missing list/fields/readiness render dependencies; explicit zero/empty/no-physical-stock differ from omission. Enemy reserves remain structurally absent. Legal focus choices, standing/acknowledged orders, batch/encounter readiness, event sequence and travel/approach fields still need actual safe backend projections. A UI Boundary is never a valid campaign save; the save intent is only an observed-state anchor for a future host operation.

The tests establish presentation/control behavior and dependency separation, not backend privacy, legality, atomic save files, simultaneous combat, target math, cloak power, travel, batch rollback or real campaign completion. Those require the accepted authoritative lifecycle.
