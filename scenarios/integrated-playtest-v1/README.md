# Integrated campaign playtest v1 — proposed, opt-in

This is one authoritative campaign for strategic orders, R&D, paid construction and tactical combat. It is not a pair of separate default/review sessions. Outer package/save format **3**, profile `campaign-tactical-v2-integrated-playtest-1`; physical format 2, B1 format 1 and nested campaign Save 14 / authored Content 11 remain explicit. `science-engineering-v1`, `ship-design-compiler-v1` and existing resolver identities are unchanged. Default activation is disabled; policy/tuning remain proposed.

The four JSON files are raw inputs assembled under the exact hash contract in [the integration contract](../../docs/integrated-playtest-contract.md). They reuse existing valid physical-review definitions/equipment. Explicit scenario changes are the scenario ID, Federation working capital **250 credits**, stationary empty routes at Sol/Cardassia and matching initial last-seen locations. No extra mature tech, prerequisite progress, Industry multiplier, research buff or tactical equipment is granted.

Admitted construction is only `ship-design.source-cruiser`, bound to immutable provision `ship.equipment-cruiser`: **200 credits, 5 Dilithium, 100 Industry**, strategic movement **6**, full constructed hull **220**, initial sector stocks **18/25/12/0**, torpedoes **7/12** and the existing exact fit/revision/provenance. Fresh stocks are part of this explicitly authored construction template, not a general resupply/refit mechanic or an inferred blueprint conversion. Existing paid FIFO completion and stationary-fleet join/new-fleet allocation rules apply. Unsupported other static/immutable v1 blueprint builds reject. The legacy default scenario and old review files/hashes are untouched.

Starting 70/30 and six equal Science shares retain the existing exact 600-unit rules. Advanced Starship Engineering remains locked until actual Construction/Propulsion breakthroughs, then needs **36000** real Engineering units. Its completion does not authorize an unsupported v2 Advanced Scout or buff the authored cruiser.

Run the real backend walkthrough:

```powershell
dotnet run --project src/HobbyDS9.Headless -c Release -- --review-integrated-playtest
```

The executed transcript applies six-way Science and 100% Science, pays construction, completes the actual ship at **turn 13**, moves it to deliberate lawful contact, resolves joint rounds, finalizes once and retains **3 torpedoes** on the built ship. It then earns and completes Engineering at **turn 131** in the same campaign, reloads/continues and compares canonical, reversed commitments, waiting/turn reloads and reordered input. This long earned progression is automation evidence, not a recommendation to grind 131 turns manually or a claim of final campaign pacing.

New independent vectors from that transcript: package SHA-256 `88a919f6f51d244f79bb638ba933ff3c17305f25231a6b793aa6d53fca5f688b`; final full-root save SHA-256 `417d1852e052b4fe1f4f2b5f611ba49b82b7d76cd6154540a58bcaefc474e865`. These do not replace a legacy/review golden.

**Contact-duration scope gate:** the existing authored tracking knowledge/freshness window remains **20 turns**. The bounded construction/contact proof happens inside it. Knowledge does not automatically refresh merely because actors continue advancing turns, and after expiry encounter admission can fail even at co-location. A broader long campaign needs an explicit decision between a different authored review window or a separately implemented lawful observation/refresh system. No hidden-position shortcut or new freshness policy was introduced here.

All starting controllers are Human, retaining the existing trusted-hotseat review. There is no new general strategic-AI ship-design policy or arbitrary v1 equipment support. The renderer gets actor-scoped immutable strategy/travel/tactical views; the trusted store must save the entire format3 wrapper and reject old bare shapes without migration. Unified UI/native/audio acceptance belongs to the later coordinated build, not this backend artifact.
