//------------------------------------------------------------------------------
// <auto-generated>
//   Generated by GenerateQuantities.rb. Do not modify!
// </auto-generated>
//------------------------------------------------------------------------------

namespace Foreman.Units
{
    using System;
    using System.Globalization;

    public partial struct Power
      : IQuantity<Power>
    {
        public Power(double watts)
        {
            Watts = watts;
        }

        public static readonly Power Zero = new Power(0.0);
        public static readonly Power Invalid = new Power(double.NaN);

        public double Watts { get; }

        double IQuantity.RawValue => Watts;

        public override string ToString()
        {
            return string.Format(
               CultureInfo.InvariantCulture,
               "{0} {1}", Watts, "W");
        }

        public override int GetHashCode()
        {
            return Watts.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is Power other && Equals(other);
        }

        public bool Equals(Power other)
        {
            return Watts.Equals(other.Watts);
        }

        public int CompareTo(Power other)
        {
            if (Watts < other.Watts)
                return -1;
            if (Watts > other.Watts)
                return 1;
            return 0;
        }

        public int CompareTo(object other)
        {
            if (other == null)
                return 1;
            if (!(other is Power))
                throw new ArgumentException($"Argument must be of type {nameof(Power)}.");
            return CompareTo((Power)other);
        }

        public static bool operator ==(Power lhs, Power rhs)
        {
            return lhs.Watts == rhs.Watts;
        }

        public static bool operator !=(Power lhs, Power rhs)
        {
            return lhs.Watts != rhs.Watts;
        }

        public static bool operator <(Power lhs, Power rhs)
        {
            return lhs.Watts < rhs.Watts;
        }

        public static bool operator >(Power lhs, Power rhs)
        {
            return lhs.Watts > rhs.Watts;
        }

        public static bool operator <=(Power lhs, Power rhs)
        {
            return lhs.Watts <= rhs.Watts;
        }

        public static bool operator >=(Power lhs, Power rhs)
        {
            return lhs.Watts >= rhs.Watts;
        }

        public static Power operator +(Power lhs, Power rhs)
        {
            return new Power(lhs.Watts + rhs.Watts);
        }

        public static Power operator -(Power lhs, Power rhs)
        {
            return new Power(lhs.Watts - rhs.Watts);
        }

        public static Power operator *(Power lhs, double rhs)
        {
            return new Power(lhs.Watts * rhs);
        }

        public static Power operator *(double lhs, Power rhs)
        {
            return new Power(lhs * rhs.Watts);
        }

        public static double operator /(Power lhs, Power rhs)
        {
            return lhs.Watts / rhs.Watts;
        }
    }
}
