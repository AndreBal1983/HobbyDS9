using System.Runtime.CompilerServices;

// Exercise the real transactional pipeline without shipping gameplay handlers.
[assembly: InternalsVisibleTo("HobbyDS9.Simulation.Tests")]
[assembly: InternalsVisibleTo("HobbyDS9.Application")]
