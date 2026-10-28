# Current demo modes and validation

Status checked 28 October 2026 against built source `20fba84c718b874c87c676b6d1ed62b8fa51420b`. This is an implementation and evidence map, not approval of the full design roadmap. Later documentation edits do not change that executable's source identity.

## Construction rules depend on the explicit campaign mode

The Stage A production contract in [architecture](architecture.md) describes the preserved base profile: costs are reserved when an order is enqueued. It does **not** describe normal construction in the newer [streamed activities profile](campaign-activities-streamed-v1.md), which is inherited by the colonisation modes.

Streamed construction queues without an ordinary upfront credit or material payment. Industry advances only as the required material tranche is available in the construction world's actual local stock. Economy can deliver goods through the existing physical logistics rules; an empire-wide balance alone is insufficient. Consumption follows cumulative integer work, so the same material quantum is not charged twice. Missing inputs pause progress; cancellation does not recover consumed materials or completed work. Buyout is still proposed, not implemented by this profile.

## Colonisation editions

| Explicit mode | Implemented increment | Save boundary |
| --- | --- | --- |
| [Expansion](colonisation-expansion-v1.md) | Real transport, settlement kit, conserved settlers, finite food, survey/travel, founding and establishment | Its own Format 12 edition and slots |
| [Growth](colonisation-growth-v2.md) | Kit-free passenger loading and immigration into owned Established colonies; one bounded rival immigration trip after its first settlement | Distinct Growth identity/vocabulary and slots; Format 12 shape retained |
| [Repeat Expansion](colonisation-repeat-player.md) | One additional rival establish-only expedition to a different world, retaining the first goal/history and real supply commitments | Complete Format 13, typed Repeat loader and separate slots |

Repeat supports two frontier settlements per AI actor under a provisional bounded policy. It does not implement unrestricted expansion, conquest, the full strategic planner or a second immigration goal. Selecting another menu does not migrate an old save. Normal complete replay must succeed before a loaded campaign replaces the live model.

The broader [colonisation/conquest design](design/colonisation-and-conquest.md) remains agreed direction with detailed rules and tuning open. These increments do not establish runtime support for every design item.

## What the current evidence proves

- Repeat's actual Unity scenario passed in 162.17 seconds: 51 ordinary human UI turns, a second distinct AI colony, 100 settlers and one real kit, four fed establishment weeks, exact whole-root Save/Menu/Continue, a fresh turn after loading, and own-view layout checks at 1280, 1920 and 3440 widths. See the [receiving receipt](E:/HobbyDS9/artifacts/colonisation-repeat-engine-footer-20261027/manifest.json).
- The exact Windows IL2CPP candidate built successfully in 71.96 seconds. Its 757 files were independently checked against their hashes and file census. See the [build receipt](E:/HobbyDS9/artifacts/colonisation-repeat-build-20261027/manifest.json).
- Native validation advanced on 28 October using the same executable: New Repeat 250/seed 42, hydroponics queued without an upfront credit/material charge, one normal production week, Save, normal window close, cold Continue, byte-identical whole-file re-export and a fresh turn 3. The two saved roots match across all 2,522,540 bytes. See the [scoped native result](E:/HobbyDS9/artifacts/colonisation-repeat-native-resume-20261028/native-result.json). The owner then stopped Computer Use; the live session and new slots were preserved without restoring over subsequent progress. The [earlier activation failure](E:/HobbyDS9/artifacts/colonisation-repeat-native-20261027/CHECKPOINT.md) remains recorded as a separate failed attempt.
- After the owner freed desktop access, the same Repeat executable loaded the historical Growth checkpoint through normal Growth Continue3. Ordinary Save1 before advancing a week matched the entire original 2,545,856-byte file. The UI retained 100 passengers, 20 food and no settlement kit; a fresh turn retained all passengers and consumed one food. Normal close and restoration of the newly captured 49-file/four-preference owner baseline were independently checked. See the [compatibility receipt](E:/HobbyDS9/artifacts/colonisation-repeat-desktop-release-20261028/CHECKPOINT.md). This adds scoped compatibility evidence to the earlier `4b723bc` [Growth](E:/HobbyDS9/artifacts/colonisation-growth-native-20261026/CHECKPOINT.md) and [passenger](E:/HobbyDS9/artifacts/colonisation-growth-passengers-native-20261026/CHECKPOINT.md) checks; broader native acceptance remains pending.

Mature Repeat Save/Continue took about 45.52/46.93 seconds in the Unity scenario; responsiveness indicators moved, but latency is unresolved. The zero-deaths result concerns the second AI camp, not every world: unattended human Mars visibly had food shortages. Intermittent native shutdown, broader native journeys, 1000-system performance, native input at all three sizes, audio and the full combat acceptance matrix remain open. The full validated-demo pointer has not been promoted to this candidate.
