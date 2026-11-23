# Isolated Unity save library trial

This source candidate binds the Save Library to the Format 18 belt mining campaign. Release preparation keeps the provider outside the ordinary game project. An isolated build and an explicit 32-digit trial ID are both required. The trial executable uses a separate product data directory for its SQLite repository and any JSON slots.

## Receiving recipe

Use a fresh linked QA worktree at the clean receiving source. Run `dotnet msbuild scripts/PrepareUnityGeneratedGalaxy.proj -t:Prepare` with the same prepared presentation and Format 18 resources as the [verified November player](ux-20261119-gui.md). Build `prototypes/HobbyDS9.CampaignRepositoryHost/HobbyDS9.CampaignRepositoryHost.csproj` in Release from that same worktree.

```powershell
$source = git rev-parse HEAD
python scripts/prepare-unity-repository-trial.py --source $source --data-bin <sealed-Data438-delivery-directory> --probe-project <A2-project-v4>
```

The script checks the Data438 source subtree and all six delivered provider files against their SHA-256 pins. It copies the tested A2 import policy and supplies the runtime assembly references, stripping configuration and standalone define in the receiving worktree. Recheck the file pins in `artifacts/repository-trial-stage.json` before the first import. The staging changes are local to the receiving worktree. The provider license notices are included under StreamingAssets.

Set `HOBBYDS9_BUILD_SOURCE_SHA` to the receiving commit. QA uses Unity 6000.6.9f1 on Windows with `--hobbyds9-evaluate-editor-patch` and executes `HobbyDS9.UnityReview.Editor.GeneratedStaticAstronomyCampaignBuild.BuildWindowsStateRepositoryTrial`. This requests Windows x64 IL2CPP, the .NET Standard API and High managed stripping. The builder verifies that the output contains the one approved R2 SQLite DLL. The build identity remains pending until a separate native receipt exists.

Launch `Start-Repository-Trial.cmd` with a generated GUID in `N` format. Start a belt mining campaign and select **Open isolated Save Library** in its campaign menu. The live campaign is eligible for safe-boundary autosaves only after opening the trial. For cold reopen, use the same GUID and open the library from a new belt mining session. Check a selected row before confirming Load. The current head of the selected branch continues that branch. A historical checkpoint opens a new branch after explicit review.

## Bound interactions and evidence

- Bounded name/kind/creation-branch queries use storage cursors. The check action validates one selected payload. Listing shows indexed metadata, including the absence of a retained payload. A cancelled or inconclusive check preserves existing health evidence.
- Named manual saves and overwrites use the existing queue, with reviewed session/reference stamps and conditional admission. Unknown writes keep their operation ID for **Check save status**. A committed receipt and availability of its branch head are shown independently. Unsaved progress and automatic failures stay visible in the running game. Save and Quit closes only after the queue confirms the current state and the requested manual save.
- Exact selected-checkpoint export writes a new JSON copy to the isolated repository export directory. A failed load leaves the playing session in place. A known fork is reused for retries within the live trial.
- The initial trial binds the existing schema 2 delivery. Corrupt-evidence quarantine, dedicated recovery selection, metadata edits, import/backup controls and export of an unsaved live session need their own receiving work. Persistent fork-outcome reconciliation after an interrupted process is still open. The ordinary launcher and JSON storage remain the live player route.

The portable binding tests use the actual Format 18 model and real SQLite with generated fixtures to exercise cold head continuation, a historical fork and its independent future, a lost fork acknowledgement, cancelled validation, a refused Save and Quit, and a durable receipt with an unavailable branch head. Full game Unity execution, the quarantine recovery path and the comparable player performance matrix remain D1 gates. See the [execution contract](design/state-repository-implementation-contract.md) and [typed capture boundary](state-repository-capture-v1.md).
