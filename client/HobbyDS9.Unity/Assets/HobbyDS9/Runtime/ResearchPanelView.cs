#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    // All costs, thresholds, eligibility and output come from detached Application facts.
    // Local allocation previews never spend or submit; Apply uses the observed stamp.
    public sealed class ResearchPanelView : VisualElement
    {
        public ResearchPanelView() { name = "science-engineering"; AddToClassList("research-panel"); }
        public void Present(CampaignSnapshot snapshot, StrategicClient client, Action<Func<string>> run)
        {
            Clear(); Add(new LcarsSectionHeader("Science & Engineering"));
            var actor = snapshot.Campaign.Civilizations.FirstOrDefault(c => c.ControllerKind == ControllerKind.Human);
            if (actor == null) { Line("No human civilization."); return; }
            var observed = ResearchOverviewSnapshot.For(snapshot, actor.CivilizationId);
            Line($"R&D Capacity {observed.ResearchOutput}\nScience {observed.SciencePercent:0.##}% ({ScienceAllocationDraft.Conceptual(observed.ScienceAllocation)}) • Engineering {observed.EngineeringPercent:0.##}% ({ScienceAllocationDraft.Conceptual(observed.EngineeringAllocation)})");
            Line($"Science reserve {observed.ScienceReserveConceptual:0.###} • Engineering reserve {observed.EngineeringReserveConceptual:0.###}");
            Line("Engineering target: " + (observed.Target.HasValue ? snapshot.Content.Engineering.Single(d => d.DefinitionId == observed.Target.Value).DisplayName : "None"));
            var allocations = Enumerable.Range(0, 21).Select(i => i * ResearchUnits.Scale / 20).ToList();
            if (!allocations.Contains(observed.ScienceAllocationUnits)) { allocations.Add(observed.ScienceAllocationUnits); allocations.Sort(); }
            var split = new DropdownField("Science / Engineering", allocations.Select(a => $"{100m * a / ResearchUnits.Scale:0.##}% / {100m * (ResearchUnits.Scale - a) / ResearchUnits.Scale:0.##}%").ToList(), allocations.IndexOf(observed.ScienceAllocationUnits)) { name = "research-split" };
            split.AddToClassList("combat-plan-field"); Add(split);
            bool planning = StrategicOptions.PlanningAccess(snapshot, actor.CivilizationId) == CommandErrorCode.None;
            split.SetEnabled(planning);
            var applySplit = Button("Apply R&D split", "apply-research-split", () => run(() => SfxFeedback.Describe(client.AllocateResearch(snapshot, actor.CivilizationId, allocations[split.index]))), false);
            split.RegisterValueChangedCallback(_ => applySplit.SetEnabled(planning && allocations[split.index] != observed.ScienceAllocationUnits));
            Add(new LcarsSectionHeader("Science"));
            var draft = new ScienceAllocationDraft(actor.Research!.ScienceAllocations);
            Line("Shares are within Science. +/− adjusts by 5 percentage points, transferring evenly to/from other disciplines. Apply confirms all six shares.");
            var labels = new Dictionary<ScienceDiscipline, Label>();
            var decreases = new Dictionary<ScienceDiscipline, Button>();
            var increases = new Dictionary<ScienceDiscipline, Button>();
            var applyScience = Button("Apply Science allocation", "apply-science-allocation", () => run(() => SfxFeedback.Describe(client.AllocateScience(snapshot, actor.CivilizationId, draft.Export()))), false);
            Button equalize = null!;
            equalize = Button("Equalize Science", "equalize-science", () =>
            {
                draft.Equalize();
                RefreshDraft();
            }, planning && observed.Science.Any(d => d.AllocationWeight != ResearchUnits.Scale / 6));
            void RefreshDraft()
            {
                foreach (var discipline in observed.Science)
                {
                    labels[discipline.Discipline].text = "Science share " + ScienceAllocationDraft.DisplayPercent(draft.Percent(discipline.Discipline)) + (draft.Changed ? " • preview" : "");
                    decreases[discipline.Discipline].SetEnabled(planning && draft.Weight(discipline.Discipline) > 0);
                    increases[discipline.Discipline].SetEnabled(planning && draft.Weight(discipline.Discipline) < ResearchUnits.Scale);
                }
                applyScience.SetEnabled(planning && draft.Changed);
                equalize.SetEnabled(planning && observed.Science.Any(d => draft.Weight(d.Discipline) != ResearchUnits.Scale / 6));
            }
            foreach (var discipline in observed.Science)
            {
                var card = new VisualElement { name = "science-" + discipline.Discipline }; card.AddToClassList("research-card"); Add(card);
                Line($"{discipline.Discipline} • Level {discipline.Level}/{discipline.MaxLevel}", card);
                labels[discipline.Discipline] = Line("", card); labels[discipline.Discipline].name = "science-share-" + discipline.Discipline;
                card.Add(new ProgressBar { value = (float)discipline.ProgressPercent, title = discipline.Maxed ? "Maximum level" : $"{discipline.ProgressPercent:0.#}% to next level" });
                foreach (var point in discipline.Breakpoints)
                    Line($"{(point.Achieved ? "✓" : "○")} Breakthrough {point.Ordinal}: {100m * point.Threshold / discipline.IntervalCost:0.#}%", card);
                if (discipline.AllocatedWhileMaxed) Line("Maximum level: this share remains in Science reserve. Reallocate explicitly to spend it elsewhere.", card);
                decreases[discipline.Discipline] = Button("− 5%", "decrease-science-" + discipline.Discipline, () => { draft.SetWeight(discipline.Discipline, Math.Max(0, draft.Weight(discipline.Discipline) - ResearchUnits.Scale / 20)); RefreshDraft(); }, planning, "Local allocation preview", card);
                increases[discipline.Discipline] = Button("+ 5%", "increase-science-" + discipline.Discipline, () => { draft.SetWeight(discipline.Discipline, Math.Min(ResearchUnits.Scale, draft.Weight(discipline.Discipline) + ResearchUnits.Scale / 20)); RefreshDraft(); }, planning, "Local allocation preview", card);
            }
            RefreshDraft();
            Add(new LcarsSectionHeader("Engineering"));
            foreach (var application in observed.Engineering)
            {
                var definition = application.Definition; var card = new VisualElement { name = "engineering-" + definition.DefinitionId.Value }; card.AddToClassList("research-card"); Add(card);
                var state = application.Band == EngineeringMaturityBand.Mature ? "Completed" : application.IsTarget ? "Current" : application.ScienceSatisfied.All(s => s) && application.PrerequisitesSatisfied.All(s => s) ? "Available" : "Locked";
                Line(definition.DisplayName + " • " + KindLabel(definition.Kind) + " • " + state, card);
                card.Add(new ProgressBar { value = application.MaturityPercent, title = $"{application.MaturityPercent}% • {(state == "Completed" ? state : application.Band.ToString())}" });
                Line("Engineering cost " + ScienceAllocationDraft.Conceptual(application.Cost), card);
                if (!string.IsNullOrWhiteSpace(definition.EffectSummary)) Line("Result: " + definition.EffectSummary, card);
                for (int i = 0; i < definition.ScienceRequirements.Count; i++)
                { var q = definition.ScienceRequirements[i]; Line($"{(application.ScienceSatisfied[i] ? "✓" : "○")} {q.Discipline} interval {q.IntervalStartLevel}→{q.IntervalStartLevel + 1}, breakthrough {q.BreakthroughOrdinal}", card); }
                for (int i = 0; i < definition.Prerequisites.Count; i++) Line((application.PrerequisitesSatisfied[i] ? "✓ " : "○ ") + snapshot.Content.Engineering.Single(d => d.DefinitionId == definition.Prerequisites[i]).DisplayName, card);
                Line("Completion unlocks capabilities and prerequisites. One current project; retargeting preserves progress.", card);
                if (!application.CanTarget) Line(CommandFeedback.Explain(application.UnavailableReason), card);
                Button("Develop " + definition.DisplayName, "target-" + definition.DefinitionId.Value, () => run(() => SfxFeedback.Describe(client.Engineering(snapshot, actor.CivilizationId, definition.DefinitionId))), application.CanTarget, CommandFeedback.Explain(application.UnavailableReason), card);
            }
            Button("Clear Engineering target", "clear-engineering-target", () => run(() => SfxFeedback.Describe(client.Engineering(snapshot, actor.CivilizationId, null))), planning && observed.Target.HasValue);
            var receipts = new Foldout { name = "research-receipts", text = "Latest-turn research", value = false }; Add(receipts);
            if (observed.Receipts.Count == 0) Line("No Research phase receipts yet.", receipts);
            foreach (var receipt in observed.Receipts)
                Line(ReceiptText(receipt) + (receipt.Discipline.HasValue ? $" • {receipt.Discipline} interval {receipt.IntervalStartLevel}" : "") + (receipt.BreakthroughOrdinal > 0 ? $" • breakthrough {receipt.BreakthroughOrdinal}" : "") + (receipt.ApplicationDefinitionId.HasValue ? " • " + snapshot.Content.Engineering.Single(d => d.DefinitionId == receipt.ApplicationDefinitionId.Value).DisplayName : ""), receipts);
        }
        private static string KindLabel(EngineeringProjectKind kind) => kind == EngineeringProjectKind.NewCapability ? "New capability" : kind == EngineeringProjectKind.CivilApplication ? "Civil application" : "Refinement";
        private static string ReceiptText(ResearchReceiptSnapshot receipt)
        {
            switch (receipt.Category)
            {
                case "allocation": return "R&D generated: " + ScienceAllocationDraft.Conceptual(receipt.Amount);
                case "science-allocated": return "Science allocated: " + ScienceAllocationDraft.Conceptual(receipt.Amount);
                case "engineering-allocated": return "Engineering allocated: " + ScienceAllocationDraft.Conceptual(receipt.Amount);
                case "science-spent": return "Science spent: " + ScienceAllocationDraft.Conceptual(receipt.Amount);
                case "science-breakthrough": return "Science breakthrough discovered";
                case "science-level": return "Science level reached: " + receipt.Amount;
                case "engineering-spent": return "Engineering progress added: " + ScienceAllocationDraft.Conceptual(receipt.Amount);
                case "engineering-band": return "Engineering stage: " + (EngineeringMaturityBand)receipt.Amount;
                case "engineering-mature": return "Engineering project completed";
                default: return receipt.Category;
            }
        }
        private Label Line(string text, VisualElement? parent = null)
        { var label = new Label(text); label.style.whiteSpace = WhiteSpace.Normal; (parent ?? this).Add(label); return label; }
        private Button Button(string text, string id, Action clicked, bool enabled = true, string tooltip = "", VisualElement? parent = null)
        { var button = new LcarsPillButton("", clicked) { text = text, name = id, tooltip = tooltip }; button.SetEnabled(enabled); (parent ?? this).Add(button); return button; }
    }
}
