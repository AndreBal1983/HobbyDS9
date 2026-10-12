#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    // Strategic-screen presentation only. Observed snapshots and existing commands
    // remain the source of all facts, eligibility and production operations.
    public sealed partial class ClientEntry
    {
        private readonly Dictionary<string, bool> strategicWorldDetails = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> strategicShipChoice = new Dictionary<string, string>();

        private void SelectInitialStrategicWorld()
        {
            var snapshot = host.Snapshot;
            var mars = StrategicScreenPresentation.InitialWorld(snapshot);
            if (mars == null) return; // Other scenarios retain their ordinary overview.
            var node = snapshot.Navigation.Nodes.FirstOrDefault(n => n.AssociatedSystemId == mars.SystemId);
            if (node == null) return;
            selectedPlanet = mars.PlanetId; selection.SelectNode(node.NodeId); activeTab = "Selection";
            message.text = "Mars selected • Live colony data • Drag map to pan / Scroll to zoom";
        }
        private void ShowStrategicEmpire(CampaignSnapshot snapshot)
        {
            var actor = Human(snapshot); Text("EMPIRE / OVERVIEW", true);
            if (actor == null) { Text("No human-controlled civilization."); return; }
            var block = new LcarsInspectorBlock(OwnerName(snapshot, actor.CivilizationId)); details.Add(block);
            var worlds = snapshot.Campaign.Colonies.Where(c => c.OwnerCivilizationId == actor.CivilizationId).ToList();
            var fleets = snapshot.Campaign.Fleets.Where(f => f.OwnerCivilizationId == actor.CivilizationId).ToList();
            block.Row("Worlds / fleets", worlds.Count + " / " + fleets.Count);
            block.Row("Population", worlds.Sum(c => c.Population).ToString());
            block.Row("Credits", actor.Credits.ToString());
            block.Row("Engineering reserve", ScienceAllocationDraft.Conceptual(actor.EngineeringReserve));
            Text("OWNED WORLDS", true);
            foreach (var world in worlds)
            {
                details.Add(new LcarsDataRow(world.Environment.DisplayName, "Industry " + world.Economy.Industry + " • Queue " + world.ProductionQueue.Count));
                var node = snapshot.Navigation.Nodes.FirstOrDefault(n => n.AssociatedSystemId == world.SystemId);
                if (node != null) ActionButton("Manage " + world.Environment.DisplayName, () => { ClearRoute(); selectedPlanet = world.PlanetId; SelectNode(node.NodeId); });
            }
            Text("FLEETS", true);
            foreach (var fleet in fleets)
            {
                details.Add(new LcarsDataRow(NodeName(snapshot, new NavigationNodeId(fleet.LocationNodeId.Value)), fleet.ShipIds.Count + " ships"));
                ActionButton("Inspect fleet", () => { ClearRoute(); selection.SelectFleet(fleet.FleetId); activeTab = "Selection"; Refresh(); });
            }
        }
        private void ShowStrategicWorld(CampaignSnapshot snapshot, ColonySnapshot world)
        {
            shell.DetailsFrame.Heading.text = world.Environment.DisplayName.ToUpperInvariant() + " / WORLD INSPECTOR";
            var economy = world.Economy;
            var summary = new LcarsInspectorBlock(world.Environment.DisplayName.ToUpperInvariant() + " / COLONY"); details.Add(summary);
            summary.Row("Owner", world.OwnerCivilizationId.HasValue ? OwnerName(snapshot, world.OwnerCivilizationId.Value) : "Unowned");
            summary.Row("Class / population", world.Environment.PlanetClass + " / " + world.Population);
            summary.Add(new LcarsMeter("Capacity", world.Population, economy.EffectiveCapacity));
            summary.Row("Stability / target", $"{world.State.Stability / 100.0:0.#}% / {economy.TargetStability / 100.0:0.#}%");
            summary.Row("Food output / demand", economy.FoodOutput + " / " + economy.FoodDemand);
            summary.Row("Food reserve", world.Food.ToString());
            summary.Row("Credits / R&D", economy.CreditOutput + " / " + economy.ResearchOutput);

            // Keep orders ahead of the optional technical detail and long histories.
            ShowStrategicProduction(snapshot, world);
            var more = new Foldout { text = "World details / governance / deliveries", value = strategicWorldDetails.TryGetValue(world.PlanetId.Value, out var expanded) && expanded, name = "world-details" };
            more.RegisterValueChangedCallback(e => { if (e.target == more) strategicWorldDetails[world.PlanetId.Value] = e.newValue; });
            // Reuse existing detail controls and governance callbacks without new
            // contracts or a second implementation of those screens.
            var count = details.contentContainer.childCount;
            ShowWorld(snapshot, world);
            var children = details.contentContainer.Children().Skip(count).ToList();
            foreach (var child in children) { child.RemoveFromHierarchy(); more.Add(child); }
            details.Add(more);
        }
        private void ShowStrategicProduction(CampaignSnapshot snapshot, ColonySnapshot world)
        {
            var block = new LcarsInspectorBlock("PRODUCTION / ORDERS") { name = "selected-world-production" }; details.Add(block);
            block.Row("Industry / queue", world.Economy.Industry + " / " + world.ProductionQueue.Count);
            block.Row("Local resources", Resources(snapshot, world.State.Stockpiles));
            var actor = Human(snapshot);
            if (actor == null || actor.CivilizationId != world.OwnerCivilizationId)
            { block.Add(new LcarsReadout("Production orders require a world you control.")); return; }
            var model = new ProductionOverview(host.Client, snapshot);
            if (world.ProductionQueue.Count == 0) block.Row("Queue", "Empty • Choose a ship design");
            void AddQueue()
            {
                foreach (var item in world.ProductionQueue)
                {
                    var designName = ProductionOverviewView.QueueName(snapshot, item);
                    block.Add(new LcarsMeter(designName, item.CompletedIndustry, item.RequiredIndustry));
                    var cancel = new LcarsPillButton("Cancel " + designName, () => Confirm("Cancel " + designName + "? Unused costs are refunded; completed industry is lost.",
                        () => SfxFeedback.Describe(model.Cancel(world.ColonyId, item.QueueItemId))));
                    cancel.AddToClassList("lcars-destructive"); cancel.SetEnabled(model.CanCancel); block.Add(cancel);
                }
            }
            // A single selector avoids many repetitive design cards pushing orders
            // below the fold. Eligibility still comes from Application.
            var options = model.Designs(world.ColonyId)
                .OrderBy(o => o.Design.RequiredEngineeringApplicationDefinitionIds.Count)
                .ThenBy(o => o.Design.DisplayName, StringComparer.Ordinal).ToList();
            if (options.Count == 0) { block.Add(new LcarsReadout("No ship designs available.")); AddQueue(); return; }
            var index = strategicShipChoice.TryGetValue(world.PlanetId.Value, out var previous)
                ? Math.Max(0, options.FindIndex(o => o.Design.DefinitionId.Value == previous)) : 0;
            var designs = new DropdownField("Ship design", options.Select(o => o.Design.DisplayName).ToList(), index);
            designs.AddToClassList("combat-plan-field"); block.Add(designs);
            var quote = new LcarsReadout(); quote.AddToClassList("lcars-order-quote"); block.Add(quote);
            var reason = new LcarsReadout(); reason.AddToClassList("lcars-order-reason"); block.Add(reason);
            var queue = new LcarsPillButton("Queue ship", () => Run(() => SfxFeedback.Describe(model.Enqueue(world.ColonyId, options[designs.index].Design.DefinitionId)))) { name = "queue-selected-design" };
            block.Add(queue);
            void UpdateDesign()
            {
                var option = options[designs.index]; var design = option.Design;
                quote.text = design.CreditCost + " credits • " + Resources(snapshot, design.ResourceCosts) + " • " + design.IndustryCost + " industry";
                reason.text = option.CanEnqueue ? "Ready to queue at this world." : CommandFeedback.Explain(option.UnavailableReason);
                reason.EnableInClassList("lcars-warning", !option.CanEnqueue);
                queue.text = "Queue ship"; queue.tooltip = reason.text; queue.SetEnabled(option.CanEnqueue);
            }
            designs.RegisterValueChangedCallback(_ =>
            {
                strategicShipChoice[world.PlanetId.Value] = options[designs.index].Design.DefinitionId.Value; UpdateDesign();
            }); UpdateDesign();
            AddQueue(); // Existing queues never push the enqueue action below the fold.
        }
    }
}
