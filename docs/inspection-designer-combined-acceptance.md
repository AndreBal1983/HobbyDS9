# Combined campaign inspection and live designer acceptance

13 October 2026. This executes only the **Prepared combined acceptance assignment** in the owner's `docs/next-step-preparation.md`. Delivery is [draft PR #18](https://github.com/AndreBal1983/HobbyDS9/pull/18). No merge is authorized by this assignment. Campaign Tactical v2 remains disabled; the equipment-foundation milestone has not started.

## Sources and integration review

Fresh remote checks confirmed main `703e395b793af41953d440d04d69e0744909caf5`, [PR #16](https://github.com/AndreBal1983/HobbyDS9/pull/16) at `27ed955296b2a4ce8b86f1b65d663c389023e99e`, and [PR #17](https://github.com/AndreBal1983/HobbyDS9/pull/17) at `4190ff854cc437a7eaa5c0085da924a70f1163c1`. Both source PRs were draft, open and unmerged. Their milestone reports and complete changed-file sets were reviewed before integration. The source changes have no shared paths against main.

A new clean worktree at `C:/Users/ambal/.codex/worktrees/inspection-designer-combined-acceptance/HobbyDS9` uses branch `codex/inspection-designer-combined-acceptance`. Ordinary merge commits `03e01ea` and `564aec7` preserve both source histories. No source branch was rebased or force-pushed. All 30 source changed-file blobs were checked against their respective source commits after integration: identical, with both source heads ancestors of the combined candidate. No production-code conflict or defect required a fix.

The [inspection implementation report](campaign-tactical-combat-v2-integration.md) and [designer acceptance report](unity-campaign-blueprint-integration-acceptance.md) retain their separate scopes. Application owns immutable catalogues, compiled blueprint records, ownership/access validation and construction eligibility. Unity submits observed-stamp commands. Inspection does not refresh an old designer stamp, make a replacement-session workspace authoritative, consume RNG, or supply missing v2 equipment. Historical revision editing creates a detached draft; publication never modifies a stored revision. Existing campaign construction, movement and combat still use the v1 authoritative resolver.

Runtime/test implementation checkpoint: `5f538fbc0c73951d1dbae1be6c5b7f13d8017586`. Subsequent delivery documentation does not change executable inputs. The exact final delivery SHA and its CI runs are recorded in PR #18 and the package manifest, avoiding a self-referential report commit.

## Portable acceptance

| Gate | Combined result |
| --- | --- |
| Locked dependency restore | Passed |
| Full .NET Release build | Passed, zero warnings/errors |
| Source-pair inventory | 1,354 passed: 1,307 base + all 24 inspection and 23 designer cases |
| Final complete .NET suite | **1,358 passed; zero failed/skipped**, including four additional combined cases |
| Blueprint validation/publication/construction, revision persistence and save/load | Passed in the complete suite |
| Movement, production and phased combat v1 regressions | Passed in the complete suite |
| Canonical/reverse/repeated 25-turn legacy campaigns | All three identical to the pinned legacy golden and RNG state |
| Standalone Tactical v2 reference golden | Passed in the complete suite |
| Tactical playground scripted checks | Four scripts and round 1/2 save/resume checks passed |
| Optional campaign inspection smoke | Gate remains closed; entire legacy state/RNG/transcript preserved |
| PrepareUnity and static asset/assembly checks | Passed |

The four [combined tests](../tests/HobbyDS9.Simulation.Tests/CombinedDesignerInspectionAcceptanceTests.cs) use the actual authored DS9 scenario, Application session, StrategicClient and live CampaignDesigner. They exercise publication of revision 1, queueing it, publication of revision 2, five real production turns, frozen revision-1 ship inspection and save/load; inspection alongside a stale original designer stamp and explicit refresh; matching-turn/revision session replacement with an obsolete designer and Production model; and unavailable Engineering component versions with rejected publication and unchanged state. No test resource grants, authored-content changes or baseline replacements were introduced.

Existing designer tests additionally cover malformed DTOs, rejected construction, foreign ownership/colonies, insufficient resources, duplicate actions, immutable history, publication identity allocation and load generation. Existing inspection tests cover source gaps, eligibility and snapshot immutability. Actual discovered inventory was inspected rather than inferred solely from arithmetic.

## Local Unity and native acceptance

These checks are local evidence, separate from portable GitHub CI.

| Gate | Result |
| --- | --- |
| Unity 6000.6.5f1 EditMode through PrepareUnity | **116/116 passed**, failed 0, skipped 0 |
| PrepareUnity Windows target | Windows x64 **IL2CPP build succeeded** |
| Render-capable combined Game-view acceptance | 24 real captures, eight states at each of three resolutions; layout/state assertions passed |
| Native Windows player | Live editing/publication, historical construction, completed ship provenance, saving, full restart/load and strategic regression passed |

The unchanged `CampaignBlueprintCapture.CaptureForAcceptance` runner was executed against this combined worktree. It starts the ordinary authored campaign, supplies the legal unarmed utility fit through the live designer, checks invalid power and genuine stale-revision rejection, explicitly refreshes and publishes revisions 1/2. It checks immutable records, exact-revision construction, five production turns and save/load. It does not grant resources, edit compiled statistics or write global user save slots. All thirteen designer pages retain their accepted controls.

With the owner's fresh desktop permission, the packaged player was tested through actual mouse/keyboard input. A local reactor edit retained the campaign's credits/state; Keep edits and Discard/continue navigation worked. Loading the capture runner's real campaign save blocked the previous workspace until explicit discard. Editing a detached revision-2 copy and publishing **Combined Native Courier III / revision 3** succeeded without charging credits or altering revisions 1/2.

The player queued historical revision 1 at owned Earth, paying 22 credits and one Dilithium once. Progress after native End Turn clicks was 8/38, 16/38, 24/38, 32/38, then completion at turn 6. The fleet inspector displayed **Capture Courier / revision 1**, hull 30/30 and shields 0, rather than the later class name. The built ship is `ship.spawn.0000000000000013`, from `blueprint.player.5df60550d21849679a77f4dafcd5b3de` revision 1. Its compiled movement is 4; the mixed fleet remains limited to 3 by its legacy Scout.

Native Save wrote turn 6/state 26 with three immutable revisions and the revision-1 ship. Closing and restarting the executable, then explicitly loading that save, restored credits 63, both fleet ships and exact revision-1 provenance. Campaign saves do not persist editable session drafts. Existing unsupported old-format saves were shown unavailable; no migration was added.

Further native regression checked Research's 70/30 split, approximately 16⅔% Science shares, scrollability and an unapplied +5% preview with unchanged authoritative state revision. Fleet selection, highlighted wormhole route preview and explicit submission worked. End Turn showed 3/7 then 6/7 partial edge progress. AI-only battles did not interrupt planning. Arrival opened the existing human phased-v1 three-way encounter; human-plan resolution produced a buffered report identifying the actual blueprint ship and hull 30→30. Finalization opened planning turn 9. Legacy static Scout enqueue charged its normal costs; confirmed cancellation emptied the queue and refunded the unused credits/Dilithium.

The independent audio controls remain visible. Audible playback/transitions and physical GPU power/coil-whine require human observation; no fresh human result is recorded here. This report makes no new auditory or electrical measurement claim. Existing VSync code is unchanged. These observations are supplemental to the prepared combined assignment's required gates.

## Responsive evidence

The runner produced actual `ScreenCapture.CaptureScreenshot` PNGs for designer, invalid power, publication rejection/success, immutable history, construction options, queued blueprint and finished ship at **1280×720, 1920×1080 and 3440×1440**. PNG dimensions and essential designer action bounds were asserted. Representative captures were visually reviewed at all three sizes, including rejection text, history, production and ship provenance.

At 1280×720 the system picker and independently scrollable editing/details panels keep controls reachable. At 1920×1080 the full system rail and unchanged artwork remain visible. At 3440×1440 artwork/stage scaling preserves anchored controls. Long blueprint identities wrap; narrow Production panels intentionally scroll and some navigation captions ellipsize as in the accepted design. No essential designer actions overlapped or were clipped in the checked states.

Native interaction separately exercised a 1920×1080 client and a fresh **1280×720** client. The compact native designer successfully selected Power, added a reactor and scrolled the version/package panel while publication/navigation remained visible. Native screenshots include window chrome. The 3440×1440 evidence is real Unity Game-view capture, not a claim of physical native input at that size.

## Compatibility and determinism

- Save format **14**, Content format **10**.
- Rules `science-engineering-v1`; compiler `ship-design-compiler-v1`; RNG `splitmix64-rejection-v1`, version 1.
- Authored scenario SHA256: `3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`.
- Canonical/reverse/repeated legacy save SHA256: `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`.
- Legacy RNG: `d8919406d6e01cf1`; turn 26, state 185, 131 AI commands, 17 battles, 12 checkpoints, nine ships/fleets, next entity 97.
- Standalone v2 reference SHA256: `ccae7836c085936ed962b9ba001764ae4252f135fdf62fa1fbe2c0ea0bcef07c`.

No save migration, agreed gameplay change, LCARS redesign or golden update occurred. Campaign v2 inspection still explicitly reports activation blocked; it does not turn the live v1 designer's ships into v2 ships.

## CI, reproduction and artifacts

The combined runtime checkpoint passed Linux/Windows/macOS [push CI](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38412413869) and [PR CI](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38412491012). Final documentation-head verification and exact SHA are recorded in [PR #18](https://github.com/AndreBal1983/HobbyDS9/pull/18). CI runs locked restore, Release, the complete .NET suite, portable Unity preparation/static checks, all three legacy campaign smokes, tactical scripted save/resume and campaign inspection; it does not execute Unity Editor or the Windows player.

Run from the integration root:

```powershell
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
dotnet msbuild scripts/PrepareUnity.proj -t:Windows '-p:UnityEditor=C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25 --combat-order reverse
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
python scripts/check-tactical-playground.py
python scripts/check-campaign-tactical-v2-inspection.py
```

For render-capable captures, licensed Unity accepts the combined Unity project path and `-executeMethod HobbyDS9.UnityClient.Editor.CampaignBlueprintCapture.CaptureForAcceptance`, without batchmode/nographics. Obtain current desktop permission before interactive acceptance.

Self-contained package: `E:/HobbyDS9/artifacts/demos/Inspection-Designer-Combined/HobbyDS9.exe`, with its complete Data/DLL folders, README, manifest, captures and native evidence. The original source package remains intact. Local logs are under this integration worktree's `artifacts/combined-acceptance/`; Unity results/build logs are in `artifacts/unity/`; 24 rendered captures and generated test saves are in `artifacts/unity/campaign-blueprints/`. These are ignored artifacts, not runtime assets committed to Git.

All three original global save slots were SHA256-inventoried and backed up before testing. Only acceptance slot 2 was temporarily used; its completed test save was copied to the evidence directory, then its exact original bytes restored after the acceptance players closed. Slots 1/3 were untouched and all three original hashes verified. Transient Unity import/build-setting serialization changes in this newly created worktree were archived and restored to source HEAD. No pre-existing user worktree or local planning document was changed.

Both source PRs and the integration PR remain draft/open, with main unchanged. A later, explicit merge request must review this combined candidate. Equipment authoring, playable campaign v2 lifecycle, refits, AI design and other roadmap work remain deferred; the source reports' existing designer limits also remain applicable.
