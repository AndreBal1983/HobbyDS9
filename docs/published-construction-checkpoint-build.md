# Published construction checkpoint build

Status: source build preparation only. Actual Unity receiving, strict build and
native validation remain separate gates; this document does not claim they pass.

The dedicated Editor entry is
`GeneratedStaticAstronomyCampaignBuild.BuildWindowsPublishedConstructionCheckpoint`.
It calls the existing guarded strict Windows x64 IL2CPP builder with the new stage
`artifacts/generated-static-astronomy-v2/Windows-PublishedConstruction-20261025`.
Unity 6000.6.5f1, the integrated single-player scene, settings restoration and
refusal to overwrite an existing stage remain unchanged. Existing builder methods
and old packages/stages are preserved.

After the strict builder succeeds, this entry writes a playtest-specific
`README.txt` and `PLAY-PUBLISHED-CONSTRUCTION.txt`. The ordinary v2 walkthrough
remains available separately. The new guide explains the explicit provisional
landing-menu foldout, supported utility starter and beam/targeting sensor fit,
full allocations in every operating profile, exact published revision and owned
colony selection, immediate payment, frozen-R1 completion through R2, and the
distinct Save/Continue controls. Unsupported designs remain publishable but
unbuildable; no default/profile/validation or support-content change is made.

The construction support table is named/versioned provisional playtest tuning,
not final balance. Current catalogue costs, crew, access, mounts and power rules
are authoritative. Ship artwork grants no equipment or capabilities. Main v2 and
historical v1 saves retain their original namespaces, rules and explicit Continue
paths, without conversion or fallback.

The final package needs an external exact-source identity and actual validation
record before delivery. The guide deliberately does not claim native acceptance
or performance. In particular, the confirmation-redraw correction has CPU
regression coverage but still needs actual Unity receiving.

Related: [profile contract](published-construction-profile-v1.md),
[ship design](design/ship-design.md), [architecture](architecture.md).
