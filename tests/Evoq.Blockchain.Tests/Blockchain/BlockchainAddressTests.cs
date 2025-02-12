using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Evoq.Blockchain.Tests;

[TestClass]
public class BlockchainAddressTests
{
    private const string ValidEthAddress = "0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2";
    private const string ValidChainId = "1";

    [TestMethod]
    public void Constructor_WithValidEthereumAddress_CreatesInstance()
    {
        // Act
        var address = new BlockchainAddress(BlockchainNamespaces.Evm, ValidChainId, ValidEthAddress);

        // Assert
        Assert.AreEqual(BlockchainNamespaces.Evm, address.Namespace);
        Assert.AreEqual(ValidChainId, address.Reference);
        Assert.AreEqual(ValidEthAddress, address.Address);
        Assert.IsTrue(address.IsEthereum);
    }

    [TestMethod]
    [DataRow(null, "1", "0x123", "Namespace")]
    [DataRow("eip155", null, "0x123", "Reference")]
    [DataRow("eip155", "1", null, "Address")]
    public void Constructor_WithNullParameters_ThrowsArgumentNullException(
        string ns, string reference, string address, string paramName)
    {
        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentNullException>(
            () => new BlockchainAddress(ns!, reference!, address!));
        Assert.AreEqual(paramName.ToLowerInvariant(), ex.ParamName);
    }

    [TestMethod]
    [DataRow("", "1", "0x123", "Namespace")]
    [DataRow("eip155", "", "0x123", "Reference")]
    [DataRow("eip155", "1", "", "Address")]
    public void Constructor_WithEmptyParameters_ThrowsInvalidBlockchainAddressException(
        string ns, string reference, string address, string fieldName)
    {
        // Act & Assert
        var ex = Assert.ThrowsException<InvalidBlockchainAddressException>(
            () => new BlockchainAddress(ns, reference, address));
        StringAssert.Contains(ex.Message, fieldName);
    }

    [TestMethod]
    [DataRow("0x123")] // Too short
    [DataRow("0xG02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2")] // Invalid hex
    [DataRow("C02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2")] // Missing 0x
    public void Constructor_WithInvalidEthereumAddress_ThrowsInvalidBlockchainAddressException(string invalidAddress)
    {
        // Act & Assert
        Assert.ThrowsException<InvalidBlockchainAddressException>(
            () => new BlockchainAddress(BlockchainNamespaces.Evm, ValidChainId, invalidAddress));
    }

    [TestMethod]
    public void Parse_ValidCaip10Address_ReturnsBlockchainAddress()
    {
        // Arrange
        var caip10 = $"{BlockchainNamespaces.Evm}:{ValidChainId}:{ValidEthAddress}";

        // Act
        var address = BlockchainAddress.Parse(caip10);

        // Assert
        Assert.AreEqual(BlockchainNamespaces.Evm, address.Namespace);
        Assert.AreEqual(ValidChainId, address.Reference);
        Assert.AreEqual(ValidEthAddress, address.Address);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("invalid")]
    [DataRow("eip155:1")] // Missing address
    [DataRow("eip155::0x123")] // Empty reference
    [DataRow("::0x123")] // Empty namespace and reference
    public void Parse_InvalidCaip10Address_ThrowsInvalidBlockchainAddressException(string invalidCaip10)
    {
        Assert.ThrowsException<InvalidBlockchainAddressException>(
            () => BlockchainAddress.Parse(invalidCaip10));
    }

    [TestMethod]
    public void TryParse_ValidCaip10Address_ReturnsTrue()
    {
        // Arrange
        var caip10 = $"{BlockchainNamespaces.Evm}:{ValidChainId}:{ValidEthAddress}";

        // Act
        bool success = BlockchainAddress.TryParse(caip10, out var address);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(address);
        Assert.AreEqual(BlockchainNamespaces.Evm, address.Value.Namespace);
        Assert.AreEqual(ValidChainId, address.Value.Reference);
        Assert.AreEqual(ValidEthAddress, address.Value.Address);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow("invalid")]
    public void TryParse_InvalidCaip10Address_ReturnsFalse(string? invalidCaip10)
    {
        // Act
        bool success = BlockchainAddress.TryParse(invalidCaip10, out var address);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(address);
    }

    [TestMethod]
    public void FromEthereum_ValidAddress_ReturnsBlockchainAddress()
    {
        // Act
        var address = BlockchainAddress.FromEthereum(ValidEthAddress, ValidChainId);

        // Assert
        Assert.AreEqual(BlockchainNamespaces.Evm, address.Namespace);
        Assert.AreEqual(ValidChainId, address.Reference);
        Assert.AreEqual(ValidEthAddress, address.Address);
    }

    [TestMethod]
    public void ToString_ReturnsValidCaip10Format()
    {
        // Arrange
        var address = new BlockchainAddress(BlockchainNamespaces.Evm, ValidChainId, ValidEthAddress);

        // Act
        string result = address.ToString();

        // Assert
        Assert.AreEqual($"{BlockchainNamespaces.Evm}:{ValidChainId}:{ValidEthAddress}", result);
    }

    [TestMethod]
    public void Equals_WithSameAddress_ReturnsTrue()
    {
        // Arrange
        var address1 = new BlockchainAddress(BlockchainNamespaces.Evm, ValidChainId, ValidEthAddress);
        var address2 = new BlockchainAddress(BlockchainNamespaces.Evm, ValidChainId, ValidEthAddress);

        // Act & Assert
        Assert.IsTrue(address1.Equals(address2));
        Assert.IsTrue(address1 == address2);
        Assert.IsFalse(address1 != address2);
    }

    [TestMethod]
    public void Equals_WithDifferentCase_ReturnsExpectedResult()
    {
        // Arrange
        var address1 = new BlockchainAddress(BlockchainNamespaces.Evm.ToUpper(), ValidChainId, ValidEthAddress);
        var address2 = new BlockchainAddress(BlockchainNamespaces.Evm.ToLower(), ValidChainId, ValidEthAddress);
        var address3 = new BlockchainAddress(BlockchainNamespaces.Evm, ValidChainId, ValidEthAddress.ToLower());

        // Act & Assert
        Assert.IsTrue(address1.Equals(address2), "Namespace should be case-insensitive");
        Assert.IsFalse(address1.Equals(address3), "Address should be case-sensitive");
    }

    [TestMethod]
    public void GetHashCode_WithSameAddress_ReturnsSameHash()
    {
        // Arrange
        var address1 = new BlockchainAddress(BlockchainNamespaces.Evm, ValidChainId, ValidEthAddress);
        var address2 = new BlockchainAddress(BlockchainNamespaces.Evm, ValidChainId, ValidEthAddress);

        // Act & Assert
        Assert.AreEqual(address1.GetHashCode(), address2.GetHashCode());
    }
}
