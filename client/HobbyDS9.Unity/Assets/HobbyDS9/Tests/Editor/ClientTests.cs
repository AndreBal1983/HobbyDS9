using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient.Tests
{
    public sealed class ClientTests
    {
        private static CampaignHost Start() => new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);
        private static CampaignHost ThreePartyCombat(int battles = 1)
        {
            var host = Start(); var save = host.Client.ExportSave();
            foreach (var actor in save.GameState.Civilizations) actor.ControllerKind = ControllerKind.Human;
            foreach (var fleet in save.GameState.Fleets)
            { fleet.LocationNodeId = "nav.bajor"; fleet.RouteNodeIds.Clear(); fleet.TransitTotalCost = 0; fleet.TransitProgress = 0; }
            if (battles == 2)
                foreach (var original in save.GameState.Fleets.ToArray())
                {
                    var source = save.GameState.Ships.Single(s => s.FleetId == original.FleetId);
                    var fleetId = original.FleetId + "-second"; var shipId = source.ShipId + "-second";
                    save.GameState.Fleets.Add(new FleetStateDto { FleetId = fleetId, OwnerCivilizationId = original.OwnerCivilizationId,
                        LocationNodeId = "nav.gamma-exit", ShipIds = new System.Collections.Generic.List<string> { shipId }, RouteNodeIds = new System.Collections.Generic.List<string>() });
                    save.GameState.Ships.Add(new ShipStateDto { ShipId = shipId, FleetId = fleetId, OwnerCivilizationId = original.OwnerCivilizationId,
                        ShipDesignDefinitionId = source.ShipDesignDefinitionId, CurrentHull = source.CurrentHull });
                    save.GameState.EntityIdentities.Add(new EntityIdentityDto { EntityId = fleetId });
                    save.GameState.EntityIdentities.Add(new EntityIdentityDto { EntityId = shipId, DefinitionId = source.ShipDesignDefinitionId });
                }
            host.LoadJson(Newtonsoft.Json.JsonConvert.SerializeObject(save));
            Assert.That(host.EndTurn().Status, Is.EqualTo(TurnStatus.AwaitingCombat));
            save = host.Client.ExportSave();
            foreach (var actor in save.GameState.Civilizations) actor.ControllerKind = actor.CivilizationId == "civ.federation" ? ControllerKind.Human : ControllerKind.AI;
            host.LoadJson(Newtonsoft.Json.JsonConvert.SerializeObject(save)); return host;
        }
        private static System.Collections.Generic.IReadOnlyList<ScienceAllocationDto> DesiredScience(CampaignSnapshot snapshot)
        {
            var research=snapshot.Campaign.Civilizations.Single(c=>c.CivilizationId.Value=="civ.federation").Research;
            var draft=new ScienceAllocationDraft(research.ScienceAllocations);draft.SetWeight(ScienceDiscipline.Propulsion,200);return draft.Export();
        }
        private static ResearchPanelView ResearchView(CampaignHost host)
        {
            var view=new ResearchPanelView();view.Present(host.Snapshot,host.Client,operation=>operation());return view;
        }
        [Test]
        public void ScienceEngineeringPanelProjectsSixDisciplinesAndLockedApplicationWithoutMutation()
        {
            var host=Start();var before=host.SaveJson();var view=ResearchView(host);
            foreach(ScienceDiscipline discipline in System.Enum.GetValues(typeof(ScienceDiscipline)))
            {
                Assert.That(view.Q<VisualElement>("science-"+discipline),Is.Not.Null);
                Assert.That(view.Q<Button>("increase-science-"+discipline),Is.Not.Null);
            }
            var application=view.Q<VisualElement>("engineering-engineering.advanced-starship-engineering");
            Assert.That(application,Is.Not.Null);Assert.That(application.Query<Label>().ToList().Any(l=>l.text.Contains("Propulsion interval 1→2, breakthrough 2")),Is.True);
            Assert.That(view.Q<Button>("target-engineering.advanced-starship-engineering").enabledSelf,Is.False);
            Assert.That(view.Query<ProgressBar>().ToList().Count,Is.EqualTo(7));Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [Test]
        public void ResearchSplitPreviewIsLocalAndSubmissionUsesRealStampedBoundary()
        {
            var host=Start();var before=host.SaveJson();var observed=host.Snapshot;var view=ResearchView(host);var field=view.Q<DropdownField>("research-split");
            // Unity dispatches value-change events only for controls attached to a panel.
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
            try
            {
                window.Show();window.rootVisualElement.Add(view);
                field.index=10;Assert.That(host.SaveJson(),Is.EqualTo(before));Assert.That(view.Q<Button>("apply-research-split").enabledSelf,Is.True);
            }
            finally { window.Close(); }
            var actor=new HobbyDS9.Contracts.EntityId("civ.federation");Assert.That(host.Client.AllocateResearch(observed,actor,300).Accepted,Is.True);
            Assert.That(host.Snapshot.Campaign.StateRevision,Is.EqualTo(observed.Campaign.StateRevision+1));
            Assert.That(host.Client.AllocateScience(observed,actor,DesiredScience(host.Snapshot)).ErrorCode,Is.EqualTo(CommandErrorCode.StaleRevision));
            var save=host.SaveJson();host.LoadJson(save);Assert.That(host.Snapshot.Campaign.Civilizations.Single(c=>c.CivilizationId==actor).Research.ScienceAllocationUnits,Is.EqualTo(300));
        }
        [Test]
        public void AllocationControlsUseApplicationAndLatestReceiptsRenderWithoutSpending()
        {
            var host=Start();var actor=new HobbyDS9.Contracts.EntityId("civ.federation");
            Assert.That(host.Client.AllocateScience(host.Snapshot,actor,DesiredScience(host.Snapshot)).Accepted,Is.True);
            Assert.That(host.EndTurn().Status,Is.EqualTo(TurnStatus.Completed));var before=host.SaveJson();var view=ResearchView(host);
            Assert.That(view.Q<Foldout>("research-receipts").Query<Label>().ToList().Any(l=>l.text.Contains("Science spent")),Is.True);
            Assert.That(view.Q<Button>("apply-science-allocation").enabledSelf,Is.False);Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [Test]
        public void EngineeringOperationalProgressClearsReloadsAndMaturesThroughRealHost()
        {
            var host=Start();var save=host.Client.ExportSave();
            var human=save.GameState.Civilizations.Single(c=>c.CivilizationId=="civ.federation");foreach(var d in human.Disciplines)d.Level=2;human.EngineeringReserve=30*ResearchUnits.Scale;
            host.LoadJson(Newtonsoft.Json.JsonConvert.SerializeObject(save));var actor=new HobbyDS9.Contracts.EntityId("civ.federation");var app=new DefinitionId("engineering.advanced-starship-engineering");
            Assert.That(ResearchView(host).Q<Button>("target-"+app.Value).enabledSelf,Is.True);
            Assert.That(host.Client.Engineering(host.Snapshot,actor,app).Accepted,Is.True);Assert.That(host.EndTurn().Status,Is.EqualTo(TurnStatus.Completed));
            var progress=ResearchOverviewSnapshot.For(host.Snapshot,actor).Engineering.Single();Assert.That(progress.Band,Is.EqualTo(EngineeringMaturityBand.Operational));
            Assert.That(host.Client.Engineering(host.Snapshot,actor,null).Accepted,Is.True);var json=host.SaveJson();host.LoadJson(json);
            Assert.That(ResearchOverviewSnapshot.For(host.Snapshot,actor).Engineering.Single().Progress,Is.EqualTo(progress.Progress));
            Assert.That(host.Client.Engineering(host.Snapshot,actor,app).Accepted,Is.True);for(int i=0;i<30;i++)
            {
                var turn=host.EndTurn();Assert.That(turn.Status,Is.Not.EqualTo(TurnStatus.Rejected));
                if(turn.Status==TurnStatus.AwaitingCombat)
                {
                    foreach(var encounter in host.Pending())Assert.That(host.Resolve(encounter).Status,Is.EqualTo(CombatSubmissionStatus.Buffered));
                    Assert.That(host.FinalizeCombats().Status,Is.EqualTo(TurnStatus.Completed));
                }
            }
            var mature=ResearchOverviewSnapshot.For(host.Snapshot,actor);Assert.That(mature.Engineering.Single().MaturityPercent,Is.EqualTo(100));Assert.That(mature.Target,Is.Null);
            var before=host.SaveJson();Assert.That(ResearchView(host).Q<Button>("target-"+app.Value).enabledSelf,Is.False);Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [Test]
        public void AwaitingCombatBlocksAllResearchPlanControls()
        {
            var host=ThreePartyCombat();var before=host.SaveJson();var view=ResearchView(host);
            Assert.That(view.Q<Button>("apply-research-split").enabledSelf,Is.False);
            foreach(var button in view.Query<Button>().ToList())Assert.That(button.enabledSelf,Is.False);
            Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [Test]
        public void SixAllocationPreviewsAndEqualizationRemainLocalUntilStampedConfirmation()
        {
            var host=Start();var before=host.SaveJson();var actor=new HobbyDS9.Contracts.EntityId("civ.federation");
            var original=host.Snapshot;var draft=new ScienceAllocationDraft(original.Campaign.Civilizations.Single(c=>c.CivilizationId==actor).Research.ScienceAllocations);
            draft.SetWeight(ScienceDiscipline.Propulsion,200);Assert.That(draft.Export().Sum(a=>a.Weight),Is.EqualTo(600));Assert.That(draft.Changed,Is.True);
            Assert.That(host.SaveJson(),Is.EqualTo(before));Assert.That(host.Client.AllocateScience(original,actor,draft.Export()).Accepted,Is.True);
            var view=ResearchView(host);Assert.That(view.Q<Button>("equalize-science").enabledSelf,Is.True);
            var current=host.Snapshot;draft.Equalize();Assert.That(draft.Export().All(a=>a.Weight==100),Is.True);
            Assert.That(host.Client.AllocateScience(current,actor,draft.Export()).Accepted,Is.True);
            var json=host.SaveJson();host.LoadJson(json);view=ResearchView(host);
            foreach(ScienceDiscipline discipline in System.Enum.GetValues(typeof(ScienceDiscipline)))
            {
                Assert.That(view.Q<Label>("science-share-"+discipline).text,Does.Contain("16⅔%"));
                Assert.That(view.Q<Button>("decrease-science-"+discipline),Is.Not.Null);
                Assert.That(view.Q<Button>("increase-science-"+discipline),Is.Not.Null);
            }
            Assert.That(view.Q<Button>("equalize-science").enabledSelf,Is.False);Assert.That(host.SaveJson(),Is.EqualTo(json));
        }
        [Test]
        public void EqualScienceProgressesAllSixAndReportsConceptualAmountsAndProjectKind()
        {
            var host=Start();Assert.That(host.EndTurn().Status,Is.EqualTo(TurnStatus.Completed));var actor=new HobbyDS9.Contracts.EntityId("civ.federation");
            var research=ResearchOverviewSnapshot.For(host.Snapshot,actor);Assert.That(research.Science.All(d=>d.Progress==280),Is.True);
            Assert.That(research.SciencePercent,Is.EqualTo(70m));Assert.That(research.EngineeringReserveConceptual,Is.EqualTo(1.2m));
            var before=host.SaveJson();var view=ResearchView(host);var labels=view.Query<Label>().ToList();
            Assert.That(labels.Any(l=>l.text.Contains("New capability")&&l.text.Contains("Locked")),Is.True);
            Assert.That(labels.Any(l=>l.text.Contains("Unlocks Advanced Scout")),Is.True);
            Assert.That(labels.Any(l=>l.text.Contains("R&D generated: 4")),Is.True);
            Assert.That(labels.Any(l=>l.text.Contains("R&D generated: 2400")),Is.False);
            Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [Test]
        public void MaxedAllocatedScienceShowsBankWarningAndZeroProgressWithoutMutation()
        {
            var host=Start();var save=host.Client.ExportSave();var human=save.GameState.Civilizations.Single(c=>c.CivilizationId=="civ.federation");
            human.Disciplines.Single(d=>d.Discipline==ScienceDiscipline.Biotech).Level=10;
            host.LoadJson(Newtonsoft.Json.JsonConvert.SerializeObject(save));var before=host.SaveJson();var view=ResearchView(host);
            Assert.That(view.Q<VisualElement>("science-Biotech").Query<Label>().ToList().Any(l=>l.text.Contains("remains in Science reserve")),Is.True);
            Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        private static CombatPanelView CombatView(CampaignHost host)
        {
            var view = new CombatPanelView();
            view.Present(host.Snapshot, host.Client, new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<FleetCombatPlanDto>>(), operation => operation());
            return view;
        }
        [Test]
        public void ThreePartyCombatExposesOnlyHumanControlsAndRejectsAiPlanOverrides()
        {
            var host = ThreePartyCombat(); var request = host.Pending().Single(); var before = host.SaveJson();
            var view = CombatView(host);
            Assert.That(view.Query<Label>().ToList().Any(l => l.text == "3-way engagement: Federation vs Cardassian Union vs Dominion"), Is.True);
            Assert.That(view.Query<Label>().ToList().Any(l => l.text.Contains("Free-for-all")), Is.True);
            foreach (var participant in request.Participants)
            {
                var section = view.Q<VisualElement>("fleet-plan-" + participant.FleetId);
                Assert.That(section.Query<DropdownField>().ToList().Count, Is.EqualTo(participant.CivilizationId == "civ.federation" ? 5 : 0));
                Assert.That(section.Query<IntegerField>().ToList().Count, Is.EqualTo(participant.CivilizationId == "civ.federation" ? 1 : 0));
            }
            var own = host.Client.PlayerPlans(host.Snapshot, request).Single();
            own.FleetId = request.Participants.First(p => p.CivilizationId != "civ.federation").FleetId;
            Assert.That(() => host.Resolve(request, new[] { own }), Throws.TypeOf<System.ArgumentException>());
            Assert.That(host.SaveJson(), Is.EqualTo(before));
        }
        [Test]
        public void BufferedBattleReportRendersHullDamageAndPhaseEventsAfterReload()
        {
            var host = ThreePartyCombat(); var request = host.Pending().Single();
            Assert.That(host.Resolve(request).Status, Is.EqualTo(CombatSubmissionStatus.Buffered));
            var saved = host.SaveJson(); host.LoadJson(saved); var view = CombatView(host);
            var report = host.Client.Reports().Single(); var section = view.Q<Foldout>("battle-report-" + request.EncounterId);
            Assert.That(section, Is.Not.Null);
            Assert.That(section.Query<Label>().ToList().Any(l => l.text.Contains("Shield damage: " + report.ShieldDamage) && l.text.Contains("Hull damage: " + report.HullDamage)), Is.True);
            foreach (var ship in report.Fleets.SelectMany(f => f.Ships))
                Assert.That(section.Query<Label>().ToList().Any(l => l.text.Contains(ship.ShipId + ": hull " + ship.StartingHull + " → " + ship.RemainingHull)), Is.True);
            foreach (var phase in report.Phases)
            {
                var events = section.Q<Foldout>("events-" + phase.Phase);
                Assert.That(events, Is.Not.Null); Assert.That(events.value, Is.False);
                foreach (var e in phase.Events) Assert.That(events.Query<Label>().ToList().Any(l => l.text.StartsWith("#" + e.Sequence + " " + e.Kind)), Is.True);
            }
            Assert.That(view.Q<Button>("finalize-combat"), Is.Not.Null);
            Assert.That(host.Snapshot.Campaign.Phase, Is.EqualTo(SessionPhase.AwaitingCombat));
            Assert.That(host.SaveJson(), Is.EqualTo(saved));
        }
        [Test]
        public void BufferedReportsRemainAccessibleWhileOtherEncountersArePending()
        {
            var host = ThreePartyCombat(2); Assert.That(host.Pending().Count, Is.EqualTo(2));
            var first = host.Pending()[0]; Assert.That(host.Resolve(first).Status, Is.EqualTo(CombatSubmissionStatus.Buffered));
            var before = host.SaveJson(); var view = CombatView(host);
            Assert.That(view.Q<Foldout>("battle-report-" + first.EncounterId), Is.Not.Null);
            Assert.That(view.Q<Button>("resolve-defaults"), Is.Not.Null); Assert.That(view.Q<Button>("finalize-combat"), Is.Null);
            Assert.That(host.SaveJson(), Is.EqualTo(before));
            Assert.That(host.Resolve(host.Pending().Single()).Status, Is.EqualTo(CombatSubmissionStatus.Buffered));
            view = CombatView(host);
            Assert.That(view.Query<Foldout>().ToList().Count(f => f.name.StartsWith("battle-report-")), Is.EqualTo(2));
            Assert.That(view.Q<Button>("finalize-combat"), Is.Not.Null);
            Assert.That(host.FinalizeCombats().Status, Is.EqualTo(TurnStatus.Completed));
        }
        [Test]
        public void AiOnlyCheckpointDoesNotRequirePlannerAndRecentReportRendersAfterFinalization()
        {
            var host = ThreePartyCombat(); var save = host.Client.ExportSave();
            foreach (var actor in save.GameState.Civilizations) actor.ControllerKind = ControllerKind.AI;
            host.LoadJson(Newtonsoft.Json.JsonConvert.SerializeObject(save));
            var flow = new CombatOrchestrator(host.Client); flow.Pump();
            Assert.That(flow.Error, Is.Null); Assert.That(flow.RequiresPlayer, Is.False);
            Assert.That(host.Snapshot.Campaign.Phase, Is.EqualTo(SessionPhase.Planning));
            var before = host.SaveJson(); var view = new CombatPanelView();
            view.Present(host.Snapshot, host.Client, new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<FleetCombatPlanDto>>(), operation => operation(), flow);
            Assert.That(view.Query<Label>().ToList().Any(l => l.text == "Recent Combat Reports"), Is.True);
            Assert.That(view.Q<Button>("resolve-defaults"), Is.Null); Assert.That(view.Q<Button>("finalize-combat"), Is.Null);
            Assert.That(view.Query<Foldout>().ToList().Count(f => f.name.StartsWith("battle-report-")), Is.EqualTo(1));
            Assert.That(view.Query<Label>().ToList().Any(l => l.text.StartsWith("Shots:")), Is.True);
            Assert.That(host.SaveJson(), Is.EqualTo(before)); flow.Pump(); Assert.That(host.SaveJson(), Is.EqualTo(before));
            Assert.That(host.Client.RecentReports.Count, Is.EqualTo(1));
        }
        [Test]
        public void PlannerSupportsSeveralPersistedHumansWithoutEditableAiControls()
        {
            var host = ThreePartyCombat(); var save = host.Client.ExportSave();
            save.GameState.Civilizations.Single(c => c.CivilizationId == "civ.cardassian").ControllerKind = ControllerKind.Human;
            host.LoadJson(Newtonsoft.Json.JsonConvert.SerializeObject(save));
            var flow = new CombatOrchestrator(host.Client); flow.Pump(); var before = host.SaveJson();
            var view = new CombatPanelView();
            view.Present(host.Snapshot, host.Client, new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<FleetCombatPlanDto>>(), operation => operation(), flow);
            var request = host.Pending().Single();
            foreach (var p in request.Participants)
            {
                var section = view.Q<VisualElement>("fleet-plan-" + p.FleetId);
                Assert.That(section.Query<DropdownField>().ToList().Count, Is.EqualTo(p.CivilizationId == "civ.dominion" ? 0 : 5));
            }
            Assert.That(host.SaveJson(), Is.EqualTo(before));
        }
        [Test]
        public void HumanReportHidesNextPlannerUntilContinueAndStillAllowsExplicitCompletion()
        {
            var host = ThreePartyCombat(2); var flow = new CombatOrchestrator(host.Client); flow.Pump();
            flow.ResolveHuman(host.Snapshot, host.Pending().First(r => r.EncounterId == flow.PendingHumanEncounter));
            var before = host.SaveJson(); var view = new CombatPanelView();
            var drafts = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<FleetCombatPlanDto>>();
            view.Present(host.Snapshot, host.Client, drafts, operation => operation(), flow);
            Assert.That(view.Q<Button>("resolve-defaults"), Is.Null); Assert.That(view.Q<Button>("continue-combat"), Is.Not.Null);
            Assert.That(view.Query<Foldout>().ToList().Any(f => f.name.StartsWith("battle-report-")), Is.True);
            Assert.That(host.SaveJson(), Is.EqualTo(before));
            flow.Continue(); view.Present(host.Snapshot, host.Client, drafts, operation => operation(), flow);
            Assert.That(view.Q<Button>("resolve-defaults"), Is.Not.Null);
            flow.ResolveHuman(host.Snapshot, host.Pending().Single());
            view.Present(host.Snapshot, host.Client, drafts, operation => operation(), flow);
            Assert.That(view.Q<Button>("finalize-combat"), Is.Not.Null); Assert.That(host.Snapshot.Campaign.Phase, Is.EqualTo(SessionPhase.AwaitingCombat));
            Assert.That(flow.Complete(host.Snapshot).Status, Is.EqualTo(TurnStatus.Completed));
            view.Present(host.Snapshot, host.Client, drafts, operation => operation(), flow);
            Assert.That(view.Query<Foldout>().ToList().Count(f => f.name.StartsWith("battle-report-")), Is.EqualTo(2));
        }
        [Test]
        public void BufferedHumanReportReconstructsOnLoadAndDoesNotResolveAgain()
        {
            var host = ThreePartyCombat(2); var flow = new CombatOrchestrator(host.Client); flow.Pump();
            flow.ResolveHuman(host.Snapshot, host.Pending().First(r => r.EncounterId == flow.PendingHumanEncounter));
            var saved = host.SaveJson(); host.LoadJson(saved); flow.Pump();
            Assert.That(flow.ReportToReview, Is.Not.Null); Assert.That(host.Client.RecentReports.Count, Is.EqualTo(1));
            flow.Pump(); Assert.That(host.SaveJson(), Is.EqualTo(saved)); Assert.That(host.Pending().Count, Is.EqualTo(1));
            var view = new CombatPanelView();
            view.Present(host.Snapshot, host.Client, new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<FleetCombatPlanDto>>(), operation => operation(), flow);
            Assert.That(view.Q<Button>("continue-combat"), Is.Not.Null); Assert.That(host.SaveJson(), Is.EqualTo(saved));
        }
        [Test]
        public void ActualScenarioOrchestrationAutoSkipsAiBattleBeforeHumanInteraction()
        {
            var host = Start(); var flow = new CombatOrchestrator(host.Client); var automatic = false;
            for (var turn = 0; turn < 25; turn++)
            {
                host.EndTurn(); flow.Pump(); Assert.That(flow.Error, Is.Null);
                if (flow.Notice != null && flow.Notice.Contains("AI battles resolved"))
                { automatic = true; Assert.That(flow.RequiresPlayer, Is.False); Assert.That(host.Snapshot.Campaign.Phase, Is.EqualTo(SessionPhase.Planning)); }
                while (host.Snapshot.Campaign.Phase == SessionPhase.AwaitingCombat)
                {
                    if (flow.ReportToReview != null) flow.Continue();
                    else if (flow.PendingHumanEncounter != null) flow.ResolveHuman(host.Snapshot, host.Pending().Single(r => r.EncounterId == flow.PendingHumanEncounter));
                    else Assert.That(flow.Complete(host.Snapshot).Status, Is.EqualTo(TurnStatus.Completed));
                    Assert.That(flow.Error, Is.Null);
                }
            }
            Assert.That(automatic, Is.True); Assert.That(host.Snapshot.Campaign.TurnNumber, Is.EqualTo(26));
            Assert.That(host.Snapshot.Campaign.StateRevision, Is.EqualTo(185));
            Assert.That(host.Client.RecentReports.Count, Is.EqualTo(17));
        }
        [Test]
        public void ProductionOverviewRendersAllOwnedWorldsWithoutCampaignMutation()
        {
            var host = Start(); var before = host.SaveJson();
            var model = new ProductionOverview(host.Client, host.Snapshot);
            var view = new ProductionOverviewView(); view.Present(model, operation => operation());
            Assert.That(model.Worlds.Select(w => w.ColonyId.Value), Is.EqualTo(new[] { "colony.mars", "colony.sol" }));
            foreach (var world in model.Worlds)
            {
                var section = view.Q<Foldout>(world.ColonyId.Value);
                Assert.That(section, Is.Not.Null);
                Assert.That(section.text, Does.Contain(world.Environment.DisplayName));
                Assert.That(section.text, Does.Contain("Industry " + world.Economy.Industry));
                foreach (var option in model.Designs(world.ColonyId))
                {
                    var row = section.Q<VisualElement>(option.Design.DefinitionId.Value);
                    Assert.That(row, Is.Not.Null);
                    Assert.That(row.Q<Button>().enabledSelf, Is.EqualTo(option.CanEnqueue));
                    Assert.That(row.Q<Label>().text, Does.Contain(option.Design.IndustryCost + " Industry"));
                    if (!option.CanEnqueue)
                        Assert.That(row.Query<Label>().ToList().Any(l => l.text.Contains(option.UnavailableReason.ToString())), Is.True);
                }
            }
            Assert.That(host.SaveJson(), Is.EqualTo(before));
        }
        [Test]
        public void ProductionOverviewCommandsUseRealBoundaryAndRenderAuthoritativeQueue()
        {
            var host = Start(); var world = new HobbyDS9.Contracts.EntityId("colony.sol");
            var model = new ProductionOverview(host.Client, host.Snapshot); var revision = host.Snapshot.Campaign.StateRevision;
            Assert.That(model.Enqueue(world, new DefinitionId("ship-design.scout")).Accepted, Is.True);
            Assert.That(host.Snapshot.Campaign.StateRevision, Is.EqualTo(revision + 1));
            var queued = new ProductionOverview(host.Client, host.Snapshot);
            var item = queued.Worlds.Single(w => w.ColonyId == world).ProductionQueue.Single();
            var view = new ProductionOverviewView(); var beforeRender = host.SaveJson();
            view.Present(queued, operation => operation());
            var row = view.Q<VisualElement>(item.QueueItemId.Value);
            Assert.That(row.Q<Label>().text, Does.Contain(item.CompletedIndustry + "/" + item.RequiredIndustry));
            Assert.That(row.Q<Label>().text, Does.Contain("Paid " + item.PaidCredits));
            Assert.That(row.Q<Button>().text, Does.StartWith("Cancel"));
            Assert.That(host.SaveJson(), Is.EqualTo(beforeRender));
            Assert.That(model.Enqueue(world, new DefinitionId("ship-design.scout")).ErrorCode, Is.EqualTo(CommandErrorCode.StaleRevision));
            Assert.That(host.SaveJson(), Is.EqualTo(beforeRender));
            Assert.That(queued.Cancel(world, item.QueueItemId).Accepted, Is.True);
            Assert.That(host.Snapshot.Campaign.StateRevision, Is.EqualTo(revision + 2));
            Assert.That(host.Snapshot.Campaign.Colonies.Single(w => w.ColonyId == world).ProductionQueue, Is.Empty);
        }
        [Test]
        public void ProductionOverviewRemainsUsableWithoutWorldSelection()
        {
            var host = Start(); var selection = new GalaxySelection();
            selection.SelectFleet(new HobbyDS9.Contracts.EntityId("fleet.federation-scout"));
            var model = new ProductionOverview(host.Client, host.Snapshot);
            var view = new ProductionOverviewView(); view.Present(model, operation => operation());
            selection.SelectNode(new NavigationNodeId("nav.bajor"));
            view.Present(model, operation => operation());
            Assert.That(view.Q<Foldout>("colony.sol"), Is.Not.Null);
            Assert.That(view.Q<Foldout>("colony.mars"), Is.Not.Null);
            Assert.That(model.Enqueue(new HobbyDS9.Contracts.EntityId("colony.sol"), new DefinitionId("ship-design.scout")).Accepted, Is.True);
        }
        [Test]
        public void ProjectStartsRealScenarioThroughApplication()
        {
            Assert.That(UnityEngine.Application.unityVersion, Is.EqualTo("6000.6.5f1"));
            Assert.That(Resources.Load<ThemeStyleSheet>("HobbyTheme"), Is.Not.Null);
            Assert.That(Resources.Load<StyleSheet>("HobbyClient"), Is.Not.Null);
            var host = Start(); var snapshot = host.Snapshot;
            Assert.That(snapshot.Campaign.ScenarioId, Is.EqualTo("ds9-strategic-slice-v1"));
            Assert.That(snapshot.Navigation.Nodes.Count, Is.EqualTo(7));
            Assert.That(snapshot.Campaign.Fleets.Count, Is.EqualTo(3));
            Assert.That(typeof(ClientEntry).Assembly.GetReferencedAssemblies().Any(a => a.Name == "HobbyDS9.Simulation"), Is.False);
        }
        [Test]
        public void EmptyNavigationNodeNeverInheritsSystemFromMatchingId()
        {
            var snapshot = Start().Snapshot;
            var node = GalaxyProjection.Nodes(new[] { new NavigationNodeSnapshot(new NavigationNodeId("system.sol"), null) }, snapshot.Campaign, snapshot.Content).Single();
            Assert.That(node.SystemId, Is.Null); Assert.That(node.Fleets, Is.Empty);
        }
        [Test]
        public void HumanOrdersUseRealApplicationAndNewtonsoftSaveRoundTrip()
        {
            var host = Start(); var client = host.Client;
            var actor = new HobbyDS9.Contracts.EntityId("civ.federation");
            var colony = new HobbyDS9.Contracts.EntityId("colony.sol");
            Assert.That(client.AllocateScience(host.Snapshot, actor, DesiredScience(host.Snapshot)).Accepted, Is.True);
            Assert.That(client.Enqueue(host.Snapshot, actor, colony, new DefinitionId("ship-design.scout")).Accepted, Is.True);
            var json = host.SaveJson(); var saved = host.Snapshot;
            var item = saved.Campaign.Colonies.Single(c => c.ColonyId == colony).ProductionQueue.Single();
            Assert.That(client.Cancel(host.Snapshot, actor, colony, item.QueueItemId).Accepted, Is.True);
            host.LoadJson(json);
            Assert.That(host.SaveJson(), Is.EqualTo(json));
            Assert.That(host.Snapshot, Is.Not.SameAs(saved));
            Assert.That(host.SaveSummary(json), Does.Contain("Turn 1"));
            Assert.That(host.Snapshot.Campaign.Colonies.Single(c => c.ColonyId == colony).ProductionQueue.Count, Is.EqualTo(1));
        }
        [Test]
        public void FailedJsonAndIncompatibleLoadPreserveLiveHost()
        {
            var host = Start(); var snapshot = host.Snapshot; var json = host.SaveJson();
            Assert.That(() => host.LoadJson("{bad-json"), Throws.Exception);
            Assert.That(() => host.LoadJson(json.Replace("ds9-strategic-slice-v1", "different-scenario")), Throws.Exception);
            Assert.That(host.Snapshot, Is.SameAs(snapshot)); Assert.That(host.SaveJson(), Is.EqualTo(json));
            Assert.That(host.EndTurn().Status, Is.Not.EqualTo(TurnStatus.Rejected));
        }
        [Test]
        public void FleetPreviewStaysLocalUntilConfirmation()
        {
            var host = Start(); var before = host.SaveJson();
            var preview = host.Client.Preview(host.Snapshot, new HobbyDS9.Contracts.EntityId("fleet.federation-scout"), new NavigationNodeId("nav.bajor"));
            Assert.That(preview.Route.TraversalCost, Is.EqualTo(7)); Assert.That(host.SaveJson(), Is.EqualTo(before));
            var map = new GalaxyMap();
            map.Present(host.Snapshot, new GalaxySelection(), _ => { }, _ => { }); map.SetRoute(preview);
            Assert.That(map.Query<Button>().ToList().Count, Is.EqualTo(10));
            Assert.That(host.SaveJson(), Is.EqualTo(before));
            Assert.That(host.Client.SubmitRoute(preview).Accepted, Is.True);
            Assert.That(host.Snapshot.Campaign.Fleets.Single(f => f.FleetId.Value == "fleet.federation-scout").RouteNodeIds.Select(n => n.Value), Is.EqualTo(new[] { "nav.bajor" }));
            Assert.That(host.SaveJson(), Is.Not.EqualTo(before));
        }
        [Test]
        public void SlotOverwriteUsesTheRealHostSerializer()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hobby-unity-save-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var host = Start(); var store = new SaveSlotStore(directory);
                store.Write(1, host.SaveJson(), false); host.EndTurn(); var changed = host.SaveJson();
                Assert.That(() => store.Write(1, changed, false), Throws.TypeOf<System.IO.IOException>());
                store.Write(1, changed, true); host.EndTurn(); host.LoadJson(store.Read(1));
                Assert.That(host.SaveJson(), Is.EqualTo(changed));
            }
            finally { if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true); }
        }
        [Test]
        public void NewtonsoftRoundTripsPendingAndBufferedCombatCheckpoints()
        {
            var host = Start();
            for (var turn = 0; turn < 10 && host.Snapshot.Campaign.Phase == SessionPhase.Planning; turn++) host.EndTurn();
            Assert.That(host.Snapshot.Campaign.Phase, Is.EqualTo(SessionPhase.AwaitingCombat));
            var pending = host.SaveJson(); host.LoadJson(pending); Assert.That(host.SaveJson(), Is.EqualTo(pending));
            foreach (var request in host.Pending()) Assert.That(host.Resolve(request).Status, Is.EqualTo(CombatSubmissionStatus.Buffered));
            var buffered = host.SaveJson(); host.LoadJson(buffered);
            Assert.That(host.Pending(), Is.Empty); Assert.That(host.SaveJson(), Is.EqualTo(buffered));
            Assert.That(host.FinalizeCombats().Status, Is.EqualTo(TurnStatus.Completed));
        }
        [TestCase(false)] [TestCase(true)]
        public void ClientHostCompletesTwentyFiveTurnsAndCombatCheckpoints(bool reverse)
        {
            var host = Start(); var encounters = 0;
            for (var i = 0; i < 25; i++)
            {
                var result = host.EndTurn(); Assert.That(result.Status, Is.Not.EqualTo(TurnStatus.Rejected));
                if (result.Status == TurnStatus.AwaitingCombat)
                {
                    Assert.That(host.EndTurn().ErrorCode, Is.EqualTo(TurnErrorCode.CombatPending));
                    var pending = host.Pending();
                    foreach (var request in reverse ? pending.Reverse() : pending)
                    { Assert.That(host.Resolve(request).Status, Is.EqualTo(CombatSubmissionStatus.Buffered)); encounters++; }
                    Assert.That(host.Snapshot.Campaign.Phase, Is.EqualTo(SessionPhase.AwaitingCombat));
                    Assert.That(host.FinalizeCombats().Status, Is.EqualTo(TurnStatus.Completed));
                }
            }
            var state = host.Snapshot.Campaign;
            Assert.That(encounters, Is.EqualTo(17)); Assert.That(state.TurnNumber, Is.EqualTo(26));
            Assert.That(state.StateRevision, Is.EqualTo(185)); Assert.That(state.Ships.Count, Is.EqualTo(9));
            Assert.That(state.NextEntitySequence, Is.EqualTo(97UL)); Assert.That(state.RngState.State, Is.EqualTo(0xd8919406d6e01cf1UL));
        }

        [Test]
        public void MixedWorldSnapshotsAndNamedResourceCostsReachTheClient()
        {
            var host=Start();var snapshot=host.Snapshot;
            var worlds=snapshot.Campaign.Colonies.Where(c=>c.SystemId.Value=="system.sol").ToList();
            Assert.That(worlds.Count,Is.EqualTo(2));
            var earth=worlds.Single(c=>c.PlanetId.Value=="planet.sol");
            Assert.That(earth.State.Cohorts.Count,Is.EqualTo(3));
            Assert.That(earth.Environment.Habitats.Sum(h=>h.ShareBps),Is.EqualTo(10000));
            Assert.That(earth.Economy.Industry,Is.GreaterThan(0));
            Assert.That(snapshot.Campaign.ShipDesigns.All(d=>d.ResourceCosts.Any(r=>r.ResourceDefinitionId=="resource.dilithium")),Is.True);
            var json=host.SaveJson();host.EndTurn();host.LoadJson(json);Assert.That(host.SaveJson(),Is.EqualTo(json));
        }
        [TestCase("Stability")]
        [TestCase("Prosperity")]
        [TestCase("AcceptsRefugees")]
        public void MissingAuthoritativeScalarRejectsLoadWithoutReplacingSession(string field)
        {
            var host=Start();var before=host.SaveJson();var json=Newtonsoft.Json.Linq.JObject.Parse(before);
            ((Newtonsoft.Json.Linq.JObject)json["GameState"]["Colonies"][0]).Remove(field);
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(()=>host.LoadJson(json.ToString()));
            Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [Test]
        public void ObsoleteMaterialsFieldRejectsLoadWithoutReplacingSession()
        {
            var host=Start();var before=host.SaveJson();var json=Newtonsoft.Json.Linq.JObject.Parse(before);
            json["GameState"]["Colonies"][0]["Materials"]=99;
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(()=>host.LoadJson(json.ToString()));
            Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [Test]
        public void GovernanceCommandRefreshesDetachedPoliticsAndPreservesStaleRejection()
        {
            var host=Start();var before=host.Snapshot;
            var result=host.Client.Governance(before,new HobbyDS9.Contracts.EntityId("civ.federation"),new HobbyDS9.Contracts.EntityId("colony.sol"),new DefinitionId("governance.autonomy"));
            Assert.That(result.Accepted,Is.True);
            Assert.That(before.Campaign.Colonies.Single(c=>c.ColonyId.Value=="colony.sol").State.Politics.GovernancePolicyDefinitionId,Is.EqualTo("governance.integration"));
            var saved=host.SaveJson();
            Assert.That(host.Client.Governance(before,new HobbyDS9.Contracts.EntityId("civ.federation"),new HobbyDS9.Contracts.EntityId("colony.sol"),new DefinitionId("governance.security")).ErrorCode,Is.EqualTo(CommandErrorCode.StaleRevision));
            Assert.That(host.SaveJson(),Is.EqualTo(saved));
            host.EndTurn();Assert.That(host.Snapshot.Campaign.Colonies.Single(c=>c.ColonyId.Value=="colony.sol").State.Politics.CohortReports,Is.Not.Empty);
        }
        [Test]
        public void WeightedPreviewAndPartialTravelRoundTripThroughRealHost()
        {
            var host=Start();var before=host.SaveJson();var preview=host.Client.Preview(host.Snapshot,new HobbyDS9.Contracts.EntityId("fleet.federation-scout"),new NavigationNodeId("nav.bajor"));
            Assert.That(preview.Route.TraversalCost,Is.EqualTo(7));Assert.That(preview.Travel.EstimatedTurns,Is.EqualTo(3));Assert.That(host.SaveJson(),Is.EqualTo(before));
            Assert.That(host.Client.SubmitRoute(preview).Accepted,Is.True);host.EndTurn();var fleet=host.Snapshot.Campaign.Fleets.Single(f=>f.FleetId.Value=="fleet.federation-scout");
            Assert.That(fleet.TransitProgress,Is.EqualTo(3));Assert.That(fleet.TransitTotalCost,Is.EqualTo(7));
            var saved=host.SaveJson();host.LoadJson(saved);Assert.That(host.SaveJson(),Is.EqualTo(saved));
        }
        [Test]
        public void IndependentEmptyHazardIsPresentedWithoutInferringSystem()
        {
            var snapshot=Start().Snapshot;var node=snapshot.Navigation.Nodes.Single(n=>n.NodeId.Value=="nav.badlands");
            Assert.That(node.AssociatedSystemId,Is.Null);Assert.That(node.PhenomenonDefinitionIds,Does.Contain("phenomenon.plasma-storm"));
            var sol=snapshot.Navigation.Nodes.Single(n=>n.NodeId.Value=="nav.sol");Assert.That(sol.AssociatedSystemId.Value.Value,Is.EqualTo("system.sol"));
            Assert.That(GalaxyProjection.Nodes(snapshot).Single(n=>n.NodeId==node.NodeId).Label,Is.EqualTo("Badlands transit"));
        }
        [Test]
        public void UnknownConnectionIsExcludedFromMapAndRoutePreviewAfterReload()
        {
            var host=Start();var json=Newtonsoft.Json.Linq.JObject.Parse(host.SaveJson());
            var knowledge=json["GameState"]["NavigationKnowledge"].Single(k=>(string)k["CivilizationId"]=="civ.federation");
            knowledge["Connections"].Single(e=>(string)e["Id"]=="connection.sol-bajor")["Level"]=0;
            host.LoadJson(json.ToString());var preview=host.Client.Preview(host.Snapshot,new HobbyDS9.Contracts.EntityId("fleet.federation-scout"),new NavigationNodeId("nav.bajor"));
            Assert.That(preview.Route.Success,Is.False);Assert.That(host.Snapshot.Navigation.Links.Any(e=>e.EndpointA.Value=="nav.sol"||e.EndpointB.Value=="nav.sol"),Is.False);
        }
    }
}
