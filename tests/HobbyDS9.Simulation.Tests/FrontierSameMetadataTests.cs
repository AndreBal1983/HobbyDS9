using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class FrontierSameMetadataTests
{
    static bool Same(object? a, object? b)
    {
        var method = typeof(CampaignTacticalReviewSession).GetMethod("FrontierSame", BindingFlags.Static | BindingFlags.NonPublic)!;
        try { return (bool)method.Invoke(null, new[] { a, b })!; }
        catch (TargetInvocationException e) when (e.InnerException != null)
        { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
    }

    [Fact] public void NullScalarsTypesAndOrderedScalarSequencesRemainExact()
    {
        Assert.True(Same(null, null)); Assert.False(Same(null, 1)); Assert.False(Same(1, null));
        Assert.True(Same(42, 42)); Assert.False(Same(42, 42L)); Assert.False(Same("A", "a"));
        Assert.True(Same(new[] { 1, 2 }, new[] { 1, 2 }));
        Assert.False(Same(new[] { 1 }, new[] { 1, 2 })); Assert.False(Same(new[] { 1, 2 }, new[] { 1 }));
        Assert.False(Same(new[] { 1, 2 }, new[] { 2, 1 }));
    }

    [Fact] public void SameReferenceAndRepeatedSharedNodesStillTraverseGetters()
    {
        var trace = new List<string>(); var shared = new Ordered(trace, "S");
        Assert.True(Same(shared, shared));
        Assert.Equal(new[] { "SA", "SA", "SM", "SM", "SZ", "SZ" }, trace);
        trace.Clear(); var graph = new Pair(shared, shared);
        Assert.True(Same(graph, graph)); Assert.Equal(12, trace.Count);
        shared.fail = true;
        var thrown = Assert.Throws<TargetInvocationException>(() => Same(shared, shared));
        Assert.IsType<MarkerException>(thrown.InnerException);
    }

    [Fact] public void PropertiesUseOrdinalLeftThenRightOrderAndFreshValues()
    {
        var trace = new List<string>(); var left = new Ordered(trace, "L"); var right = new Ordered(trace, "R");
        Assert.True(Same(left, right)); Assert.Equal(new[] { "LA", "RA", "LM", "RM", "LZ", "RZ" }, trace);
        trace.Clear(); right.a = 9; right.fail = true;
        Assert.False(Same(left, right)); Assert.Equal(new[] { "LA", "RA" }, trace);
        trace.Clear(); right.a = 1;
        Assert.Throws<TargetInvocationException>(() => Same(left, right));
        Assert.Equal(new[] { "LA", "RA", "LM", "RM", "LZ", "RZ" }, trace);
    }

    [Fact] public async Task IndependentComparisonsActuallyOverlapWithoutSharingValues()
    {
        using var barrier = new Barrier(2);
        var traces = new[] { new List<string>(), new List<string>() };
        var tasks = Enumerable.Range(0, 2).Select(i => Task.Run(() =>
        {
            var left = new Gated(traces[i], "L", i, () => Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(20))));
            var right = new Gated(traces[i], "R", i, null);
            return Same(left, right);
        })).ToArray();
        Assert.All(await Task.WhenAll(tasks), Assert.True);
        Assert.All(traces, t => Assert.Equal(new[] { "L", "R" }, t));
    }

    [Theory] [InlineData("move-left")] [InlineData("move-right")]
    [InlineData("current-left")] [InlineData("current-right")] [InlineData("getter")]
    public void FailuresAfterBothEnumeratorsExistDisposeInOriginalOrder(string failure)
    {
        var trace = new List<string>();
        object value = failure == "getter" ? new Ordered(trace, "V") { fail = true } : 1;
        var left = new Sequence(trace, "L", new[] { value }, failure.EndsWith("left") ? failure.Split('-')[0] : "");
        var right = new Sequence(trace, "R", new[] { value }, failure.EndsWith("right") ? failure.Split('-')[0] : "");
        var error = Record.Exception(() => Same(left, right)); Assert.NotNull(error);
        if (failure == "getter") Assert.IsType<MarkerException>(Assert.IsType<TargetInvocationException>(error).InnerException);
        else Assert.IsType<MarkerException>(error);
        Assert.Equal(new[] { "L.dispose", "R.dispose" }, trace.Where(s => s.EndsWith("dispose")));
        Assert.Equal(1, trace.Count(s => s == "L.move"));
        Assert.Equal(failure == "move-left" ? 0 : 1, trace.Count(s => s == "R.move"));
        Assert.Equal(failure.StartsWith("move") ? 0 : 1, trace.Count(s => s == "L.current"));
        Assert.Equal(failure.StartsWith("move") || failure == "current-left" ? 0 : 1, trace.Count(s => s == "R.current"));
    }

    [Fact] public void SecondEnumeratorAcquisitionFailureKeepsOriginalNoDisposalBehavior()
    {
        var trace = new List<string>();
        Assert.Throws<MarkerException>(() => Same(new Sequence(trace, "L", new object[] { 1 }), new Sequence(trace, "R", new object[] { 1 }, "acquire")));
        Assert.Equal(new[] { "L.acquire", "R.acquire" }, trace);
    }

    [Fact] public void FirstDisposeFailureStillPreventsSecondDispose()
    {
        var trace = new List<string>();
        Assert.Throws<MarkerException>(() => Same(new Sequence(trace, "L", Array.Empty<object>(), "dispose"), new Sequence(trace, "R", Array.Empty<object>())));
        Assert.Equal(new[] { "L.acquire", "R.acquire", "L.move", "R.move", "L.dispose" }, trace);
    }

    [Fact] public void SequenceMismatchStopsBeforeLaterValuesAndDisposesBoth()
    {
        var trace = new List<string>();
        Assert.False(Same(new Sequence(trace, "L", new object[] { 1, new Ordered(trace, "bad") { fail = true } }),
            new Sequence(trace, "R", new object[] { 2, 1 })));
        Assert.Equal(new[] { "L.acquire", "R.acquire", "L.move", "R.move", "L.current", "R.current", "L.dispose", "R.dispose" }, trace);
    }

    [Fact] public void ActualClosedDtoValuesAreRecheckedAndNeverMutated()
    {
        var left = new CampaignFrontierReceiptDto { ActorId = "actor", FleetId = "fleet", ArrivalNodeId = "node", Turn = 1,
            ChartedNodeIds = new List<string> { "a", "b" }, ChartedLaneIds = new List<string>() };
        var right = JsonSerializer.Deserialize<CampaignFrontierReceiptDto>(JsonSerializer.Serialize(left))!;
        var before = JsonSerializer.Serialize(left);
        Assert.True(Same(left, right)); right.ChartedNodeIds.Reverse(); Assert.False(Same(left, right));
        right.ChartedNodeIds.Reverse(); right.Turn++; Assert.False(Same(left, right));
        Assert.Equal(before, JsonSerializer.Serialize(left));
    }

    sealed class MarkerException : Exception { }
    sealed class Pair
    {
        readonly object first, second;
        public Pair(object first, object second) { this.first = first; this.second = second; }
        public object A => first; public object B => second;
    }
    // Instrumentation is deliberately fields, not additional reflected properties.
    sealed class Ordered
    {
        readonly List<string> trace; readonly string label;
        public int a = 1; public bool fail;
        public Ordered(List<string> trace, string label) { this.trace = trace; this.label = label; }
        public int Z { get { trace.Add(label + "Z"); if (fail) throw new MarkerException(); return 3; } }
        public int A { get { trace.Add(label + "A"); return a; } }
        public int M { get { trace.Add(label + "M"); return 2; } }
    }
    sealed class Gated
    {
        readonly List<string> trace; readonly string label; readonly int value; readonly Action? gate;
        public Gated(List<string> trace, string label, int value, Action? gate) { this.trace = trace; this.label = label; this.value = value; this.gate = gate; }
        public int Value { get { trace.Add(label); gate?.Invoke(); return value; } }
    }
    sealed class Sequence : IEnumerable
    {
        readonly List<string> trace; readonly string label, failure; readonly object[] values;
        public Sequence(List<string> trace, string label, object[] values, string failure = "") { this.trace = trace; this.label = label; this.values = values; this.failure = failure; }
        public IEnumerator GetEnumerator()
        {
            trace.Add(label + ".acquire"); if (failure == "acquire") throw new MarkerException();
            return new Cursor(this);
        }
        sealed class Cursor : IEnumerator, IDisposable
        {
            readonly Sequence owner; int at = -1;
            public Cursor(Sequence owner) { this.owner = owner; }
            public bool MoveNext() { owner.trace.Add(owner.label + ".move"); if (owner.failure == "move") throw new MarkerException(); return ++at < owner.values.Length; }
            public object Current { get { owner.trace.Add(owner.label + ".current"); if (owner.failure == "current") throw new MarkerException(); return owner.values[at]; } }
            public void Reset() => throw new NotSupportedException();
            public void Dispose() { owner.trace.Add(owner.label + ".dispose"); if (owner.failure == "dispose") throw new MarkerException(); }
        }
    }
}
