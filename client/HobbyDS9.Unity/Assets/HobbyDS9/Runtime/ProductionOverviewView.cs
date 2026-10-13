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
        private string? selectedWorld;
        public ProductionOverviewView()
        { name = "production-overview"; AddToClassList("production-panel"); }
        public void Present(ProductionOverview model, Action<Func<string>> run)
        {
            Clear();
            Add(new LcarsSectionHeader("Empire production") { name = "production-heading" });
            Add(new Label(model.Actor == null ? "No human civilization." : "Empire-wide • Credits " + model.Actor.Credits));
            if (model.Worlds.Count == 0) Add(new Label("No owned worlds."));
            if(selectedWorld!=null && !model.Worlds.Any(w=>w.ColonyId.Value==selectedWorld))selectedWorld=null;
            var captions=new[]{"All owned colonies"}.Concat(model.Worlds.Select(w=>w.Environment.DisplayName+" / "+w.ColonyId.Value)).ToList();
            var colony=new DropdownField("Owned colony",captions,selectedWorld==null?0:model.Worlds.ToList().FindIndex(w=>w.ColonyId.Value==selectedWorld)+1) {name="production-owned-colony"};
            colony.RegisterValueChangedCallback(_=> {selectedWorld=colony.index==0?null:model.Worlds[colony.index-1].ColonyId.Value;Present(model,run);});Add(colony);
            foreach (var world in model.Worlds.Where(w=>selectedWorld==null || w.ColonyId.Value==selectedWorld))
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
                    var designName = QueueName(model.Observed, item);
                    var row = new VisualElement { name = item.QueueItemId.Value }; section.Add(row);
                    row.Add(new Label(designName + $" • Industry {item.CompletedIndustry}/{item.RequiredIndustry}\nPaid {item.PaidCredits} credits • " + Resources(model.Observed, item.PaidResources)));
                    var cancel = new LcarsPillButton("", () =>
                    {
                        var previous = row.Q<VisualElement>("cancel-confirmation"); if (previous != null) previous.RemoveFromHierarchy();
                        var confirmation = new VisualElement { name = "cancel-confirmation" }; confirmation.AddToClassList("confirmation");
                        confirmation.Add(new Label("Cancel? Unused credits/resources are refunded proportionally; completed Industry is lost."));
                        confirmation.Add(new LcarsPillButton("", () => run(() => SfxFeedback.Describe(model.Cancel(world.ColonyId, item.QueueItemId)))) { text = "Confirm cancellation" });
                        confirmation.Add(new LcarsPillButton("", () => confirmation.RemoveFromHierarchy()) { text = "Keep item" }); row.Add(confirmation);
                    }) { text = "Cancel " + designName };
                    cancel.AddToClassList("lcars-destructive"); cancel.SetEnabled(model.CanCancel); row.Add(cancel);
                }
                var options = new Foldout { text = "Ship designs", value = true }; section.Add(options);
                var published=new Foldout {text="Published blueprints / exact revisions",value=true,name="blueprint-options-"+id};section.Add(published);
                var blueprintOptions=model.Blueprints(world.ColonyId);
                if(blueprintOptions.Count==0)published.Add(new Label("No owned published revisions. Publish a class in Ship design."));
                foreach(var option in blueprintOptions)
                {
                    var blueprint=option.Blueprint.Blueprint;var cost=blueprint.Performance.Cost!;
                    var row=new VisualElement {name="blueprint-option-"+blueprint.BlueprintId+"-r"+blueprint.Revision};published.Add(row);
                    row.Add(new Label(blueprint.Fit.DisplayName+" • revision "+blueprint.Revision+(option.Blueprint.IsLatestRevision?" / latest":" / historical")));
                    row.Add(new Label(blueprint.BlueprintId+"\n"+cost.Credits+" credits • "+cost.Industry+" Industry • "+string.Join(", ",cost.Resources.Select(r=>r.Amount+" "+r.ResourceDefinitionId))));
                    row.Add(new Label("Prerequisites: owned colony with shipbuilding; local resources and credits. Engineering was validated at publication; this frozen revision remains buildable."));
                    row.Add(new Label(CommandFeedback.Explain(option.ErrorCode)+" ["+option.ErrorCode+"]"));
                    var queued=false;
                    var queue=new LcarsPillButton("",()=> {
                        if(queued)return; queued=true;
                        row.Query<Button>().ForEach(b=>b.SetEnabled(false));
                        run(()=>SfxFeedback.Describe(model.EnqueueBlueprint(world.ColonyId,option)));
                    }) {text="Queue revision "+blueprint.Revision,name="queue-blueprint-"+blueprint.BlueprintId+"-r"+blueprint.Revision};
                    queue.SetEnabled(option.CanBuild);row.Add(queue);
                }
                foreach (var option in model.Designs(world.ColonyId))
                {
                    var design = option.Design;
                    var row = new VisualElement { name = design.DefinitionId.Value }; options.Add(row);
                    row.Add(new Label(design.DisplayName + $"\n{design.CreditCost} credits • " + Resources(model.Observed, design.ResourceCosts) + $" • {design.IndustryCost} Industry"));
                    row.Add(new Label($"Hull {design.MaxHull} • Shields {design.CombatProfile.ShieldCapacity}"));
                    row.Add(new Label("Required Mature Engineering: " + (design.RequiredEngineeringApplicationDefinitionIds.Count == 0 ? "None" : string.Join(", ", design.RequiredEngineeringApplicationDefinitionIds.Select(id => model.Observed.Content.Engineering.Single(r => r.DefinitionId == id).DisplayName)))));
                    var reason = CommandFeedback.Explain(option.UnavailableReason);
                    if (!option.CanEnqueue) row.Add(new Label(reason));
                    var queue = new LcarsPillButton("", () => run(() => SfxFeedback.Describe(model.Enqueue(world.ColonyId, design.DefinitionId))))
                        { text = "Queue " + design.DisplayName, tooltip = reason };
                    queue.SetEnabled(option.CanEnqueue); row.Add(queue);
                }
            }
        }
        // Campaign queues pin an immutable blueprint revision, not a static design.
        // Resolve only the pinned revision for its caption; identity remains visible.
        internal static string QueueName(CampaignSnapshot snapshot, ProductionQueueItemSnapshot item) => item.Blueprint != null
            ? item.Blueprint.BlueprintId + " • revision " + item.Blueprint.Revision
            : snapshot.Campaign.ShipDesigns.Single(d => d.DefinitionId == item.ShipDesignDefinitionId).DisplayName;
        private static string Resources(CampaignSnapshot snapshot, IReadOnlyList<ResourceAmountSnapshot> amounts) => amounts.Count == 0
            ? "no strategic resources" : string.Join(", ", amounts.Select(r => r.Amount + " " + snapshot.Content.Resources.Single(d => d.DefinitionId == r.ResourceDefinitionId).DisplayName));
    }
}
