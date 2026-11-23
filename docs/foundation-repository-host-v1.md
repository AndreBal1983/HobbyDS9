# Foundational repository host adapter v1

22 November 2026. **Isolated opt-in adapter and tests; no production activation.**
This implements a bounded foundational carrier seam for the [proposed repository
ADR](design/state-repository-savegame-adr.md). It does not change current gameplay,
JSON saves, Unity integration, or the separate outer Cycle 14/17/18 carriers.

`prototypes/HobbyDS9.FoundationRepositoryHost` targets C# 9 / .NET Standard 2.1.
It references Persistence and Application (and therefore Contracts) and has no
SQLite, filesystem or Unity dependency. Newtonsoft.Json is provisionally pinned
to exact NuGet version `[13.0.3]`, with a restore lock, only in this opt-in host
and its dependent test project. The current Unity manifest names Unity package
`com.unity.nuget.newtonsoft-json` 3.2.1; this adapter's explicit NuGet pin does not
claim Unity package/binary or IL2CPP acceptance.

## API and ownership

- `new FoundationCheckpointValidator(byte[] scenarioBytes)` copies and owns the
  exact source bytes, strictly decodes the actual `ScenarioDto`, overwrites its
  content hash with lowercase SHA256 of those original bytes, and uses the real
  `CampaignApplication.StartScenario` validation path. This is the hashing behavior
  of the current [CampaignHost](../client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/CampaignHost.cs).
- `Validate(byte[])` implements `ICheckpointValidator`, owns a copy, loads using
  the matching actual scenario and `CampaignApplication.LoadSave`, and compares
  the entire decoded DTO root with the entire exported root. Object members are
  compared in ordinal canonical order; array order and every value must agree.
  Loader normalization that changes a list or fact is rejected. This is a full
  foundational root proof, not a metadata check or genesis-replay claim.
- `Restore(byte[])` repeats the complete validation and returns a fresh
  `ICampaignSession`; it never adopts or modifies a caller's running session.
- `EditionIdentity` gives the complete bounded descriptor. `ReadScenarioBytes()`
  and `CheckpointImage.ReadPayload()` return detached copies. A checkpoint retains
  the original exact JSON bytes, including whitespace, rather than reserialized
  text. Hand-constructed checkpoint metadata alone proves no game validity.

## Strict wire policy

Both scenario and payload are bounded to 64 MiB and JSON depth 128. Decode uses
strict UTF-8 and an RFC 8259 syntax pass before Newtonsoft. BOMs, comments, single
quotes, trailing commas, duplicate object names, extra roots, undefined, NaN,
constructors and unpaired escaped surrogates reject. DTO members match exact
case; unknown fields reject, including outer-carrier sidecars. Strings, booleans,
arrays, objects and integral scalar facts require their actual JSON token types;
numeric strings, enum strings and floating-point integers are rejected. UInt64
facts never pass through a double. Range checks remain checked. The current
CampaignHost requirement for all nonnullable Contracts value-type properties is
preserved for authored scenarios, whose existing optional authoring fields remain
supported. Saved envelopes additionally require every serializable emitted DTO
member to be present recursively, including nullable/reference members and lists.
Explicit nulls remain subject to the actual matching loader's validity rules;
omission cannot silently supply null, an empty list or another DTO initializer.
There are no payload omissions, upgrades or guessed defaults.

For a stored checkpoint read through `ICheckpointValidator`, the repository
can quarantine a definitive UTF-8 or JSON grammar failure even if the stored
checksum matches. The typed corruption exception covers only strict text
decoding and syntax. Well-formed data with an unrecognized edition or content
and later DTO or loader failures follow existing validation behavior. Direct
`Validate` and `Restore` callers keep their existing argument-error contract.

The descriptor is a concatenation of `decimalUtf8ByteLength:value` fields in this
fixed order: descriptor schema `foundation-repository-descriptor-v1`, carrier
`foundationSaveEnvelope`, core save format, content format, scenario ID, exact
scenario content hash, rules version, RNG algorithm and version, codec identity
`strict-foundation-newtonsoft-json-13.0.3-v1`, actual Application-supported campaign
resolver ID and version. Values cannot contain NUL; each value is at most 4096
UTF-8 bytes and the complete identity at most 16384 characters. Current formats
are save14/content10, RNG splitmix64-rejection-v1/version1 and campaign phased
resolver/version1. The carrier is explicitly not an outer format14 root.

## Evidence and limits

`tests/HobbyDS9.FoundationRepositoryHost.Tests` generates every checkpoint from
the repository's `scenarios/ds9-slice/scenario.json`, including a deterministic
authored variant with co-located human fleets to reach actual AwaitingCombat.
Tests cover original byte ownership/hash/whitespace, descriptor fields, complete
re-export, progressed turn/revision/phase metadata, independent restore ownership,
next-turn continuation equality, current accepted-command deduplication, pending
and buffered combat/finalization/next continuation, resolver/result tampering,
strict malformed/scalar/version/scenario/RNG rejection, missing empty Blueprints,
EntityIdentities, nullable ResearchPaceFactor and nested null DefinitionId member
rejection without input/session mutation, lossless UInt64 maximum
and values above double precision, bounds, and portable/provider-free metadata.

The dedicated test project also references Data.Sqlite and imports its isolated
native-trial target; the production adapter remains provider-free. Actual SQLite
tests import progressed, pending and partially buffered real foundational roots,
prove idempotent reimport, exact migration-backup/export/original preservation,
reopen and equivalent real continuation. Invalid and wrong-scenario imports
publish nothing and leave their source bytes untouched. These tests explicitly
use `-p:NativeSqliteOverride=C:/HobbyDS9/artifacts/tools/sqlite/3.54.1-win-x64/e_sqlite3.dll`;
the target verifies its approved SHA256 before staging it only into test output.
Native distribution packaging, crash recovery, Unity playback and outer Cycle
carriers require separate evidence. No owner files or current player are used.
