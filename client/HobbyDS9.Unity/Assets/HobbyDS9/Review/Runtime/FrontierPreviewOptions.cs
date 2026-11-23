#nullable enable
namespace HobbyDS9.UnityReview
{
    // Menu selection is explicit. No bootstrap/profile/default-save override.
    public static class FrontierPreviewOptions
    {
        public const bool DefaultEnabled=false;
        public const string ResourceRoot="FrontierPackages/v1/";
        public const string SaveDirectory="HobbyDS9FrontierPreviewOnly-v5";
        public static string[] Slots()=>new[]{"frontier-1","frontier-2","frontier-3"};
    }
}
