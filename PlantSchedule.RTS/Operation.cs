using System;
using System.Drawing;

namespace PlantSchedule.DTO
{
    using System;

    public class Operation : IEquatable<Operation>
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Order { get; set; }
        public double Duration { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        // 1) == operator
        public static bool operator ==(Operation left, Operation right)
        {
            // both null or one is null?
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        // 1) != operator
        public static bool operator !=(Operation left, Operation right)
            => !(left == right);

        // 2) Equals(object) override
        public override bool Equals(object obj)
            => Equals(obj as Operation);

        // 2) IEquatable<Operation>
        public bool Equals(Operation other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Name == other.Name
                && Unit == other.Unit
                && Order == other.Order
                && Duration == other.Duration
                && Start == other.Start
                && End == other.End;
        }

        // 3) GetHashCode override
        public override int GetHashCode()
        {
            // In .NET Core / .NET 5+, you can do:
            // return HashCode.Combine(Name, Unit, Order, Duration, Start, End);

            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                hash = hash * 23 + (Unit?.GetHashCode() ?? 0);
                hash = hash * 23 + (Order?.GetHashCode() ?? 0);
                hash = hash * 23 + Duration.GetHashCode();
                hash = hash * 23 + Start.GetHashCode();
                hash = hash * 23 + End.GetHashCode();
                return hash;
            }
        }
    }

}