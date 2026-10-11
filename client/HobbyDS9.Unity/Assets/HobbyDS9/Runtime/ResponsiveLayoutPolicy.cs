#nullable enable
using System;

namespace HobbyDS9.UnityClient
{
    public enum LayoutMode { Compact, Standard, Wide }

    // All dimensions are reference-panel units, not hardware pixels. USS colors
    // and type/spacing tokens live in the single LcarsTokens.uss resource.
    public static class LcarsMetrics
    {
        public const double ReferenceHeight = 1080;
        public const double MinimumReadableScale = 0.9;
        public const double CompactBelow = 1700;
        public const double WideFrom = 2300;
        public const double Bar = 30;
        public const double CompactRail = 104;
        public const double Rail = 136;
        public const double Details = 420;
        public const double CompactDetails = 380;
        public const double Production = 360;
        public const double MinimumMap = 640;
        public const double ElbowWidth = 104;
        public const double ElbowHeight = 64;
    }
    public sealed class LayoutPlan
    {
        public LayoutMode Mode { get; }
        public double RailWidth => Mode == LayoutMode.Compact ? LcarsMetrics.CompactRail : LcarsMetrics.Rail;
        public double DetailsWidth => Mode == LayoutMode.Compact ? LcarsMetrics.CompactDetails : LcarsMetrics.Details;
        public bool ProductionAutoDock => Mode == LayoutMode.Wide;
        public bool ProductionDrawer => Mode != LayoutMode.Wide;
        public bool HorizontalNavigation => Mode == LayoutMode.Compact;
        public bool CompactHeader => Mode == LayoutMode.Compact;
        internal LayoutPlan(LayoutMode mode) { Mode = mode; }
    }
    public static class ResponsiveLayoutPolicy
    {
        private static double Valid(double value, string name)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(name); return value; }
        public static LayoutPlan ForLogicalSize(double width, double height)
        {
            Valid(width, nameof(width)); Valid(height, nameof(height));
            // Short viewport heights also use compact reflow; never shrink fonts.
            return new LayoutPlan(width < LcarsMetrics.CompactBelow || height < 850 ? LayoutMode.Compact
                : width >= LcarsMetrics.WideFrom ? LayoutMode.Wide : LayoutMode.Standard);
        }
        public static double PanelScaleMultiplier(double viewportHeight)
            => Math.Max(1, LcarsMetrics.ReferenceHeight * LcarsMetrics.MinimumReadableScale / Valid(viewportHeight, nameof(viewportHeight)));
        public static LayoutPlan ForViewport(double width, double height)
        {
            Valid(width, nameof(width)); Valid(height, nameof(height));
            var scale = height / LcarsMetrics.ReferenceHeight * PanelScaleMultiplier(height);
            return ForLogicalSize(width / scale, height / scale);
        }
    }
    public sealed class ProductionVisibility
    {
        public bool? UserChoice { get; private set; }
        public bool IsOpen(LayoutPlan plan) => UserChoice ?? plan.ProductionAutoDock;
        public void Choose(bool visible) { UserChoice = visible; }
        public void Toggle(LayoutPlan plan) { Choose(!IsOpen(plan)); }
    }
    public readonly struct ElbowMetrics
    {
        public double Thickness { get; }
        public double RadiusX { get; }
        public double RadiusY { get; }
        public bool CanDraw => Thickness > 0;
        public ElbowMetrics(double width, double height)
        {
            if (double.IsNaN(width) || double.IsNaN(height) || double.IsInfinity(width) || double.IsInfinity(height) || width <= 0 || height <= 0)
            { Thickness = RadiusX = RadiusY = 0; return; }
            Thickness = Math.Min(LcarsMetrics.Bar, Math.Min(width, height) * 0.4);
            RadiusX = Math.Min(88, width); RadiusY = Math.Min(56, height);
        }
    }
}
