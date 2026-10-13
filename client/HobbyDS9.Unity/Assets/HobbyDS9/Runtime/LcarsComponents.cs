#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    public sealed class LcarsPillButton : Button
    {
        public LcarsPillButton(string label, Action clicked) : base(() => { SfxDirector.Cue(LcarsSfx.ButtonPress); clicked(); })
        { text = label; AddToClassList("lcars-pill"); }
    }
    public sealed class LcarsSectionHeader : Label
    {
        public LcarsSectionHeader(string text) : base(text) { AddToClassList("lcars-section-heading"); }
    }
    public sealed class LcarsReadout : Label
    {
        public LcarsReadout(string text = "") : base(text) { AddToClassList("lcars-readout"); }
    }
    public sealed class LcarsPanelFrame : VisualElement
    {
        public Label Heading { get; }
        public VisualElement Body { get; }
        public LcarsPanelFrame(string heading, VisualElement body)
        {
            AddToClassList("lcars-panel-frame"); Heading = new LcarsSectionHeader(heading);
            Heading.AddToClassList("lcars-panel-title"); Add(Heading); Body = body; Add(body);
        }
    }
    public sealed class LcarsNavSegment : Button
    {
        private readonly string caption;
        public string Section { get; }
        public bool Selected { get; private set; }
        public LcarsNavSegment(string section, string label, Action selected) : base(() => { SfxDirector.Cue(LcarsSfx.ButtonPress); selected(); })
        { Section = section; caption = label.ToUpperInvariant(); text = caption; name = "nav-" + section.ToLowerInvariant(); AddToClassList("lcars-nav-segment"); }
        public void Select(bool value)
        {
            Selected = value; EnableInClassList("active-tab", value);
            text = (value ? "› " : "") + caption; tooltip = caption + (value ? " (selected)" : "");
        }
    }
    public sealed class LcarsNavigationRail : VisualElement
    {
        public IReadOnlyList<LcarsNavSegment> Sections => sections.AsReadOnly();
        private readonly List<LcarsNavSegment> sections = new List<LcarsNavSegment>();
        public LcarsNavSegment Production { get; } = null!;
        public LcarsNavigationRail(Action<string> select, Action toggleProduction)
        {
            name = "lcars-navigation"; AddToClassList("lcars-navigation");
            foreach (var id in new[] { "Empire", "Selection", "Research", "Designer", "Production", "Combat", "Saves", "Audio" })
            {
                var segment = new LcarsNavSegment(id, id == "Designer" ? "Ship design" : id == "Saves" ? "Save / Load" : id,
                    () => { if (id == "Production") toggleProduction(); else select(id); });
                Add(segment);
                if (id == "Production") Production = segment; else sections.Add(segment);
            }
        }
        public void Select(string section)
        { foreach (var segment in sections) segment.Select(segment.Section == section); }
        public void ShowProduction(bool open)
        {
            Production.text = "PRODUCTION " + (open ? "−" : "+");
            Production.tooltip = open ? "Close production drawer/dock" : "Open empire production";
            Production.EnableInClassList("production-open", open);
        }
    }
    // Compact presentation primitives; no campaign calculations or command ownership.
    public sealed class LcarsDataRow : VisualElement
    {
        public LcarsDataRow(string caption, string value)
        {
            AddToClassList("lcars-data-row"); pickingMode = PickingMode.Ignore;
            var key = new Label(caption) { pickingMode = PickingMode.Ignore };
            key.AddToClassList("lcars-data-key"); Add(key);
            var data = new Label(value) { pickingMode = PickingMode.Ignore };
            data.AddToClassList("lcars-data-value"); Add(data);
        }
    }
    public sealed class LcarsMeter : VisualElement
    {
        public LcarsMeter(string caption, double value, double maximum)
        {
            AddToClassList("lcars-meter"); pickingMode = PickingMode.Ignore;
            Add(new LcarsDataRow(caption, $"{value:0.#} / {maximum:0.#}"));
            var track = new VisualElement { pickingMode = PickingMode.Ignore }; track.AddToClassList("lcars-meter-track"); Add(track);
            var fill = new VisualElement { pickingMode = PickingMode.Ignore }; fill.AddToClassList("lcars-meter-fill");
            fill.style.width = Length.Percent((float)(maximum > 0 ? Math.Max(0, Math.Min(1, value / maximum)) * 100 : 0)); track.Add(fill);
        }
    }
    public sealed class LcarsInspectorBlock : VisualElement
    {
        public LcarsInspectorBlock(string heading)
        { AddToClassList("lcars-inspector-block"); Add(new LcarsSectionHeader(heading)); }
        public void Row(string caption, string value) => Add(new LcarsDataRow(caption, value));
    }
    public sealed class LcarsCompactReadout : VisualElement
    {
        public Label Value { get; } = new LcarsReadout();
        public LcarsCompactReadout(string caption)
        {
            AddToClassList("lcars-compact-readout");
            var key = new Label(caption) { pickingMode = PickingMode.Ignore }; key.AddToClassList("lcars-readout-key"); Add(key); Add(Value);
        }
    }
    public sealed class LcarsTopBar : VisualElement
    {
        public Label Primary { get; } = new LcarsReadout();
        public Label Secondary { get; } = new LcarsReadout();
        public Label Diagnostic { get; } = new LcarsReadout();
        public LcarsCompactReadout Credits { get; } = new LcarsCompactReadout("CREDITS");
        public LcarsCompactReadout Turn { get; } = new LcarsCompactReadout("TURN");
        public LcarsCompactReadout Phase { get; } = new LcarsCompactReadout("PHASE");
        public Button EndTurn { get; }
        public Button FitMap { get; }
        public LcarsTopBar(Action endTurn, Action fitMap)
        {
            name = "lcars-header"; AddToClassList("lcars-header");
            var title = new Label("HOBBY DS9\nSTRATEGIC"); title.AddToClassList("lcars-title"); Add(title);
            var readouts = new VisualElement(); readouts.AddToClassList("lcars-header-readouts"); Add(readouts);
            Primary.AddToClassList("lcars-primary-status"); Secondary.AddToClassList("lcars-secondary-status");
            Diagnostic.AddToClassList("lcars-diagnostic"); readouts.Add(Primary); readouts.Add(Secondary); readouts.Add(Diagnostic);
            Add(Credits); Add(Turn); Add(Phase);
            var controls = new VisualElement(); controls.AddToClassList("lcars-header-controls"); Add(controls);
            EndTurn = new LcarsPillButton("END TURN", endTurn) { name = "end-turn" };
            EndTurn.AddToClassList("lcars-primary-action"); FitMap = new LcarsPillButton("FIT MAP", fitMap) { name = "fit-map" };
            controls.Add(EndTurn); controls.Add(FitMap);
        }
    }
    // Vector L-shaped structural corner, with separate outer/inner cubic curves.
    // UI Toolkit repaints on layout/style changes; no Update/timer/raster assets.
    public sealed class LcarsElbow : VisualElement
    {
        private readonly float jointWidth;
        public LcarsElbow(float jointWidth = 0)
        {
            this.jointWidth = jointWidth;
            AddToClassList("lcars-elbow"); pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }
        private void Draw(MeshGenerationContext context)
        {
            if (jointWidth > 0) { DrawShellJoint(context); return; }
            var rect = contentRect; var metrics = new ElbowMetrics(rect.width, rect.height);
            if (!metrics.CanDraw) return;
            var t = (float)metrics.Thickness; var rx = (float)metrics.RadiusX; var ry = (float)metrics.RadiusY;
            const float k = 0.55228475f;
            var painter = context.painter2D; painter.fillColor = resolvedStyle.color;
            painter.BeginPath(); painter.MoveTo(new Vector2(rect.width, 0)); painter.LineTo(new Vector2(rx, 0));
            painter.BezierCurveTo(new Vector2(rx * (1-k), 0), new Vector2(0, ry * (1-k)), new Vector2(0, ry));
            painter.LineTo(new Vector2(0, rect.height)); painter.LineTo(new Vector2(t, rect.height)); painter.LineTo(new Vector2(t, ry));
            painter.BezierCurveTo(new Vector2(t, ry - k * (ry-t)), new Vector2(rx - k * (rx-t), t), new Vector2(rx, t));
            painter.LineTo(new Vector2(rect.width, t)); painter.ClosePath(); painter.Fill();
        }
        private void DrawShellJoint(MeshGenerationContext context)
        {
            var r=contentRect;
            if(r.width<=0 || r.height<=0)return;
            var t=Math.Min((float)LcarsMetrics.Bar,r.height);
            var outer=Math.Min(48,r.height);
            var innerY=r.height-t;
            var innerX=Math.Min(48,Math.Max(0,r.width-jointWidth));
            const float k=.55228475f;
            var p=context.painter2D;p.fillColor=resolvedStyle.color;
            p.BeginPath();p.MoveTo(new Vector2(r.width,0));p.LineTo(new Vector2(outer,0));
            p.BezierCurveTo(new Vector2(outer*(1-k),0),new Vector2(0,outer*(1-k)),new Vector2(0,outer));
            p.LineTo(new Vector2(0,r.height));p.LineTo(new Vector2(jointWidth,r.height));
            // Join exactly to the navigation rail at the frame's bottom, and to
            // the structural bar's 30-unit top strip at the right edge.
            p.BezierCurveTo(new Vector2(jointWidth,r.height-k*innerY),new Vector2(jointWidth+innerX*(1-k),t),new Vector2(jointWidth+innerX,t));
            p.LineTo(new Vector2(r.width,t));p.ClosePath();p.Fill();
        }
    }
}
