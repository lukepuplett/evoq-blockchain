using System;
using System.Linq;

namespace Evoq.Blockchain;

/// <summary>
/// A type that can be converted to a byte array.
/// </summary>
public interface IByteArray
{
    /// <summary>
    /// Converts the type to a byte array.
    /// </summary>
    /// <returns>The byte array.</returns>
    byte[] ToByteArray();
}

/// <summary>
/// A hex string that is stored as a byte array.
/// </summary>
public readonly struct Hex : IEquatable<Hex>, IByteArray
{
    public static readonly Hex Empty = new Hex(Array.Empty<byte>());
    public static readonly Hex Zero = new Hex(new byte[] { 0 });

    //

    private readonly byte[] _value;

    //

    /// <summary>
    /// Constructs a <see cref="Hex"/> struct from a byte array.
    /// </summary>
    /// <param name="value">The byte array to store.</param>
    /// <exception cref="ArgumentNullException">Thrown if the input byte array is null.</exception>
    public Hex(byte[] value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    //

    /// <summary>
    /// Gets the length of the hex string.
    /// </summary>
    public int Length => _value.Length;

    //

    /// <summary>
    /// Parses a hex string into a <see cref="Hex"/> struct.
    /// </summary>
    /// <param name="hex">The hex string to parse.</param>
    /// <returns>A <see cref="Hex"/> struct representing the parsed hex string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the input string is null or empty.</exception>
    /// <exception cref="FormatException">Thrown if the input string is not a valid hex string.</exception>
    public static Hex Parse(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            throw new ArgumentNullException(nameof(hex));

        // Remove 0x prefix if present
        string normalized = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? hex[2..]
            : hex;

        // Special case - if after removing 0x we have empty string, return Empty
        if (normalized == string.Empty)
            return Empty;

        // Special case - if single '0', return Zero
        if (normalized == "0")
            return Zero;

        // Check for odd length (invalid hex)
        if (normalized.Length % 2 != 0)
            throw new FormatException("Hex string must have an even number of characters");

        // Check for invalid hex characters
        if (!normalized.All(c => Uri.IsHexDigit(c)))
            throw new FormatException("Invalid hex character in string");

        byte[] bytes = new byte[normalized.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(normalized.Substring(i * 2, 2), 16);
        }

        return new Hex(bytes);
    }

    public static implicit operator Hex(string hex) => Parse(hex);

    /// <summary>
    /// Returns a copy of the underlying byte array.
    /// </summary>
    /// <returns></returns>
    public byte[] ToByteArray() => _value.ToArray();

    public override string ToString()
    {
        // Special case: if we have a single zero byte, return "0x0"
        if (_value.Length == 1 && _value[0] == 0)
            return "0x0";

        return "0x" + BitConverter.ToString(_value).Replace("-", "").ToLowerInvariant();
    }

    public bool Equals(Hex other)
    {
        // Compare lengths first
        if (_value.Length != other._value.Length)
            return false;

        // Then compare the actual bytes
        return _value.AsSpan().SequenceEqual(other._value);
    }

    public override bool Equals(object? obj)
    {
        return obj is Hex other && Equals(other);
    }

    public override int GetHashCode()
    {
        return this.ToString().GetHashCode();
    }

    public static bool operator ==(Hex left, Hex right) => left.Equals(right);
    public static bool operator !=(Hex left, Hex right) => !left.Equals(right);

    /// <summary>
    /// Returns true if this represents a zero value (0x0, 0x00, 0x0000, etc.)
    /// </summary>
    public bool IsZeroValue()
    {
        return _value.All(b => b == 0);
    }

    /// <summary>
    /// Compares two Hex instances based on their numerical values, ignoring leading zeros
    /// </summary>
    public bool ValueEquals(Hex other)
    {
        // If both are zero, they're equal regardless of length
        if (IsZeroValue() && other.IsZeroValue())
            return true;

        // Find first non-zero byte in each
        ReadOnlySpan<byte> thisSpan = _value;
        ReadOnlySpan<byte> otherSpan = other._value;

        while (thisSpan.Length > 0 && thisSpan[0] == 0)
        {
            thisSpan = thisSpan[1..];
        }

        while (otherSpan.Length > 0 && otherSpan[0] == 0)
        {
            otherSpan = otherSpan[1..];
        }

        // Different lengths after trimming zeros means different values
        if (thisSpan.Length != otherSpan.Length)
            return false;

        // Compare byte by byte
        for (int i = 0; i < thisSpan.Length; i++)
        {
            if (thisSpan[i] != otherSpan[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the hex string padded with leading zeros to reach the specified length.
    /// Length is the number of hex characters (not including 0x prefix).
    /// </summary>
    /// <param name="length">The desired length of the hex string (not including 0x prefix)</param>
    /// <returns>A padded hex string</returns>
    /// <exception cref="ArgumentException">Thrown if the requested length is less than the current value's length</exception>
    public string ToPadded(int length)
    {
        // Get current hex string without prefix
        string current = BitConverter.ToString(_value).Replace("-", "").ToLowerInvariant();

        // Check if padding is possible
        if (current.Length > length)
            throw new ArgumentException($"Cannot pad to length {length}, current value requires {current.Length} characters", nameof(length));

        // Add padding
        return "0x" + current.PadLeft(length, '0');
    }
}