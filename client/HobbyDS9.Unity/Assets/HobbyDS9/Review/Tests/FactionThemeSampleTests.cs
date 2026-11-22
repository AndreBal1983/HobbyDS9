#nullable enable
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    public sealed class FactionThemeSampleTests
    {
        private static FactionThemeTokens Tokens()
        {
            for(var dir=new DirectoryInfo(UnityEngine.Application.dataPath);dir!=null;dir=dir.Parent)
            {
                var path=Path.Combine(dir.FullName,"art","faction-style-v1","theme-tokens.json");if(!File.Exists(path))continue;
                var audit=File.ReadAllText(Path.Combine(dir.FullName,"art","faction-style-v1","catalogue-audit.json"));
                Assert.That(FactionThemeTokens.TryParse(File.ReadAllText(path),audit,out var tokens,out var error),Is.True,error);return tokens!;
            }
            Assert.Fail("Actual frozen Art theme JSON is required.");return null!;
        }
        [TestCase("federation")][TestCase("klingon")][TestCase("cardassian")][TestCase("romulan")][TestCase("dominion")][TestCase("ilex")][TestCase("namar")][TestCase("neutral")]
        public void ReferenceSampleKeepsSharedStateMeaningTargetsAndProposalLabel(string profile)
        {
            var tokens=Tokens();var sample=FactionThemeSample.ForProfilePreview(tokens,profile);
            Assert.That(sample.Selection.IsProfilePreview,Is.True);Assert.That(sample.Selection.Profile.Id,Is.EqualTo(profile));Assert.That(sample.styleSheets.count,Is.Zero);
            Assert.That(sample.Q<Label>("faction-theme-proposal-label").text,Is.EqualTo("PROPOSED REFERENCE SAMPLE"));
            Assert.That(sample.Q<Label>("faction-theme-artifact-status").text,Is.EqualTo(FactionThemeTokens.ProposedStatus));
            Assert.That(sample.Q("faction-theme-family-chrome").tooltip,Does.Contain(sample.Selection.Profile.Family.Frame).And.Contain(sample.Selection.Profile.Family.SectionMotif));
            foreach(var state in tokens.States)
            {
                var label=sample.Q<Label>("faction-theme-label-"+state.Key);Assert.That(label.text,Is.EqualTo(state.Value.Label));
                ColorUtility.TryParseHtmlString(state.Value.Color,out var expected);Assert.That(label.style.color.value,Is.EqualTo(expected));
                Assert.That(sample.Q("faction-theme-state-"+state.Key).tooltip,Does.Contain(state.Value.Shape));
            }
            Assert.That(sample.Query<Button>().ToList().All(b=>b.style.minHeight.value.value>=44),Is.True);
            Assert.That(sample.ReducedMotion,Is.True);Assert.That(sample.TransitionMilliseconds,Is.Zero);
        }
        [Test]public void LawfulBindingAndManualPreviewStaySeparateAndDoNotStyleTheParent()
        {
            var tokens=Tokens();var parent=new VisualElement();parent.style.backgroundColor=Color.magenta;
            var hidden=FactionThemeSample.ForIdentity(tokens,new FactionThemeIdentity(FactionThemeIdentityKind.Civilization,"civilization.dominion",false));
            parent.Add(hidden);Assert.That(hidden.Selection.Profile.Id,Is.EqualTo("neutral"));Assert.That(hidden.Selection.IsProfilePreview,Is.False);
            var preview=FactionThemeSample.ForProfilePreview(tokens,"romulan",false);parent.Add(preview);
            Assert.That(preview.Selection.Profile.Id,Is.EqualTo("romulan"));Assert.That(preview.Selection.IsProfilePreview,Is.True);Assert.That(preview.TransitionMilliseconds,Is.EqualTo(tokens.TransitionMs));
            Assert.That(parent.style.backgroundColor.value,Is.EqualTo(Color.magenta));Assert.That(parent.styleSheets.count,Is.Zero);
        }
    }
}
