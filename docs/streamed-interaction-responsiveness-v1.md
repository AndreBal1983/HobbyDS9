# Streamed interaction workers

Status: source/CPU candidate; Unity receiving and native responsiveness are not yet
established. This presentation increment changes no gameplay, content identity,
save bytes, codec, profile or balance.

Only streamed campaign activity review, Confirm and Planning End Turn use the new
UI path. Existing synchronous APIs and earlier-profile constructors remain. The
background host path requires the existing explicit thread-safe codec opt-in;
custom codecs retain their synchronous behavior unless they supply that capability.
The
UI reserves a one-use operation on its current model/controller/frame, shows an
animated indeterminate busy panel, and executes the expensive work on a worker.
There is no completion percentage claim. Music, the UI scheduler and resize are
not intentionally blocked by command validation.

Application creates a detached, stamped streamed session through Simulation's
existing published fork. The fork owns physical core, IDs, journal/transcript,
mixed queues and consumption history, retained built provenance, observations,
findings, organisation and mutable cache containers. Existing privately retained
fixed definitions, topology, frozen map and immutable playback values share their
established ownership. The inner-fork `publishedExecuting` flag is explicitly
reset to false: all unchanged public mutation, canonical-core synchronization,
capacity, journal and final-validation steps run normally. No serialization or
full load replay is used to clone; actual save/load still performs full replay.

The live source is unchanged throughout worker execution. A candidate's accepted
result is prepared work, not live acceptance. The worker prepares the complete
source-host-bound actor frame, generated presentation and Commission report.
The main-thread completion reconciles research drafts before committing, verifies
the exact operation/source/controller/frame again, and publishes the session and
controller frame with plain nonthrowing assignments under the short host gate.
Local class and organisation drafts remain outside the campaign and are retained;
End Turn continues to reject outstanding edits. Selection follows the previous
refresh policy, including clearing ship selection when selecting the single new
encounter. Activity-page selection is copied as strings and restored against the
new actor observation.

All conflicting host writers reject while the lease is active, including old
sync travel, tactical, strategic, cloak, organisation, save/load and reopen paths.
Other source-backed reads reject promptly. The already validated exact committed
frame/presentation pair has a narrowly bound render-validation path that does not
wait on the worker's source-fork lock. Both object identity and private source
session identity are required; it is not a general admission shortcut or cache
of dynamic costs/knowledge. Normal presentation reads check the actual stamp.

Cancellation before publication discards the candidate without live payment,
resource, ID, random-state or journal changes. Cancellation cannot roll back a
published command or label it cancelled. An abandoned running worker retains the
host reservation and model Busy state until it drains, preventing re-enable or
new Begin from waiting on its source read lock. The re-enabled view shows busy
feedback; stale completion/finally cannot clear a newer operation or publish into
a different owner. This does not promise immediate cancellation of CPU work.

Activity review stores only copied command/caption/selection in the Entry-owned
operation. Busy redraw may dispose the old panel. Completion constructs a fresh
panel and displays exact detached eligibility there without re-evaluation or an
old-panel callback. Confirm uses the same captured command and stamp once, with
ordinary authoritative revalidation and no automatic retry.

Focused checks compare complete sync/worker roots, include exact-capacity
rejection and real tactical phase fork equivalence, late failure/cancellation,
held-worker rendering/writer exclusion, one-use and stale completion ownership,
unrelated drafts, and the normal strict load/advance/reload of a compressed
immutable actual 1000-system test checkpoint. No owner save is used. These CPU
checks do not establish a frame-time improvement: extra branch copies may increase
total work even when removing it from a Unity callback.

Actual receiving still needs bounded waits in the existing streamed test helpers
(their former two-frame assumption is insufficient), actual animated preview /
Confirm / End Turn completion, disable/re-enable and resize, exact Save/Continue,
and the retained mission/activity assertions. No threshold or existing campaign
assertion is relaxed by this source packet.
