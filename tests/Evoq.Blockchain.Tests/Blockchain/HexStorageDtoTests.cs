using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Evoq.Blockchain.Tests;

/// <summary>
/// Tests that demonstrate seamless Hex usage in storage DTOs without any manual conversion code.
/// </summary>
[TestClass]
public class HexStorageDtoTests
{
    private readonly JsonSerializerOptions _options;

    public HexStorageDtoTests()
    {
        // This is all that's needed to enable automatic Hex serialization!
        _options = new JsonSerializerOptions().ConfigureForHex();
    }

    #region Sample DTOs

    /// <summary>
    /// Sample transaction DTO showing how Hex properties work seamlessly in storage objects.
    /// </summary>
    public class TransactionDto
    {
        public Hex Hash { get; set; }
        public Hex From { get; set; }
        public Hex To { get; set; }
        public Hex Value { get; set; }
        public Hex GasPrice { get; set; }
        public long GasLimit { get; set; }
        public string? Data { get; set; }
        public Hex? BlockHash { get; set; } // Optional - might be null for pending transactions
    }

    /// <summary>
    /// Sample block DTO demonstrating Hex arrays and nested objects.
    /// </summary>
    public class BlockDto
    {
        public Hex Hash { get; set; }
        public Hex ParentHash { get; set; }
        public long Number { get; set; }
        public long Timestamp { get; set; }
        public Hex StateRoot { get; set; }
        public Hex TransactionsRoot { get; set; }
        public TransactionDto[] Transactions { get; set; } = Array.Empty<TransactionDto>();
    }

    /// <summary>
    /// Sample wallet DTO showing mixed usage patterns.
    /// </summary>
    public class WalletDto
    {
        public string Name { get; set; } = "";
        public Hex Address { get; set; }
        public Dictionary<string, long> TokenBalances { get; set; } = new();
        public List<Hex> WatchedAddresses { get; set; } = new();
        public Hex? LastTransactionHash { get; set; }
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void Serialize_TransactionDto_WorksSeamlessly()
    {
        // Arrange - Create a transaction DTO with Hex properties
        var transaction = new TransactionDto
        {
            Hash = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            From = "0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b",
            To = "0x8ba1f109551bd432803012645aac136c31d68965",
            Value = "0x0de0b6b3a7640000", // 1 ETH in wei
            GasPrice = "0x02540be400", // 10 Gwei
            GasLimit = 21000,
            Data = null,
            BlockHash = "0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890"
        };

        // Act - Serialize to JSON (no manual conversion needed!)
        string json = JsonSerializer.Serialize(transaction, _options);

        // Assert - Verify JSON structure
        var expectedJson = "{" +
            "\"Hash\":\"0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef\"," +
            "\"From\":\"0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b\"," +
            "\"To\":\"0x8ba1f109551bd432803012645aac136c31d68965\"," +
            "\"Value\":\"0x0de0b6b3a7640000\"," +
            "\"GasPrice\":\"0x02540be400\"," +
            "\"GasLimit\":21000," +
            "\"Data\":null," +
            "\"BlockHash\":\"0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890\"" +
            "}";

        Assert.AreEqual(expectedJson, json);
    }

    [TestMethod]
    public void Deserialize_TransactionDto_WorksSeamlessly()
    {
        // Arrange - JSON with hex strings
        var json = "{" +
            "\"Hash\":\"0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef\"," +
            "\"From\":\"0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b\"," +
            "\"To\":\"0x8ba1f109551bd432803012645aac136c31d68965\"," +
            "\"Value\":\"0x0de0b6b3a7640000\"," +
            "\"GasPrice\":\"0x02540be400\"," +
            "\"GasLimit\":21000," +
            "\"Data\":null," +
            "\"BlockHash\":\"0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890\"" +
            "}";

        // Act - Deserialize from JSON (no manual conversion needed!)
        var transaction = JsonSerializer.Deserialize<TransactionDto>(json, _options);

        // Assert - Verify all Hex properties are correctly parsed
        Assert.IsNotNull(transaction);
        Assert.AreEqual("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef", transaction.Hash.ToString());
        Assert.AreEqual("0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b", transaction.From.ToString());
        Assert.AreEqual("0x8ba1f109551bd432803012645aac136c31d68965", transaction.To.ToString());
        Assert.AreEqual("0x0de0b6b3a7640000", transaction.Value.ToString());
        Assert.AreEqual("0x02540be400", transaction.GasPrice.ToString());
        Assert.AreEqual(21000, transaction.GasLimit);
        Assert.IsNull(transaction.Data);
        Assert.IsNotNull(transaction.BlockHash);
        Assert.AreEqual("0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890", transaction.BlockHash.Value.ToString());
    }

