using System;
using System.Numerics;
using Evoq.Blockchain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Blockchain.Tests;

[TestClass]
public class HexTests
{
    // Static member tests
    [TestMethod]
    public void Empty_IsEmptyByteArray()
    {
        Assert.AreEqual(0, Hex.Empty.Length);
        Assert.AreEqual("0x", Hex.Empty.ToString());
        CollectionAssert.AreEqual(Array.Empty<byte>(), Hex.Empty.ToByteArray());
    }

    [TestMethod]
    public void Zero_IsSingleZeroByte()
    {
        Assert.AreEqual(1, Hex.Zero.Length);
        Assert.AreEqual("0x0", Hex.Zero.ToString());
        CollectionAssert.AreEqual(new byte[] { 0x00 }, Hex.Zero.ToByteArray());
    }

    // Constructor tests
    [TestMethod]
    [DataRow("0x1234", new byte[] { 0x12, 0x34 })]
    [DataRow("0x", new byte[] { })]
    public void Constructor_FromByteArray_CreatesCorrectHex(string expected, byte[] input)
    {
        Hex hex = new Hex(input);
        Assert.AreEqual(expected, hex.ToString());
    }

    // Parse tests - valid inputs
    [TestMethod]
    [DataRow("0x1234", "0x1234")]
    [DataRow("1234", "0x1234")]
    [DataRow("0xabcd", "0xabcd")]
    [DataRow("ABCD", "0xabcd")]  // Should normalize to lowercase
    public void Parse_ValidHexString_ReturnsCorrectHex(string input, string expected)
    {
        Hex hex = Hex.Parse(input);
        Assert.AreEqual(expected, hex.ToString());
    }

    [TestMethod]
    [DataRow("0x", "0x")]
    [DataRow("0x0", "0x0")]
    [DataRow("00", "0x0")]
    [DataRow("0000", "0x0000")]
    public void Parse_ZeroValues_PreservesLength(string input, string expected)
    {
        Hex hex = Hex.Parse(input);
        Assert.AreEqual(expected, hex.ToString());

        // Special case for "0x0"
        if (expected == "0x0")
            Assert.AreEqual(1, hex.Length);
        else
            Assert.AreEqual(expected.Length > 2 ? (expected.Length - 2) / 2 : 0, hex.Length);
    }

    // Parse tests - invalid inputs
    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    public void Parse_NullOrEmpty_ThrowsArgumentNullException(string input)
    {
        Assert.ThrowsException<ArgumentNullException>(() => Hex.Parse(input));
    }

    [TestMethod]
    [DataRow("0xGG")]   // Invalid hex chars
    [DataRow("WXYZ")]   // Invalid hex chars
    public void Parse_InvalidFormat_ThrowsFormatException(string input)
    {
        Assert.ThrowsException<FormatException>(() => Hex.Parse(input));
    }

    [TestMethod]
    [DataRow("0x1")]
    [DataRow("0x123")]
    [DataRow("1")]
    [DataRow("123")]
    public void Parse_OddLength_ThrowsFormatException(string input)
    {
        Assert.ThrowsException<FormatException>(() => Hex.Parse(input));
    }

    // Implicit conversion tests
    [TestMethod]
    public void ImplicitConversion_FromValidString_CreatesHex()
    {
        Hex hex = "0x1234";
        Assert.AreEqual("0x1234", hex.ToString());
    }

    // Equality tests
    [TestMethod]
    public void Equals_SameHexLength_ReturnsTrue()
    {
        Hex hex1 = "0x1234";
        Hex hex2 = "0x1234";

        Assert.IsTrue(hex1.Equals(hex2));
        Assert.IsTrue(hex1 == hex2);
    }

    [TestMethod]
    public void Equals_DifferentHexLength_ReturnsFalse()
    {
        Hex hex1 = "0x01";    // Changed: use 0x01 instead of 0x0
        Hex hex2 = "0x0001";  // Changed: use 0x0001 instead of 0x00
        Hex hex3 = "0x000001"; // Changed: use 0x000001 instead of 0x0000

        Assert.IsFalse(hex1.Equals(hex2), "Hex1 and Hex2 should not be equal");
        Assert.IsFalse(hex2.Equals(hex3), "Hex2 and Hex3 should not be equal");
        Assert.IsFalse(hex1.Equals(hex3), "Hex1 and Hex3 should not be equal");

        Assert.IsTrue(hex1 != hex2, "Hex1 and Hex2 should not be equal");
        Assert.IsTrue(hex2 != hex3, "Hex2 and Hex3 should not be equal");
        Assert.IsTrue(hex1 != hex3, "Hex1 and Hex3 should not be equal");
    }

