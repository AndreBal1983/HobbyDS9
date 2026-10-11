#nullable enable
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityClient
{
    public static class SfxFeedback
    {
        public static string Describe(CommandResult result)
        { SfxDirector.Cue(SfxActionFeedback.For(result)); return CommandFeedback.Describe(result); }
    }
}
