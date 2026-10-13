using System;
using System.Globalization;
using System.Numerics;

namespace HobbyDS9.CampaignTacticalV2.Prototypes
{
    // Review arithmetic only. Never supplied to a campaign session.
    public readonly struct ExactTime : IComparable<ExactTime>, IEquatable<ExactTime>
    {
        public BigInteger Numerator { get; }
        public BigInteger Denominator { get; }
        public ExactTime(BigInteger numerator, BigInteger denominator)
        {
            if (denominator <= 0) throw new ArgumentOutOfRangeException(nameof(denominator));
            var gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
            Numerator = numerator / gcd; Denominator = denominator / gcd;
        }
        public static ExactTime Zero => new ExactTime(0, 1);
        public static ExactTime One => new ExactTime(1, 1);
        private static void Check(ExactTime value)
        { if (value.Denominator <= 0) throw new ArgumentException("Uninitialized rational."); }
        public int CompareTo(ExactTime other)
        { Check(this); Check(other); return (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator); }
        public bool Equals(ExactTime other) => Numerator == other.Numerator && Denominator == other.Denominator;
        public override bool Equals(object? obj) => obj is ExactTime t && Equals(t);
        public override int GetHashCode() => Numerator.GetHashCode() ^ Denominator.GetHashCode();
        public override string ToString() => Numerator.ToString(CultureInfo.InvariantCulture) + "/" + Denominator.ToString(CultureInfo.InvariantCulture);
        public static ExactTime operator +(ExactTime a, ExactTime b)
        { Check(a); Check(b); return new ExactTime(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator); }
        public static ExactTime operator -(ExactTime a, ExactTime b)
        { Check(a); Check(b); return new ExactTime(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator); }
        public static ExactTime operator *(ExactTime a, long b)
        { Check(a); return new ExactTime(a.Numerator * b, a.Denominator); }
        public static bool operator <(ExactTime a, ExactTime b) => a.CompareTo(b) < 0;
        public static bool operator >(ExactTime a, ExactTime b) => a.CompareTo(b) > 0;
        public static bool operator <=(ExactTime a, ExactTime b) => a.CompareTo(b) <= 0;
        public static bool operator >=(ExactTime a, ExactTime b) => a.CompareTo(b) >= 0;
    }
}
