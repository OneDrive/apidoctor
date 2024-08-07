using System;
using System.Collections.Generic;

namespace Kibali;

public class OrderedPairEqualityComparer(StringComparer comparer) : IEqualityComparer<(string, string)>
{
    private readonly StringComparer _stringComparer = comparer;

    public bool Equals((string, string) x, (string, string) y)
    {
        // Check if the items are equal regardless of order
        return (_stringComparer.Equals(x.Item1, y.Item1) && _stringComparer.Equals(x.Item2, y.Item2)) ||
               (_stringComparer.Equals(x.Item1, y.Item2) && _stringComparer.Equals(x.Item2, y.Item1));
    }

    public int GetHashCode((string, string) obj)
    {
        int h1 = obj.Item1?.GetHashCode() ?? 0;
        int h2 = obj.Item2?.GetHashCode() ?? 0;

        // Use a commutative and associative operation to combine hash codes
        // This ensures that (A, B) and (B, A) have the same hash code
        return h1 ^ h2;
    }
}
