# DS9 strategic slice content

[scenario.json](scenario.json) defines `ds9-strategic-slice-v1`: three civilizations, six graph nodes, four colonies and five undirected edges. Federation is Human; Cardassian Union and Dominion are AI. The Bajoran Wormhole–Gamma Exit edge is tagged Wormhole; every edge costs one movement step. Labels remain in content and rules contain no franchise-specific behavior.

Sol, Cardassia and the Founders' Homeworld are owned home systems; Bajor and both wormhole nodes are unowned. Home colonies and neutral Bajor start at population 1000/capacity 2000, food 20/materials 10 using economy.baseline. Civilizations begin with 50 credits, research reserve 0, no target/unlocks and empty colony queues. Each faction starts with one scout and stationary fleet at its home system.

The baseline Scout has industry/material/credit costs 20/8/10, hull 10 and no research requirements. Advanced Scout costs 24/10/12, has hull 15 and requires research.advanced-engineering (cost 9, no prerequisites). Different civilizations with co-located fleets generate frozen post-Movement encounters. The production resolver is deterministic no-loss; there are no weapons, damage rolls, speed differences or faction-specific abilities. Initial phase is Planning with empty pending/resolved combat lists. No legacy code, artwork or audio has been imported.

ContentFormatVersion is **4**, SaveFormatVersion **8**, and RulesVersion **phase-1-campaign-combat-v1**. The host assigns ContentHash as lowercase SHA256 of exact UTF-8 file bytes, including labels/whitespace; JSON excludes its own hash. Reload requires matching content/rules identity. Canonical rules-only hashing is deferred.
