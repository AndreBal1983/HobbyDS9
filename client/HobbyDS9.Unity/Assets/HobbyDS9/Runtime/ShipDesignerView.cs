#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    // Persistent local editor. All budgets/capabilities/validation are provided by
    // IShipDesignPreviewProvider; this class only renders and edits installation lists.
    public sealed class ShipDesignerView : VisualElement, IDisposable
    {
        public ShipDesignDraft Draft { get; }
        public Button ResetButton { get; }
        public Button CloseButton { get; }
        private readonly VisualElement columns, confirmation = new VisualElement(), installed = new VisualElement(), summary = new VisualElement(), comparison = new VisualElement(), groups = new VisualElement();
        private readonly Label state = new LcarsReadout(), notice = new LcarsReadout(), description = new LcarsReadout(), facts = new LcarsReadout(), catalogueIssues = new LcarsReadout(), capacity = new LcarsReadout();
        private readonly TextField className;
        private readonly DropdownField templates, roles, catalogue, hull;
        private readonly Label hullFacts = new LcarsReadout();
        private readonly Button replace;
        private readonly List<Button> profileButtons = new List<Button>();
        private readonly Action close;
        private string? selectedInstalled;
        private bool compare;
        private readonly ShipSchematic schematic = new ShipSchematic();
        public ShipDesignerView(ShipDesignDraft draft, Action closeRequested)
        {
            Draft = draft; close = closeRequested; name = "ship-designer"; AddToClassList("ship-designer");
            var banner = new LcarsReadout(draft.Provider.StatusLabel) { name = "designer-provider-status" }; banner.AddToClassList("sd-demo-banner"); Add(banner);
            var actions = new VisualElement(); actions.AddToClassList("sd-actions"); Add(actions);
            state.name = "designer-state"; state.AddToClassList("sd-state"); actions.Add(state);
            var compareButton = new LcarsPillButton("Compare start", () => { compare = !compare; RefreshPresentation(); }) { name = "designer-compare" }; actions.Add(compareButton);
            ResetButton = new LcarsPillButton("Reset edits", () => Draft.RequestReset()) { name = "designer-reset" }; actions.Add(ResetButton);
            CloseButton = new LcarsPillButton("Return to map", () => RequestLeave(close)) { name = "designer-close" }; actions.Add(CloseButton);
            confirmation.name = "designer-confirmation"; confirmation.AddToClassList("confirmation"); Add(confirmation);
            confirmation.Add(new LcarsReadout("Discard this modified local draft? Campaign state is unaffected."));
            var confirmationActions = new VisualElement(); confirmationActions.AddToClassList("sd-buttons"); confirmation.Add(confirmationActions);
            confirmationActions.Add(new LcarsPillButton("Stay / keep edits", Draft.CancelDiscard) { name = "designer-keep" });
            confirmationActions.Add(new LcarsPillButton("Discard / continue", Draft.ConfirmDiscard) { name = "designer-discard" });

            var body = Scroll("designer-body"); body.AddToClassList("sd-body"); Add(body);
            body.contentContainer.style.flexGrow = 1;
            columns = new VisualElement(); columns.AddToClassList("sd-columns"); body.Add(columns);
            var left = Scroll("designer-class"); var leftFrame = Frame("01 / CLASS DRAFT",left,"sd-class-frame"); columns.Add(leftFrame);
            className = new TextField("Ship class name") { name = "designer-name" }; left.Add(className);
            className.RegisterValueChangedCallback(e => Draft.Rename(e.newValue));
            hull = new DropdownField("Hull / version",draft.Provider.Hulls.Select(h=>h.Caption).ToList(),0) { name = "designer-hull" }; left.Add(hull);
            hull.RegisterValueChangedCallback(_ => Draft.SetHull(Draft.Provider.Hulls[hull.index].Id));
            left.Add(hullFacts);
            roles = new DropdownField("Desired role",new List<string> {"Surveyor","Escort","Custom"},0) { name = "designer-role" }; left.Add(roles);
            roles.RegisterValueChangedCallback(e => Draft.SetRole(e.newValue));
            templates = new DropdownField("Start from template",draft.Provider.Templates.Select(t=>t.Caption).ToList(),0) { name = "designer-template" }; left.Add(templates);
            templates.RegisterValueChangedCallback(_ =>
            {
                var id = Draft.Provider.Templates[templates.index].Id;
                Draft.RequestTemplate(id); RefreshPresentation();
            });
            left.Add(new LcarsReadout("Local session draft only. Edits do not submit orders, unlock Engineering or enter campaign saves."));
            var construction = new LcarsPillButton("Construction unavailable",()=>{}) { name = "designer-construction", tooltip = "Demo only. No Build, publish, refit or construction command is connected." };
            construction.SetEnabled(false); left.Add(construction);
            comparison.AddToClassList("sd-comparison"); left.Add(comparison);

            var centre = Scroll("designer-schematic-panel"); columns.Add(Frame("02 / INSTALLATION GROUPS",centre,"sd-centre-frame"));
            schematic.name = "designer-schematic"; centre.Add(schematic);
            centre.Add(new LcarsReadout("SCHEMATIC ONLY / GROUPS, NOT SPATIAL PACKING"));
            capacity.name = "designer-capacity"; centre.Add(capacity);
            groups.name = "designer-installation-groups"; centre.Add(groups);
            notice.name = "designer-preview-notice"; notice.AddToClassList("sd-preview-notice"); centre.Add(notice);

            var right = new VisualElement {name="designer-components"}; right.AddToClassList("sd-catalogue-body");
            columns.Add(Frame("03 / COMPONENTS",right,"sd-catalogue-frame"));
            catalogue = new DropdownField("Available catalogue",draft.Provider.Catalogue.Select(c=>c.Name).ToList(),0) { name = "designer-catalogue" }; right.Add(catalogue);
            catalogue.RegisterValueChangedCallback(_ => RefreshCatalogue());
            var catalogueInfo=Scroll("designer-catalogue-details"); right.Add(catalogueInfo);
            description.name = "designer-component-description"; catalogueInfo.Add(description);
            facts.name = "designer-component-costs"; facts.AddToClassList("sd-catalogue-facts"); catalogueInfo.Add(facts);
            catalogueIssues.name = "designer-catalogue-issues"; catalogueInfo.Add(catalogueIssues);
            var catalogueActions = new VisualElement(); catalogueActions.AddToClassList("sd-buttons"); right.Add(catalogueActions);
            catalogueActions.Add(new LcarsPillButton("Add package",()=>Draft.Add(CatalogueItem.Id)) { name = "designer-add" });
            replace = new LcarsPillButton("Replace selected",()=> { if(selectedInstalled!=null) Draft.Replace(selectedInstalled,CatalogueItem.Id); }) { name = "designer-replace" }; catalogueActions.Add(replace);
            right.Add(new LcarsSectionHeader("INSTALLED / QUANTITY"));
            right.Add(new LcarsReadout("Select a stack to replace • + / − changes quantity"));
            var installedScroll=Scroll("designer-installed-scroll"); right.Add(installedScroll);
            installed.name = "designer-installed"; installedScroll.Add(installed);

            var footer = new VisualElement(); footer.AddToClassList("sd-footer"); body.Add(footer);
            var profiles = new VisualElement(); profiles.AddToClassList("sd-buttons"); footer.Add(profiles);
            foreach(DesignerProfile mode in Enum.GetValues(typeof(DesignerProfile)))
            {
                var button = new LcarsPillButton(mode.ToString().ToUpperInvariant(),()=>Draft.SelectProfile(mode)) { name = "designer-profile-"+mode.ToString().ToLowerInvariant() };
                profileButtons.Add(button); profiles.Add(button);
            }
            var footerScroll = Scroll("designer-profile-results"); summary.AddToClassList("sd-summary"); footerScroll.Add(summary); footer.Add(footerScroll);
            Draft.Changed += RefreshPresentation;
            RegisterCallback<GeometryChangedEvent>(e => ApplyLogicalWidth(e.newRect.width));
            ApplyLogicalWidth(1700); RefreshPresentation();
        }
        public void Dispose() => Draft.Changed -= RefreshPresentation;
        public void RequestLeave(Action leave) => Draft.RequestDiscard(leave);
        public void ApplyLogicalWidth(float width)
        {
            if(width<=0 || float.IsNaN(width) || float.IsInfinity(width)) return;
            EnableInClassList("sd-narrow",width<1450);
        }
        private static ScrollView Scroll(string id)
        { var scroll = new ScrollView(ScrollViewMode.Vertical) { name = id }; scroll.AddToClassList("sd-scroll"); return scroll; }
        private static LcarsPanelFrame Frame(string title,VisualElement body,string style)
        { var frame = new LcarsPanelFrame(title,body); frame.AddToClassList("sd-frame"); frame.AddToClassList(style); return frame; }
        private DesignerComponent CatalogueItem => Draft.Provider.Catalogue[catalogue.index];
        private string StatusCaption(DesignerStatus status) => (Draft.Provider.FixtureResults ? "DEMO / " : "")
            + (status==DesignerStatus.HardError?"ERROR":status==DesignerStatus.Warning?"WARNING":status==DesignerStatus.Valid?"VALID":"NOT EVALUATED");
        private static void StatusClass(VisualElement element,DesignerStatus status)
        {
            element.EnableInClassList("sd-valid",status==DesignerStatus.Valid);
            element.EnableInClassList("sd-warning",status==DesignerStatus.Warning);
            element.EnableInClassList("sd-error",status==DesignerStatus.HardError);
            element.EnableInClassList("sd-unavailable",status==DesignerStatus.NotEvaluated);
        }
        private void RefreshCatalogue(DesignerPreview? observed = null)
        {
            var item = CatalogueItem; description.text = item.Description; facts.text = (Draft.Provider.FixtureResults?"DEMO UNIT FACTS / ":"UNIT FACTS / ")+item.Facts;
            var issues = (observed ?? Draft.Preview()).Issues.Where(i=>i.SystemId==item.Id).ToList();
            catalogueIssues.text = string.Join("\n",issues.Select(i=>StatusCaption(i.Severity)+" • "+i.Explanation));
            StatusClass(catalogueIssues,issues.Any(i=>i.Severity==DesignerStatus.HardError)?DesignerStatus.HardError:DesignerStatus.Warning);
            replace.SetEnabled(selectedInstalled!=null && Draft.Current.Installed.ContainsKey(selectedInstalled) && selectedInstalled!=item.Id);
        }
        private void RefreshPresentation()
        {
            var fit = Draft.Current; var preview = Draft.Preview();
            className.SetValueWithoutNotify(fit.Name); roles.SetValueWithoutNotify(fit.Role);
            var hullVersion=Draft.Provider.Hulls.Single(h=>h.Id==fit.HullId); hull.SetValueWithoutNotify(hullVersion.Caption); hullFacts.text=hullVersion.Facts;
            templates.SetValueWithoutNotify(Draft.Provider.Templates.Single(t=>t.Id==Draft.TemplateId).Caption);
            state.text = (Draft.Modified?"MODIFIED LOCAL DRAFT":"TEMPLATE / LOCAL DRAFT")+" • "+StatusCaption(preview.Status); StatusClass(state,preview.Status);
            ResetButton.SetEnabled(Draft.Modified); confirmation.style.display = Draft.ConfirmationPending?DisplayStyle.Flex:DisplayStyle.None;
            notice.text = preview.Notice; StatusClass(notice,preview.Status);
            capacity.text = preview.UsedCapacity.HasValue ? $"CAPACITY / {preview.UsedCapacity} used / {preview.TotalCapacity} total / {preview.FreeCapacity} available" : "CAPACITY / Not evaluated for this fit";
            groups.Clear();
            if(preview.UsedCapacity.HasValue && preview.TotalCapacity.HasValue) groups.Add(new LcarsMeter("Used capacity",preview.UsedCapacity.Value,preview.TotalCapacity.Value));
            foreach(var group in new[] {"Core","Mission","Defence"})
            {
                var section = new LcarsInspectorBlock(group.ToUpperInvariant()); groups.Add(section);
                foreach(var item in Draft.Provider.Catalogue.Where(c=>c.Group==group))
                {
                    if(fit.Installed.TryGetValue(item.Id,out var count)) section.Row(item.Name,"× "+count);
                    foreach(var issue in preview.Issues.Where(i=>i.SystemId==item.Id))
                    { var label = new LcarsReadout(StatusCaption(issue.Severity)+" • "+issue.Explanation); StatusClass(label,issue.Severity); section.Add(label); }
                }
            }
            if(preview.Issues.Count>0)
            {
                var validation=new LcarsInspectorBlock("VALIDATION / "+StatusCaption(preview.Status)); groups.Add(validation);
                foreach(var issue in preview.Issues)
                { var label=new LcarsReadout(StatusCaption(issue.Severity)+" • "+issue.Explanation); StatusClass(label,issue.Severity); validation.Add(label); }
            }
            installed.Clear();
            foreach(var item in Draft.Provider.Catalogue.Where(c=>fit.Installed.ContainsKey(c.Id)))
            {
                var row = new VisualElement(); row.AddToClassList("sd-installed-row"); installed.Add(row);
                var select = new LcarsPillButton(item.Name+" × "+fit.Installed[item.Id],()=> { selectedInstalled=item.Id; RefreshPresentation(); });
                select.EnableInClassList("sd-selected",selectedInstalled==item.Id); row.Add(select);
                var controls = new VisualElement(); controls.AddToClassList("sd-buttons"); row.Add(controls);
                controls.Add(new LcarsPillButton("−",()=>Draft.SetQuantity(item.Id,fit.Installed[item.Id]-1)) { name = "quantity-minus-"+item.Id, tooltip = "Remove one "+item.Name });
                controls.Add(new LcarsPillButton("+",()=>Draft.Add(item.Id)) { name = "quantity-plus-"+item.Id, tooltip = "Add one "+item.Name });
                controls.Add(new LcarsPillButton("Remove",()=>Draft.Remove(item.Id)) { name = "remove-"+item.Id });
                foreach(var issue in preview.Issues.Where(i=>i.SystemId==item.Id))
                { var label = new LcarsReadout(issue.Explanation); StatusClass(label,issue.Severity); row.Add(label); }
            }
            if(selectedInstalled!=null && !fit.Installed.ContainsKey(selectedInstalled)) selectedInstalled=null;
            for(var i=0;i<profileButtons.Count;i++) profileButtons[i].EnableInClassList("sd-selected",i==(int)Draft.Profile);
            summary.Clear();
            AddMetrics(summary,"POWER / "+Draft.Profile.ToString().ToUpperInvariant(),preview.Power);
            AddMetrics(summary,"EFFECTIVE CAPABILITIES",preview.Capabilities);
            AddMetrics(summary,Draft.Provider.FixtureResults?"TOTAL COSTS / DEMO":"TOTAL COSTS / PREVIEW",preview.Costs);
            comparison.Clear(); comparison.style.display = compare?DisplayStyle.Flex:DisplayStyle.None;
            if(compare)
            {
                comparison.Add(new LcarsSectionHeader("START → DRAFT"));
                comparison.Add(new LcarsDataRow("Name",Draft.Starting.Name+" → "+fit.Name));
                comparison.Add(new LcarsDataRow("Role",Draft.Starting.Role+" → "+fit.Role));
                foreach(var item in Draft.Provider.Catalogue)
                {
                    var before = Draft.Starting.Installed.TryGetValue(item.Id,out var old)?old:0;
                    var after = fit.Installed.TryGetValue(item.Id,out var current)?current:0;
                    if(before!=after) comparison.Add(new LcarsDataRow(item.Name,before+" → "+after));
                }
                var start = Draft.Preview(true);
                comparison.Add(new LcarsDataRow("Capacity",(start.UsedCapacity?.ToString()??"—")+" → "+(preview.UsedCapacity?.ToString()??"Not evaluated")));
                comparison.Add(new LcarsDataRow("Profile status",StatusCaption(start.Status)+" → "+StatusCaption(preview.Status)));
                foreach(var pair in new[] {Tuple.Create(start.Power,preview.Power),Tuple.Create(start.Capabilities,preview.Capabilities),Tuple.Create(start.Costs,preview.Costs)})
                    foreach(var metric in pair.Item1)
                        comparison.Add(new LcarsDataRow(metric.Caption,metric.Value+" → "+(pair.Item2.FirstOrDefault(m=>m.Caption==metric.Caption)?.Value??"Not evaluated")));
                comparison.Add(new LcarsReadout("Compare the same operating profile. Unknown preview values are never treated as zero."));
            }
            RefreshCatalogue(preview); schematic.MarkDirtyRepaint();
        }
        private static void AddMetrics(VisualElement parent,string heading,IReadOnlyList<DesignerMetric> values)
        {
            var block = new LcarsInspectorBlock(heading); block.AddToClassList("sd-metric-block"); parent.Add(block);
            if(values.Count==0) block.Add(new LcarsReadout("Not evaluated"));
            foreach(var metric in values) block.Row(metric.Caption,metric.Value);
        }
    }
    // Vector explanation only; no hit targets, packing rules, assets or simulation RNG.
    public sealed class ShipSchematic : VisualElement
    {
        public ShipSchematic()
        {
            pickingMode=PickingMode.Ignore; AddToClassList("sd-schematic");
            generateVisualContent += Draw; RegisterCallback<GeometryChangedEvent>(_=>MarkDirtyRepaint());
            foreach(var item in new[] {Tuple.Create("MISSION / SENSORS & LABS",6f,6f),Tuple.Create("CORE / POWER & DRIVE",45f,38f),Tuple.Create("DEFENCE / SHIELDS & MOUNTS",6f,72f)})
            {
                var label=new Label(item.Item1) {pickingMode=PickingMode.Ignore};
                label.style.left=Length.Percent(item.Item2); label.style.top=Length.Percent(item.Item3); Add(label);
            }
        }
        private void Draw(MeshGenerationContext context)
        {
            var r=contentRect; if(r.width<=0 || r.height<=0) return;
            var p=context.painter2D; var x=r.width*.5f; var y=r.height*.45f;
            p.fillColor=new Color(.28f,.39f,.50f); p.strokeColor=new Color(.65f,.55f,.77f); p.lineWidth=3;
            p.BeginPath(); p.MoveTo(new Vector2(x-r.width*.24f,y));
            p.BezierCurveTo(new Vector2(x-r.width*.24f,y-r.height*.4f),new Vector2(x+r.width*.24f,y-r.height*.4f),new Vector2(x+r.width*.24f,y));
            p.BezierCurveTo(new Vector2(x+r.width*.24f,y+r.height*.25f),new Vector2(x+r.width*.24f,y+r.height*.25f),new Vector2(x+r.width*.04f,y+r.height*.22f));
            p.LineTo(new Vector2(x+r.width*.04f,r.height*.90f)); p.LineTo(new Vector2(x-r.width*.04f,r.height*.90f)); p.LineTo(new Vector2(x-r.width*.04f,y+r.height*.22f));
            p.BezierCurveTo(new Vector2(x-r.width*.24f,y+r.height*.25f),new Vector2(x-r.width*.24f,y+r.height*.25f),new Vector2(x-r.width*.24f,y)); p.ClosePath(); p.Fill(); p.Stroke();
            p.strokeColor=new Color(.85f,.58f,.32f); p.lineWidth=8;
            foreach(var side in new[] {-1,1})
            { var offset=side*r.width*.34f; p.BeginPath(); p.MoveTo(new Vector2(x+offset,r.height*.45f)); p.LineTo(new Vector2(x+offset,r.height*.90f)); p.Stroke(); }
            p.lineWidth=2; p.BeginPath(); p.MoveTo(new Vector2(x-r.width*.32f,r.height*.65f)); p.LineTo(new Vector2(x+r.width*.32f,r.height*.65f)); p.Stroke();
        }
    }
}
