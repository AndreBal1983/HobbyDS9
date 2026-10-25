# Frontier Commission v1

Status: source/CPU and actual Unity receiving accepted. Separate Windows build
and native mission acceptance remain pending. This adds a finite mission outcome to the existing
[streamed campaign](campaign-activities-streamed-v1.md), with continued sandbox
play after completion. It does not add a campaign terminal state or rewards.

The portable Simulation derives four independent historical accomplishments for
the controlled actor: a completed streamed structure, a completed published ship,
a completed and claimed finite non-minor scientific finding, and a completed
independent-society assessment. The starting ship, previews, pending or cancelled
orders and foreign completions do not count. A later ship loss does not erase its
launch. The scientific finding must have a positive frozen budget accounted for
by learned plus explicitly unlearnable insight; reaching a field's limit does
not turn a successful study into a failed expedition. Failed/aborted studies do
not count. Distant study, accepted contact and refused contact are valid society
assessments; cooperation and coercion are not required.

`mission.frontier-commission.v1`, revision 1, uses the
`frontier-commission-retained-evidence-v1` query policy. It is available only for
the exact official activity-content semantic fingerprint
`d6f7e70ca6c7ee98c528355f258ac98d2abbb6be67ef2f1f80e4e1480601671b`.
The fingerprint uses a new query-only domain and the existing explicit canonical
content writer, including all choice semantics and text. An otherwise valid
custom package with the same content ID/revision returns `UnsupportedContent`
with no objectives/evidence; its ordinary sandbox remains usable. Package IDs or
caller-provided fingerprints do not authorize a mission.

The query reads privately owned validated state under the session gate. It does
not replay or mutate state, journal, random state, IDs or resource balances.
Only own retained receipts and legitimately observed own findings are projected;
report prose is presentation and never determines accomplishment. The detached
immutable result includes the actor, turn, revision and source package hash.
The host rejects foreign/stale frames; the model rejects Busy/Closed and earlier
profiles. The content fingerprint is currently recomputed on each bounded query
(official canonical content: 5,556 bytes); no global cache is introduced.

Existing and new Format10 saves retain their exact format, domains, slots and
bytes. Full ordered replay remains mandatory on load, after which the report is
derived again. No new save field, codec, command, profile or migration is added.
The exact pre-existing package writer was extracted without changing its bytes.
Old official, reordered and custom package hash goldens have been checked against
a separately retained pre-extraction assembly baseline; that baseline's assembly
identity is recorded in the source packet rather than described as a new build.

The Unity candidate adds a scrollable Commission report and progress navigation
button, with normal guarded links to world construction, expeditions and the
known map. It shares the ordinary pending-work, modal and owner/epoch navigation
gates and preserves research drafts. It reveals no unknown site locations or
remaining hidden-site counts, and promises no guaranteed recovery after finite
opportunities are exhausted.

Focused CPU evidence covers real 250-system command journeys, accepted/refused
and distant-study outcomes, partial/completed exact replay, terminal research,
actual tactical destruction after construction, automatic-AI receipt isolation,
custom-content rejection, query purity and host/model lifecycle guards. These
are portable/.NET checks, not native play or Unity rendering claims.

Actual Unity receiving at `1ca42ea` passed the full streamed campaign case in
260.608 seconds with 17 captures: mission progress 0/2/3/4, identical reports
across Save/Continue, scrolling/navigation at three resolutions and continued
sandbox play. The initial run exposed a clipped sidebar caption; the shorter
“Mission 4/4” measures 93 pixels within 102 pixels of available space, compared
with 130 pixels for the old caption. Exact campaign validation remains in place;
mature checkpoint latency is still substantial. See the separate evidence at
`E:/HobbyDS9/artifacts/frontier-commission-engine-r2-20261025`.
