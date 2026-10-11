#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    // No map-selection dependency and no authoritative calculations or mutable DTOs.
    public sealed class ProductionOverviewView : ScrollView
    {
        private readonly Dictionary<string, bool> expanded = new Dictionary<string, bool>();
        public ProductionOverviewView()
        { name = "production-overview"; AddToClassList("production-panel"); }
        public void Present(ProductionOverview model, Action<Func<string>> run)
        {
            Clear();
            Add(new LcarsSectionHeader("Empire production") { name = "production-heading" });
            Add(new Label(model.Actor == null ? "No human civilization." : "Empire-wide • Credits " + model.Actor.Credits));
            if (model.Worlds.Count == 0) Add(new Label("No owned worlds."));
            foreach (var world in model.Worlds)
            {
                var id = world.ColonyId.Value;
                var section = new Foldout { name = id, text = world.Environment.DisplayName + " • Industry " + world.Economy.Industry,
                    value = !expanded.TryGetValue(id, out var open) || open };
                section.AddToClassList("production-world");
                section.RegisterValueChangedCallback(e => { if (e.target == section) expanded[id] = e.newValue; });
                Add(section);
                section.Add(new Label("Production queue"));
                if (world.ProductionQueue.Count == 0) section.Add(new Label("Queue is empty."));
                foreach (var item in world.ProductionQueue)
                {
                    var design = model.Observed.Campaign.ShipDesigns.Single(d => d.DefinitionId == item.ShipDesignDefinitionId);
                    var row = new VisualElement { name = item.QueueItemId.Value }; section.Add(row);
                    row.Add(new Label(design.DisplayName + $" • Industry {item.CompletedIndustry}/{item.RequiredIndustry}\nPaid {item.PaidCredits} credits • " + Resources(model.Observed, item.PaidResources)));
                    var cancel = new Button(() =>
                    {
                        var previous = row.Q<VisualElement>("cancel-confirmation"); if (previous != null) previous.RemoveFromHierarchy();
                        var confirmation = new VisualElement { name = "cancel-confirmation" }; confirmation.AddToClassList("confirmation");
                        confirmation.Add(new Label("Cancel? Unused credits/resources are refunded proportionally; completed Industry is lost."));
                        confirmation.Add(new Button(() => run(() => CommandFeedback.Describe(model.Cancel(world.ColonyId, item.QueueItemId)))) { text = "Confirm cancellation" });
                        confirmation.Add(new Button(() => confirmation.RemoveFromHierarchy()) { text = "Keep item" }); row.Add(confirmation);
                    }) { text = "Cancel " + design.DisplayName };
                    cancel.AddToClassList("lcars-destructive"); cancel.SetEnabled(model.CanCancel); row.Add(cancel);
                }
                var options = new Foldout { text = "Ship designs", value = true }; section.Add(options);
                foreach (var option in model.Designs(world.ColonyId))
                {
                    var design = option.Design;
                    var row = new VisualElement { name = design.DefinitionId.Value }; options.Add(row);
                    row.Add(new Label(design.DisplayName + $"\n{design.CreditCost} credits • " + Resources(model.Observed, design.ResourceCosts) + $" • {design.IndustryCost} Industry"));
                    row.Add(new Label($"Hull {design.MaxHull} • Shields {design.CombatProfile.ShieldCapacity}"));
                    row.Add(new Label("Required research: " + (design.RequiredResearchDefinitionIds.Count == 0 ? "None" : string.Join(", ", design.RequiredResearchDefinitionIds.Select(id => model.Observed.Content.Research.Single(r => r.DefinitionId == id).DisplayName)))));
                    var reason = CommandFeedback.Explain(option.UnavailableReason);
                    if (!option.CanEnqueue) row.Add(new Label(reason + " [" + option.UnavailableReason + "]"));
                    var queue = new Button(() => run(() => CommandFeedback.Describe(model.Enqueue(world.ColonyId, design.DefinitionId))))
                        { text = "Queue " + design.DisplayName, tooltip = reason };
                    queue.SetEnabled(option.CanEnqueue); row.Add(queue);
                }
            }
        }
        private static string Resources(CampaignSnapshot snapshot, IReadOnlyList<ResourceAmountSnapshot> amounts) => amounts.Count == 0
            ? "no strategic resources" : string.Join(", ", amounts.Select(r => r.Amount + " " + snapshot.Content.Resources.Single(d => d.DefinitionId == r.ResourceDefinitionId).DisplayName));
    }
}
