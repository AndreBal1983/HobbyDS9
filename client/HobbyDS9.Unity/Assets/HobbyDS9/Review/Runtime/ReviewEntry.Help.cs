#nullable enable
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private void DrawPlayerHelp()
        {
            var page=PagePanel("Help & credits");page.AddToClassList("player-help");
            var groups=new VisualElement();groups.AddToClassList("settings-groups");page.Add(groups);
            VisualElement Group(string title,string detail)
            {
                var group=new VisualElement();group.AddToClassList("settings-group");groups.Add(group);
                var heading=Readout(title);heading.AddToClassList("console-section");group.Add(heading);group.Add(Readout(detail));return group;
            }
            var navigation=Group("Explore & inspect","Select a system or body to inspect it. Space focuses a planet; double-click or Enter opens it. On the map, review a route before submitting it. Travel advances on End turn.");
            AddButton(navigation,"Open map",()=>Navigate(ReviewWorkspacePage.Map));
            var development=Group("Research & construction","One Engineering project can be active. Switching or pausing retains progress. A new version does not refit ships automatically. Construction uses the current campaign's actual eligibility, costs and local materials.");
            AddButton(development,"Open research",()=>Navigate(ReviewWorkspacePage.Research));
            AddButton(development,"Open construction",()=>Navigate(ReviewWorkspacePage.Production));
            Group("Battle decisions","You command your own fleets. Review and commit one joint round at a time, then confirm the complete aftermath. Inspection, static ship previews and resolved-event playback do not change orders or stocks. Enemy orders and private ammunition remain hidden.");
            Group("Saving & separate campaigns","Save keeps accepted state. Unsubmitted orders and allocation edits remain local. Each campaign edition has separate compatible slots; Combat playtest uses its own archive and Continue. A failed load keeps your current campaign.");
            var credits=new Foldout{text="Visual & audio credits",value=false};page.Add(credits);
            credits.Add(Readout("Earth/Mars surface maps: Solar System Scope · CC BY 4.0. Other planetary terrain is decorative. Ship previews use the received project asset catalogue; a preview does not supply gameplay statistics."));
            credits.Add(Readout("Music and console sounds are controlled separately in Audio. Track selection and the current playback state are shown there."));
            AddButton(credits,"Open Audio",()=>Navigate(ReviewWorkspacePage.Audio));
            var diagnostics=new Foldout{text="View diagnostics",value=false};page.Add(diagnostics);
            diagnostics.Add(Readout(workspace!.BoundaryIdentity));diagnostics.Add(Readout(model!.Message));
            diagnostics.Add(Readout("The computer owns its decisions. This player view does not expose opponent actor switching."));
            if(model.Current.Frame.Strategy!=null)
                foreach(var receipt in model.Current.Frame.Strategy.Research.Receipts)
                    diagnostics.Add(Readout("Research record: "+receipt.Category+" · internal value "+receipt.Amount));
            AddButton(diagnostics,"Refresh current view",()=>Do(()=>model!.Refresh()));
        }
    }
}
