using System;

namespace Marten.Events.Daemon.New;

public class EventRequest
{
    public long Floor { get; init; }
    public long HighWater { get; init; }
    public int BatchSize { get; init; }

    public override string ToString()
    {
        return $"{nameof(Floor)}: {Floor}, {nameof(HighWater)}: {HighWater}, {nameof(BatchSize)}: {BatchSize}";
    }

    protected bool Equals(EventRequest other)
    {
        return Floor == other.Floor && HighWater == other.HighWater && BatchSize == other.BatchSize;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((EventRequest)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Floor, HighWater, BatchSize);
    }
}