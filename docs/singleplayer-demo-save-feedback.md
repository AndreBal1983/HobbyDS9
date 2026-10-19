# Save/load feedback beside unsent edits

19 October 2026. **CPU-compiled presentation correction; native rendering pending.** Actual native review showed an empty Demo 3 load correctly preserving the accepted campaign and unsent research draft, but the global unsent-edit hint hid the NotFound result.

The Save/Load page now has a named `save-load-result` banner above the slot controls, independent of the global guidance priority. It describes the last checkpoint result and whether accepted state/local edits were preserved. Empty, incompatible, occupied and failed-write/serialization cases have plain-language explanations; successful save distinguishes committed data from unsent local edits. It is explicitly the last result, not a claim that the currently selected slot was the attempted slot. The global unsent-edit guidance remains unchanged.

No persistence, validation, draft, command or rollback logic changed. Existing actual-source failed-load and atomic-failure cases remain the behavior evidence; the new banner still needs actual graphics/native visibility acceptance. The frozen executable/package was not modified.
