namespace HobbyDS9.UnityReview
{
    // Presentation only: planned reflow, not rendered/native acceptance.
    public static class ReviewLayoutPolicy
    {
        public static bool StackPanels(float logicalWidth) => logicalWidth<1500;
        public const float MinimumControlWidth=220;
        public const float MinimumReadableFont=16;
    }
}
