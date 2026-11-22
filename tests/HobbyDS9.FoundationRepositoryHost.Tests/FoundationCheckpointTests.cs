using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.FoundationRepositoryHost;
using HobbyDS9.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.FoundationRepositoryHost.Tests;

public sealed partial class FoundationCheckpointTests
{
    private static byte[] Source() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "scenario.json"));
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);
    private static byte[] Encode(object value) => Bytes(JsonConvert.SerializeObject(value));
    private static string Json(ICampaignSession session) => JsonConvert.SerializeObject(session.ExportSave());
    private static (FoundationCheckpointValidator Validator, ICampaignSession Session) Fixture(bool combat = false)
    {
        var raw = Source();
        if (combat)
        {
            // A deterministic authored test variant of the real scenario, not an owner save.
            var root = JObject.Parse(Encoding.UTF8.GetString(raw));
            foreach (var fleet in root["InitialState"]!["Fleets"]!) fleet["LocationNodeId"] = "nav.sol";
            foreach (var actor in root["InitialState"]!["Civilizations"]!) actor["ControllerKind"] = 1;
            // A second real encounter permits a partially buffered checkpoint.
            var state = root["InitialState"]!;
            foreach (var ship in ((JArray)state["Ships"]!).ToArray())
            {
                var copy = (JObject)ship.DeepClone(); copy["ShipId"] = ship["ShipId"]!.Value<string>() + "-second";
                copy["FleetId"] = ship["FleetId"]!.Value<string>() + "-second"; ((JArray)state["Ships"]!).Add(copy);
                ((JArray)state["EntityIdentities"]!).Add(new JObject { ["EntityId"] = copy["ShipId"], ["DefinitionId"] = copy["ShipDesignDefinitionId"] });
            }
            foreach (var fleet in ((JArray)state["Fleets"]!).ToArray())
            {
                var copy = (JObject)fleet.DeepClone(); copy["FleetId"] = fleet["FleetId"]!.Value<string>() + "-second";
                copy["ShipIds"] = new JArray(fleet["ShipIds"]!.Select(id => id.Value<string>() + "-second"));
                copy["LocationNodeId"] = "nav.bajor"; ((JArray)state["Fleets"]!).Add(copy);
                ((JArray)state["EntityIdentities"]!).Add(new JObject { ["EntityId"] = copy["FleetId"], ["DefinitionId"] = null });
            }
            raw = Bytes(root.ToString(Formatting.None));
        }
        var validator = new FoundationCheckpointValidator(raw);
        var scenario = JsonConvert.DeserializeObject<ScenarioDto>(Encoding.UTF8.GetString(raw))!;
        scenario.ContentHash = CheckpointImage.Hash(raw);
        return (validator, CampaignApplication.StartScenario(scenario));
    }
    private static void Complete(ICampaignSession session)
    {
        var state = session.ExportSave().GameState;
        Assert.NotEqual(TurnStatus.Rejected, session.AdvanceTurn(state.TurnNumber, state.StateRevision).Status);
        foreach (var request in session.GetPendingCombatRequests())
        {
            state = session.ExportSave().GameState;
            Assert.Equal(CombatSubmissionStatus.Buffered, session.SubmitCombatResult(session.ResolveCombat(request), state.TurnNumber, state.StateRevision).Status);
        }
        state = session.ExportSave().GameState;
        if (state.Phase == SessionPhase.AwaitingCombat)
            Assert.Equal(TurnStatus.Completed, session.FinalizeCombat(state.TurnNumber, state.StateRevision).Status);
    }
    private static void AssertReload(FoundationCheckpointValidator validator, ICampaignSession session)
    {
        var bytes = Encode(session.ExportSave());
        var image = validator.Validate(bytes);
        var restored = validator.Restore(image.ReadPayload());
        Assert.Equal(Json(session), Json(restored));
        Assert.Equal(session.ExportSave().GameState.TurnNumber, image.Turn);
        Assert.Equal(session.ExportSave().GameState.StateRevision, image.Revision);
        Assert.Equal(session.ExportSave().GameState.Phase.ToString(), image.Phase);
        Assert.Equal(CheckpointImage.Hash(bytes), image.PayloadHash);
    }

    [Fact] public void PortableProviderFreeAssemblyAndPinnedCodec()
    {
        var assembly = typeof(FoundationCheckpointValidator).Assembly;
        Assert.Equal(".NETStandard,Version=v2.1", assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.Contains("Sqlite", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("13.0.3", typeof(JsonConvert).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
    }

    [Fact] public void ExactScenarioAndOriginalWhitespaceBytesAreOwnedAndDescriptorIsComplete()
    {
        var raw = Source(); var expected = raw.ToArray(); var validator = new FoundationCheckpointValidator(raw);
        raw[0] = 0; var escaped = validator.ReadScenarioBytes(); escaped[0] = 0;
        Assert.Equal(expected, validator.ReadScenarioBytes());
        var (_, session) = Fixture();
        var bytes = Bytes(" \r\n" + JsonConvert.SerializeObject(session.ExportSave(), Formatting.Indented) + "\t\n");
        var original = bytes.ToArray(); var image = validator.Validate(bytes); bytes[0] = 0;
        var exposed = image.ReadPayload(); exposed[0] = 0;
        Assert.Equal(original, image.ReadPayload());
        Assert.Equal(CheckpointImage.Hash(expected), session.ExportSave().ContentHash);
        Assert.Equal(validator.EditionIdentity, image.EditionIdentity);
        var fields = new List<string>(); var descriptor = validator.EditionIdentity; var offset = 0;
        while (offset < descriptor.Length)
        {
            var colon = descriptor.IndexOf(':', offset); var count = int.Parse(descriptor[offset..colon]); offset = colon + 1;
            // All current fixture fields are ASCII. The documented wire length is UTF-8 bytes.
            fields.Add(descriptor.Substring(offset, count)); offset += count;
        }
        Assert.Equal(new[] { "foundation-repository-descriptor-v1", "foundationSaveEnvelope", "14", "10",
            "ds9-strategic-slice-v1", CheckpointImage.Hash(expected), "science-engineering-v1", "splitmix64-rejection-v1", "1",
            "strict-foundation-newtonsoft-json-13.0.3-v1", "combat-resolver.phased", "1" }, fields);
        Assert.Equal(Json(session), Json(validator.Restore(image.ReadPayload())));
    }

    [Fact] public void ProgressedMetadataAndNextContinuationAreEquivalentAndSessionsAreFresh()
    {
        var (validator, session) = Fixture(); Complete(session); Complete(session); AssertReload(validator, session);
        var payload = Encode(session.ExportSave()); var restored = validator.Restore(payload); var separate = validator.Restore(payload);
        Complete(restored); Assert.Equal(Json(session), Json(separate));
        Complete(session); Assert.Equal(Json(session), Json(restored));
        var forged = new CheckpointImage(payload, "metadata-is-not-validation", 99, 999, "Planning");
        Assert.NotEqual(forged.Turn, validator.Validate(forged.ReadPayload()).Turn);
    }

    [Fact] public void AcceptedCurrentTurnCommandDedupSurvivesRestore()
    {
        var (validator, session) = Fixture(); var state = session.ExportSave().GameState;
        var command = new CommandEnvelope { CommandId = "cmd.repository-dedup", ActorCivilizationId = "civ.federation",
            ExpectedTurn = state.TurnNumber, ExpectedRevision = state.StateRevision, Kind = CommandKind.SetFleetRoute,
            SetFleetRoute = new SetFleetRoutePayload { FleetId = "fleet.federation-scout", RouteNodeIds = new() { "nav.bajor" } } };
        Assert.True(session.SubmitCommand(command).Accepted);
        var restored = validator.Restore(Encode(session.ExportSave())); AssertReload(validator, restored);
        Assert.Equal(CommandErrorCode.DuplicateCommand, restored.SubmitCommand(command).ErrorCode);
        Complete(session); Complete(restored); Assert.Equal(Json(session), Json(restored));
    }

    [Fact] public void ActualPendingBufferedAndFinalizedCombatRootsRestoreAndContinue()
    {
        var (validator, session) = Fixture(combat: true);
        Assert.Equal(TurnStatus.AwaitingCombat, session.AdvanceTurn(1, 0).Status);
        Assert.NotEmpty(session.GetPendingCombatRequests()); AssertReload(validator, session);
        var restored = validator.Restore(Encode(session.ExportSave()));
        foreach (var request in session.GetPendingCombatRequests())
        {
            var state = session.ExportSave().GameState;
            Assert.Equal(CombatSubmissionStatus.Buffered, session.SubmitCombatResult(session.ResolveCombat(request), state.TurnNumber, state.StateRevision).Status);
            state = restored.ExportSave().GameState;
            Assert.Equal(CombatSubmissionStatus.Buffered, restored.SubmitCombatResult(restored.ResolveCombat(request), state.TurnNumber, state.StateRevision).Status);
            Assert.Equal(Json(session), Json(restored)); AssertReload(validator, session);
        }
        Assert.NotEmpty(session.GetResolvedCombats()); Assert.Empty(session.GetPendingCombatRequests());
        var stamp = session.ExportSave().GameState;
        Assert.Equal(TurnStatus.Completed, session.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision).Status);
        Assert.Equal(TurnStatus.Completed, restored.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision).Status);
        Assert.Equal(Json(session), Json(restored)); AssertReload(validator, session);
        Complete(session); Complete(restored); Assert.Equal(Json(session), Json(restored));
    }

    [Theory]
    [InlineData("future")][InlineData("scenario")][InlineData("rules")][InlineData("hash")]
    [InlineData("rng")][InlineData("rngversion")][InlineData("rngnull")][InlineData("nullstate")]
    [InlineData("unknown")][InlineData("case")][InlineData("missing")][InlineData("nullscalar")]
    [InlineData("stringint")][InlineData("floatint")][InlineData("boolint")][InlineData("enumstring")]
    [InlineData("uintfloat")][InlineData("uintstring")][InlineData("uintoverflow")][InlineData("uintnegative")]
    public void IncompatibleAndLossyWireRootsReject(string mutation)
    {
        var (validator, session) = Fixture(); var root = JObject.Parse(Json(session)); var state = (JObject)root["GameState"]!;
        switch (mutation)
        {
            case "future": root["SaveFormatVersion"] = 15; break;
            case "scenario": root["ScenarioId"] = "scenario.other"; break;
            case "rules": root["RulesVersion"] = "future"; break;
            case "hash": root["ContentHash"] = new string('a', 64); break;
            case "rng": state["RngState"]!["AlgorithmId"] = "future"; break;
            case "rngversion": state["RngState"]!["AlgorithmVersion"] = 2; break;
            case "rngnull": state["RngState"]!["State"] = null; break;
            case "nullstate": root["GameState"] = null; break;
            case "unknown": state["OuterCycleHistory"] = new JArray(); break;
            case "case": root["saveFormatVersion"] = root["SaveFormatVersion"]; root.Remove("SaveFormatVersion"); break;
            case "missing": state.Remove("StateRevision"); break;
            case "nullscalar": state["StateRevision"] = null; break;
            case "stringint": state["TurnNumber"] = "1"; break;
            case "floatint": state["TurnNumber"] = 1.0; break;
            case "boolint": state["TurnNumber"] = true; break;
            case "enumstring": state["Phase"] = "Planning"; break;
            case "uintfloat": state["NextEntitySequence"] = 18446744073709551616.0; break;
            case "uintstring": state["RngState"]!["State"] = "18446744073709551615"; break;
            case "uintoverflow": state["NextEntitySequence"] = new JValue(System.Numerics.BigInteger.Parse("18446744073709551616")); break;
            case "uintnegative": state["NextEntitySequence"] = -1; break;
        }
        Assert.Throws<ArgumentException>(() => validator.Validate(Bytes(root.ToString(Formatting.None))));
    }

    [Fact] public void ExactUInt64MaximumAndBeyondDoublePrecisionArePreserved()
    {
        var (validator, session) = Fixture(); var save = session.ExportSave();
        save.GameState.NextEntitySequence = ulong.MaxValue; save.GameState.RngState.State = 9007199254740993UL;
        var payload = Encode(save); var image = validator.Validate(payload); var result = validator.Restore(image.ReadPayload()).ExportSave();
        Assert.Equal(ulong.MaxValue, result.GameState.NextEntitySequence); Assert.Equal(9007199254740993UL, result.GameState.RngState.State);
        Assert.Equal(payload, image.ReadPayload());
    }

    [Theory]
    [InlineData("Blueprints")][InlineData("EntityIdentities")]
    [InlineData("ResearchPaceFactor")][InlineData("nested-null-reference")]
    public void EveryEmittedSaveMemberMustBePresentWithoutChangingInputOrSession(string missing)
    {
        var (validator, session) = Fixture(); var originalSession = Json(session);
        var root = JObject.Parse(originalSession); var state = (JObject)root["GameState"]!;
        if (missing == "nested-null-reference")
        {
            var identity = state["EntityIdentities"]!.OfType<JObject>()
                .First(i => i["DefinitionId"]!.Type == JTokenType.Null);
            Assert.True(identity.Remove("DefinitionId"));
        }
        else
        {
            if (missing == "Blueprints") Assert.Empty((JArray)state[missing]!);
            Assert.True(state.Remove(missing));
        }
        var payload = Bytes(root.ToString(Formatting.Indented)); var unchanged = payload.ToArray();
        Assert.Throws<ArgumentException>(() => validator.Validate(payload));
        Assert.Throws<ArgumentException>(() => validator.Restore(payload));
        Assert.Equal(unchanged, payload); Assert.Equal(originalSession, Json(session));
        AssertReload(validator, session);
    }

    [Fact] public void LoaderCanonicalizationCannotSilentlyRewriteTheCompleteInputRoot()
    {
        var (validator, session) = Fixture(); var root = JObject.Parse(Json(session));
        var identities = (JArray)root["GameState"]!["EntityIdentities"]!;
        root["GameState"]!["EntityIdentities"] = new JArray(identities.Reverse().Select(i => i.DeepClone()));
        // The game accepts an unsorted list and exports canonical entity order.
        // The adapter requires complete-root agreement rather than silently rewriting it.
        Assert.Throws<ArgumentException>(() => validator.Validate(Bytes(root.ToString(Formatting.None))));
    }

    [Fact] public void ActualPendingResolverAndBufferedResultTamperingRejects()
    {
        var (validator, session) = Fixture(combat: true); session.AdvanceTurn(1, 0);
        var pending = JObject.Parse(Json(session));
        var resolver = pending.Descendants().OfType<JProperty>().First(p => p.Name == "ResolverVersion"); resolver.Value = 999;
        Assert.Throws<ArgumentException>(() => validator.Validate(Bytes(pending.ToString(Formatting.None))));
        var request = session.GetPendingCombatRequests()[0]; var state = session.ExportSave().GameState;
        session.SubmitCombatResult(session.ResolveCombat(request), state.TurnNumber, state.StateRevision);
        var buffered = JObject.Parse(Json(session));
        buffered["GameState"]!["ResolvedCombats"]![0]!["Result"]!["ContentHash"] = new string('b', 64);
        Assert.Throws<ArgumentException>(() => validator.Validate(Bytes(buffered.ToString(Formatting.None))));
    }

    [Theory]
    [InlineData("duplicate")][InlineData("nestedduplicate")][InlineData("trailing")][InlineData("comment")]
    [InlineData("singlequote")][InlineData("after")][InlineData("leadingzero")][InlineData("nan")]
    [InlineData("utf8")][InlineData("bom")][InlineData("depth")][InlineData("empty")][InlineData("null")]
    [InlineData("surrogatehigh")][InlineData("surrogatelow")][InlineData("undefined")][InlineData("constructor")]
    public void MalformedWireRejectsBeforeLoad(string mutation)
    {
        var (validator, session) = Fixture(); var json = Json(session);
        var bytes = mutation switch
        {
            "duplicate" => Bytes(json.Replace("\"SaveFormatVersion\":14", "\"SaveFormatVersion\":14,\"SaveFormatVersion\":14")),
            "nestedduplicate" => Bytes(json.Replace("\"AlgorithmVersion\":1", "\"AlgorithmVersion\":1,\"AlgorithmVersion\":1")),
            "trailing" => Bytes(json[..^1] + ",}"),
            "comment" => Bytes("/*comment*/" + json),
            "singlequote" => Bytes(json.Replace("\"SaveFormatVersion\"", "'SaveFormatVersion'")),
            "after" => Bytes(json + "{}"),
            "leadingzero" => Bytes(json.Replace("\"SaveFormatVersion\":14", "\"SaveFormatVersion\":014")),
            "nan" => Bytes(json.Replace("\"SaveFormatVersion\":14", "\"SaveFormatVersion\":NaN")),
            "utf8" => new byte[] { 0xc3, 0x28 },
            "bom" => Encoding.UTF8.GetPreamble().Concat(Bytes(json)).ToArray(),
            "depth" => Bytes(new string('[', 130) + "0" + new string(']', 130)),
            "surrogatehigh" => Bytes(json.Replace("science-engineering-v1", "\\ud800")),
            "surrogatelow" => Bytes(json.Replace("science-engineering-v1", "\\udc00")),
            "undefined" => Bytes(json.Replace("\"SaveFormatVersion\":14", "\"SaveFormatVersion\":undefined")),
            "constructor" => Bytes("new Date(0)"),
            "null" => Bytes("null"),
            _ => Array.Empty<byte>()
        };
        Assert.Throws<ArgumentException>(() => validator.Validate(bytes));
    }

    [Fact] public void OversizedAndInvalidScenariosRejectAndScenarioWhitespaceChangesEdition()
    {
        var (validator, _) = Fixture();
        Assert.Throws<ArgumentException>(() => validator.Validate(new byte[CheckpointImage.MaximumPayloadBytes + 1]));
        Assert.Throws<ArgumentException>(() => new FoundationCheckpointValidator(Bytes("{}")));
        Assert.Throws<ArgumentException>(() => new FoundationCheckpointValidator(new byte[] { 0xff }));
        var other = new FoundationCheckpointValidator(Bytes(" " + Encoding.UTF8.GetString(Source())));
        Assert.NotEqual(validator.EditionIdentity, other.EditionIdentity);
        var (_, session) = Fixture(); Assert.Throws<ArgumentException>(() => other.Validate(Encode(session.ExportSave())));
    }
}
