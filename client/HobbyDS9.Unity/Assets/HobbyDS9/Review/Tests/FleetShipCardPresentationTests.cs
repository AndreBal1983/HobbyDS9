#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    // EditMode hierarchy tests. No PlayMode bootstrap, private saves or player input.
    public sealed class FleetShipCardPresentationTests
    {
        private ReviewUiModel model=null!;
        private ReviewWorkspace workspace=null!;
        private VisualElement root=null!;
        private UnityEditor.EditorWindow window=null!;
        private VisualElement retainedControls=null!;
        private readonly Dictionary<Button,Action> callbacks=new Dictionary<Button,Action>();
        [SetUp] public void Setup()
        {
            byte[] Bytes(string name)=>Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+name).bytes;
            var codec=new ReviewNewtonsoftCodec();
            var package=ReviewExactPackageLoader.LoadPlayableFleet(Bytes("playable"),Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
            // Only a test-owned unused storage path; these tests never Save/Load/create files.
            model=new ReviewUiModel(package,codec,new ReviewPlayableFleetSlotStore(Path.Combine(Path.GetTempPath(),"HobbyDS9-FleetCards-"+Guid.NewGuid().ToString("N")),codec));
            workspace=new ReviewWorkspace(new ReviewCombatFlow(model));root=new VisualElement();
            try
            {
                // An owned EditMode panel; no PlayMode or ReviewEntry bootstrap.
                // Field.value assignments require a panel to dispatch ChangeEvents.
                window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
                window.titleContent=new GUIContent("Fleet card event test");
                window.Show();window.rootVisualElement.Add(root);
                retainedControls=new VisualElement();window.rootVisualElement.Add(retainedControls);
                Assert.That(root.panel,Is.Not.Null,"Test changes require an actual panel dispatcher");
            }
            catch { CleanupPanel();throw; }
        }
        [TearDown] public void CleanupPanel()
        {
            try
            {
                if(root!=null)root.Clear();
                if(retainedControls!=null)retainedControls.Clear();
                callbacks.Clear();
                if(model!=null&&!model.Closed)model.Close(true); // local test model close, no campaign command/save
            }
            finally
            {
                try {if(window!=null)window.Close();}
                finally
                {
                    if(window!=null)UnityEngine.Object.DestroyImmediate(window);
                    window=null!;model=null!;workspace=null!;root=null!;retainedControls=null!;
                }
            }
        }
        private void Draw(){root.Clear();callbacks.Clear();var presenter=new ReviewFleetRoster(model,workspace,(parent,text,action)=>{var b=new Button{ text=text };parent.Add(b);callbacks[b]=action;return b;},action=>action(),(_,__)=>Assert.Fail("No command review expected"),_=>{});presenter.Render(root);}
        private void Activate(string id){var b=root.Q<Button>(id);Assert.That(b,Is.Not.Null,id);Assert.That(b.enabledSelf,Is.True);callbacks[b]();}
        [Test] public void SelectionBuildsRightCardsFromActualConditionWithoutChangingCampaign()
        {
            var frame=model.Current.Frame;var fleet=model.NamedFleets().First();Draw();Activate("fleet-roster-"+fleet.Fleet.FleetId.Value);Draw();
            Assert.That(root.ClassListContains("fleet-card-workspace"),Is.False,"Shared navigation host must not retain Fleet styling");
            var aside=root.Q<ScrollView>(className:"fleet-roster-aside");Assert.That(aside.ClassListContains("fleet-card-workspace"),Is.True);var summary=aside.Q("fleet-navigation-summary");var cards=aside.Q("fleet-selected-ship-cards");
            Assert.That(summary,Is.Not.Null);Assert.That(aside.IndexOf(summary),Is.LessThan(aside.IndexOf(cards)));
            foreach(var ship in frame.Strategy!.Ships.Where(s=>s.FleetId==fleet.Fleet.FleetId))
            {
                var id=ship.ShipId.Value;var card=cards.Q("fleet-ship-card-"+id);Assert.That(card,Is.Not.Null);
                var own=frame.OwnShips.Single(s=>s.Condition.ShipId==id);
                foreach(var i in Enumerable.Range(0,4))
                {
                    var meter=card.Q("fleet-shield-"+id+"-"+i);Assert.That(meter,Is.Not.Null);
                    Assert.That(meter.Q<Label>().text,Does.Contain(own.Condition.Shields[i]+" / "+own.ShieldMaximum[i]));
                    Assert.That(meter.Q<ProgressBar>().value,Is.InRange(0f,100f));
                }
                Assert.That(card.Q<Label>(className:"fleet-ship-title").text,Does.StartWith("Class / fit: "));
                Assert.That(card.Query<Label>().ToList().Any(l=>l.text=="Ship ID: "+id),Is.True);
                Assert.That(card.Q<Button>("fleet-inspect-"+id),Is.Not.Null);
            }
            Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.OrganizationShips,Is.Empty);
        }
        [Test] public void MemberSelectionAndFoldoutRedrawRetainUnsentDraftWithoutOrders()
        {
            var frame=model.Current.Frame;var fleet=model.NamedFleets().First(f=>f.CanOrganize);workspace.SelectedFleet=fleet.Fleet.FleetId.Value;
            model.OrganizationName=new string('N',180);Draw();
            var toggle=root.Query<Toggle>().ToList().First(t=>t.name.StartsWith("fleet-member-",StringComparison.Ordinal));
            Assert.That(toggle.panel,Is.SameAs(root.panel));Assert.That(toggle.enabledInHierarchy,Is.True);
            var memberEvents=0;toggle.RegisterValueChangedCallback(_=>memberEvents++);toggle.value=true;
            Assert.That(memberEvents,Is.GreaterThan(0),"Real field setter must dispatch before draft assertions");
            var member=toggle.name.Substring("fleet-member-".Length);Assert.That(model.OrganizationShips.Contains(member),Is.True);
            Assert.That(workspace.FleetOrganizationExpanded,Is.True,"Selecting a member opens organization details");
            var fold=root.Q<Foldout>("fleet-organization-foldout");fold.value=true;Draw();Assert.That(root.Q<Foldout>(fold.name).value,Is.True,"New presenter uses workspace state");
            workspace.Navigate(ReviewWorkspacePage.Map);workspace.Navigate(ReviewWorkspacePage.FleetOrders);Draw();
            Assert.That(root.Q<Foldout>(fold.name).value,Is.True);
            workspace.SelectedFleet=null;Draw();Assert.That(root.Q("fleet-selected-ship-cards"),Is.Null);
            workspace.SelectedFleet=fleet.Fleet.FleetId.Value;Draw();
            Assert.That(root.Q<Toggle>("fleet-member-"+member).value,Is.True);Assert.That(root.Q<TextField>("fleet-organization-name").value,Is.EqualTo(new string('N',180)));
            Assert.That(model.Current.Frame,Is.SameAs(frame));
        }
        [Test] public void RetainedFoldoutCannotWriteAfterRefreshOrRenderReplacement()
        {
            workspace.SelectedFleet=model.NamedFleets().First(f=>f.CanOrganize).Fleet.FleetId.Value;
            Draw();var old=root.Q<Foldout>("fleet-organization-foldout");var before=model.Current.Frame;
            var oldMember=root.Query<Toggle>().ToList().First(t=>t.name.StartsWith("fleet-member-",StringComparison.Ordinal));
            var memberId=oldMember.name.Substring("fleet-member-".Length);
            var oldMemberEvents=0;var oldFoldEvents=0;
            oldMember.RegisterValueChangedCallback(_=>oldMemberEvents++);
            old.RegisterValueChangedCallback(_=>oldFoldEvents++);
            Assert.That(old.panel,Is.SameAs(root.panel));Assert.That(oldMember.panel,Is.SameAs(root.panel));
            Assert.That(workspace.FleetOrganizationExpanded,Is.False);
            Assert.That(root.Contains(old),Is.True,"This is still the contained render; the frame guard must reject it");
            Assert.That(model.Refresh(),Is.True);Assert.That(model.Current.Frame,Is.Not.SameAs(before));
            old.value=true;oldMember.value=true;
            Assert.That(oldFoldEvents,Is.GreaterThan(0));Assert.That(oldMemberEvents,Is.GreaterThan(0),"Stale guard tested with an actually delivered event");
            Assert.That(model.OrganizationShips.Contains(memberId),Is.False,"Still-contained stale member cannot change draft");
            Assert.That(workspace.FleetOrganizationExpanded,Is.False,"Retained old control cannot write after frame refresh");
            Draw();Assert.That(root.Contains(old),Is.False,"New presenter replaces old hierarchy");
            Assert.That(old.panel,Is.Null,"Old render really detached");
            // Retarget old controls to a sibling test host solely to deliver real events.
            // They are NOT in the current render, even though their dispatcher is live.
            retainedControls.Add(old);retainedControls.Add(oldMember);
            Assert.That(root.Contains(old),Is.False);Assert.That(root.Contains(oldMember),Is.False);
            Assert.That(oldMember.panel,Is.SameAs(root.panel));
            var previousMemberEvents=oldMemberEvents;var previousFoldEvents=oldFoldEvents;
            old.value=false;old.value=true;oldMember.value=false;oldMember.value=true;
            Assert.That(oldMemberEvents,Is.GreaterThan(previousMemberEvents));Assert.That(oldFoldEvents,Is.GreaterThan(previousFoldEvents));
            Assert.That(model.OrganizationShips.Contains(memberId),Is.False,"Old member outside current render cannot change draft");
            Assert.That(workspace.FleetOrganizationExpanded,Is.False,"Old control outside current render cannot write workspace state");
            var refreshed=model.Current.Frame;
            var currentMember=root.Q<Toggle>("fleet-member-"+memberId);
            Assert.That(currentMember.panel,Is.SameAs(root.panel));Assert.That(currentMember.enabledInHierarchy,Is.True);
            currentMember.value=true;
            Assert.That(model.OrganizationShips.Contains(memberId),Is.True,"Current member still updates local draft");
            var current=root.Q<Foldout>("fleet-organization-foldout");current.value=true;
            Assert.That(workspace.FleetOrganizationExpanded,Is.True,"Current attached hierarchy updates the local workspace flag");
            Draw();Assert.That(root.Q<Foldout>("fleet-organization-foldout").value,Is.True);
            Assert.That(model.Current.Frame,Is.SameAs(refreshed),"Local controls submit no authoritative command");
        }
        [Test] public void MissingActivityArtIsExplicitAndInspectDoesNotSelectTransferMembers()
        {
            var frame=model.Current.Frame;var fleet=model.NamedFleets().First();workspace.SelectedFleet=fleet.Fleet.FleetId.Value;Draw();
            var first=frame.Strategy!.Ships.First(s=>s.FleetId==fleet.Fleet.FleetId).ShipId.Value;
            Assert.That(root.Query<Label>().ToList().Any(l=>l.text=="Exact hull image unavailable."),Is.True,"Legacy fleet fixture has no Activity projection");
            Activate("fleet-inspect-"+first);Assert.That(workspace.SelectedOwnShip,Is.EqualTo(first));
            Assert.That(model.OrganizationShips,Is.Empty);Assert.That(model.Current.Frame,Is.SameAs(frame));
        }
    }
}
