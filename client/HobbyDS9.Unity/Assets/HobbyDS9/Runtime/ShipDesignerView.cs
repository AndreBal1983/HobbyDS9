#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient
{
    // Progressive presentation; the existing draft and Application own editing/preview.
    public sealed class ShipDesignerView : VisualElement, IDisposable
    {
        public ShipDesignDraft Draft { get; }
        public Button ResetButton { get; }
        public Button CloseButton { get; }
        public string SelectedSystem { get; private set; }="Overview";
        public static readonly string[] Systems={"Overview","Hull","Power","Propulsion","Crew","Command","Sensors","Science","Shields","Weapons","Support","Profiles","Validation"};
        private static readonly string[] Captions={"Overview","Hull","Power","Propulsion","Crew / Life support","Command / Computing","Sensors","Science","Shields","Weapons","Mission / Support","Operating profiles","Cost / Validation"};
        private readonly VisualElement workspace=new VisualElement(), confirmation=new VisualElement(), navigation=new VisualElement();
        private readonly Label state=new LcarsReadout(), capacity=new LcarsReadout(), cost=new LcarsReadout(), hullCaption=new LcarsReadout();
        private readonly TextField className;
        private readonly DropdownField templates, roles;
        private readonly DropdownField systemPicker;
        private readonly ShipDesignerGraphics? graphics;
        private string? selectedComponent, replacementSource;
        private string? renderedSystem, renderedComponent;
        private bool comparisonOpen;
        public ShipDesignerView(ShipDesignDraft draft,Action closeRequested,ShipDesignerGraphics? graphicCatalogue=null)
        {
            Draft=draft; graphics=graphicCatalogue??Resources.Load<ShipDesignerGraphics>("ShipDesignerGraphics");
            name="ship-designer";AddToClassList("ship-designer");
            var header=new VisualElement();header.AddToClassList("sd-shell");Add(header);
            var identity=new VisualElement();identity.AddToClassList("sd-identity");header.Add(identity);
            className=new TextField("Class") {name="designer-name",isDelayed=true};identity.Add(className);className.RegisterValueChangedCallback(e=>Draft.Rename(e.newValue));
            roles=new DropdownField("Role",draft.Provider.Roles.Select(r=>r.Caption).ToList(),0) {name="designer-role"};identity.Add(roles);
            roles.RegisterValueChangedCallback(_=>Draft.SetRole(Draft.Provider.Roles[roles.index].Id));
            templates=new DropdownField("Template",draft.Provider.Templates.Select(t=>t.Caption).ToList(),0) {name="designer-template"};identity.Add(templates);
            templates.RegisterValueChangedCallback(_=>Draft.RequestTemplate(Draft.Provider.Templates[templates.index].Id));
            var actions=Buttons(header);actions.AddToClassList("sd-actions");
            state.name="designer-state";actions.Add(state);capacity.name="designer-capacity";actions.Add(capacity);cost.name="designer-cost-summary";actions.Add(cost);actions.Add(hullCaption);
            ResetButton=new LcarsPillButton("Reset",Draft.RequestReset) {name="designer-reset"};actions.Add(ResetButton);
            CloseButton=new LcarsPillButton("Back",()=>RequestLeave(closeRequested)) {name="designer-close"};actions.Add(CloseButton);
            actions.Add(new LcarsPillButton("Review issues",()=>SelectSystem("Validation")) {name="designer-global-issues"});
            confirmation.name="designer-confirmation";confirmation.AddToClassList("confirmation");Add(confirmation);confirmation.Add(new LcarsReadout("Discard this modified local draft?"));
            var confirms=Buttons(confirmation);confirms.Add(new LcarsPillButton("Keep edits",Draft.CancelDiscard) {name="designer-keep"});confirms.Add(new LcarsPillButton("Discard / continue",Draft.ConfirmDiscard) {name="designer-discard"});
            var body=new VisualElement {name="designer-body"};body.AddToClassList("sd-body");Add(body);
            var nav=new ScrollView(ScrollViewMode.Vertical) {name="designer-system-navigation"};nav.AddToClassList("sd-nav-scroll");body.Add(nav);navigation.AddToClassList("sd-navigation");nav.Add(navigation);
            for(var i=0;i<Systems.Length;i++){var id=Systems[i];navigation.Add(new LcarsPillButton(Captions[i],()=>SelectSystem(id)) {name="designer-system-"+id.ToLowerInvariant()});}
            systemPicker=new DropdownField("System",Captions.ToList(),0) {name="designer-system-picker"};systemPicker.AddToClassList("sd-system-picker");body.Add(systemPicker);
            systemPicker.RegisterValueChangedCallback(_=>SelectSystem(Systems[systemPicker.index]));
            workspace.name="designer-workspace";workspace.AddToClassList("sd-workspace");body.Add(workspace);
            var provenance=Buttons(this);provenance.AddToClassList("sd-provenance");provenance.Add(new LcarsReadout(draft.Provider.StatusLabel) {name="designer-provider-status"});
            var pending=new LcarsPillButton("Blueprint publication integration pending",()=>{}) {name="designer-construction"};pending.SetEnabled(false);provenance.Add(pending);
            Draft.Changed+=Refresh;RegisterCallback<GeometryChangedEvent>(e=>ApplyLogicalWidth(e.newRect.width));ApplyLogicalWidth(1700);Refresh();
        }
        public void Dispose()=>Draft.Changed-=Refresh;
        public void RequestLeave(Action leave)=>Draft.RequestDiscard(leave);
        public void ApplyLogicalWidth(float width){if(width<=0||float.IsNaN(width)||float.IsInfinity(width))return;EnableInClassList("sd-narrow",width<1450);EnableInClassList("sd-wide",width>=2200);}
        public void SelectSystem(string id){if(!Systems.Contains(id))throw new ArgumentException("Unknown system",nameof(id));SelectedSystem=id;selectedComponent=null;replacementSource=null;Refresh();}
        private static string Caption(string id)=>Captions[Array.IndexOf(Systems,id)];
        private static VisualElement Buttons(VisualElement parent){var e=new VisualElement();e.AddToClassList("sd-buttons");parent.Add(e);return e;}
        private static ScrollView Scroll(string id){var e=new ScrollView(ScrollViewMode.Vertical) {name=id};e.AddToClassList("sd-scroll");return e;}
        private static LcarsInspectorBlock Block(VisualElement parent,string title){var e=new LcarsInspectorBlock(title);e.AddToClassList("sd-block");parent.Add(e);return e;}
        private static void Metrics(VisualElement parent,string title,IEnumerable<DesignerMetric> metrics){var b=Block(parent,title);foreach(var m in metrics)b.Row(m.Caption,m.Value);}
        private static string Status(DesignerStatus s)=>s==DesignerStatus.HardError?"HARD ERROR":s==DesignerStatus.NotEvaluated?"NOT EVALUATED":s.ToString().ToUpperInvariant();
        private static void StatusClass(VisualElement e,DesignerStatus s){e.EnableInClassList("sd-error",s==DesignerStatus.HardError);e.EnableInClassList("sd-warning",s==DesignerStatus.Warning);e.EnableInClassList("sd-valid",s==DesignerStatus.Valid);e.EnableInClassList("sd-unavailable",s==DesignerStatus.NotEvaluated);}
        private void Refresh()
        {
            var offsets=new Dictionary<string,Vector2>();
            if(renderedSystem==SelectedSystem)
                foreach(var scroll in workspace.Query<ScrollView>().ToList())
                    if(scroll.name!="designer-component-detail" || renderedComponent==selectedComponent)offsets[scroll.name]=scroll.scrollOffset;
            var fit=Draft.Current;var p=Draft.Preview();className.SetValueWithoutNotify(fit.Name);roles.SetValueWithoutNotify(Draft.Provider.Roles.Single(r=>r.Id==fit.Role).Caption);templates.SetValueWithoutNotify(Draft.Provider.Templates.Single(t=>t.Id==Draft.TemplateId).Caption);
            hullCaption.text=Draft.Provider.Hulls.Single(h=>h.Id==fit.HullId).Caption;state.text=Status(p.Status)+(Draft.Modified?" / EDITED":" / TEMPLATE");StatusClass(state,p.Status);
            capacity.text=p.UsedCapacity.HasValue?$"Capacity {p.UsedCapacity} / {p.TotalCapacity}":"Capacity not evaluated";cost.text=Metric(p.Costs,"Construction");ResetButton.SetEnabled(Draft.Modified);
            confirmation.style.display=Draft.ConfirmationPending?DisplayStyle.Flex:DisplayStyle.None;this.Q<Button>("designer-global-issues").text=p.Issues.Count+" issues";
            foreach(var id in Systems)navigation.Q<Button>("designer-system-"+id.ToLowerInvariant()).EnableInClassList("sd-selected",id==SelectedSystem);
            systemPicker.SetValueWithoutNotify(Caption(SelectedSystem));
            workspace.Clear();workspace.Add(new LcarsSectionHeader(Caption(SelectedSystem).ToUpperInvariant()) {name="designer-workspace-title"});
            if(SelectedSystem=="Overview")Overview(p);else if(SelectedSystem=="Hull")Hull(p);else if(SelectedSystem=="Profiles")Profiles(p);else if(SelectedSystem=="Validation")Validation(p);else SystemPage(p);
            renderedSystem=SelectedSystem;renderedComponent=selectedComponent;
            foreach(var scroll in workspace.Query<ScrollView>().ToList())
                if(offsets.TryGetValue(scroll.name,out var offset))scroll.schedule.Execute(()=>scroll.scrollOffset=offset);
        }
        private ShipSystemGraphic Graphic(string id)=>new ShipSystemGraphic(id,Caption(id),graphics!);
        private IEnumerable<DesignerMetric> Headlines(DesignerPreview p,string system)
        {
            if(system=="Overview")
            {
                return new[]{new DesignerMetric("Crew required / embarked",Metric(p.Capabilities,"Required / embarked crew")),new DesignerMetric("Hull / movement",Metric(p.Capabilities,"Compiled hull / movement")),
                    new DesignerMetric("Powered shields",p.Capabilities.FirstOrDefault(m=>m.Caption=="ShieldCapacity")?.Value??"No powered output"),
                    new DesignerMetric("Weapons / long · medium · close",string.Join(" / ",new[]{"LongRangeAttack","MediumRangeAttack","CloseRangeAttack"}.Select(k=>p.Capabilities.FirstOrDefault(m=>m.Caption==k)?.Value??"—"))),
                    new DesignerMetric("Sensors / science",string.Join(" / ",new[]{"Sensors","Science"}.Select(k=>p.Capabilities.FirstOrDefault(m=>m.Caption==k)?.Value??"—")))};
            }
            bool Relevant(string c)=>system=="Overview"?new[]{"Required / embarked crew","Compiled hull / movement","ShieldCapacity","Sensors","Science","LongRangeAttack","MediumRangeAttack","CloseRangeAttack"}.Contains(c):
                system=="Weapons"?c.Contains("Attack")||c.Contains("Accuracy")||c.Contains("mount"):system=="Shields"?c.Contains("Shield"):system=="Propulsion"?c.Contains("Movement")||c.Contains("Propulsion")||c.Contains("Evasion")||c.Contains("movement"):
                system=="Crew"?c.Contains("Crew")||c.Contains("crew"):system=="Command"?c.Contains("Computer"):system=="Sensors"?c=="Sensors":system=="Science"?c=="Science":c=="OperationalRange";
            return p.Capabilities.Where(m=>Relevant(m.Caption));
        }
        private void Overview(DesignerPreview p)
        {
            var s=Scroll("designer-overview");workspace.Add(s);var stage=new VisualElement();stage.AddToClassList("sd-overview-stage");s.Add(stage);
            var art=new VisualElement();art.AddToClassList("sd-overview-art");stage.Add(art);art.Add(Graphic("Overview"));art.Add(new LcarsReadout("Illustrative side view / powered performance shown alongside"));
            var facts=new VisualElement();facts.AddToClassList("sd-overview-facts");stage.Add(facts);Metrics(facts,"SHIP AT A GLANCE / "+Draft.Profile,Headlines(p,"Overview"));Metrics(facts,"POWER",p.Power);Metrics(facts,"CONSTRUCTION / UPKEEP",p.Costs);
            foreach(var i in p.Issues.Take(2))Issue(s,i);if(p.Issues.Count>2)s.Add(new LcarsReadout("See Cost / Validation for all "+p.Issues.Count+" issues."));Comparison(s,p,true);
        }
        private void Hull(DesignerPreview p)
        {
            var s=Scroll("designer-hull-page");workspace.Add(s);s.Add(Graphic("Hull"));var hull=new DropdownField("Hull / version",Draft.Provider.Hulls.Select(h=>h.Caption).ToList(),0) {name="designer-hull"};s.Add(hull);
            hull.SetValueWithoutNotify(Draft.Provider.Hulls.Single(h=>h.Id==Draft.Current.HullId).Caption);hull.RegisterValueChangedCallback(_=>Draft.SetHull(Draft.Provider.Hulls[hull.index].Id));s.Add(new LcarsReadout(Draft.Provider.Hulls.Single(h=>h.Id==Draft.Current.HullId).Facts));
            if(p.UsedCapacity.HasValue&&p.TotalCapacity.HasValue)s.Add(new LcarsMeter("Used capacity",p.UsedCapacity.Value,p.TotalCapacity.Value));Context(s,p);Comparison(s,p,true);
        }
        private void SystemPage(DesignerPreview p)
        {
            var relevant=Draft.Provider.Catalogue.Where(c=>c.SystemId==SelectedSystem).ToList();if(!relevant.Any(c=>c.Id==selectedComponent))selectedComponent=relevant.FirstOrDefault()?.Id;
            var split=new VisualElement();split.AddToClassList("sd-focus-columns");workspace.Add(split);var centre=Scroll("designer-system-content");centre.AddToClassList("sd-system-content");split.Add(centre);centre.Add(Graphic(SelectedSystem));
            if(SelectedSystem=="Crew"){var crew=new LongField("Embarked crew") {name="designer-crew",isDelayed=true};crew.SetValueWithoutNotify(Draft.Current.EmbarkedCrew);crew.RegisterValueChangedCallback(e=>Draft.SetCrew(e.newValue));centre.Add(crew);}
            Metrics(centre,"POWERED PERFORMANCE / "+Draft.Profile,SelectedSystem=="Power"?p.Power:Headlines(p,SelectedSystem));
            var list=Block(centre,"PACKAGES / "+SelectedSystem.ToUpperInvariant());list.name="designer-component-list";
            foreach(var c in relevant)
            {
                var qty=Draft.Current.Installed.TryGetValue(c.Id,out var q)?q:0;var row=new VisualElement {name="component-row-"+c.Id};row.AddToClassList("sd-component-row");list.Add(row);
                var select=new LcarsPillButton(c.Name+"  × "+qty,()=>{selectedComponent=c.Id;Refresh();}) {name="select-component-"+c.Id};select.EnableInClassList("sd-selected",c.Id==selectedComponent);row.Add(select);
                var provider=Draft.Provider as FixtureShipDesignPreviewProvider;if(provider!=null)foreach(var m in provider.ComponentFacts(c.Id,Draft.Profile).Where(m=>new[]{"Capacity / crew","Credits / industry","Full / minimum power"}.Contains(m.Caption)))row.Add(new LcarsDataRow(m.Caption,m.Value));
                row.Add(new LcarsReadout(provider==null?"Availability from preview":provider.Available(c.Id)?"Available • compatibility checked by preview":"Version unavailable"));
            }
            if(relevant.Count==0)list.Add(new LcarsReadout("No packages in this catalogue."));Context(centre,p);Comparison(centre,p,false);
            var detail=Scroll("designer-component-detail");detail.AddToClassList("sd-detail");split.Add(detail);if(selectedComponent!=null)Component(detail,relevant.Single(c=>c.Id==selectedComponent),p);
        }
        private void Component(VisualElement detail,DesignerComponent item,DesignerPreview p)
        {
            var fit=Draft.Current;var count=fit.Installed.TryGetValue(item.Id,out var q)?q:0;detail.Add(new LcarsSectionHeader(item.Name));detail.Add(new LcarsReadout(Caption(SelectedSystem)+" package / "+Draft.Profile+" performance") {name="designer-component-description"});
            var facts=(Draft.Provider as FixtureShipDesignPreviewProvider)?.ComponentFacts(item.Id,Draft.Profile);
            var countRow=Block(detail,"INSTALLED");countRow.Row("Quantity",count.ToString());var controls=Buttons(detail);
            var add=new LcarsPillButton("Add",()=>Draft.Add(item.Id)) {name="designer-add"};add.SetEnabled((Draft.Provider as FixtureShipDesignPreviewProvider)?.Available(item.Id)??true);controls.Add(add);
            var remove=new LcarsPillButton("Remove",()=>Draft.Remove(item.Id)) {name="remove-"+item.Id};remove.SetEnabled(count>0);controls.Add(remove);
            var minus=new LcarsPillButton("−1",()=>Draft.SetQuantity(item.Id,count-1)) {name="quantity-minus-"+item.Id};minus.SetEnabled(count>0);controls.Add(minus);controls.Add(new LcarsPillButton("+1",()=>Draft.Add(item.Id)) {name="quantity-plus-"+item.Id});
            var installed=Draft.Provider.Catalogue.Where(c=>c.SystemId==SelectedSystem&&fit.Installed.ContainsKey(c.Id)).ToList();
            if(installed.Count>0)
            {
                if(!installed.Any(c=>c.Id==replacementSource))replacementSource=installed[0].Id;var selector=new DropdownField("Replace installed",installed.Select(c=>c.Name).ToList(),installed.FindIndex(c=>c.Id==replacementSource)) {name="designer-replacement-source"};detail.Add(selector);
                selector.RegisterValueChangedCallback(_=>{replacementSource=installed[selector.index].Id;Refresh();});var replace=new LcarsPillButton("Replace with this",()=>Draft.Replace(replacementSource!,item.Id)) {name="designer-replace"};replace.SetEnabled(replacementSource!=item.Id&&add.enabledSelf);detail.Add(replace);
            }
            if(facts!=null)Metrics(detail,"PACKAGE / "+Draft.Profile,facts);else detail.Add(new LcarsReadout(item.Facts));
            var proposed=Draft.PreviewCopy();proposed.Add(item.Id);var next=proposed.Preview();var delta=Block(detail,"IF YOU ADD ONE");delta.name="designer-proposed-preview";
            delta.Row("Capacity",(p.UsedCapacity?.ToString()??"—")+" → "+(next.UsedCapacity?.ToString()??"—"));delta.Row("Allocated power",Metric(p.Power,"Actual allocation")+" → "+Metric(next.Power,"Actual allocation"));delta.Row("Construction",Metric(p.Costs,"Construction")+" → "+Metric(next.Costs,"Construction"));delta.Row("Design status",Status(p.Status)+" → "+Status(next.Status));
            detail.Add(new LcarsReadout("New or replaced packages start unpowered. Set explicit allocations in Operating profiles."));foreach(var issue in p.Issues.Where(i=>i.SystemId==item.Id))Issue(detail,issue);
        }
        private static string Metric(IEnumerable<DesignerMetric> values,string key)=>values.FirstOrDefault(m=>m.Caption==key)?.Value??"Not evaluated";
        private void Profiles(DesignerPreview p)
        {
            var buttons=Buttons(workspace);foreach(DesignerProfile mode in Enum.GetValues(typeof(DesignerProfile))){var b=new LcarsPillButton(mode.ToString(),()=>Draft.SelectProfile(mode)) {name="designer-profile-"+mode.ToString().ToLowerInvariant()};b.EnableInClassList("sd-selected",Draft.Profile==mode);buttons.Add(b);}
            var s=Scroll("designer-profile-results");workspace.Add(s);Metrics(s,"AVAILABLE / REQUESTED / ALLOCATED",p.Power.Where(m=>m.Caption=="Available / requested"||m.Caption=="Actual allocation"));
            foreach(var item in Draft.Provider.Catalogue.Where(c=>Draft.Current.Installed.ContainsKey(c.Id)))
            {
                var row=Block(s,item.Name+" × "+Draft.Current.Installed[item.Id]);row.AddToClassList("sd-profile-row");var power=Draft.Current.Power(item.Id,Draft.Profile);var fields=Buttons(row);
                var requested=new LongField("Requested") {name="power-requested-"+item.Id,isDelayed=true};requested.SetValueWithoutNotify(power.RequestedPower);var allocated=new LongField("Allocated") {name="power-allocated-"+item.Id,isDelayed=true};allocated.SetValueWithoutNotify(power.AllocatedPower);
                requested.RegisterValueChangedCallback(e=>Draft.SetPower(item.Id,e.newValue,Draft.Current.Power(item.Id,Draft.Profile).AllocatedPower));allocated.RegisterValueChangedCallback(e=>Draft.SetPower(item.Id,Draft.Current.Power(item.Id,Draft.Profile).RequestedPower,e.newValue));fields.Add(requested);fields.Add(allocated);
                var id=Draft.Current.ToDto().Installations!.Single(i=>i.ComponentVersionId==item.Id).InstallationId;var output=p.Capabilities.FirstOrDefault(m=>m.Caption==id)?.Value;row.Row("Actual output",string.IsNullOrEmpty(output)?"No evaluated output":output!);foreach(var issue in p.Issues.Where(i=>i.SystemId==item.Id && (!i.Profile.HasValue || i.Profile==Draft.Profile)))Issue(row,issue);
            }
            Metrics(s,"RESULTING CAPABILITIES",Headlines(p,"Overview"));Context(s,p);
        }
        private void Validation(DesignerPreview p)
        {
            var s=Scroll("designer-validation");workspace.Add(s);Metrics(s,"TOTAL COST / UPKEEP",p.Costs);var b=Block(s,"COMPLETE VALIDATION / "+Status(p.Status));if(p.Issues.Count==0)b.Add(new LcarsReadout("No validation issues."));
            foreach(var issue in p.Issues){Issue(b,issue);var system=IssueSystem(issue);b.Add(new LcarsPillButton("Review "+Caption(system),()=>SelectSystem(system)) {name="issue-route-"+b.childCount});}Comparison(s,p,true);
        }
        public string IssueSystem(DesignerIssue issue)
        {
            var item=Draft.Provider.Catalogue.FirstOrDefault(c=>c.Id==issue.SystemId);if(item!=null&&Systems.Contains(item.SystemId))return item.SystemId;var t=issue.Explanation.ToLowerInvariant();
            if(t.Contains("mount")||t.Contains("unarmed"))return "Weapons";if(t.Contains("computer")||t.Contains("command"))return "Command";if(t.Contains("crew")||t.Contains("life support"))return "Crew";if(t.Contains("propulsion"))return "Propulsion";
            if(t.Contains("capacity")||t.Contains("hull")||t.Contains("envelope"))return "Hull";if(t.Contains("allocation")||t.Contains("profile"))return "Profiles";if(t.Contains("power")||t.Contains("reactor"))return "Power";return "Validation";
        }
        private static void Issue(VisualElement parent,DesignerIssue issue){var e=new LcarsReadout(Status(issue.Severity)+(issue.Profile.HasValue?" / "+issue.Profile:"")+" • "+issue.Explanation);StatusClass(e,issue.Severity);parent.Add(e);}
        private void Context(VisualElement parent,DesignerPreview p){var issues=p.Issues.Where(i=>IssueSystem(i)==SelectedSystem).ToList();if(issues.Count==0)return;var b=Block(parent,"THIS SYSTEM / VALIDATION");foreach(var issue in issues)Issue(b,issue);}
        private void Comparison(VisualElement parent,DesignerPreview p,bool full)
        {
            var f=new Foldout {name="designer-comparison",text="Compared with starting design",value=comparisonOpen};parent.Add(f);f.RegisterValueChangedCallback(e=>comparisonOpen=e.newValue);var start=Draft.Preview(true);f.Add(new LcarsDataRow("Capacity",(start.UsedCapacity?.ToString()??"—")+" → "+(p.UsedCapacity?.ToString()??"—")));
            foreach(var c in Draft.Provider.Catalogue.Where(c=>full||c.SystemId==SelectedSystem)){var a=Draft.Starting.Installed.TryGetValue(c.Id,out var x)?x:0;var b=Draft.Current.Installed.TryGetValue(c.Id,out var y)?y:0;if(a!=b)f.Add(new LcarsDataRow(c.Name,a+" → "+b));}
            foreach(var m in (full?Headlines(start,"Overview"):Headlines(start,SelectedSystem)).Concat(start.Costs))f.Add(new LcarsDataRow(m.Caption,m.Value+" → "+Metric((full?Headlines(p,"Overview"):Headlines(p,SelectedSystem)).Concat(p.Costs),m.Caption)));
        }
    }
}
