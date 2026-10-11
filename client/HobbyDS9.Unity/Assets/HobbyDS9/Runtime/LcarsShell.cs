#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    // Persistent tree: resize changes classes/bounds, not campaign/views/controls.
    public sealed class LcarsShell : VisualElement
    {
        public LcarsTopBar Header { get; }
        public LcarsNavigationRail Navigation { get; }
        public LcarsPanelFrame DetailsFrame { get; }
        public LcarsPanelFrame ProductionFrame { get; }
        public ScrollView Details { get; } = new ScrollView(ScrollViewMode.Vertical);
        public Label Message { get; } = new LcarsReadout("Drag empty map to pan • Scroll to zoom • Select nodes or fleets");
        public LayoutPlan Plan { get; private set; } = ResponsiveLayoutPolicy.ForLogicalSize(1920,1080);
        public ProductionVisibility ProductionChoice { get; } = new ProductionVisibility();
        public bool ProductionOpen => ProductionChoice.IsOpen(Plan);
        public bool DetailsOpen { get; private set; } = true;
        private readonly VisualElement workspace;
        private readonly VisualElement mapFrame;
        private readonly Button contextToggle;
        private double previousWidth = -1, previousHeight = -1;
        public LcarsShell(VisualElement map, VisualElement production, Action<string> select, Action endTurn, Action fitMap)
        {
            name = "lcars-shell"; AddToClassList("lcars-shell");
            Header = new LcarsTopBar(endTurn, fitMap); Add(Header);
            contextToggle = new LcarsPillButton("DETAILS −", ToggleDetails) { name = "toggle-details" };
            Header.Add(contextToggle);
            var frameBar = new VisualElement { name = "lcars-frame-bar", pickingMode = PickingMode.Ignore };
            frameBar.AddToClassList("lcars-frame-bar");
            var elbow = new LcarsElbow { name = "lcars-shell-elbow" };
            elbow.style.width = (float)LcarsMetrics.ElbowWidth; elbow.style.height = (float)LcarsMetrics.ElbowHeight;
            frameBar.Add(elbow);
            var bar = new Label("STRATEGIC OPERATIONS") { pickingMode = PickingMode.Ignore }; bar.AddToClassList("lcars-structural-bar"); frameBar.Add(bar); Add(frameBar);
            var body = new VisualElement { name = "lcars-body" }; body.AddToClassList("lcars-body"); Add(body);
            Navigation = new LcarsNavigationRail(select, ToggleProduction); body.Add(Navigation);
            workspace = new VisualElement { name = "lcars-workspace" }; workspace.AddToClassList("lcars-workspace"); body.Add(workspace);
            ProductionFrame = new LcarsPanelFrame("PRODUCTION / WORLDS", production) { name = "production-frame" }; workspace.Add(ProductionFrame);
            mapFrame = new VisualElement { name = "galaxy-map-frame" }; mapFrame.AddToClassList("lcars-map-frame"); mapFrame.Add(map); workspace.Add(mapFrame);
            Details.AddToClassList("details-panel"); DetailsFrame = new LcarsPanelFrame("EMPIRE / CONTEXT", Details) { name = "details-frame" }; workspace.Add(DetailsFrame);
            Message.AddToClassList("message-bar"); Add(Message);
            RegisterCallback<GeometryChangedEvent>(e => ApplyLogicalSize(e.newRect.width,e.newRect.height));
            Navigation.Select("Empire"); ApplyLogicalSize(1920,1080);
        }
        public void SelectSection(string section)
        { Navigation.Select(section); DetailsFrame.Heading.text = section == "Saves" ? "SAVE / LOAD" : section.ToUpperInvariant() + " / CONTEXT"; DetailsOpen = true; ApplyPanels(); }
        public void OpenProduction() { if (!ProductionOpen) SfxDirector.Cue(LcarsSfx.ScreenOpen); ProductionChoice.Choose(true); ApplyPanels(); }
        public void ToggleProduction() { if (!ProductionOpen) SfxDirector.Cue(LcarsSfx.ScreenOpen); ProductionChoice.Toggle(Plan); ApplyPanels(); }
        public void ToggleDetails()
        {
            if (Plan.Mode == LayoutMode.Compact && ProductionOpen) { ProductionChoice.Choose(false); DetailsOpen = true; }
            else DetailsOpen = !DetailsOpen;
            ApplyPanels();
        }
        public void ApplyLogicalSize(double width,double height)
        {
            if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height)) return;
            if (width == previousWidth && height == previousHeight) return;
            previousWidth = width; previousHeight = height;
            var next = ResponsiveLayoutPolicy.ForLogicalSize(width,height);
            Plan = next;
            EnableInClassList("layout-compact",Plan.Mode == LayoutMode.Compact);
            EnableInClassList("layout-standard",Plan.Mode == LayoutMode.Standard);
            EnableInClassList("layout-wide",Plan.Mode == LayoutMode.Wide);
            // No raw hardware-pixel breakpoints, no root bounds derived from children.
            Navigation.style.width = Plan.HorizontalNavigation ? new StyleLength(StyleKeyword.Auto) : new StyleLength((float)Plan.RailWidth);
            DetailsFrame.style.width = (float)Math.Min(Plan.DetailsWidth,Math.Max(1,width - 32));
            ProductionFrame.style.width = (float)Math.Min(LcarsMetrics.Production,Math.Max(1,width - 32));
            ApplyPanels();
        }
        private void ApplyPanels()
        {
            ProductionFrame.style.display = ProductionOpen ? DisplayStyle.Flex : DisplayStyle.None;
            Navigation.ShowProduction(ProductionOpen);
            var drawer = Plan.ProductionDrawer;
            ProductionFrame.EnableInClassList("lcars-drawer",drawer);
            ProductionFrame.style.position = drawer ? Position.Absolute : Position.Relative;
            ProductionFrame.style.left = drawer ? new StyleLength(0) : new StyleLength(StyleKeyword.Auto);
            ProductionFrame.style.top = drawer ? new StyleLength(0) : new StyleLength(StyleKeyword.Auto);
            ProductionFrame.style.bottom = drawer ? new StyleLength(0) : new StyleLength(StyleKeyword.Auto);
            // Keep drawers above map hit targets but do not cover the rest of it.
            if (drawer && ProductionOpen) ProductionFrame.BringToFront();
            else if (!drawer) ProductionFrame.SendToBack();
            DetailsFrame.style.display = DetailsOpen && !(Plan.Mode == LayoutMode.Compact && ProductionOpen) ? DisplayStyle.Flex : DisplayStyle.None;
            contextToggle.text = "DETAILS " + (DetailsOpen ? "−" : "+");
            contextToggle.style.display = Plan.Mode == LayoutMode.Compact ? DisplayStyle.Flex : DisplayStyle.None;
            // Extremely narrow windows can collapse details via the visible toggle.
            DetailsFrame.EnableInClassList("lcars-context-drawer",Plan.ContextDrawer);
        }
    }
}
