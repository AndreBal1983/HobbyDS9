#nullable enable
using System;
using System.Collections;
using System.Linq;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    // Shared by existing real-session captures; no commands, synthetic gameplay or added test bootstrap.
    internal static class DemoCombatOrderPaneChecks
    {
        internal static IEnumerator Check(VisualElement root)
        {
            var dock=root.Q<ScrollView>("central-order-dock");
            var combat=root.Q(className:"demo-combat");
            if(dock==null||root.Q("review-one-use-confirmation")!=null||combat==null||!combat.Contains(dock))yield break;
            var original=dock.scrollOffset;var viewport=dock.contentViewport;
            void Horizontal(VisualElement e)
            {
                Assert.That(e.worldBound.xMin,Is.GreaterThanOrEqualTo(viewport.worldBound.xMin-1),e.name+" left inside order viewport");
                Assert.That(e.worldBound.xMax,Is.LessThanOrEqualTo(viewport.worldBound.xMax+1),e.name+" right inside order viewport");
            }
            void TextFits(TextElement text)
            {
                if(string.IsNullOrEmpty(text.text)||text.resolvedStyle.display==DisplayStyle.None||text.worldBound.width<=0)return;
                var width=text.contentRect.width;Assert.That(width,Is.GreaterThan(0),text.text);
                var unwrapped=text.MeasureTextSize(text.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined);
                if(unwrapped.x>width+1)Assert.That(text.resolvedStyle.whiteSpace,Is.EqualTo(WhiteSpace.Normal),text.text+" must wrap");
                var required=text.MeasureTextSize(text.text,width,VisualElement.MeasureMode.Exactly,0,VisualElement.MeasureMode.Undefined);
                Assert.That(text.contentRect.height+1,Is.GreaterThanOrEqualTo(required.y),text.text+" complete caption height");
            }
            void PaneFits()
            {
                Assert.That(viewport.worldBound.width,Is.GreaterThan(0));Horizontal(dock.contentContainer);
                Assert.That(dock.horizontalScroller.highValue,Is.LessThanOrEqualTo(1),"Order pane must not have horizontal overflow, even with its bar hidden");
                foreach(var element in dock.contentContainer.Query<VisualElement>().ToList().Where(e=>e is Button||e is DropdownField||e.ClassListContains("review-field-row")))Horizontal(element);
                foreach(var text in dock.contentContainer.Query<TextElement>().ToList()){if(text.worldBound.width>0)Horizontal(text);TextFits(text);}
            }
            PaneFits();
            // Disabled legal choices must remain readable/reachable too. Test the exact viewport, not the whole window.
            var controls=dock.contentContainer.Query<VisualElement>().ToList().Where(e=>e is Button||e is DropdownField).ToList();
            foreach(var control in controls)
            {
                dock.ScrollTo(control);yield return null;yield return null;PaneFits();
                Assert.That(control.worldBound.yMin,Is.GreaterThanOrEqualTo(viewport.worldBound.yMin-1),control.name+" reachable top");
                Assert.That(control.worldBound.yMax,Is.LessThanOrEqualTo(viewport.worldBound.yMax+1),control.name+" reachable bottom");
                if(control is DropdownField field)
                {
                    var value=field.Q<TextElement>(className:"unity-base-popup-field__text");Assert.That(value,Is.Not.Null);
                    var selected=field.value;
                    foreach(var caption in field.choices)
                    {
                        field.SetValueWithoutNotify(caption);yield return null;yield return null;dock.ScrollTo(field);yield return null;
                        PaneFits();TextFits(value);Assert.That(value.text,Is.EqualTo(caption),"Full selected dropdown caption");
                        Assert.That(field.worldBound.yMax,Is.LessThanOrEqualTo(viewport.worldBound.yMax+1),"Wrapped option vertically reachable");
                    }
                    field.SetValueWithoutNotify(selected);yield return null;yield return null;
                }
            }
            var draft=root.Q<Button>("combat-draft-travel");
            if(draft!=null)
            {
                foreach(var intent in new[]{CampaignTravelIntent.Stay,CampaignTravelIntent.StayEngage,CampaignTravelIntent.Leave,CampaignTravelIntent.Follow})
                    Assert.That(dock.Q<Button>("combat-travel-choice-"+intent),Is.Not.Null,"Every legal planning intent remains in the pane");
                Assert.That(draft.text,Is.EqualTo("Draft selected next-turn order"));
                var footer=root.Q("central-combat-footer");Assert.That(footer.Contains(draft),Is.True,"Draft action stays discoverable beside review");
                foreach(var action in footer.Query<Button>().ToList())
                {
                    Assert.That(action.worldBound.xMin,Is.GreaterThanOrEqualTo(footer.worldBound.xMin));Assert.That(action.worldBound.xMax,Is.LessThanOrEqualTo(footer.worldBound.xMax));
                    Assert.That(action.worldBound.yMin,Is.GreaterThanOrEqualTo(footer.worldBound.yMin));Assert.That(action.worldBound.yMax,Is.LessThanOrEqualTo(footer.worldBound.yMax));TextFits(action);
                }
            }
            dock.scrollOffset=original;yield return null;yield return null;PaneFits();
        }
    }
}
