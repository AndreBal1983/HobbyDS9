# Tactical presenter prototype — isolated draft delivery

13 October 2026. **Test-owned presenter/read-model package and dedicated portable CI. No production Unity v2 adapter or campaign activation.**

## Base, source and packaging

Fresh fetch verified origin/main **53d2e64973d9081a4d0090e771f5e777fc55cf40**. A new managed worktree at C:/Users/ambal/.codex/worktrees/tactical-presenter-prototype-ci/HobbyDS9 uses branch **codex/tactical-presenter-prototype-ci**. It started at that exact base.

An ordinary local merge **d020f5ba0ef995c3a46186a7366f03db121eafa8** incorporates source **202b26b8e79a87ef212c9789c1ae861ce02b9062**, preserving the complete 455bf10 → 785bd46 → 202b26b specification/prototype/hardening history. Both the verified main and source head are ancestors of delivery. Neither original worktree/branch was modified, rebased or force-pushed. This local integration is not a GitHub PR merge.

The source/main common ancestor is 4e1a365fbae52591ddfc593200fae7b1f79a53db. Reviewing the resulting fresh-main diff matters: no older production checkout is copied back. No equipment, Stage A or unfinished Stage B branch is imported. References to those sources remain documentation only.

Allowed delivery paths: four UX/presenter documents, tests/HobbyDS9.TacticalUxPrototype.Tests/** and the new .github/workflows/tactical-presenter-prototype.yml. Production Unity, Application/Contracts/Simulation, scenarios, main solution membership, SDK/build settings, scripts and the existing .github/workflows/ci.yml remain identical to the verified base.

The exact final head, DRAFT PR and exact-head workflow run/job URLs are recorded in its PR description and handover after publication, avoiding a self-referential commit SHA in this document. The branch remains a draft delivery, with no merge authorization.

## Dedicated workflow

[Tactical presenter prototype](../.github/workflows/tactical-presenter-prototype.yml) runs on push and pull_request with contents:read only. It uses repository conventions actions/checkout@v5, actions/setup-dotnet@v5 and global.json, with independent ubuntu-latest/windows-latest/macos-latest jobs and fail-fast:false.

Every job executes:

```text
dotnet restore tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj --locked-mode -warnaserror
dotnet build tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj --configuration Release --no-restore -warnaserror
dotnet test tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=presenter-prototype.trx" --results-directory artifacts/tactical-presenter-prototype
```

A PowerShell step checks the TRX for **115 total/passed, zero failed/not-executed**, and labels the job summary as test-owned mock data. Results upload independently per OS, including after failure. No extra tests were added to raise counts. Normal ci.yml is not edited and does not discover this outside-solution project; its existing workflow may also run automatically, which does not establish new full/native acceptance.

## Executed versus pending evidence

Fresh delivery worktree: locked restore passed; Release **zero warnings/errors**; **115 passed, zero failed/skipped**, SDK 10.0.401. Local TRX: artifacts/tactical-presenter-prototype/presenter-prototype.trx. The cases are unchanged from the reviewed hardening: original 53 interactions plus 50 chronology and 12 package cases. Assembly isolation/no production dependencies remain tested; the only linked accepted client source is unchanged presentation-only ResponsiveLayoutPolicy.cs.

Dedicated exact-source-head CI is verified separately after push. Do not infer its success from local Windows execution or a prior commit. A PR-triggered synthetic merge run is distinct from the push run on the exact source SHA. If a portability defect appears, fix it narrowly and reverify the new final head.

No production acceptance suite was rerun locally, and no desktop, Unity Editor/EditMode, native build, GUI/native clipping or playable v2 campaign is claimed. See [hardening evidence](tactical-v2-presenter-hardening.md), [full plan/B1–B11 gates](unity-tactical-v2-implementation-acceptance-plan.md), [UX specification](campaign-tactical-v2-playable-ux-spec.md) and [test-project instructions](../tests/HobbyDS9.TacticalUxPrototype.Tests/README.md).

## Mock versus real boundary

Every prototype view is explicitly labelled PROTOTYPE / TEST-OWNED DATA / CAMPAIGN V2 DISABLED. Test code supplies detached observations, callbacks, legal choices, completion/readiness flags and package stock/readiness facts. The presenter never resolves combat, reveals enemies, changes stocks, chooses automatic firing targets, calculates power/cadence or advances travel.

There is no production Application/Simulation/Stage A assembly reference, recording-fake runtime adapter, scene or plugin. The package's conservative chronology is a test protocol, not acceptance of final campaign stamps. Supplied per-package readiness is displayed without calculation; enemy reserves/next orders remain structurally absent from visible models.

A live adapter still needs accepted lawful projections, actual global/boundary receipt semantics, current per-package pools/readiness, real stamped round/cloak/strategic commands, private commitments/save/resume/filtered reports, physical travel/contact and atomic batch completion. The real-campaign script at 1280×720, 1920×1080 and 3440×1440 remains pending those gates and later explicit native/Editor authorization.
