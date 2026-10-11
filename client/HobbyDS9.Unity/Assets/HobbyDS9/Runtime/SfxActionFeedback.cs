#nullable enable
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityClient
{
    // Typed outcomes only; no message parsing and no passive-refresh success.
    public static class SfxActionFeedback
    {
        public static LcarsSfx For(CommandResult result) => result.Accepted ? LcarsSfx.Success : LcarsSfx.Error;
        public static LcarsSfx For(TurnResult result) => result.Status == TurnStatus.Rejected ? LcarsSfx.Error : LcarsSfx.Success;
        public static LcarsSfx For(CombatSubmissionResult result) => result.Status == CombatSubmissionStatus.Buffered ? LcarsSfx.Success : LcarsSfx.Error;
    }
}
