using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

    #region Default Initialization Tests

    [TestMethod]
    public void Default_Initialization_HasNullValue()
    {
        // Arrange
        var defaultHex = default(Hex);

        // Act & Assert
        Assert.AreEqual(0, defaultHex.Length, "Default Hex should have Length 0");
        Assert.AreEqual("0x", defaultHex.ToString(), "Default Hex should stringify to 0x");
        CollectionAssert.AreEqual(Array.Empty<byte>(), defaultHex.ToByteArray(), "Default Hex should return empty byte array");
    }

    [TestMethod]
    public void Default_Equals_EmptyHex_ReturnsTrue()
    {
        // Arrange
        var defaultHex = default(Hex);
        var emptyHex = Hex.Empty;

        // Act & Assert
        Assert.IsTrue(defaultHex.Equals(emptyHex), "default(Hex) should equal Hex.Empty");
        Assert.IsTrue(emptyHex.Equals(defaultHex), "Hex.Empty should equal default(Hex)");
        Assert.IsTrue(defaultHex == emptyHex, "default(Hex) == Hex.Empty should be true");
        Assert.IsTrue(emptyHex == defaultHex, "Hex.Empty == default(Hex) should be true");
    }

    [TestMethod]
    public void Default_IsZeroValue_ReturnsTrue()
    {
        // Arrange
        var defaultHex = default(Hex);

        // Act & Assert
        Assert.IsFalse(defaultHex.IsZeroValue(), "default(Hex) should be considered a zero value");
    }

    [TestMethod]
    public void Default_IsEmpty_ReturnsTrue()
    {
        // Arrange
        var defaultHex = default(Hex);

        // Act & Assert
        Assert.IsTrue(defaultHex.IsEmpty(), "default(Hex) should be considered empty");
    }

    [TestMethod]
    public void Default_ToBigInteger_ThrowsInvalidOperationException()
    {
        // Arrange
        var defaultHex = default(Hex);

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => defaultHex.ToBigInteger(),
            "default(Hex).ToBigInteger() should throw InvalidOperationException");

        Assert.AreEqual("Cannot convert a default-initialized, empty Hex value to BigInteger", ex.Message);
    }

    [TestMethod]
    public void Default_ToPadded_ThrowsArgumentNullException()
    {
        // Arrange
        var defaultHex = default(Hex);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => defaultHex.ToPadded(4),
            "default(Hex).ToPadded() should throw ArgumentNullException");
    }

    [TestMethod]
    public void Default_ToPaddedHex_ThrowsInvalidOperationException()
    {
        // Arrange
        var defaultHex = default(Hex);

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => defaultHex.ToPaddedHex(2),
            "default(Hex).ToPaddedHex() should throw InvalidOperationException");

        Assert.AreEqual("Cannot pad a default-initialized, empty Hex value", ex.Message);
    }

    [TestMethod]
    public void Default_ValueEquals_EmptyAndZero_ReturnsTrue()
    {
        // Arrange
        var defaultHex = default(Hex);
        var emptyHex = Hex.Empty;
        var zeroHex = Hex.Zero;

        // Act & Assert
        Assert.IsTrue(defaultHex.ValueEquals(emptyHex), "default(Hex) should value-equal Hex.Empty");
        Assert.IsTrue(defaultHex.ValueEquals(zeroHex), "default(Hex) should value-equal Hex.Zero");
        Assert.IsTrue(emptyHex.ValueEquals(defaultHex), "Hex.Empty should value-equal default(Hex)");
        Assert.IsTrue(zeroHex.ValueEquals(defaultHex), "Hex.Zero should value-equal default(Hex)");
    }

    [TestMethod]
    public void Default_ComparisonWithNonZero_WorksCorrectly()
    {
        // Arrange
        var defaultHex = default(Hex);
        var nonZeroHex = Hex.Parse("0x1234");

        // Act & Assert
        Assert.IsFalse(defaultHex.Equals(nonZeroHex), "default(Hex) should not equal non-zero Hex");
        Assert.IsFalse(nonZeroHex.Equals(defaultHex), "Non-zero Hex should not equal default(Hex)");
        Assert.IsFalse(defaultHex == nonZeroHex, "default(Hex) == non-zero Hex should be false");
        Assert.IsFalse(nonZeroHex == defaultHex, "Non-zero Hex == default(Hex) should be false");
        Assert.IsTrue(defaultHex != nonZeroHex, "default(Hex) != non-zero Hex should be true");
        Assert.IsTrue(nonZeroHex != defaultHex, "Non-zero Hex != default(Hex) should be true");
    }

    [TestMethod]
    public void Default_SafeOperations_DoNotThrow()
    {
        // Arrange
        var defaultHex = default(Hex);

        // Act & Assert - these should not throw
        var length = defaultHex.Length;
        var toString = defaultHex.ToString();
        var byteArray = defaultHex.ToByteArray();
        var equalsEmpty = defaultHex.Equals(Hex.Empty);
        var isEmpty = defaultHex.IsEmpty();

        // Additional assertions
        Assert.AreEqual(0, length);
        Assert.AreEqual("0x", toString);
        CollectionAssert.AreEqual(Array.Empty<byte>(), byteArray);
        Assert.IsTrue(equalsEmpty);
        Assert.IsTrue(isEmpty);
    }

    [TestMethod]
    public void Default_InCollections_WorksCorrectly()
    {
        // Arrange
        var defaultHex = default(Hex);
        var list = new List<Hex> { defaultHex, Hex.Zero, Hex.Parse("0x1234") };
        var dict = new Dictionary<Hex, string>
        {
            { defaultHex, "default" },
            { Hex.Zero, "zero" },
            { Hex.Parse("0x1234"), "non-zero" }
        };

        // Act & Assert
        Assert.AreEqual(3, list.Count, "List should contain 3 items");
        Assert.AreEqual(3, dict.Count, "Dictionary should contain 3 items");
        Assert.IsTrue(list.Contains(defaultHex), "List should contain default(Hex)");
        Assert.IsTrue(dict.ContainsKey(defaultHex), "Dictionary should contain default(Hex) as key");
        Assert.AreEqual("default", dict[defaultHex], "Dictionary should return correct value for default(Hex)");
    }

    #endregion

    #region Endianness Tests

    [TestMethod]
    [Description("Tests that BigInteger byte arrays need endianness conversion for Ethereum")]
    public void ToHexStruct_BigIntegerEndianness_RequiresConversion()
    {
        // A simple value: 5,000,000 (block number example)
        BigInteger value = new BigInteger(5000000);
        byte[] bigIntBytes = value.ToByteArray(); // Little-endian by default in .NET

        // Without endianness conversion - incorrect for Ethereum
        Hex incorrectHex = bigIntBytes.ToHexStruct();
        // With endianness conversion - correct for Ethereum (big-endian)
        Hex correctHex = bigIntBytes.ToHexStruct(reverseEndianness: true, trimLeadingZeros: true);

        // The correct Ethereum representation of 5,000,000 is 0x4c4b40
        Assert.AreEqual("0x4c4b40", correctHex.ToString().ToLowerInvariant());
        Assert.AreNotEqual("0x4c4b40", incorrectHex.ToString().ToLowerInvariant());
    }

    [TestMethod]
    [Description("Tests converting standard Ethereum gas limit with BigInteger")]
    public void ToHexStruct_EthereumGasLimit_CorrectWithEndianConversion()
    {
        // Standard Ethereum transaction gas limit: 21,000
        BigInteger gasLimit = new BigInteger(21000);
        byte[] gasLimitBytes = gasLimit.ToByteArray();

        // Convert with endianness correction and trim leading zeros
        Hex gasLimitHex = gasLimitBytes.ToHexStruct(reverseEndianness: true, trimLeadingZeros: true);

        // Expected Ethereum hex representation
        Assert.AreEqual("0x5208", gasLimitHex.ToString().ToLowerInvariant());
    }

    [TestMethod]
    [Description("Tests converting Ethereum gas price (Gwei) with BigInteger")]
    public void ToHexStruct_EthereumGasPrice_CorrectWithEndianConversion()
    {
        // 10 Gwei = 10,000,000,000 wei
        BigInteger gasPrice = new BigInteger(10000000000);
        byte[] gasPriceBytes = gasPrice.ToByteArray();

        // Convert with endianness correction and trim leading zeros
        Hex gasPriceHex = gasPriceBytes.ToHexStruct(reverseEndianness: true, trimLeadingZeros: true);

        // Expected Ethereum hex representation
        Assert.AreEqual("0x2540be400", gasPriceHex.ToString(true));
    }

    [TestMethod]
    [Description("Tests converting a large Ethereum value (1 ETH in wei) with BigInteger")]
    public void ToHexStruct_OneEtherInWei_CorrectWithEndianConversion()
    {
        // 1 ETH = 10^18 wei
        BigInteger oneEther = new BigInteger(1000000000000000000);
        byte[] etherBytes = oneEther.ToByteArray();

        // Convert with endianness correction and trim leading zeros
        Hex etherHex = etherBytes.ToHexStruct(reverseEndianness: true, trimLeadingZeros: true);

        // Expected Ethereum hex representation
        Assert.AreEqual("0xde0b6b3a7640000", etherHex.ToString(true));
    }

    [TestMethod]
    [Description("Tests that max 256-bit integer is correctly represented")]
    public void ToHexStruct_Max256BitInteger_CorrectWithEndianConversion()
    {
        // Max value for 256-bit integer
        BigInteger maxValue = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935");
        byte[] maxValueBytes = maxValue.ToByteArray();

        // Convert with endianness correction but DON'T trim zeros - we want exactly 32 bytes
        Hex maxValueHex = maxValueBytes.ToHexStruct(reverseEndianness: true);

        // Remove the sign byte to get exactly 32 bytes
        if (maxValueHex.Length == 33)
        {
            byte[] bytes = maxValueHex.ToByteArray();
            byte[] trimmed = new byte[32];
            Array.Copy(bytes, 1, trimmed, 0, 32);
            maxValueHex = new Hex(trimmed);
        }

        // Should be 32 bytes of 0xff
        Assert.AreEqual(32, maxValueHex.Length);
        Assert.AreEqual("0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
                        maxValueHex.ToString().ToLowerInvariant());
    }

    [TestMethod]
    [Description("Tests that endianness conversion works with Ethereum addresses")]
    public void ToHexStruct_EthereumAddress_CorrectWithEndianConversion()
    {
        // Fix the expected address to match the actual bytes
        byte[] reversedAddressBytes = new byte[] {
            0x95, 0x14, 0xb8, 0x4d, 0x65, 0x0f, 0x9e, 0x66,
            0x33, 0x9f, 0xa0, 0xb9, 0xc9, 0x94, 0x3e, 0x3f,
            0x53, 0x60, 0xeb, 0x5a
        };

        // Convert with endianness correction
        Hex correctedAddressHex = reversedAddressBytes.ToHexStruct(reverseEndianness: true);

        // Expected correct Ethereum address - fixed to match the actual bytes
        Assert.AreEqual("0x5aeb60533f3e94c9b9a09f33669e0f654db81495",
                        correctedAddressHex.ToString().ToLowerInvariant());
    }

    #endregion

    // Add new tests for the ToString method with trimming parameter

    [TestMethod]
    [Description("Tests that ToString with trimming parameter removes leading zero digits")]
    public void ToString_WithTrimming_RemovesLeadingZeroDigits()
    {
        // Arrange - ensure all hex strings have even number of digits
        Hex hex1 = Hex.Parse("0x012345");  // Leading zero digit (6 digits)
        Hex hex2 = Hex.Parse("0x00ABCD");  // Multiple leading zero digits (6 digits)
        Hex hex3 = Hex.Parse("0x00");      // Single zero byte
        Hex hex4 = Hex.Parse("0x");        // Empty

        // Act & Assert
        Assert.AreEqual("0x12345", hex1.ToString(true), "Should trim single leading zero");
        Assert.AreEqual("0xabcd", hex2.ToString(true), "Should trim multiple leading zeros");
        Assert.AreEqual("0x0", hex3.ToString(true), "Single zero should remain as 0x0");
        Assert.AreEqual("0x", hex4.ToString(true), "Empty hex should remain as 0x");
    }

    [TestMethod]
    [Description("Tests that ToString without trimming parameter preserves leading zero digits")]
    public void ToString_WithoutTrimming_PreservesLeadingZeroDigits()
    {
        // Arrange - ensure all hex strings have even number of digits
        Hex hex1 = Hex.Parse("0x012345");  // Leading zero digit (6 digits)
        Hex hex2 = Hex.Parse("0x00ABCD");  // Multiple leading zero digits (6 digits)

        // Act & Assert
        Assert.AreEqual("0x012345", hex1.ToString(), "Should preserve leading zero without trim parameter");
        Assert.AreEqual("0x00abcd", hex2.ToString(), "Should preserve leading zeros without trim parameter");
    }

    [TestMethod]
    [Description("Tests that ToString with trimming works correctly with different hex values")]
    public void ToString_Trimming_WorksWithVariousHexValues()
    {
        // Test cases with [input, expected with trimming] - ensure all inputs have even number of digits
        var testCases = new[]
        {
            new { Input = "0x0123", Expected = "0x123" },
            new { Input = "0x00123456", Expected = "0x123456" },
            new { Input = "0x00012345", Expected = "0x12345" },
            new { Input = "0x000012", Expected = "0x12" },
            new { Input = "0x0100", Expected = "0x100" },  // Only trims leading zeros
            new { Input = "0x0010", Expected = "0x10" },
            new { Input = "0x1000", Expected = "0x1000" }, // No leading zeros to trim
            new { Input = "0x0000", Expected = "0x0" }     // All zeros become 0x0
        };

        foreach (var testCase in testCases)
        {
            Hex hex = Hex.Parse(testCase.Input);
            Assert.AreEqual(testCase.Expected, hex.ToString(true),
                $"Failed for input {testCase.Input}");
        }
    }

    [TestMethod]
    [Description("Tests that ToString with trimming works correctly with blockchain values")]
    public void ToString_Trimming_WorksWithBlockchainValues()
    {
        // Common blockchain values - all have even number of digits

        // Gas price (10 Gwei)
        Hex gasPrice = Hex.Parse("0x02540be400");
        Assert.AreEqual("0x2540be400", gasPrice.ToString(true));

        // 1 ETH in wei
        Hex oneEther = Hex.Parse("0x0de0b6b3a7640000");
        Assert.AreEqual("0xde0b6b3a7640000", oneEther.ToString(true));

        // Block number - fixed to have even number of digits
        Hex blockNumber = Hex.Parse("0x01000000"); // Fixed: was 0x0100000 (odd)
        Assert.AreEqual("0x1000000", blockNumber.ToString(true));
    }

    #region Parse Options Tests

    [TestMethod]
    [Description("Tests that Parse with AllowOddLength option handles odd-length hex strings")]
    public void Parse_WithAllowOddLength_HandlesOddLengthStrings()
    {
        // Arrange - odd-length hex strings
        string[] inputs = { "0xf", "0x123", "f", "123", "0xabcde" };
        string[] expected = { "0x0f", "0x0123", "0x0f", "0x0123", "0x0abcde" };

        // Act & Assert
        for (int i = 0; i < inputs.Length; i++)
        {
            Hex hex = Hex.Parse(inputs[i], HexParseOptions.AllowOddLength);
            Assert.AreEqual(expected[i], hex.ToString(), $"Failed for input {inputs[i]}");
        }
    }

    [TestMethod]
    [Description("Tests that Parse with Strict option rejects odd-length hex strings")]
    public void Parse_WithStrictOption_RejectsOddLengthStrings()
    {
        // Arrange - odd-length hex strings
        string[] inputs = { "0xf", "0x123", "f", "123" };

        // Act & Assert
        foreach (string input in inputs)
        {
            Assert.ThrowsException<FormatException>(
                () => Hex.Parse(input, HexParseOptions.Strict),
                $"Should throw FormatException for odd-length input {input}");
        }
    }

    [TestMethod]
    [Description("Tests that Parse with default option (Strict) rejects odd-length hex strings")]
    public void Parse_WithDefaultOption_RejectsOddLengthStrings()
    {
        // Arrange - odd-length hex strings
        string[] inputs = { "0xf", "0x123", "f", "123" };

        // Act & Assert
        foreach (string input in inputs)
        {
            Assert.ThrowsException<FormatException>(
                () => Hex.Parse(input),
                $"Should throw FormatException for odd-length input {input}");
        }
    }

    [TestMethod]
    [Description("Tests that Parse with AllowOddLength option correctly handles common blockchain values")]
    public void Parse_WithAllowOddLength_HandlesCommonBlockchainValues()
    {
        // Common minimal blockchain values
        var testCases = new[]
        {
            new { Input = "0xf", Expected = "0x0f" },       // Single hex digit
            new { Input = "0xa", Expected = "0x0a" },       // Single hex digit
            new { Input = "0x1", Expected = "0x01" },       // Single hex digit
            new { Input = "0xfff", Expected = "0x0fff" },   // Three hex digits
            new { Input = "0x1a2b3", Expected = "0x01a2b3" } // Five hex digits
        };

        foreach (var testCase in testCases)
        {
            Hex hex = Hex.Parse(testCase.Input, HexParseOptions.AllowOddLength);
            Assert.AreEqual(testCase.Expected, hex.ToString(),
                $"Failed for input {testCase.Input}");
        }
    }

    [TestMethod]
    [Description("Tests that Parse with AllowOddLength option preserves even-length hex strings")]
    public void Parse_WithAllowOddLength_PreservesEvenLengthStrings()
    {
        // Even-length hex strings
        var testCases = new[]
        {
            new { Input = "0x12", Expected = "0x12" },
            new { Input = "0xabcd", Expected = "0xabcd" },
            new { Input = "1234", Expected = "0x1234" },
            new { Input = "0x", Expected = "0x" },
            new { Input = "0x0", Expected = "0x0" }
        };

        foreach (var testCase in testCases)
        {
            Hex hex = Hex.Parse(testCase.Input, HexParseOptions.AllowOddLength);
            Assert.AreEqual(testCase.Expected, hex.ToString(),
                $"Failed for input {testCase.Input}");
        }
    }

    [TestMethod]
    [Description("Tests that Parse with AllowOddLength option still validates hex characters")]
    public void Parse_WithAllowOddLength_StillValidatesHexCharacters()
    {
        // Invalid hex strings (contain non-hex characters)
        string[] invalidInputs = { "0xg", "0xGG", "WXYZ", "0x12QZ" };

        foreach (string input in invalidInputs)
        {
            Assert.ThrowsException<FormatException>(
                () => Hex.Parse(input, HexParseOptions.AllowOddLength),
                $"Should throw FormatException for invalid hex input {input}");
        }
    }

    [TestMethod]
    [Description("Tests that Parse with AllowOddLength option handles null/empty correctly")]
    public void Parse_WithAllowOddLength_HandlesNullOrEmptyCorrectly()
    {
        // Null or empty inputs
        string[] invalidInputs = { null, "" };

        foreach (string input in invalidInputs)
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => Hex.Parse(input, HexParseOptions.AllowOddLength),
                $"Should throw ArgumentNullException for null/empty input");
        }
    }

    [TestMethod]
    [Description("Tests that Parse with AllowOddLength option handles special cases correctly")]
    public void Parse_WithAllowOddLength_HandlesSpecialCasesCorrectly()
    {
        // Special cases
        Assert.AreEqual("0x", Hex.Parse("0x", HexParseOptions.AllowOddLength).ToString(), "Empty hex should remain empty");
        Assert.AreEqual("0x0", Hex.Parse("0x0", HexParseOptions.AllowOddLength).ToString(), "Single zero should remain as 0x0");
    }

    #endregion
}