    [TestMethod]
    public void GetHashCode_DifferentLengths_ProducesDifferentHashesExceptForSpecialZero()
    {
        Hex hex1 = "0x0";       // Special 0x0 is a single zero byte
        Hex hex2 = "0x00";      // 0x00 is also a single zero byte, as above
        Hex hex3 = "0x0000";    // 0x0000 is four zero bytes

        Assert.AreEqual(hex1.GetHashCode(), hex2.GetHashCode(), "0x0 and 0x00 should have the same hashes");

        Assert.AreNotEqual(hex2.GetHashCode(), hex3.GetHashCode(), "0x00 and 0x0000 should have different hashes");
        Assert.AreNotEqual(hex1.GetHashCode(), hex3.GetHashCode(), "0x0 and 0x0000 should have different hashes");
    }

    [TestMethod]
    public void GetHashCode_WithSameValue_ReturnsSameHash()
    {
        // Arrange
        var hex1 = Hex.Parse("0xAbCd12");
        var hex2 = Hex.Parse("0xabcd12");
        var hex3 = Hex.Parse("ABCD12");

        // Act & Assert
        Assert.AreEqual(hex1.GetHashCode(), hex2.GetHashCode(), "Different case should produce same hash");
        Assert.AreEqual(hex2.GetHashCode(), hex3.GetHashCode(), "With/without prefix should produce same hash");
        Assert.AreEqual(hex1.GetHashCode(), hex3.GetHashCode(), "Different case and prefix should produce same hash");
    }

    // ToString tests
    [TestMethod]
    public void ToString_AlwaysIncludesPrefix()
    {
        Hex hex = "1234";
        StringAssert.StartsWith(hex.ToString(), "0x");
    }

    [TestMethod]
    [Description("Note: Tests that single zero byte returns 0x0")]
    public void ToString_SpecialZero_ReturnsSpecialZero()
    {
        Hex hex = "0x0";
        Assert.AreEqual("0x0", hex.ToString());

        // Round trip should preserve the format
        Hex roundTrip = Hex.Parse(hex.ToString());
        Assert.AreEqual(hex.ToString(), roundTrip.ToString());
    }

    [TestMethod]
    [Description("Note: Tests that single zero byte returns 0x0")]
    public void ToString_SingleZeroByte_ReturnsSpecialZero()
    {
        Hex hex = "0x00";
        Assert.AreEqual("0x0", hex.ToString());

        // Round trip should preserve the format
        Hex roundTrip = Hex.Parse(hex.ToString());
        Assert.AreEqual(hex.ToString(), roundTrip.ToString());
    }

    // ToByteArray tests
    [TestMethod]
    [DataRow("0x1234", new byte[] { 0x12, 0x34 })]
    [DataRow("0xff00", new byte[] { 0xff, 0x00 })]
    [DataRow("0x0000", new byte[] { 0x00, 0x00 })]
    [DataRow("0x0", new byte[] { 0x00 })]
    [DataRow("0x", new byte[] { })]
    [DataRow("0xabcdef", new byte[] { 0xab, 0xcd, 0xef })]
    public void ToByteArray_RoundTrip_PreservesValue(string hexString, byte[] expectedBytes)
    {
        // String -> Hex -> Bytes
        Hex hex = Hex.Parse(hexString);
        byte[] bytes = hex.ToByteArray();
        CollectionAssert.AreEqual(expectedBytes, bytes);

        // Bytes -> Hex -> String
        Hex roundTrip = new Hex(bytes);
        Assert.AreEqual(hexString.ToLowerInvariant(), roundTrip.ToString());
    }

    [TestMethod]
    public void ToByteArray_EmptyHex_ReturnsEmptyArray()
    {
        Hex hex = Hex.Parse("0x");
        byte[] bytes = hex.ToByteArray();
        Assert.AreEqual(0, bytes.Length);
    }

    [TestMethod]
    [DataRow("0x1234", "0x001234", true)]      // Same value with leading zeros
    [DataRow("0x0", "0x00", true)]      // Different representations of zero
    [DataRow("0x0", "0x0000", true)]    // Different representations of zero
    [DataRow("0x01", "0x0001", true)]   // Same value with leading zeros
    [DataRow("0x1234", "0x001234", true)] // Same value with leading zeros
    [DataRow("0x01", "0x02", false)]    // Different values
    [DataRow("0x1234", "0x123400", false)] // Different values (trailing zeros matter)
    public void ValueEquals_ComparesNumericalValues(string input1, string input2, bool expected)
    {
        Hex hex1 = Hex.Parse(input1);
        Hex hex2 = Hex.Parse(input2);
        Assert.AreEqual(expected, hex1.ValueEquals(hex2));
    }

