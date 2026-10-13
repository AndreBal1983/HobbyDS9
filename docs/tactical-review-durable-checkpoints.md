# Durable trusted tactical review checkpoints

14 October 2026. **Headless B1 review storage implemented; activation remains disabled.** This is a new isolated branch stacked on draft [PR #23](https://github.com/AndreBal1983/HobbyDS9/pull/23) at **0ab1af51e41bb20292c9a36f059bdd387cf4a1fc**. The underlying actual Application checkpoint remains PR #22 **e117c95c7961bc424f190572f4e23a8bad7cfd08**.

Read AGENTS, [architecture/save contracts](architecture.md), [existing slot pattern](../client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/SaveSlotStore.cs), [Stage B core format matrix](campaign-tactical-v2-stage-b-core.md) and [prior real-client acceptance](tactical-review-client-integration.md). Architecture has historical version paragraphs; the pinned core report/contracts specify this carrier: review Save 1, full embedded legacy Save 14, exact package/rules/resolver identity. No version or migration is introduced.

## Scope and ownership

[Portable checkpoint seam](../prototypes/HobbyDS9.TacticalReviewClient/ReviewCheckpoints.cs) and [controller operations](../prototypes/HobbyDS9.TacticalReviewClient/ReviewController.Checkpoints.cs) extend only the isolated client/host. [Disk store](../prototypes/HobbyDS9.TacticalReviewStorage/DiskReviewCheckpointStore.cs), [JSON codec](../prototypes/HobbyDS9.TacticalReviewStorage/JsonReviewHostCodec.cs) and [explicit package-file loader](../prototypes/HobbyDS9.TacticalReviewStorage/ReviewPackageFiles.cs) are a .NET 10 headless host adapter, portable across tested OSes. JSON/file I/O stays out of the .NET Standard 2.1 gameplay contracts and client projections; this assembly is not a Unity dependency.

Simulation, Contracts, Application, movement/scenario production, ci.yml, main solution and production Unity are unchanged. Existing in-memory checkpoint methods remain compatible. No live client activation, slot screen, campaign battle archive, desktop/native tools, crypto/authentication or automatic recovery is added.

## Explicit directory, slots and atomic file visibility

The caller supplies a non-root absolute **review-only directory**. There is no default user-save location, search or slot enumeration. A slot is 1–32 ASCII characters: starts with lowercase a–z or 0–9, then those characters or dash/underscore. Uppercase, Unicode, traversal, separators, absolute paths, leading underscore, periods and whitespace are rejected.

Only the fixed filename review-checkpoint-{slot}.json is addressed. Temporary files are unique .review-checkpoint-{slot}.{guid}.tmp paths in that same directory. Full paths are checked for direct lexical containment; configured directory/existing slot reparse points are rejected. This is not a security sandbox against hostile concurrent filesystem manipulation. The caller must own/use a dedicated directory and coordinate a single writer; no cross-process locking protocol is claimed.

Write protocol:

1. Export the current complete private carrier under the host lock and serialize before file mutation. No draft/controller state enters the carrier.
2. Reject an occupied slot unless overwrite is explicit.
3. Create a uniquely named file with CreateNew, write the complete UTF-8 JSON and flush its file stream to disk.
4. For an existing destination use same-directory File.Replace; for a new slot use File.Move without overwrite. Recheck occupancy before committing. Never delete the previous checkpoint or fall back to copy/delete.
5. Cleanup addresses only the temporary file created by that operation. It neither searches for abandoned files nor deletes another operation's temporary file. Cleanup failure does not misreport an already completed commit as a failed save.

**Supported atomicity scope:** local filesystems on which .NET supports same-directory rename/replacement atomically. The dedicated CI tests creation, replacement, reload and process reopening on Windows/Linux/macOS runners; no separate certification of NTFS/ext4/APFS variants is asserted. Unsupported replacement fails rather than degrading to an unsafe protocol.

Injected failures before writing, after a partial temporary write and after flush/before replacement retain the exact previous file and live state/draft, cleaning the operation's temporary file. Serialization failures retain it too. A slot occupied during staging is retained when overwrite was not permitted.

File flush plus atomic rename is **not a proven power-loss durability guarantee**: directory fsync, process-kill/power-cut testing, network/cloud-synced/removable filesystem behavior and concurrent multi-writer correctness are outside acceptance. A process crash can leave its own temporary file; there is no guessed recovery or silent fallback to another slot. The requested slot is always the only load source.

## Validated load and safe presentation

The disk contains the existing complete CampaignTacticalReviewSaveDto, including private commitments/opponent state needed for resume. Only the trusted store/codec/host consume it. The serializer rejects unknown JSON members; the actual CampaignTacticalReviewApplication.LoadReviewSave validates package identity, supported formats/resolver, private replay/state/resources and provenance.

Read/deserialization, candidate construction and actor-scoped projection all finish before replacing the running session. Corrupt, truncated, null, wrong-package, unsupported legacy/review or unknown-field files leave the old host/frame/draft/generation intact. An unknown actor cannot replace a session. Missing/corrupt slots do not fall back to another valid file.

Public metadata contains **Slot, Turn, CampaignRevision only**. Results expose safe frames and fixed ReviewCheckpointError codes; neither raw exceptions/inner exceptions, private JSON nor filesystem paths reach the renderer. The headless probe suppresses raw package/file exception text as well.

Saving leaves unsent edits local. Dirty load/reopen requires explicit discard consent; successful load/reopen clears drafts and inspection. Pending mutation work blocks save/load/reopen. Same-host load starts a new generation. Reopening builds a new trusted host with a fresh host identity, so stale work cannot execute there even if local generation numbers coincide. Old callbacks cannot affect the replacement controller.

## Real process evidence

[Storage tests](../tests/HobbyDS9.TacticalReviewStorage.Tests/DurableCheckpointTests.cs) use only real Application sessions and task-owned temporary directories. The [separate executable probe](../prototypes/HobbyDS9.TacticalReviewStorage.Probe/Program.cs) opens exactly supplied package/profile files and slot in a **new OS process**; it never receives the parent's in-memory carrier/session.

The waiting-commitment test:

- Starts uninterrupted and resumed campaigns from identical exact source bytes; both accept actor A's Round 1 command.
- Saves the resumed private checkpoint while another actor has an unsent local draft. That draft is deliberately absent from the file.
- Reopens the slot in a separate process, resumes B's commitment, makes matching Flank orders in Round 2, resolves the remaining rounds and finalizes once.
- Writes the completed checkpoint to a distinct explicit slot. Its complete JSON bytes and SHA256 equal the uninterrupted final checkpoint; actor reports and persisted own stocks match.
- The original host remains Waiting, showing that the child did not reuse/mutate it. A new trusted host can also open the child's final file in Planning with activation disabled.

Separate-process inspection of each Round 1/2/3 boundary yields identical lawful reports/views. Mixed-cloak inspection with accepted opposing orders still hides the cruiser/fresh enemy orders and all private save fields. A corrupt child-process load prints only the fixed InvalidCheckpoint code.

Local SDK 10.0.401, locked restore and Release **0 warnings/errors**:

| Suite | Executed count | Result |
| --- | --- | --- |
| New durable storage / separate-process integration | **47** | Passed; 0 failed/skipped |
| Preserved real Application client | **51** | Passed; 0 failed/skipped |
| Preserved mock presenter | **115** | Passed; 0 failed/skipped |

The 47 cases include failed overwrites/serialization, invalid names/directories, corrupted carriers, duplicate saves/loads, explicit consent, stale host identity, metadata privacy, actor validation and no ordinary campaign-file discovery. Previous test sources are unchanged. The prior **1,621-test gameplay/legacy evidence** and unchanged vectors are retained from PR #23; that unchanged suite was not redundantly rerun locally for this host-only task. Existing repository workflows may rerun it automatically on push.

Reproduce the explicit storage csproj with dotnet restore --locked-mode, dotnet build -c Release --no-restore and dotnet test -c Release --no-build --no-restore. Its build copies the separate probe into the test output. [Dedicated CI](../.github/workflows/tactical-review-storage.yml) runs storage 47 / real-client 51 / mock 115 separately on all three platforms, verifies TRX counters and uploads evidence. Exact head/run are recorded in the draft PR/handover.

## Remaining boundaries

- No Unity slot interface/native acceptance, automatic between-battle stock recovery, gameplay policy or travel API binding.
- The later Primary [physical facade PR #24](https://github.com/AndreBal1983/HobbyDS9/pull/24) was reported at **e4d6194a98984ce06c33f1d46a1446878509fffa**, using a **complete physical composite format 2**. It is not imported or bound here. This B1 typed codec rejects incompatible versions/wrappers; it must never extract a nested B1 carrier to pretend physical resume.
- Future carrier specialization requires an explicit typed codec/store and exact Application validation against the complete authoritative root/package. The host seam separates storage from authority without guessing a future schema, filename migration or compatibility policy.
- Filesystem atomicity beyond tested local host behavior, hostile races, encryption, process-kill recovery and cross-writer protocols remain deferred.

This branch/PR remain reviewable drafts. PR #23 and its histories are preserved; no merge, force push or other-chat message is part of delivery.
