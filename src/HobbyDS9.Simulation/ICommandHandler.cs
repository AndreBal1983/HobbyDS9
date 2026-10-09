using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Internal composition seam. Public sessions have no gameplay handlers yet.
    // None means success; any stable non-None code rejects the staging transaction.
    internal interface ICommandHandler
    {
        CommandErrorCode Execute(GameStateDto stagingState, CommandEnvelope command);
    }
}
