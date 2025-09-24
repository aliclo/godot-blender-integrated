using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;
using Godot.Collections;

public class ArrayEqualityComparer<[MustBeVariant] T> : IEqualityComparer<Array<T>>
{
    public bool Equals(Array<T> x, Array<T> y)
    {
        return x.SequenceEqual(y);
    }

    public int GetHashCode([DisallowNull] Array<T> obj)
    {
        int result = 1;

        foreach (var e in obj)
        {
            result = result * 31 + e.GetHashCode();
        }

        return result;
    }
}