using System;
using System.Linq;
using System.Text.Json;
using Evoq.Blockchain.Merkle;

namespace Evoq.Blockchain.Tests.Merkle;

[TestClass]
public class MerkleV2TreeTests
{
    [TestMethod]
    public void CreateV2Tree_ShouldHaveCorrectMetadata()
    {
        // Arrange & Act
        var tree = new MerkleTree(MerkleTreeVersionStrings.V2_0);

        // Add some test leaves
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("ssn", "123-45-6789", Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("address", "123 Main St", Hex.Parse("0x112233"), MerkleTree.ComputeSha256Hash);

        // Compute the root
        tree.RecomputeSha256Root();

        // Act
        string json = tree.ToJson();

        // Assert
        // Parse the JSON to verify structure
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Verify v2.0 format
        Assert.IsTrue(root.TryGetProperty("header", out var header));
        Assert.IsTrue(root.TryGetProperty("leaves", out var leaves));
        Assert.IsTrue(root.TryGetProperty("root", out var rootHash));

        // Verify header properties with correct v2.0 values
        Assert.AreEqual(MerkleTreeHashAlgorithmStrings.Sha256, header.GetProperty("alg").GetString());
        Assert.AreEqual(MerkleTreeVersionStrings.V2_0, header.GetProperty("typ").GetString());

        // Verify leaves
        var leavesArray = leaves.EnumerateArray().ToArray();
        Assert.AreEqual(3, leavesArray.Length);

        // Verify we can parse it back
        var parsedTree = MerkleTree.Parse(json);
        Assert.IsTrue(parsedTree.VerifySha256Root(), "Parsed tree should verify correctly");

        // Verify the parsed tree maintains v2.0 metadata
        Assert.AreEqual(MerkleTreeVersionStrings.V2_0, parsedTree.Metadata.Version);
        Assert.AreEqual(MerkleTreeHashAlgorithmStrings.Sha256, parsedTree.Metadata.HashAlgorithm);
    }

    [TestMethod]
    public void RoundTrip_ShouldPreserveVersion()
    {
        // Arrange - Create a v2.0 tree
        var tree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Act - Roundtrip through JSON
        string json = tree.ToJson();
        var parsedTree = MerkleTree.Parse(json);
        string roundtrippedJson = parsedTree.ToJson();

        // Assert - Verify version is preserved
        var jsonDoc = JsonDocument.Parse(roundtrippedJson);
        var root = jsonDoc.RootElement;

        // Should still be v2.0 format
        Assert.IsTrue(root.TryGetProperty("header", out var header));
        Assert.IsFalse(root.TryGetProperty("metadata", out _));
        Assert.AreEqual(MerkleTreeVersionStrings.V2_0, header.GetProperty("typ").GetString());
        Assert.AreEqual(MerkleTreeHashAlgorithmStrings.Sha256, header.GetProperty("alg").GetString());
    }

    [TestMethod]
    public void VersionString_ShouldNotBeEscapedInJson()
    {
        // Arrange - Create a v2.0 tree
        var tree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Act - Convert to JSON
        string json = tree.ToJson();

        // Assert - Verify the version string is not escaped
        var jsonDoc = JsonDocument.Parse(json);
        var header = jsonDoc.RootElement.GetProperty("header");
        var typ = header.GetProperty("typ").GetString();

        Assert.AreEqual(MerkleTreeVersionStrings.V2_0, typ,
            "Version string should not be escaped in JSON output");
        Assert.IsFalse(json.Contains("\\u002B"),
            "Plus sign should not be escaped as Unicode in JSON output");
    }
}