# Independent interface design prototype

Source for the complete HobbyDS9 concept gallery. No runtime imports, network/game APIs, save access or generation credits. All values are fictional. Feature statuses describe scoped existing capabilities separately from proposed layouts.

From repository root, use the bundled Node runtime with Playwright on NODE_PATH:

```powershell
$env:NODE_PATH='C:/Users/ambal/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules'
& 'C:/Users/ambal/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe' tools/interface-mockups/render.cjs
& 'C:/Users/ambal/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/interface-mockups/package.py
& 'C:/Users/ambal/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe' tools/interface-mockups/review.cjs
```

Requires installed Chrome and Pillow. Rendering explicitly disables GPU; no Unity/Blender/native app is launched. Optional `MOCKUP_SMOKE=1` renders eight representative views at all sizes before the same interaction checks; remove it for full delivery. Do not run two render recipes against the same output concurrently.

Output: `artifacts/art/coherent-interface/gallery.html`, interactive `index.html`, all size-specific PNGs, coverage/asset hashes, browser-check report, contact sheets/storyboards and ZIP. A lightweight `HobbyDS9-Interactive-Interface-Preview.zip` includes all interactive screens without PNG snapshots. Re-run the idempotent packager after the gallery check to include its new report in the ZIPs. These are browser concepts, not gameplay or native UI evidence. Open gallery locally; no server is required.

See [inventory](../../docs/art/coherent-interface/inventory.md) and [design handoff](../../docs/art/coherent-interface/design-handoff.md). Existing reference-model and current demo work remain separate.
