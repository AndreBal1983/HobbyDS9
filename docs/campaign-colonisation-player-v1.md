# Colonisation player integration v1

Status, 26 October 2026: provisional player integration with separately scoped receiving/build/native evidence. See the [V2 starting-roster status](campaign-colonisation-roster-v2.md) for the current playtest and its limits. Earlier packages and save slots remain preserved. This describes implemented player integration, not completed acceptance of the full colonisation roadmap.

The menu adds an explicit **Colonisation campaign (provisional playtest)** with New and three Continue choices. It retains the existing streamed campaign and earlier Continue paths. Colonisation saves use Format11 and their own slot directory; there is no conversion or decoder fallback. A loaded campaign completes ordinary full ordered replay before replacing the current host or discarding local edits.

The same campaign shell, map, research, designer, music, combat, exact hull art and four-part Frontier Commission remain available. Its mission counts real historical infrastructure/ship/science/contact receipts. A settlement kit or automatic camp gives no infrastructure credit. AI procurement and travel remain supported; autonomous AI colonisation planning is not included.

## Player loop

1. Under Worlds, use the direct + on a ready transport and settlement kit. The same queue also offers the three existing ship roles, actual buildings and the provisional frontier mine. Orders consume Industry and delivered materials gradually, with no normal construction credit charge. Local stocks, consumed materials, current output and queue progress are separate facts.
2. Use the ordinary map to visit systems and discover physical bodies. Planet survey lists discovered bodies only. Start or cancel a survey with an owned capable ship. Completed surveys expose environment and historically stamped control; remote control is not guaranteed current, and known presence does not imply whole-system sovereignty.
3. At an active established owned world, review a real source cohort, 100 settlers, one locally manufactured kit, 8–20 food and any selected local materials. Source population and transport limits are revalidated on Confirm. Move the transport along an ordinary known route. Supplies are finite; prolonged shortages can kill settlers.
4. At a surveyed destination, request deployment. All actors are adjudicated together after the week's combat; competing landings may remain disputed. A founding camp has one cohort, actual food, cumulative deaths, work and fed-week progress. It needs multiple fed weeks before establishment. Passenger unloading cannot bypass this process.
5. Reuse the empty transport. Supply-only loading carries no passengers or kit, and delivery requires actual presence. Founding evacuation carries its surviving cohort to an established active owned receiver. An empty outpost can admit new settlers only through a real equipped recommissioning expedition. Retained expedition history remains visible after a transport is destroyed.

Science and minor-contact activities use their existing lawful choices and outcomes. Leaving an active survey or scientific expedition prompts before committing travel. Ordinary travel needs no interruption prompt.

## Ownership and limits

Views contain detached own-world, own-manifest and lawfully observed-body data. They do not expose all-actor roots, hidden body counts, rival cargo or full settlement proofs. Transport capabilities are derived from validated immutable publication sources before presentation; frame capture does not recompile fits or replay saves. Successful worker operations prepare the complete frame and observations, then atomically publish on the owning thread. Previews and abandoned candidates cannot grant authority or change the live campaign.

The named content values remain provisional playtest engineering. Finite journal, record, ID, revision and root-byte/work budgets may reject an optional order without changing the last valid campaign. Terminal recording reservations do not guarantee unblocked return routes or unlimited future advances. Conquest, inhabited-world settlement, mature-city evacuation and a general cargo/trade system remain outside this slice.

See [colonisation and conquest](design/colonisation-and-conquest.md), [time and movement](design/time-movement-mobilisation.md), [exploration](design/exploration-missions.md) and [architecture](architecture.md). Reference compilation proves API compatibility only; actual layout, hit targets, animated busy feedback, material disposal and IL2CPP typed Continue require receiving evidence.
