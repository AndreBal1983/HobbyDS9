# Session-owned physical navigation topology

Status: source/CPU candidate; no Unity or player performance acceptance.

A physical CampaignTacticalReviewSession constructs one WeightedNavigationGraph
only after ordinary package validation and private CopyDefinitions. Nonphysical
tactical sessions retain no graph. Published transaction forks share that graph
and the exact same privately retained definitions, while mutable campaign state,
knowledge, effects, orders, provenance and ledgers remain detached.

Direct Travel, TravelValidation and GeneratedCampaign graph users reuse this
physical-session graph. The narrow internal CampaignPhysicalMovement.Execute
overload requires ReferenceEquals binding to its original definition owner before
using an explicitly supplied graph. Its existing overload still constructs a new
graph from current input and performs ordinary validation. Public graph
construction is unchanged. Edge returns a defensive DTO copy; read-only public
snapshot lists never expose graph-owned mutable topology.

No dynamic route, cost, knowledge, movement budget, effect or authority result is
cached. Current state is still passed to every query/proof/movement call. All
package, state, contact, movement proof, source/provenance and complete ordered
save replay validations and final root equality remain. There is no global cache,
hash-based permission, save/format/profile/content change, or Foundation/Logistics
call-site expansion.

The disposable diagnostic on the accepted actual 20-week findings checkpoint
attributed 1,102,895,176 cumulative allocated bytes to 1,330 graph constructors.
Nested validation totals are inclusive, not additive; this was an instrumented
.NET diagnostic and establishes no Unity/player speed or peak-memory claim.
The bounded candidate comparison uses the same whole actual root and approved
package in independent .NET processes, one complete public replay per version,
then requires byte-exact public export. Its evidence packet records hashes,
commands, compiler results, allocations and cold/JIT limitations.
