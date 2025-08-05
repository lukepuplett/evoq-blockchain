using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Evoq.Blockchain.Tests;

[TestClass]
public class HexSerializationTests
{
    private readonly JsonSerializerOptions _options;

    public HexSerializationTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new HexJsonConverter() }
        };
    }

    #region Basic Serialization Tests

    [TestMethod]
    public void Serialize_SimpleHex_SerializesToString()
    {
        // Arrange
        Hex hex = Hex.Parse("0x1234abcd");

        // Act
        string json = JsonSerializer.Serialize(hex, _options);

        // Assert
        Assert.AreEqual("\"0x1234abcd\"", json);
    }

    [TestMethod]
    public void Serialize_EmptyHex_SerializesToEmptyString()
    {
        // Arrange
        Hex hex = Hex.Empty;

        // Act
        string json = JsonSerializer.Serialize(hex, _options);

        // Assert
        Assert.AreEqual("\"0x\"", json);
    }

    [TestMethod]
    public void Serialize_ZeroHex_SerializesToZeroString()
    {
        // Arrange
        Hex hex = Hex.Zero;

        // Act
        string json = JsonSerializer.Serialize(hex, _options);

        // Assert
        Assert.AreEqual("\"0x0\"", json);
    }

    [TestMethod]
    public void Serialize_LargeHex_SerializesToString()
    {
        // Arrange
        Hex hex = Hex.Parse("0x1234567890abcdef1234567890abcdef");

        // Act
        string json = JsonSerializer.Serialize(hex, _options);

        // Assert
        Assert.AreEqual("\"0x1234567890abcdef1234567890abcdef\"", json);
    }

    #endregion

    #region Basic Deserialization Tests

    [TestMethod]
    public void Deserialize_ValidHexString_DeserializesToHex()
    {
        // Arrange
        string json = "\"0x1234abcd\"";

        // Act
        Hex hex = JsonSerializer.Deserialize<Hex>(json, _options);

        // Assert
        Assert.AreEqual("0x1234abcd", hex.ToString());
    }

    [TestMethod]
    public void Deserialize_EmptyHexString_DeserializesToEmptyHex()
    {
        // Arrange
        string json = "\"0x\"";

        // Act
        Hex hex = JsonSerializer.Deserialize<Hex>(json, _options);

        // Assert
        Assert.AreEqual(Hex.Empty, hex);
        Assert.AreEqual("0x", hex.ToString());
    }

    [TestMethod]
    public void Deserialize_ZeroHexString_DeserializesToZeroHex()
    {
        // Arrange
        string json = "\"0x0\"";

        // Act
        Hex hex = JsonSerializer.Deserialize<Hex>(json, _options);

        // Assert
        Assert.AreEqual("0x0", hex.ToString());
    }

    [TestMethod]
    public void Deserialize_HexStringWithoutPrefix_DeserializesToHex()
    {
        // Arrange
        string json = "\"1234abcd\"";

        // Act
        Hex hex = JsonSerializer.Deserialize<Hex>(json, _options);

        // Assert
        Assert.AreEqual("0x1234abcd", hex.ToString());
    }

    [TestMethod]
    public void Deserialize_LargeHexString_DeserializesToHex()
    {
        // Arrange
        string json = "\"0x1234567890abcdef1234567890abcdef\"";

        // Act
        Hex hex = JsonSerializer.Deserialize<Hex>(json, _options);

        // Assert
        Assert.AreEqual("0x1234567890abcdef1234567890abcdef", hex.ToString());
    }

    #endregion

    #region Round-trip Tests

    [TestMethod]
    [DataRow("0x")]
    [DataRow("0x0")]
    [DataRow("0x1234")]
    [DataRow("0xabcdef")]
    [DataRow("0x1234567890abcdef")]
    [DataRow("0xfedcba0987654321")]
    public void RoundTrip_SerializeDeserialize_PreservesValue(string hexString)
    {
        // Arrange
        Hex original = Hex.Parse(hexString);

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        Hex roundTrip = JsonSerializer.Deserialize<Hex>(json, _options);

        // Assert
        Assert.AreEqual(original, roundTrip);
        Assert.AreEqual(original.ToString(), roundTrip.ToString());
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public void Deserialize_InvalidHexString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"0xGG\""; // Invalid hex characters

        // Act & Assert
        Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<Hex>(json, _options));
    }

    [TestMethod]
    public void Deserialize_OddLengthHexString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"0x123\""; // Odd length

        // Act & Assert
        Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<Hex>(json, _options));
    }

    [TestMethod]
    public void Deserialize_NonStringToken_ThrowsJsonException()
    {
        // Arrange
        string json = "123"; // Number instead of string

        // Act & Assert
        Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<Hex>(json, _options));
    }

    [TestMethod]
    public void Deserialize_NullToken_ThrowsJsonException()
    {
        // Arrange
        string json = "null";

        // Act & Assert
        Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<Hex>(json, _options));
    }

    #endregion

    #region DTO Integration Tests

    public class TestDto
    {
        public Hex Hash { get; set; }
        public string Name { get; set; } = "";
        public Hex? OptionalHash { get; set; }
    }

    [TestMethod]
    public void Serialize_DtoWithHex_SerializesCorrectly()
    {
        // Arrange
        var dto = new TestDto
        {
            Hash = Hex.Parse("0x1234abcd"),
            Name = "Test",
            OptionalHash = Hex.Parse("0xdeadbeef")
        };

        // Act
        string json = JsonSerializer.Serialize(dto, _options);

        // Assert
        var expected = "{\"Hash\":\"0x1234abcd\",\"Name\":\"Test\",\"OptionalHash\":\"0xdeadbeef\"}";
        Assert.AreEqual(expected, json);
    }

    [TestMethod]
    public void Deserialize_DtoWithHex_DeserializesCorrectly()
    {
        // Arrange
        string json = "{\"Hash\":\"0x1234abcd\",\"Name\":\"Test\",\"OptionalHash\":\"0xdeadbeef\"}";

        // Act
        var dto = JsonSerializer.Deserialize<TestDto>(json, _options);

        // Assert
        Assert.IsNotNull(dto);
        Assert.AreEqual("0x1234abcd", dto.Hash.ToString());
        Assert.AreEqual("Test", dto.Name);
        Assert.IsNotNull(dto.OptionalHash);
        Assert.AreEqual("0xdeadbeef", dto.OptionalHash.Value.ToString());
    }

    [TestMethod]
    public void Serialize_DtoWithNullOptionalHex_SerializesCorrectly()
    {
        // Arrange
        var dto = new TestDto
        {
            Hash = Hex.Parse("0x1234abcd"),
            Name = "Test",
            OptionalHash = null
        };

        // Act
        string json = JsonSerializer.Serialize(dto, _options);

        // Assert
        var expected = "{\"Hash\":\"0x1234abcd\",\"Name\":\"Test\",\"OptionalHash\":null}";
        Assert.AreEqual(expected, json);
    }

    [TestMethod]
    public void Deserialize_DtoWithNullOptionalHex_DeserializesCorrectly()
    {
        // Arrange
        string json = "{\"Hash\":\"0x1234abcd\",\"Name\":\"Test\",\"OptionalHash\":null}";

        // Act
        var dto = JsonSerializer.Deserialize<TestDto>(json, _options);

        // Assert
        Assert.IsNotNull(dto);
        Assert.AreEqual("0x1234abcd", dto.Hash.ToString());
        Assert.AreEqual("Test", dto.Name);
        Assert.IsNull(dto.OptionalHash);
    }

    [TestMethod]
    public void RoundTrip_DtoWithHex_PreservesAllValues()
    {
        // Arrange
        var original = new TestDto
        {
            Hash = Hex.Parse("0x1234abcd"),
            Name = "Test",
            OptionalHash = Hex.Parse("0xdeadbeef")
        };

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        var roundTrip = JsonSerializer.Deserialize<TestDto>(json, _options);

        // Assert
        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(original.Hash, roundTrip.Hash);
        Assert.AreEqual(original.Name, roundTrip.Name);
        Assert.AreEqual(original.OptionalHash, roundTrip.OptionalHash);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Serialize_DefaultHex_SerializesToEmptyString()
    {
        // Arrange
        Hex defaultHex = default(Hex);

        // Act
        string json = JsonSerializer.Serialize(defaultHex, _options);

        // Assert
        Assert.AreEqual("\"0x\"", json);
    }

    [TestMethod]
    public void Deserialize_EmptyString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"\""; // Empty string (not "0x")

        // Act & Assert
        Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<Hex>(json, _options));
    }

    [TestMethod]
    public void Serialize_VeryLargeHex_SerializesCorrectly()
    {
        // Arrange - 32 bytes (256 bits) of data
        var bytes = new byte[32];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i % 256);
        }
        Hex hex = new Hex(bytes);

        // Act
        string json = JsonSerializer.Serialize(hex, _options);

        // Assert
        StringAssert.StartsWith(json, "\"0x");
        StringAssert.EndsWith(json, "\"");
        Assert.AreEqual(hex.ToString().Length + 2, json.Length); // +2 for quotes
    }

    #endregion

    #region Blockchain Value Tests

    [TestMethod]
    public void Serialize_EthereumAddress_SerializesCorrectly()
    {
        // Arrange - 20 byte Ethereum address
        Hex address = Hex.Parse("0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b");

        // Act
        string json = JsonSerializer.Serialize(address, _options);

        // Assert
        Assert.AreEqual("\"0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b\"", json);
    }

    [TestMethod]
    public void Serialize_EthereumTransactionHash_SerializesCorrectly()
    {
        // Arrange - 32 byte transaction hash
        Hex txHash = Hex.Parse("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef");

        // Act
        string json = JsonSerializer.Serialize(txHash, _options);

        // Assert
        Assert.AreEqual("\"0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef\"", json);
    }

    [TestMethod]
    public void RoundTrip_BlockchainValues_PreservesValues()
    {
        // Arrange - Common blockchain values
        var values = new[]
        {
            Hex.Parse("0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b"), // Address
            Hex.Parse("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"), // Hash
            Hex.Parse("0x5208"), // Gas limit
            Hex.Parse("0x02540be400"), // Gas price (fixed: was odd length)
            Hex.Parse("0x0de0b6b3a7640000") // 1 ETH in wei (fixed: was odd length)
        };

        foreach (var original in values)
        {
            // Act
            string json = JsonSerializer.Serialize(original, _options);
            Hex roundTrip = JsonSerializer.Deserialize<Hex>(json, _options);

            // Assert
            Assert.AreEqual(original, roundTrip, $"Failed for value: {original}");
        }
    }

    #endregion
}