#if HOBBYDS9_CPU_PROFILE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HobbyDS9.Simulation
{
    // Non-shipping, synchronous test harness instrumentation only. Inclusive/exclusive
    // values include instrumentation overhead; no timing value controls simulation.
    internal static class CpuProfile
    {
        internal sealed class Metric
        {
            public string Name { get; set; } = "";
            public long Calls { get; set; }
            public double InclusiveMs { get; set; }
            public double ExclusiveMs { get; set; }
            public long InclusiveAllocatedBytes { get; set; }
            public long ExclusiveAllocatedBytes { get; set; }
        }
        private sealed class Frame : IDisposable
        {
            internal readonly string Name;
            private readonly long start = Stopwatch.GetTimestamp(), allocated = GC.GetAllocatedBytesForCurrentThread();
            internal long ChildTicks, ChildBytes;
            internal Frame(string name) { Name = name; }
            public void Dispose()
            {
                long ticks = Stopwatch.GetTimestamp()-start, bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
                frames.Pop();
                if(frames.Count>0){frames.Peek().ChildTicks+=ticks;frames.Peek().ChildBytes+=bytes;}
                if(!metrics.TryGetValue(Name,out var m))metrics[Name]=m=new Metric{Name=Name};
                m.Calls++;m.InclusiveMs+=ticks*1000.0/Stopwatch.Frequency;m.ExclusiveMs+=(ticks-ChildTicks)*1000.0/Stopwatch.Frequency;
                m.InclusiveAllocatedBytes+=bytes;m.ExclusiveAllocatedBytes+=bytes-ChildBytes;
            }
        }
        private sealed class Empty : IDisposable { public void Dispose() { } }
        private static readonly IDisposable empty = new Empty();
        private static readonly Stack<Frame> frames = new Stack<Frame>();
        private static readonly Dictionary<string,Metric> metrics = new Dictionary<string,Metric>(StringComparer.Ordinal);
        private static readonly Dictionary<string,long> counters = new Dictionary<string,long>(StringComparer.Ordinal);
        internal static bool Enabled { get; set; }
        internal static void Reset() { if(frames.Count!=0)throw new InvalidOperationException("Open profile scope");metrics.Clear();counters.Clear(); }
        internal static IDisposable Measure(string name) { if(!Enabled)return empty;var f=new Frame(name);frames.Push(f);return f; }
        internal static void Count(string name,long count=1) { if(Enabled)counters[name]=counters.TryGetValue(name,out var old)?old+count:count; }
        internal static bool Compare(string name,Func<bool> comparison) { using(Measure(name))return comparison(); }
        internal static object Snapshot() => new { Stages=metrics.Values.OrderByDescending(m=>m.ExclusiveMs).ToArray(), Counters=new Dictionary<string,long>(counters) };
    }
}
#endif