    [TestMethod]
    public void RoundTrip_ComplexBlockDto_PreservesAllData()
    {
        // Arrange - Create a complex block with transactions
        var block = new BlockDto
        {
            Hash = "0xabc123def456abc123def456abc123def456abc123def456abc123def456abc123",
            ParentHash = "0xdef456abc123def456abc123def456abc123def456abc123def456abc123def456",
            Number = 12345678,
            Timestamp = 1640995200, // 2022-01-01 00:00:00 UTC
            StateRoot = "0x789abc123def789abc123def789abc123def789abc123def789abc123def789abc",
            TransactionsRoot = "0x456def789abc456def789abc456def789abc456def789abc456def789abc456def",
            Transactions = new[]
            {
                new TransactionDto
                {
                    Hash = "0x111222333444555666777888999aaabbbcccdddeeefffaaa111222333444",
                    From = "0x1111111111111111111111111111111111111111",
                    To = "0x2222222222222222222222222222222222222222",
                    Value = "0x01bc16d674ec800000", // 2 ETH
                    GasPrice = "0x012a05f200", // 5 Gwei
                    GasLimit = 21000,
                    Data = "Hello World",
                    BlockHash = "0xabc123def456abc123def456abc123def456abc123def456abc123def456abc123"
                },
                new TransactionDto
                {
                    Hash = "0x555666777888999aaabbbcccdddeeefffaaa111222333444555666777888",
                    From = "0x3333333333333333333333333333333333333333",
                    To = "0x4444444444444444444444444444444444444444",
                    Value = "0x", // 0 ETH (contract call)
                    GasPrice = "0x0174876e8000", // 100 Gwei
                    GasLimit = 50000,
                    Data = "0x095ea7b3", // approve() method signature
                    BlockHash = null // Pending transaction
                }
            }
        };

        // Act - Round-trip through JSON
        string json = JsonSerializer.Serialize(block, _options);
        var deserializedBlock = JsonSerializer.Deserialize<BlockDto>(json, _options);

        // Assert - Verify complete preservation
        Assert.IsNotNull(deserializedBlock);
        Assert.AreEqual(block.Hash, deserializedBlock.Hash);
        Assert.AreEqual(block.ParentHash, deserializedBlock.ParentHash);
        Assert.AreEqual(block.Number, deserializedBlock.Number);
        Assert.AreEqual(block.Timestamp, deserializedBlock.Timestamp);
        Assert.AreEqual(block.StateRoot, deserializedBlock.StateRoot);
        Assert.AreEqual(block.TransactionsRoot, deserializedBlock.TransactionsRoot);
        Assert.AreEqual(block.Transactions.Length, deserializedBlock.Transactions.Length);

        // Verify transactions
        for (int i = 0; i < block.Transactions.Length; i++)
        {
            var original = block.Transactions[i];
            var deserialized = deserializedBlock.Transactions[i];

            Assert.AreEqual(original.Hash, deserialized.Hash);
            Assert.AreEqual(original.From, deserialized.From);
            Assert.AreEqual(original.To, deserialized.To);
            Assert.AreEqual(original.Value, deserialized.Value);
            Assert.AreEqual(original.GasPrice, deserialized.GasPrice);
            Assert.AreEqual(original.GasLimit, deserialized.GasLimit);
            Assert.AreEqual(original.Data, deserialized.Data);
            Assert.AreEqual(original.BlockHash, deserialized.BlockHash);
        }
    }