    [TestMethod]
    [DataRow("0x1234", 4, "0x1234")]       // No padding needed
    [DataRow("0x1234", 6, "0x001234")]     // Pad 2 zeros
    [DataRow("0x0", 2, "0x00")]            // Pad single zero
    [DataRow("0x", 4, "0x0000")]           // Pad empty hex
    [DataRow("0x1234", 8, "0x00001234")]   // Pad multiple zeros
    public void ToPadded_PadsToCorrectLength(string input, int length, string expected)
    {
        Hex hex = Hex.Parse(input);
        Assert.AreEqual(expected, hex.ToPadded(length));
    }

    [TestMethod]
    [DataRow("0x1234", 2)]     // Too short
    [DataRow("0x123456", 4)]   // Too short
    public void ToPadded_ThrowsOnInsufficientLength(string input, int length)
    {
        Hex hex = Hex.Parse(input);
        Assert.ThrowsException<ArgumentException>(() => hex.ToPadded(length));
    }

    [TestMethod]
    [DataRow("0x1234", 2, "0x1234")]       // No padding needed (already 2 bytes)
    [DataRow("0x1234", 4, "0x00001234")]   // Pad to 4 bytes
    [DataRow("0x0", 1, "0x0")]            // Pad single zero to 1 byte
    [DataRow("0x", 2, "0x0000")]           // Pad empty hex to 2 bytes
    [DataRow("0x1234", 8, "0x0000000000001234")] // Pad to 8 bytes
    public void ToPaddedHex_PadsToCorrectByteLength(string input, int byteLength, string expected)
    {
        Hex hex = Hex.Parse(input);
        Hex padded = hex.ToPaddedHex(byteLength);

        Assert.AreEqual(expected, padded.ToString());
        Assert.AreEqual(byteLength, padded.Length);
    }

