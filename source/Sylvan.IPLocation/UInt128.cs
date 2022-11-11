// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: this code was copied from the .NET 7.0 implementation
// removing all the unused members.

using System.Runtime.InteropServices;

namespace System;

[StructLayout(LayoutKind.Sequential)]
readonly struct UInt128    
{
    const int Size = 16;

#if BIGENDIAN
    readonly ulong _upper;
    readonly ulong _lower;
#else
    readonly ulong _lower;
    readonly ulong _upper;
#endif

    public UInt128(ulong upper, ulong lower)
    {
        _lower = lower;
        _upper = upper;
    }

    public int CompareTo(UInt128 value)
    {
        if (this < value)
        {
            return -1;
        }
        else if (this > value)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    public bool Equals(UInt128 other)
    {
        return this == other;
    }

    //
    // IComparisonOperators
    //

    public static bool operator <(UInt128 left, UInt128 right)
    {
        return (left._upper < right._upper)
            || (left._upper == right._upper) && (left._lower < right._lower);
    }

    public static bool operator <=(UInt128 left, UInt128 right)
    {
        return (left._upper < right._upper)
            || (left._upper == right._upper) && (left._lower <= right._lower);
    }

    public static bool operator >(UInt128 left, UInt128 right)
    {
        return (left._upper > right._upper)
            || (left._upper == right._upper) && (left._lower > right._lower);
    }

    public static bool operator >=(UInt128 left, UInt128 right)
    {
        return (left._upper > right._upper)
            || (left._upper == right._upper) && (left._lower >= right._lower);
    }


    public static bool operator ==(UInt128 left, UInt128 right) => (left._lower == right._lower) && (left._upper == right._upper);

    public static bool operator !=(UInt128 left, UInt128 right) => (left._lower != right._lower) || (left._upper != right._upper);

    //
    // IShiftOperators
    //

    public static UInt128 operator >>(UInt128 value, int shiftAmount) 
    {
        // C# automatically masks the shift amount for UInt64 to be 0x3F. So we
        // need to specially handle things if the 7th bit is set.

        shiftAmount &= 0x7F;

        if ((shiftAmount & 0x40) != 0)
        {
            // In the case it is set, we know the entire upper bits must be zero
            // and so the lower bits are just the upper shifted by the remaining
            // masked amount

            ulong lower = value._upper >> shiftAmount;
            return new UInt128(0, lower);
        }
        else if (shiftAmount != 0)
        {
            // Otherwise we need to shift both upper and lower halves by the masked
            // amount and then or that with whatever bits were shifted "out" of upper

            ulong lower = (value._lower >> shiftAmount) | (value._upper << (64 - shiftAmount));
            ulong upper = value._upper >> shiftAmount;

            return new UInt128(upper, lower);
        }
        else
        {
            return value;
        }
    }

    public static explicit operator int(UInt128 value)
    {
        if (value._upper != 0)
        {
            throw new OverflowException();
        }
        return checked((int)value._lower);
    }
}
