using System;
using System.Numerics;
using System.Globalization;

namespace HobbyDS9.Simulation
{
    // Production-owned exact arithmetic. No reference to Stage A prototype assemblies.
    internal readonly struct StrategicTravelTime : IComparable<StrategicTravelTime>, IEquatable<StrategicTravelTime>
    {
        internal BigInteger N { get; }
        internal BigInteger D { get; }
        internal StrategicTravelTime(BigInteger n,BigInteger d)
        {
            if(d<=0)throw new ArgumentOutOfRangeException(nameof(d));
            var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);N=n/gcd;D=d/gcd;
        }
        internal static StrategicTravelTime Zero=>new StrategicTravelTime(0,1);
        internal static StrategicTravelTime One=>new StrategicTravelTime(1,1);
        public int CompareTo(StrategicTravelTime b)=>(N*b.D).CompareTo(b.N*D);
        public bool Equals(StrategicTravelTime b)=>N==b.N&&D==b.D;
        public override bool Equals(object? o)=>o is StrategicTravelTime t&&Equals(t);
        public override int GetHashCode()=>N.GetHashCode()^D.GetHashCode();
        internal string Numerator=>N.ToString(CultureInfo.InvariantCulture);
        internal string Denominator=>D.ToString(CultureInfo.InvariantCulture);
        public static StrategicTravelTime operator +(StrategicTravelTime a,StrategicTravelTime b)=>new StrategicTravelTime(a.N*b.D+b.N*a.D,a.D*b.D);
        public static StrategicTravelTime operator -(StrategicTravelTime a,StrategicTravelTime b)=>new StrategicTravelTime(a.N*b.D-b.N*a.D,a.D*b.D);
        public static StrategicTravelTime operator *(StrategicTravelTime a,long b)=>new StrategicTravelTime(a.N*b,a.D);
        public static bool operator <(StrategicTravelTime a,StrategicTravelTime b)=>a.CompareTo(b)<0;
        public static bool operator >(StrategicTravelTime a,StrategicTravelTime b)=>a.CompareTo(b)>0;
        public static bool operator <=(StrategicTravelTime a,StrategicTravelTime b)=>a.CompareTo(b)<=0;
        public static bool operator >=(StrategicTravelTime a,StrategicTravelTime b)=>a.CompareTo(b)>=0;
    }
}
