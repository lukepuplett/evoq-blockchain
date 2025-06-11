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

    [TestMethod]
    public void AddPrivateLeaf_ShouldCreateLeafWithOnlyHash()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var hash = Hex.Parse("0x1234567890abcdef");

        // Act
        var leaf = tree.AddPrivateLeaf(hash);
        tree.RecomputeSha256Root();

        // Assert
        Assert.IsTrue(leaf.IsPrivate, "Private leaf should be marked as private");
        Assert.AreEqual(hash, leaf.Hash, "Hash should match the provided hash");
        Assert.IsTrue(leaf.Data.IsEmpty(), "Data should be empty");
        Assert.IsTrue(leaf.Salt.IsEmpty(), "Salt should be empty");
        Assert.AreEqual(string.Empty, leaf.ContentType, "Content type should be empty");

        // Verify the leaf is properly serialized
        string json = tree.ToJson(MerkleTree.ComputeSha256Hash);

        Console.WriteLine(json);

        var jsonDoc = JsonDocument.Parse(json);
        var leaves = jsonDoc.RootElement.GetProperty("leaves").EnumerateArray().ToArray();

        Assert.AreEqual(1, leaves.Length, "Should have one leaf");
        var leafJson = leaves[0];

        Assert.IsTrue(leafJson.TryGetProperty("hash", out var hashProp), "Should have hash property");
        Assert.AreEqual(hash.ToString(), hashProp.GetString(), "Hash should match");
        Assert.IsFalse(leafJson.TryGetProperty("data", out _), "Should not have data property");
        Assert.IsFalse(leafJson.TryGetProperty("salt", out _), "Should not have salt property");
        Assert.IsFalse(leafJson.TryGetProperty("contentType", out _), "Should not have contentType property");
    }

    [TestMethod]
    public void RoundTrip_WithPrivateLeaf_ShouldPreservePrivacy()
    {
        // Arrange - Create a tree with one private leaf
        var tree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var hash = Hex.Parse("0x1234567890abcdef");
        var leaf = tree.AddPrivateLeaf(hash);
        tree.RecomputeSha256Root();

        // Act - Roundtrip through JSON
        string json = tree.ToJson();
        var parsedTree = MerkleTree.Parse(json);

        // Assert
        Assert.AreEqual(1, parsedTree.Leaves.Count, "Should have one leaf after roundtrip");
        var roundtrippedLeaf = parsedTree.Leaves[0];

        // Verify the leaf is still private
        Assert.IsTrue(roundtrippedLeaf.IsPrivate, "Leaf should still be private after roundtrip");
        Assert.AreEqual(hash, roundtrippedLeaf.Hash, "Hash should be preserved");
        Assert.IsTrue(roundtrippedLeaf.Data.IsEmpty(), "Data should still be empty");
        Assert.IsTrue(roundtrippedLeaf.Salt.IsEmpty(), "Salt should still be empty");
        Assert.AreEqual(string.Empty, roundtrippedLeaf.ContentType, "Content type should be empty.");

        // Verify the tree still validates
        Assert.IsTrue(parsedTree.VerifySha256Root(), "Tree should still verify after roundtrip");

        // Verify the JSON still only contains the hash
        string roundtrippedJson = parsedTree.ToJson();
        var jsonDoc = JsonDocument.Parse(roundtrippedJson);
        var leaves = jsonDoc.RootElement.GetProperty("leaves").EnumerateArray().ToArray();

        Assert.AreEqual(1, leaves.Length, "Should have one leaf in JSON");
        var leafJson = leaves[0];

        Assert.IsTrue(leafJson.TryGetProperty("hash", out var hashProp), "Should have hash property");
        Assert.AreEqual(hash.ToString(), hashProp.GetString(), "Hash should match");
        Assert.IsFalse(leafJson.TryGetProperty("data", out _), "Should not have data property");
        Assert.IsFalse(leafJson.TryGetProperty("salt", out _), "Should not have salt property");
        Assert.IsFalse(leafJson.TryGetProperty("contentType", out _), "Should not have contentType property");
    }
}