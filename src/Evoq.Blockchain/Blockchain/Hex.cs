using System;
using System.Linq;
using System.Numerics;

namespace Evoq.Blockchain;

/// <summary>
/// Specifies how to interpret the sign bit when converting to BigInteger.
/// </summary>
public enum HexSignedness
{
    /// <summary>
    /// Interpret the hex value as a signed number, where the high bit indicates sign.
    /// </summary>
    Signed,

    /// <summary>
    /// Interpret the hex value as an unsigned number, ensuring a positive BigInteger result.
    /// </summary>
    Unsigned
}

/// <summary>
/// Specifies the byte order when converting between Hex and BigInteger.
/// </summary>
public enum HexEndianness
{
    /// <summary>
    /// Most significant byte first (conventional hex representation).
    /// </summary>
    BigEndian,

    /// <summary>
    /// Least significant byte first (used by BigInteger constructor).
    /// </summary>
    LittleEndian
}

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
    /// <summary>
    /// An empty <see cref="Hex"/> value.
    /// </summary>
    public static readonly Hex Empty = new Hex(Array.Empty<byte>());

    /// <summary>
    /// A <see cref="Hex"/> value representing zero.
    /// </summary>
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

    /// <summary>
    /// Implicitly converts a string to a <see cref="Hex"/> struct.
    /// </summary>
    /// <param name="hex">The hex string to convert.</param>
    /// <returns>A <see cref="Hex"/> struct representing the hex string.</returns>
    public static implicit operator Hex(string hex) => Parse(hex);

    /// <summary>
    /// Returns a copy of the underlying byte array.
    /// </summary>
    /// <returns></returns>
    public byte[] ToByteArray() => _value.ToArray();

    /// <summary>
    /// Returns a string representation of the <see cref="Hex"/> struct.
    /// </summary>
    /// <returns>A string representation of the <see cref="Hex"/> struct.</returns>
    public override string ToString()
    {
        // Special case: if we have a single zero byte, return "0x0"
        if (_value.Length == 1 && _value[0] == 0)
            return "0x0";

        return "0x" + BitConverter.ToString(_value).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Compares two <see cref="Hex"/> instances for equality.
    /// </summary>
    /// <param name="other">The other <see cref="Hex"/> instance to compare to.</param>
    /// <returns>True if the two instances are equal, false otherwise.</returns>
    public bool Equals(Hex other)
    {
        // Compare lengths first
        if (_value.Length != other._value.Length)
            return false;

        // Then compare the actual bytes
        return _value.AsSpan().SequenceEqual(other._value);
    }

    /// <summary>
    /// Determines if this <see cref="Hex"/> instance is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare to.</param>
    /// <returns>True if the object is a <see cref="Hex"/> instance and equal to this instance, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Hex other && Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this <see cref="Hex"/> instance.
    /// </summary>
    /// <returns>The hash code for this <see cref="Hex"/> instance.</returns>
    public override int GetHashCode()
    {
        return this.ToString().GetHashCode();
    }

    /// <summary>
    /// Compares two <see cref="Hex"/> instances for equality.
    /// </summary>
    /// <param name="left">The left <see cref="Hex"/> instance.</param>
    /// <param name="right">The right <see cref="Hex"/> instance.</param>
    /// <returns>True if the two instances are equal, false otherwise.</returns>
    public static bool operator ==(Hex left, Hex right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="Hex"/> instances for inequality.
    /// </summary>
    /// <param name="left">The left <see cref="Hex"/> instance.</param>
    /// <param name="right">The right <see cref="Hex"/> instance.</param>
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
    /// Returns a string representation of this hex value, padded with leading zeros to reach the specified length.
    /// </summary>
    /// <param name="length">The desired length of the hex string (excluding the 0x prefix).</param>
    /// <returns>A padded hex string.</returns>
    /// <exception cref="ArgumentException">Thrown if the current hex value cannot be represented in the specified length.</exception>
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

    /// <summary>
    /// Creates a new Hex instance with the value padded to the specified length in bytes.
    /// </summary>
    /// <param name="byteLength">The desired length in bytes.</param>
    /// <returns>A new Hex instance with the value padded to the specified length.</returns>
    /// <exception cref="ArgumentException">Thrown if the current hex value cannot be represented in the specified byte length.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if byteLength is negative.</exception>
    public Hex ToPaddedHex(int byteLength)
    {
        if (byteLength < 0)
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Byte length cannot be negative");

        // Handle special case for zero length
        if (byteLength == 0)
            return Hex.Empty;

        // If current length is already equal to or greater than the requested length, return a copy
        if (_value.Length >= byteLength)
            return new Hex(_value);

        // Create a new byte array with the desired length
        byte[] paddedBytes = new byte[byteLength];

        // Copy the existing bytes to the end of the new array (to pad with leading zeros)
        Array.Copy(_value, 0, paddedBytes, byteLength - _value.Length, _value.Length);

        // Return a new Hex instance with the padded bytes
        return new Hex(paddedBytes);
    }

    /// <summary>
    /// Converts the hex value to a BigInteger using default parameters (unsigned, big-endian).
    /// </summary>
    /// <returns>A BigInteger representing the hex value.</returns>
    /// <remarks>
    /// This method treats the hex value as an unsigned big-endian number, which is the conventional 
    /// representation for hex values in blockchain contexts. For more control over the conversion,
    /// use the overload that accepts signedness and endianness parameters.
    /// </remarks>
    public BigInteger ToBigInteger()
    {
        return ToBigInteger(HexSignedness.Unsigned, HexEndianness.BigEndian);
    }

    /// <summary>
    /// Converts the hex value to a BigInteger with specified signedness and endianness.
    /// </summary>
    /// <param name="signedness">Whether to treat the hex value as signed or unsigned.</param>
    /// <param name="endianness">The byte order to use for the conversion.</param>
    /// <returns>A BigInteger representing the hex value.</returns>
    /// <remarks>
    /// <para>
    /// When converting between Hex and BigInteger, two important considerations are endianness and sign handling:
    /// </para>
    /// 
    /// <para>
    /// <strong>Endianness:</strong> Hex strings are conventionally big-endian (most significant byte first), 
    /// while .NET's BigInteger constructor expects little-endian (least significant byte first). 
    /// This method handles the conversion between these formats based on the specified endianness parameter.
    /// </para>
    /// 
    /// <para>
    /// <strong>Sign Handling:</strong> BigInteger in .NET is signed, using the most significant bit of the 
    /// most significant byte to determine the sign. If this bit is set, the number is interpreted as negative.
    /// When <see cref="HexSignedness.Unsigned"/> is specified (the default), this method ensures the value 
    /// is treated as unsigned by adding a leading zero byte when necessary to prevent negative interpretation.
    /// </para>
    /// 
    /// <para>
    /// For blockchain applications, the default parameters (unsigned, big-endian) are typically appropriate,
    /// as most blockchain values are unsigned integers represented in big-endian format.
    /// </para>
    /// </remarks>
    public BigInteger ToBigInteger(HexSignedness signedness, HexEndianness endianness)
    {
        // Handle empty or zero cases
        if (_value.Length == 0 || IsZeroValue())
            return BigInteger.Zero;

        // Create a copy of the byte array to work with
        byte[] bytes = _value.ToArray();

        // Handle endianness - BigInteger constructor expects little-endian
        if (endianness == HexEndianness.BigEndian)
        {
            Array.Reverse(bytes);
        }

        // Handle signedness - ensure the number is interpreted as unsigned if requested
        if (signedness == HexSignedness.Unsigned)
        {
            // If the high bit is set (which would make BigInteger interpret it as negative),
            // add an extra zero byte to ensure it's interpreted as positive
            if ((bytes[bytes.Length - 1] & 0x80) != 0)
            {
                Array.Resize(ref bytes, bytes.Length + 1);
                bytes[bytes.Length - 1] = 0;
            }
        }

        return new BigInteger(bytes);
    }

    /// <summary>
    /// Creates a Hex value from a BigInteger.
    /// </summary>
    /// <param name="value">The BigInteger value to convert.</param>
    /// <param name="endianness">The byte order to use for the conversion.</param>
    /// <returns>A Hex representing the BigInteger value.</returns>
    /// <remarks>
    /// <para>
    /// This method converts a BigInteger to a Hex value, handling endianness and sign representation:
    /// </para>
    /// 
    /// <para>
    /// <strong>Endianness:</strong> BigInteger.ToByteArray() returns bytes in little-endian format 
    /// (least significant byte first), while hex values are conventionally represented in big-endian 
    /// format (most significant byte first). By default, this method converts to big-endian format,
    /// but you can specify little-endian if needed.
    /// </para>
    /// 
    /// <para>
    /// <strong>Sign Handling:</strong> When converting positive BigInteger values that have their 
    /// high bit set, BigInteger.ToByteArray() adds an extra zero byte to indicate the value is positive.
    /// This method removes that extra byte when it's not needed for the numerical value, producing a
    /// more compact hex representation.
    /// </para>
    /// 
    /// <para>
    /// <strong>Zero Handling:</strong> For BigInteger.Zero, this method returns Hex.Zero (0x0).
    /// </para>
    /// 
    /// <para>
    /// <strong>Negative Values:</strong> Negative BigInteger values are preserved in the resulting Hex.
    /// When converting back to BigInteger, use HexSignedness.Signed to maintain the negative value.
    /// </para>
    /// </remarks>
    public static Hex FromBigInteger(BigInteger value, HexEndianness endianness = HexEndianness.BigEndian)
    {
        // Handle zero case
        if (value == BigInteger.Zero)
            return Zero;

        // Convert to byte array (BigInteger.ToByteArray() returns little-endian)
        byte[] bytes = value.ToByteArray();

        // If the value is positive but has a leading zero byte (added by BigInteger.ToByteArray to indicate sign),
        // we can remove it
        if (bytes.Length > 1 && bytes[bytes.Length - 1] == 0 && (bytes[bytes.Length - 2] & 0x80) == 0)
        {
            Array.Resize(ref bytes, bytes.Length - 1);
        }

        // Handle endianness
        if (endianness == HexEndianness.BigEndian)
        {
            Array.Reverse(bytes);
        }

        return new Hex(bytes);
    }

    /// <summary>
    /// Creates a new Hex instance with the specified value padded to the given byte length.
    /// </summary>
    /// <param name="hex">The hex string to parse and pad.</param>
    /// <param name="byteLength">The desired length in bytes.</param>
    /// <returns>A new Hex instance with the value padded to the specified length.</returns>
    /// <exception cref="ArgumentNullException">Thrown if hex is null.</exception>
    /// <exception cref="FormatException">Thrown if hex is not a valid hex string.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if byteLength is negative.</exception>
    public static Hex CreatePadded(string hex, int byteLength)
    {
        return Parse(hex).ToPaddedHex(byteLength);
    }

    /// <summary>
    /// Creates a new Hex instance with the specified value padded to the given byte length.
    /// </summary>
    /// <param name="hex">The Hex value to pad.</param>
    /// <param name="byteLength">The desired length in bytes.</param>
    /// <returns>A new Hex instance with the value padded to the specified length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if byteLength is negative.</exception>
    public static Hex CreatePadded(Hex hex, int byteLength)
    {
        return hex.ToPaddedHex(byteLength);
    }
}