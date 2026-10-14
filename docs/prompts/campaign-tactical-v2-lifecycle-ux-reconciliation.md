# Codex 2 tactical v2 UX reconciliation assignment

Status: **prepared parallel documentation assignment; not dispatched**. Reconcile the existing UX specification with [the lifecycle milestone](../campaign-tactical-v2-lifecycle-spec.md) and latest [tactical decisions](../design/tactical-combat.md). Documentation only; no production Unity/backend files, desktop tools, branch merging or runtime claims.

Use the existing `codex/campaign-tactical-v2-ux-spec` worktree if suitable. Last reported local revision is `d4d12ae`; inspect current state first and preserve local work. Read the accepted combined/equipment reports from verified checkpoints. Keep current runtime support separate from proposed UI. Do not require unmerged backend APIs or manufacture signatures.

## Required revisions

- One fleet equals one command unit. No separate tactical escort wing created automatically; transports and escorts share a strategic fleet to benefit from screening.
- Delete the allied-fleet protection picker and rewrite Escort/Transport examples as ship roles inside one fleet. Different friendly fleets remain separate and cannot screen one another.
- Keep the main controls to fleet maneuver plus enemy-fleet focus. Remove player ship-priority/per-ship firing controls; ship selection still opens inspection.
- Show fleet focus as preference, preserve it when automatic legal fallback occurs and explain fallback in reports. No targeting guarantees or hidden target leakage.
- Battles begin Distant. Preserve individual equipment/resource inspection and explain composition contributions without arbitrary label-based buffs.
- Add a strategic fleet cloak setting and the fully cloaked, mixed and mutual-cloak examples. Round-one protection is not an extra attack; reveal at its end. Incoming torpedoes can trigger ordinary defensive reactions without exposing launchers.
- Show visible enemy types/hull/shields, hiding reserves/fresh orders across controls and reports. A no-visible-enemy encounter communicates caution without invented identities/counts.
- After battle show retained damage/stocks, stopped travel and fresh next-turn strategic orders. Pursuit follows normal map movement; no catch-roll modal or remote interception minigame. Mutual re-engagement occurs next turn.
- Retain the revised proposal that saving committed state preserves unsent local edits; distinguish that proposal from accepted backend save behavior.

Update wireframes, examples and D1–D8 register. Mark resolved decisions as agreed with their date/source, keep remaining formulas, travel timing and persistence/version policies open. Replace outdated scope statements and apparent still-open cloak/formation questions. For every proposed UI action identify required authoritative behavior, not a fictional API name.

Add acceptance examples for same-route separate fleets, same-fleet escorted transport, automatic focus fallback, hidden enemy reserves, three cloak cases, saved round boundary and successive depleted battles. Controls must remain readable at the existing three resolutions, but textual wireframes are not rendered/native acceptance.

Use repository-relative links only when the target files actually exist in the delivery branch; otherwise retain clearly identified pinned/local review sources. Deliver a reviewed documentation commit and concise change/remaining-gates report. Do not publish, push, merge, message another chat or begin Unity runtime work unless separately instructed. Later Unity implementation waits for a proven authoritative campaign lifecycle.
