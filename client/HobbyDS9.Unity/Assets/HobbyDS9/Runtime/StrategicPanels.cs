#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using EntityId = HobbyDS9.Contracts.EntityId;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    public sealed partial class ClientEntry
    {
        private string activeTab = "Empire";
        private SessionPhase? lastPhase;
        private readonly Dictionary<string, Button> tabs = new Dictionary<string, Button>();
        private readonly Dictionary<string, List<FleetCombatPlanDto>> combatPlans = new Dictionary<string, List<FleetCombatPlanDto>>();
        private EntityId? routeFleet;
        private RoutePreview? routePreview;
        private SaveSlotStore saves = null!;
        private int saveSlot = 1;
        private EntityId? selectedPlanet;

        private static CivilizationSnapshot? Human(CampaignSnapshot snapshot) => snapshot.Campaign.Civilizations
            .Where(c => c.ControllerKind == ControllerKind.Human).OrderBy(c => c.CivilizationId.Value, StringComparer.Ordinal).FirstOrDefault();
        private static string ResearchName(CampaignSnapshot snapshot, DefinitionId id) => snapshot.Content.Research.Single(r => r.DefinitionId == id).DisplayName;
        private static string NodeName(CampaignSnapshot snapshot, NavigationNodeId id) => GalaxyProjection.Nodes(snapshot).Single(n => n.NodeId == id).Label;
        private void AddStrategicTabs(VisualElement root)
        {
            var bar = new VisualElement(); bar.AddToClassList("strategic-tabs");
            foreach (var name in new[] { "Empire", "Selection", "Research", "Saves", "Combat" })
            {
                var tab = new Button(() => { if (host == null) return; activeTab = name; Refresh(); }) { text = name == "Saves" ? "Save / Load" : name };
                tabs[name] = tab; bar.Add(tab);
            }
            root.Add(bar);
        }
        private void ClearRoute() { routeFleet = null; routePreview = null; }
        private void SelectNode(NavigationNodeId id)
        {
            if (routeFleet.HasValue)
            {
                try { routePreview = host.Client.Preview(host.Snapshot, routeFleet.Value, id); }
                catch (Exception error) { message.text = "Route preview unavailable: " + error.Message; ClearRoute(); }
            }
            else selection.SelectNode(id);
            activeTab = "Selection"; Refresh();
        }
        private Button ActionButton(string label, Action action, bool enabled = true, string explanation = "")
        {
            var button = new Button(action) { text = label, tooltip = explanation };
            button.SetEnabled(enabled); details.Add(button); return button;
        }
        private void Confirm(string prompt, Func<string> operation)
        {
            // Inline, only one confirmation at a time; refreshing removes it.
            details.Q<VisualElement>("confirmation")?.RemoveFromHierarchy();
            var box = new VisualElement { name = "confirmation" }; box.AddToClassList("confirmation");
            var label = new Label(prompt); label.style.whiteSpace = WhiteSpace.Normal; box.Add(label);
            box.Add(new Button(() => Run(operation)) { text = "Confirm" });
            box.Add(new Button(() => box.RemoveFromHierarchy()) { text = "Keep current state" });
            details.Add(box); details.ScrollTo(box);
        }
        private void ShowEmpire(CampaignSnapshot snapshot)
        {
            var actor = Human(snapshot); Text("Empire", true);
            if (actor == null) { Text("No human-controlled civilization in this campaign."); return; }
            Text(OwnerName(snapshot, actor.CivilizationId));
            Text($"Credits {actor.Credits} • Research reserve {actor.ResearchReserve}");
            Text("Research: " + (actor.ResearchTargetDefinitionId.HasValue ? ResearchName(snapshot, actor.ResearchTargetDefinitionId.Value) : "No target selected"));
            Text($"Campaign population movement: {snapshot.Campaign.PopulationStatistics.Migrated} normal migrants • {snapshot.Campaign.PopulationStatistics.RefugeesMoved} refugees");
            Text("Owned colonies", true);
            foreach (var colony in snapshot.Campaign.Colonies.Where(c => c.OwnerCivilizationId == actor.CivilizationId))
            {
                Text($"{colony.Environment.DisplayName} • Population {colony.Population}/{colony.PopulationCapacity}\nFood {colony.Food} • Industry {colony.Economy.Industry} • Queue {colony.ProductionQueue.Count}");
                var node = snapshot.Navigation.Nodes.FirstOrDefault(n => n.AssociatedSystemId == colony.SystemId);
                if (node != null) ActionButton("Manage " + colony.Environment.DisplayName, () => { ClearRoute(); selectedPlanet = colony.PlanetId; SelectNode(node.NodeId); });
            }
            Text("Fleets", true);
            foreach (var fleet in snapshot.Campaign.Fleets.Where(f => f.OwnerCivilizationId == actor.CivilizationId))
                ActionButton(fleet.FleetId.Value + " • " + SystemName(snapshot, fleet.LocationSystemId), () =>
                { ClearRoute(); selection.SelectFleet(fleet.FleetId); activeTab = "Selection"; Refresh(); });
        }
        private void ShowResearch(CampaignSnapshot snapshot)
        {
            Text("Research", true); var actor = Human(snapshot);
            if (actor == null) { Text("No human civilization selected."); return; }
            Text("Research reserve: " + actor.ResearchReserve + ". The reserve is spent by turn processing when the target can complete.");
            foreach (var option in StrategicOptions.Research(snapshot, actor.CivilizationId))
            {
                var definition = option.Definition;
                Text(definition.DisplayName + " • " + option.State, true);
                Text("Cost: " + definition.ResearchCost);
                Text("Prerequisites: " + (definition.Prerequisites.Count == 0 ? "None" : string.Join(", ", definition.Prerequisites.Select(id => ResearchName(snapshot, id)))));
                var explanation = CommandFeedback.Explain(option.UnavailableReason);
                if (!option.CanSelect) Text(explanation);
                ActionButton("Research " + definition.DisplayName, () => Run(() => CommandFeedback.Describe(host.Client.Research(snapshot, actor.CivilizationId, definition.DefinitionId))), option.CanSelect, explanation);
            }
        }
        private void ShowSelection(CampaignSnapshot snapshot)
        {
            if (selection.Fleet.HasValue)
            {
                ShowFleet(snapshot, snapshot.Campaign.Fleets.Single(f => f.FleetId == selection.Fleet.Value)); return;
            }
            if (!selection.Node.HasValue) { Text("Select a system or fleet on the map.", true); return; }
            var node = GalaxyProjection.Nodes(snapshot).Single(n => n.NodeId == selection.Node.Value);
            Text(node.Label, true); Text("Navigation: " + node.NodeId.Value);
            if (node.SystemId.HasValue)
            {
                var system = snapshot.Campaign.StarSystems.Single(s => s.SystemId == node.SystemId.Value);
                Text(system.OwnerCivilizationId.HasValue ? "Owner: " + OwnerName(snapshot, system.OwnerCivilizationId.Value) : "Unowned");
                var worlds = snapshot.Campaign.Colonies.Where(c => c.SystemId == system.SystemId).ToList();
                if (worlds.Count > 0)
                {
                    var index = Math.Max(0, worlds.FindIndex(c => c.PlanetId == selectedPlanet));
                    selectedPlanet = worlds[index].PlanetId;
                    var field = new DropdownField("World", worlds.Select(c => c.Environment.DisplayName).ToList(), index);
                    field.AddToClassList("combat-plan-field");
                    field.RegisterValueChangedCallback(_ => { selectedPlanet = worlds[field.index].PlanetId; Refresh(); }); details.Add(field);
                    ShowColony(snapshot, worlds[index]);
                }
            }
            else Text("Transit region; no associated star system.");
            foreach (var link in snapshot.Navigation.Links.Where(l => l.EndpointA == node.NodeId || l.EndpointB == node.NodeId))
                Text($"{link.Kind} link • traversal cost {link.TraversalCost}");
            foreach (var fleet in node.Fleets)
                ActionButton("Select " + fleet.FleetId.Value, () => { ClearRoute(); selection.SelectFleet(fleet.FleetId); Refresh(); });
        }
        private void ShowColony(CampaignSnapshot snapshot, ColonySnapshot colony)
        {
            ShowWorld(snapshot, colony);
            Text($"Population {colony.Population} / {colony.PopulationCapacity}\nFood {colony.Food} • Industry {colony.Economy.Industry}");
            if (colony.OwnerCivilizationId.HasValue)
                Text("Owner credits: " + snapshot.Campaign.Civilizations.Single(c => c.CivilizationId == colony.OwnerCivilizationId.Value).Credits);
            var actor = Human(snapshot);
            var canManage = actor != null && colony.OwnerCivilizationId == actor.CivilizationId && StrategicOptions.PlanningAccess(snapshot, actor.CivilizationId) == CommandErrorCode.None;
            Text("Production queue", true);
            if (colony.ProductionQueue.Count == 0) Text("Queue is empty.");
            foreach (var item in colony.ProductionQueue)
            {
                var design = snapshot.Campaign.ShipDesigns.Single(d => d.DefinitionId == item.ShipDesignDefinitionId);
                Text($"{design.DisplayName} • Industry {item.CompletedIndustry}/{item.RequiredIndustry}\nPaid {item.PaidCredits} credits • {Resources(snapshot, item.PaidResources)}");
                ActionButton("Cancel " + design.DisplayName, () => Confirm("Cancel this queue item? Unused credits and strategic resources are refunded proportionally; completed industry is lost.",
                    () => CommandFeedback.Describe(host.Client.Cancel(snapshot, actor!.CivilizationId, colony.ColonyId, item.QueueItemId))), canManage,
                    canManage ? "Cancel and refund the unused portion" : "Only your colonies in Planning can receive orders.");
            }
            Text("Ship designs", true);
            if (actor == null) return;
            foreach (var option in StrategicOptions.Production(snapshot, actor.CivilizationId, colony.ColonyId))
            {
                var design = option.Design;
                Text(design.DisplayName, true);
                Text($"{design.CreditCost} credits • {Resources(snapshot, design.ResourceCosts)} • {design.IndustryCost} industry\nHull {design.MaxHull} • Shields {design.CombatProfile.ShieldCapacity}");
                Text("Required research: " + (design.RequiredResearchDefinitionIds.Count == 0 ? "None" : string.Join(", ", design.RequiredResearchDefinitionIds.Select(id => ResearchName(snapshot, id)))));
                var explanation = CommandFeedback.Explain(option.UnavailableReason);
                if (!option.CanEnqueue) Text(explanation);
                ActionButton("Queue " + design.DisplayName, () => Run(() => CommandFeedback.Describe(host.Client.Enqueue(snapshot, actor.CivilizationId, colony.ColonyId, design.DefinitionId))), option.CanEnqueue, explanation);
            }
        }
        private void ShowFleet(CampaignSnapshot snapshot, FleetSnapshot fleet)
        {
            Text("Fleet", true); Text(fleet.FleetId.Value); Text("Owner: " + OwnerName(snapshot, fleet.OwnerCivilizationId));
            var node = snapshot.Navigation.Nodes.FirstOrDefault(n => n.AssociatedSystemId == fleet.LocationSystemId);
            Text("Location: " + SystemName(snapshot, fleet.LocationSystemId) + (node == null ? "" : "\nNavigation: " + node.NodeId.Value));
            Text("Queued route: " + (fleet.RouteSystemIds.Count == 0 ? "None" : string.Join(" → ", fleet.RouteSystemIds.Select(id => SystemName(snapshot, id)))));
            var actor = Human(snapshot);
            var controlled = actor != null && actor.CivilizationId == fleet.OwnerCivilizationId && StrategicOptions.PlanningAccess(snapshot, actor.CivilizationId) == CommandErrorCode.None;
            ActionButton(routeFleet.HasValue ? "Choose another destination on the map" : "Set / replace route", () =>
            { routeFleet = fleet.FleetId; routePreview = null; Refresh(); }, controlled && node != null);
            if (routeFleet == fleet.FleetId)
            {
                Text("Choose a destination node on the map. Previewing does not issue an order.");
                if (routePreview != null)
                {
                    var preview = routePreview;
                    Text("Destination: " + NodeName(snapshot, preview.Destination), true);
                    if (!preview.Route.Success) Text("Route unavailable: " + preview.Route.ErrorCode);
                    else
                    {
                        Text("Traversal cost: " + preview.Route.TraversalCost + " (not a turn estimate)");
                        Text("Start: " + NodeName(snapshot, preview.Origin));
                        for (var i = 0; i < preview.Route.Steps.Count; i++) Text((i + 1) + ". " + NodeName(snapshot, preview.Route.Steps[i]));
                        if (preview.Route.Steps.Count == 0) Text("This clears the queued route; the fleet is already here.");
                    }
                    ActionButton("Confirm route", () => Run(() =>
                    {
                        var result = host.Client.SubmitRoute(preview); ClearRoute(); return CommandFeedback.Describe(result);
                    }), controlled && preview.Route.Success);
                }
                ActionButton("Cancel route preview", () => { ClearRoute(); Refresh(); });
            }
            Text("Ships", true);
            foreach (var ship in snapshot.Campaign.Ships.Where(s => s.FleetId == fleet.FleetId))
            {
                var design = snapshot.Campaign.ShipDesigns.Single(d => d.DefinitionId == ship.ShipDesignDefinitionId);
                Text($"{design.DisplayName}\n{ship.ShipId.Value}\nHull {ship.CurrentHull}/{design.MaxHull} • Shield capacity {design.CombatProfile.ShieldCapacity}");
            }
        }
        private static string Resources(CampaignSnapshot snapshot, IReadOnlyList<ResourceAmountSnapshot> values) => values.Count == 0 ? "No strategic resources"
            : string.Join(", ", values.Select(r => r.Amount + " " + snapshot.Content.Resources.Single(d => d.DefinitionId == r.ResourceDefinitionId).DisplayName));
        private void ShowWorld(CampaignSnapshot snapshot, ColonySnapshot world)
        {
            var e = world.Economy; var p = world.Environment;
            Text(p.DisplayName + " • Class " + p.PlanetClass, true);
            Text($"Stability {world.State.Stability / 100.0:0.#}% → target {e.TargetStability / 100.0:0.#}%\nNatural capacity {e.NaturalCapacity} • Effective {e.EffectiveCapacity}\nUnhoused/over-capacity {e.Crowding} • Experienced habitability {e.AverageHabitability / 100.0:0.#}%");
            Text($"Current output estimates: Credits {e.CreditOutput} • Local industry {e.Industry} • R&D {e.ResearchOutput}\nFood output {e.FoodOutput} • Demand {e.FoodDemand} • Reserve {world.Food}\nShortage turns {world.State.FoodShortageTurns} • Logistics capacity {e.LogisticsCapacity}");
            Text("Population", true);
            foreach (var c in world.State.Cohorts)
            {
                var species = snapshot.Content.Species.Single(d => d.DefinitionId == c.SpeciesDefinitionId);
                Text($"{species.DisplayName}: {c.Population} • {c.LegalStatus}\nSatisfaction {c.Satisfaction / 100.0:0.#}% • Integration {c.Integration / 100.0:0.#}%\nCivic alignment {c.CivicAlignment / 100.0:0.#}% • Radicalisation {c.Radicalisation / 100.0:0.#}%");
                foreach (var memory in c.Memories) Text("Memory: " + memory.Kind + " • " + memory.Magnitude + " • expires turn " + memory.ExpiryTurn);
            }
            Text("Environment", true);
            Text($"Gravity {p.Gravity / 10000.0:0.##}g • Pressure {p.Pressure / 10000.0:0.##} • Radiation {p.Radiation}\nAtmosphere: {p.Atmosphere}");
            foreach (var h in p.Habitats) Text($"{h.Kind}: {h.ShareBps / 100.0:0.#}% surface • natural capacity {h.NaturalCapacity}");
            foreach (var a in e.Allocations)
            {
                var cohort = world.State.Cohorts.Single(c => c.CohortId == a.CohortId);
                var species = snapshot.Content.Species.Single(d => d.DefinitionId == cohort.SpeciesDefinitionId);
                Text($"{species.DisplayName} → {a.Habitat?.ToString() ?? "Artificial"}: {a.Population} • habitability {a.Habitability / 100.0:0.#}%");
            }
            Text("Infrastructure / resources", true);
            foreach (var i in world.State.Infrastructure)
            {
                var definition = snapshot.Content.Infrastructure.Single(d => d.DefinitionId == i.DefinitionId);
                Text(definition.DisplayName + " × " + i.Count + " • extraction " + Resources(snapshot, definition.Extraction));
            }
            Text("Local stock: " + Resources(snapshot, world.State.Stockpiles));
            Text("Desired stock: " + Resources(snapshot, world.State.DesiredStockpiles));
            var deliveries = snapshot.Campaign.LastDeliveries.Where(r => r.OriginPlanetId == world.PlanetId.Value || r.DestinationPlanetId == world.PlanetId.Value).ToList();
            Text("Last turn deliveries", true);
            if (deliveries.Count == 0) Text("No deliveries recorded. Food and strategic resources remain local until delivered.");
            foreach (var r in deliveries)
                Text($"{r.ResourceDefinitionId}: {r.OriginPlanetId} → {r.DestinationPlanetId}\nNeed {r.ImportNeed} • Reachable surplus {r.ReachableSurplus} • Capacity {r.RouteCapacity}\nReceiving stability {r.StabilityFactor / 100.0:0.#}% • Integrity {r.RouteIntegrity / 100.0:0.#}% • Delivered {r.Delivered}\nRoute: {string.Join(" → ", r.RouteSystemIds.Select(id => SystemName(snapshot, id)))}");
        }
        private void ShowSaves()
        {
            Text("Campaign saves", true); Text("Three local slots. Saving includes pending encounters and buffered combat results. Unsaved combat plans and map camera state are presentation-only.");
            var slots = new DropdownField("Slot", Enumerable.Range(1, SaveSlotStore.SlotCount).Select(i => "Campaign " + i).ToList(), saveSlot - 1);
            slots.AddToClassList("combat-plan-field");
            slots.RegisterValueChangedCallback(_ => { saveSlot = slots.index + 1; Refresh(); }); details.Add(slots);
            var slot = saveSlot; var exists = saves.Exists(slot); var valid = false;
            if (exists)
            {
                try { Text(host.SaveSummary(saves.Read(slot))); Text("Saved " + saves.ModifiedUtc(slot).ToLocalTime().ToString("g")); valid = true; }
                catch (Exception error) { Text("Save unavailable: " + error.Message); }
            }
            else Text("Empty slot");
            string Save()
            {
                saves.Write(slot, host.SaveJson(), exists);
                return "Campaign saved in slot " + slot + ".";
            }
            ActionButton("Save current campaign", () =>
            {
                if (exists) Confirm("Overwrite the campaign in slot " + slot + "?", Save); else Run(Save);
            });
            ActionButton("Load saved campaign", () => Confirm("Replace the live campaign with slot " + slot + "? Unsaved progress will be lost.", () =>
            {
                host.LoadJson(saves.Read(slot));
                // Only reset the view after a successful validated session replacement.
                ClearRoute(); selection = new GalaxySelection(); selectedPlanet = null; combatPlans.Clear(); lastPhase = null;
                activeTab = "Empire"; map.ResetView();
                return "Campaign loaded from slot " + slot + ".";
            }), valid);
            Text("Stored in the application's persistent user-data folder. A failed load keeps the current campaign intact.");
        }
    }
}
