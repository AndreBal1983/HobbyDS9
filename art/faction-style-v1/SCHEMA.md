# Faction presentation token schema v1

Proposed design, no default/runtime activation. Read `theme-tokens.json` with `schemaVersion=1`.

Frozen shared keys: `shared.colors`, `shared.states`, `shared.typography`, `shared.layout`, `shared.motion`, `shared.audio`. Family geometry is in `families`. Every `profiles[]` item has explicit presentation `id`, `family`, five `colors` keys (background/surface/raised/brandPrimary/brandSecondary), taxonomy/status and separate `civilizationDefinitionIds` / `minorDefinitionIds` arrays. Unknown maps to `neutral`. Empty arrays do not register invented IDs.

All seven audited groups plus neutral have profiles. Runtime civilization IDs exist only for Federation/Cardassian/Dominion; Klingon/Romulan are documented style proposals, Ilex/Namar are independent minors. Preserve semantic state labels/shapes/colors, data ordering, navigation hierarchy and 44px controls across families; brand and chrome differ. No LCARS recolor mandate for non-Federation families.

Theme lookup is explicit and presentation-only, with lawful identity. It must not consult private state, infer faction from names/models/music, alter commands/rules/AI/authorization, activate database mode, or change offline/online authority. Keep current default behavior; GUI owns an add-only binding/sample. JSON is an authoring/reference asset, not a request to add a JSON package or bind every profile to the current New Campaign chooser. No new audio file/automatic faction playlist binding.

The complete guide/sample/provenance packet is available through [README.md](README.md) under the same version. Source audit pin is `2fc0dbca46073121379dd9491c859a02a6f22dcd`; source work-order ancestry is older and does not imply newer assets were deleted.