    [TestMethod]
    public void Serialize_WalletWithCollections_HandlesComplexStructures()
    {
        // Arrange - Wallet with complex Hex usage patterns
        var wallet = new WalletDto
        {
            Name = "My Main Wallet",
            Address = "0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b",
            TokenBalances = new Dictionary<string, long>
            {
                { "0x0000000000000000000000000000000000000000", 1000000000000000000 }, // ETH
                { "0xA0b86a33E6411a3D2E16b6bd7d05a8b6e5b6fE16", 5000000 }, // USDC
                { "0x514910771AF9Ca656af840dff83E8264EcF986CA", 100000000000000000 } // LINK
            },
            WatchedAddresses = new List<Hex>
            {
                "0x1111111111111111111111111111111111111111",
                "0x2222222222222222222222222222222222222222",
                "0x3333333333333333333333333333333333333333"
            },
            LastTransactionHash = "0x999888777666555444333222111aaabbbcccdddeeefffaaa99988877766611"
        };

        // Act
        string json = JsonSerializer.Serialize(wallet, _options);

        // Assert - Verify structure (basic validation - full JSON would be quite long)
        Assert.IsTrue(json.Contains("\"Address\":\"0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b\""));
        Assert.IsTrue(json.Contains("\"0x0000000000000000000000000000000000000000\":1000000000000000000"));
        Assert.IsTrue(json.Contains("\"0x1111111111111111111111111111111111111111\""));
        Assert.IsTrue(json.Contains("\"LastTransactionHash\":\"0x999888777666555444333222111aaabbbcccdddeeefffaaa99988877766611\""));

        // Verify round-trip
        var deserializedWallet = JsonSerializer.Deserialize<WalletDto>(json, _options);
        Assert.IsNotNull(deserializedWallet);
        Assert.AreEqual(wallet.Address, deserializedWallet.Address);
        Assert.AreEqual(wallet.TokenBalances.Count, deserializedWallet.TokenBalances.Count);
        Assert.AreEqual(wallet.WatchedAddresses.Count, deserializedWallet.WatchedAddresses.Count);
        Assert.AreEqual(wallet.LastTransactionHash, deserializedWallet.LastTransactionHash);
    }

    [TestMethod]
    public void HandlesNullOptionalHexProperties_Gracefully()
    {
        // Arrange - Transaction with null optional properties
        var transaction = new TransactionDto
        {
            Hash = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            From = "0x742d35cc6634c0532925a3b8d0b9ebb6d0bfed8b",
            To = "0x8ba1f109551bd432803012645aac136c31d68965",
            Value = "0x0de0b6b3a7640000",
            GasPrice = "0x02540be400",
            GasLimit = 21000,
            Data = null,
            BlockHash = null // This is the key test - null optional Hex
        };

        // Act - Round-trip
        string json = JsonSerializer.Serialize(transaction, _options);
        var deserializedTransaction = JsonSerializer.Deserialize<TransactionDto>(json, _options);

        // Assert
        Assert.IsNotNull(deserializedTransaction);
        Assert.AreEqual(transaction.Hash, deserializedTransaction.Hash);
        Assert.IsNull(deserializedTransaction.BlockHash);
        Assert.IsTrue(json.Contains("\"BlockHash\":null"));
    }

    #endregion

    #region Demonstration Tests

    [TestMethod]
    public void DemonstrateEaseOfUse_NoManualConversion()
    {
        // This test demonstrates how easy it is to work with Hex in DTOs
        
        // 1. Create DTO with natural Hex usage - no string conversions needed!
        var dto = new TransactionDto
        {
            Hash = "0x1234abcd", // Implicit conversion from string
            From = Hex.Parse("0x5678efab"), // Explicit parsing
            To = new Hex(new byte[] { 0x9a, 0xbc, 0xde }), // From bytes
            Value = "0x01bc16d674ec800000", // 2 ETH - just assign the string!
            GasPrice = "0x02540be400", // 10 Gwei - natural usage
            GasLimit = 21000
        };

        // 2. Serialize - completely automatic, no custom code needed
        string json = JsonSerializer.Serialize(dto, _options);

        // 3. Deserialize - completely automatic, Hex properties just work
        var roundTrip = JsonSerializer.Deserialize<TransactionDto>(json, _options);

        // 4. Use the Hex values naturally - they're fully functional Hex objects
        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(4, roundTrip.Hash.Length); // 4 bytes
        Assert.IsTrue(roundTrip.Value.ToBigInteger() > 0); // Can use BigInteger conversion
        Assert.AreEqual("0x5678efab", roundTrip.From.ToString()); // String representation

        // 5. The consuming application doesn't need to worry about Hex serialization at all!
        // Storage DTOs with Hex properties "just work" with no additional code.
    }

    #endregion
}