    [TestMethod]
    public void ToPaddedHex_NegativeLength_ThrowsArgumentOutOfRangeException()
    {
        Hex hex = Hex.Parse("0x1234");
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => hex.ToPaddedHex(-1));
    }

    [TestMethod]
    public void ToPaddedHex_ZeroLength_ReturnsEmptyHex()
    {
        Hex hex = Hex.Parse("0x1234");
        Hex padded = hex.ToPaddedHex(0);

        Assert.AreEqual(Hex.Empty, padded);
        Assert.AreEqual("0x", padded.ToString());
    }

    [TestMethod]
    public void ToPaddedHex_SameLength_ReturnsCopy()
    {
        Hex original = Hex.Parse("0x1234");
        Hex padded = original.ToPaddedHex(2); // 0x1234 is already 2 bytes

        Assert.AreEqual(original, padded);
        Assert.AreNotSame(original, padded); // Should be a new instance
    }

    [TestMethod]
    [DataRow("0x1234", 4, "0x00001234")]   // Pad to 4 bytes
    [DataRow("0x0", 2, "0x0000")]          // Pad single zero to 2 bytes
    [DataRow("0x", 3, "0x000000")]         // Pad empty hex to 3 bytes
    public void CreatePadded_FromString_CreatesCorrectlyPaddedHex(string input, int byteLength, string expected)
    {
        Hex padded = Hex.CreatePadded(input, byteLength);

        Assert.AreEqual(expected, padded.ToString());
        Assert.AreEqual(byteLength, padded.Length);
    }

    [TestMethod]
    [DataRow("0x1234", 4, "0x00001234")]   // Pad to 4 bytes
    [DataRow("0x0", 2, "0x0000")]          // Pad single zero to 2 bytes
    [DataRow("0x", 3, "0x000000")]         // Pad empty hex to 3 bytes
    public void CreatePadded_FromHex_CreatesCorrectlyPaddedHex(string input, int byteLength, string expected)
    {
        Hex original = Hex.Parse(input);
        Hex padded = Hex.CreatePadded(original, byteLength);

        Assert.AreEqual(expected, padded.ToString());
        Assert.AreEqual(byteLength, padded.Length);
    }

    [TestMethod]
    public void CreatePadded_NullString_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => Hex.CreatePadded(null, 4));
    }

    #region BigInteger Tests

    [TestMethod]
    [DataRow("0x00", 0)]
    [DataRow("0x01", 1)]
    [DataRow("0x0A", 10)]
    [DataRow("0x10", 16)]
    [DataRow("0xFF", 255)]
    public void ToBigInteger_BasicValues_ConvertsCorrectly(string hexString, long expected)
    {
        Hex hex = Hex.Parse(hexString);
        BigInteger result = hex.ToBigInteger();
        Assert.AreEqual(new BigInteger(expected), result);
    }

    [TestMethod]
    public void ToBigInteger_EndiannessDifference_HandlesCorrectly()
    {
        // 0x1234 in big-endian is 4660 decimal (0x1234)
        // 0x1234 in little-endian is 13330 decimal (0x3412)
        Hex hex = Hex.Parse("0x1234");

        BigInteger bigEndianResult = hex.ToBigInteger(HexSignedness.Unsigned, HexEndianness.BigEndian);
        BigInteger littleEndianResult = hex.ToBigInteger(HexSignedness.Unsigned, HexEndianness.LittleEndian);

        Assert.AreEqual(new BigInteger(4660), bigEndianResult);
        Assert.AreEqual(new BigInteger(13330), littleEndianResult);
    }

    [TestMethod]
    public void ToBigInteger_SignBitHandling_WorksCorrectly()
    {
        // 0x80 has the high bit set
        // As unsigned: 128
        // As signed: -128
        Hex hex = Hex.Parse("0x80");

        BigInteger unsignedResult = hex.ToBigInteger(HexSignedness.Unsigned, HexEndianness.BigEndian);
        BigInteger signedResult = hex.ToBigInteger(HexSignedness.Signed, HexEndianness.BigEndian);

        Assert.AreEqual(new BigInteger(128), unsignedResult);
        Assert.AreEqual(new BigInteger(-128), signedResult);
    }

    [TestMethod]
    public void ToBigInteger_EmptyHex_ReturnsZero()
    {
        Hex hex = Hex.Parse("0x");
        BigInteger result = hex.ToBigInteger();
        Assert.AreEqual(BigInteger.Zero, result);
    }

    [TestMethod]
    public void ToBigInteger_Zero_ReturnsZero()
    {
        Hex hex = Hex.Parse("0x0");
        BigInteger result = hex.ToBigInteger();
        Assert.AreEqual(BigInteger.Zero, result);
    }

    [TestMethod]
    public void ToBigInteger_LargeValue_ConvertsCorrectly()
    {
        // A large hex value that exceeds standard integer types
        Hex hex = Hex.Parse("0x1234567890ABCDEF1234567890ABCDEF");
        BigInteger result = hex.ToBigInteger();

        // Calculate expected value
        BigInteger expected = BigInteger.Parse("1234567890ABCDEF1234567890ABCDEF",
                                              System.Globalization.NumberStyles.HexNumber);

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("0x00")]
    [DataRow("0x01")]
    [DataRow("0xFF")]
    [DataRow("0x1234")]
    [DataRow("0x1234567890ABCDEF")]
    public void ToBigInteger_RoundTrip_PreservesValue(string hexString)
    {
        Hex original = Hex.Parse(hexString);
        BigInteger bigInt = original.ToBigInteger();
        Hex roundTrip = Hex.FromBigInteger(bigInt);

        Assert.IsTrue(original.ValueEquals(roundTrip),
            $"Round trip failed: Original: {original}, Result: {roundTrip}");
    }

    [TestMethod]
    public void FromBigInteger_Zero_ReturnsZeroHex()
    {
        BigInteger zero = BigInteger.Zero;
        Hex result = Hex.FromBigInteger(zero);

        Assert.AreEqual(Hex.Zero, result);
        Assert.AreEqual("0x0", result.ToString());
    }

    [TestMethod]
    public void FromBigInteger_NegativeValue_PreservesValue()
    {
        BigInteger negative = new BigInteger(-128);
        Hex result = Hex.FromBigInteger(negative);

        // Converting back should give us the same value
        BigInteger roundTrip = result.ToBigInteger(HexSignedness.Signed, HexEndianness.BigEndian);
        Assert.AreEqual(negative, roundTrip);
    }

    [TestMethod]
    public void FromBigInteger_EndiannessDifference_HandlesCorrectly()
    {
        BigInteger value = new BigInteger(4660); // Decimal for 0x1234

        Hex bigEndianResult = Hex.FromBigInteger(value, HexEndianness.BigEndian);
        Hex littleEndianResult = Hex.FromBigInteger(value, HexEndianness.LittleEndian);

        Assert.AreEqual("0x1234", bigEndianResult.ToString());
        Assert.AreEqual("0x3412", littleEndianResult.ToString());
    }

    #endregion
}
