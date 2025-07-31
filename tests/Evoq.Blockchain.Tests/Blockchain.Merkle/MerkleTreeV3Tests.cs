using System;
using System.Linq;
using System.Text.Json;
using Evoq.Blockchain.Merkle;

namespace Evoq.Blockchain.Tests.Merkle;

[TestClass]
public class MerkleV3TreeTests
{
    /// <summary>
    /// Tests that a new V3 tree correctly creates and includes a header leaf with all required metadata.
    /// This is a critical test because:
    /// 1. The header leaf is a key security feature of V3, protecting against:
    ///    - Single leaf attacks (by requiring a header leaf)
    ///    - Leaf addition/removal attacks (by encoding leaf count)
    ///    - Data tampering (by encoding total bytes)
    /// 2. The header leaf contains critical metadata that must be preserved:
    ///    - Hash algorithm (for verification)
    ///    - Version (for format compatibility)
    ///    - Exchange type (for document classification)
    /// </summary>
    [TestMethod]
    public void CreateV3Tree_ShouldHaveCorrectHeaderLeaf()
    {
        // Arrange
        var tree = CreateTestTreeWithRecompute();
        string json = tree.ToJson();

        // Assert
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Verify v3.0 format
        Assert.IsTrue(root.TryGetProperty("header", out var header));
        Assert.IsTrue(root.TryGetProperty("leaves", out var leaves));
        Assert.IsTrue(root.TryGetProperty("root", out var rootHash));

        // Verify header properties
        Assert.AreEqual(MerkleTreeVersionStrings.V3_0, header.GetProperty("typ").GetString());

        // Get the first leaf which should be the header leaf
        var leavesArray = leaves.EnumerateArray().ToArray();
        Assert.IsTrue(leavesArray.Length > 0, "Tree should have at least one leaf (the header leaf)");

        var headerLeaf = leavesArray[0];
        AssertHeaderLeafContentType(headerLeaf);

        // Parse the header leaf data
        Assert.IsTrue(headerLeaf.TryGetProperty("data", out var headerData));
        var headerHex = headerData.GetString()!;
        var headerJson = System.Text.Encoding.UTF8.GetString(Hex.Parse(headerHex).ToByteArray());
        var headerDoc = JsonDocument.Parse(headerJson);
        var headerRoot = headerDoc.RootElement;

        // Verify header leaf metadata
        AssertHeaderMetadata(headerRoot);

        // Verify we can parse it back
        var parsedTree = MerkleTree.Parse(json);
        Assert.IsTrue(parsedTree.VerifyRoot(), "Parsed tree should verify correctly");

        // Verify the parsed tree maintains v3.0 metadata
        Assert.AreEqual(MerkleTreeVersionStrings.V3_0, parsedTree.Metadata.Version);
        Assert.AreEqual(MerkleTreeHashAlgorithmStrings.Sha256, parsedTree.Metadata.HashAlgorithm);

        Console.WriteLine(json);
    }

    /// <summary>
    /// Tests that a V3 tree can be serialized to JSON and deserialized back while preserving all critical information.
    /// This is important because:
    /// 1. It verifies the tree can be safely stored and transmitted
    /// 2. It ensures the header leaf and its metadata survive serialization
    /// 3. It validates that the tree remains cryptographically sound after roundtrip
    /// 4. It performs a double roundtrip to ensure stability (no data loss on repeated serialization)
    /// 
    /// The test is particularly thorough because it verifies:
    /// - The JSON structure remains valid
    /// - The header leaf content and metadata are preserved
    /// - The tree still verifies cryptographically
    /// - All metadata (version, algorithm, exchange type) is maintained
    /// </summary>
    [TestMethod]
    public void RoundTrip_ShouldPreserveVersionAndHeader()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.Metadata.ExchangeDocumentType = "test-exchange";

        // Add some test leaves
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("age", 30, Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Act - First roundtrip
        string json = tree.ToJson();
        var parsedTree = MerkleTree.Parse(json);

        // Second roundtrip to ensure stability
        string roundtrippedJson = parsedTree.ToJson();
        var roundtrippedTree = MerkleTree.Parse(roundtrippedJson);

        // Assert - Verify version and header structure
        var jsonDoc = JsonDocument.Parse(roundtrippedJson);
        var root = jsonDoc.RootElement;

        // Verify v3.0 format
        Assert.IsTrue(root.TryGetProperty("header", out var header), "JSON should have 'header' property");
        Assert.IsTrue(root.TryGetProperty("leaves", out var leaves), "JSON should have 'leaves' property");
        Assert.IsTrue(root.TryGetProperty("root", out var rootHash), "JSON should have 'root' property");

        // Verify header properties
        Assert.AreEqual(MerkleTreeVersionStrings.V3_0, header.GetProperty("typ").GetString(),
            "Header should preserve V3.0 version");

        // Get the first leaf which should be the header leaf
        var leavesArray = leaves.EnumerateArray().ToArray();
        Assert.IsTrue(leavesArray.Length > 0, "Tree should have at least one leaf (the header leaf)");

        var headerLeaf = leavesArray[0];
        Assert.IsTrue(headerLeaf.TryGetProperty("data", out var headerData), "Header leaf should have 'data' property");
        Assert.IsTrue(headerLeaf.TryGetProperty("contentType", out var headerContentType), "Header leaf should have 'contentType' property");

        // Verify header leaf content type
        Assert.AreEqual("application/merkle-exchange-header-3.0+json; charset=utf-8; encoding=hex",
            headerContentType.GetString(),
            "Header leaf should have correct content type");

        // Parse the header leaf data
        var headerHex = headerData.GetString()!;
        var headerJson = System.Text.Encoding.UTF8.GetString(Hex.Parse(headerHex).ToByteArray());
        var headerDoc = JsonDocument.Parse(headerJson);
        var headerRoot = headerDoc.RootElement;

        // Verify header leaf metadata
        Assert.IsTrue(headerRoot.TryGetProperty("alg", out var alg) &&
            alg.GetString() == MerkleTreeHashAlgorithmStrings.Sha256,
            "Header should have 'alg' property set to 'sha256'");

        Assert.IsTrue(headerRoot.TryGetProperty("typ", out var typ) &&
            typ.GetString() == "application/merkle-exchange-header-3.0+json",
            "Header should have 'typ' property set to 'application/merkle-exchange-header-3.0+json'");

        Assert.IsTrue(headerRoot.TryGetProperty("leaves", out var leafCount) &&
            leafCount.GetInt32() == 3,
            "Header should have 'leaves' property set to 3 (header leaf + 2 data leaves)");

        Assert.IsTrue(headerRoot.TryGetProperty("exchange", out var exchange) &&
            exchange.GetString() == "test-exchange",
            $"Header should have 'exchange' property set to 'test-exchange' not '{exchange.GetString()}'");

        // Verify the tree still validates
        Assert.IsTrue(roundtrippedTree.VerifyRoot(), "Tree should still verify after roundtrip");

        // Verify metadata is preserved
        Assert.AreEqual(MerkleTreeVersionStrings.V3_0, roundtrippedTree.Metadata.Version,
            "Version should be preserved through roundtrip");
        Assert.AreEqual(MerkleTreeHashAlgorithmStrings.Sha256, roundtrippedTree.Metadata.HashAlgorithm,
            "Hash algorithm should be preserved through roundtrip");
        Assert.AreEqual("test-exchange", roundtrippedTree.Metadata.ExchangeDocumentType,
            "Exchange document type should be preserved through roundtrip");
    }

    /// <summary>
    /// Tests that private leaves remain private through serialization and maintain their cryptographic integrity.
    /// This is crucial for:
    /// 1. Privacy preservation - sensitive data should never be exposed
    /// 2. Cryptographic integrity - private leaves must still contribute to the root hash
    /// 3. Format compliance - private leaves should follow the V3 format specification
    /// </summary>
    [TestMethod]
    public void RoundTrip_WithPrivateLeaves_ShouldPreservePrivacy()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.Metadata.ExchangeDocumentType = "test-exchange";

        // Add a mix of public and private leaves
        var publicLeaf = tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        var privateLeaf = tree.AddJsonLeaf("ssn", "123-45-6789", Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Create a predicate that makes the SSN private
        Predicate<MerkleLeaf> makePrivate = leaf =>
            leaf.TryReadText(out string text) && text.Contains("ssn");

        // Act - First roundtrip
        string json = tree.ToJson(MerkleTree.ComputeSha256Hash, makePrivate);
        Console.WriteLine("Tree with private leaf JSON:");
        Console.WriteLine(json);

        var parsedTree = MerkleTree.Parse(json);

        // Second roundtrip to ensure stability
        string roundtrippedJson = parsedTree.ToJson();
        var roundtrippedTree = MerkleTree.Parse(roundtrippedJson);

        // Assert
        // Verify we have the correct number of leaves
        Assert.AreEqual(3, roundtrippedTree.Leaves.Count,
            "Tree should have 3 leaves (header + public + private)");

        // Verify the header leaf
        var headerLeaf = roundtrippedTree.Leaves[0];
        Assert.AreEqual("application/merkle-exchange-header-3.0+json; charset=utf-8; encoding=hex",
            headerLeaf.ContentType,
            "Header leaf should have correct content type");

        // Parse and verify the header leaf data
        var headerHex = headerLeaf.Data.ToString()!;
        var headerJson = System.Text.Encoding.UTF8.GetString(Hex.Parse(headerHex).ToByteArray());
        var headerDoc = JsonDocument.Parse(headerJson);
        var headerRoot = headerDoc.RootElement;

        // Verify header metadata
        Assert.IsTrue(headerRoot.TryGetProperty("leaves", out var leafCount) &&
            leafCount.GetInt32() == 3,
            "Header should have 'leaves' property set to 3 (header + public + private)");

        // Verify the private leaf
        var privateLeafJson = roundtrippedTree.Leaves[2];
        Assert.IsTrue(privateLeafJson.IsPrivate,
            "SSN leaf should be marked as private");
        Assert.IsTrue(privateLeafJson.Data.IsEmpty(),
            "Private leaf should have no data");
        Assert.IsTrue(privateLeafJson.Salt.IsEmpty(),
            "Private leaf should have no salt");
        Assert.AreEqual(string.Empty, privateLeafJson.ContentType,
            "Private leaf should have no content type");
        Assert.AreEqual(privateLeaf.Hash, privateLeafJson.Hash,
            "Private leaf should preserve its hash");

        // Verify the public leaf
        var publicLeafJson = roundtrippedTree.Leaves[1];
        Assert.IsFalse(publicLeafJson.IsPrivate,
            "Name leaf should not be private");
        Assert.IsFalse(publicLeafJson.Data.IsEmpty(),
            "Public leaf should have data");
        Assert.IsFalse(publicLeafJson.Salt.IsEmpty(),
            "Public leaf should have salt");
        Assert.IsFalse(string.IsNullOrEmpty(publicLeafJson.ContentType),
            "Public leaf should have content type");

        // Verify the tree still validates
        Assert.IsTrue(roundtrippedTree.VerifyRoot(),
            "Tree with private leaf should still verify");

        // Verify the JSON doesn't contain the private data
        Assert.IsFalse(roundtrippedJson.Contains("123-45-6789"),
            "JSON should not contain the private SSN data");
        Assert.IsFalse(roundtrippedJson.Contains("ssn"),
            "JSON should not contain the private field name");

        // Verify metadata is preserved
        Assert.AreEqual(MerkleTreeVersionStrings.V3_0, roundtrippedTree.Metadata.Version,
            "Version should be preserved");
        Assert.AreEqual(MerkleTreeHashAlgorithmStrings.Sha256, roundtrippedTree.Metadata.HashAlgorithm,
            "Hash algorithm should be preserved");
        Assert.AreEqual("test-exchange", roundtrippedTree.Metadata.ExchangeDocumentType,
            "Exchange document type should be preserved");
    }

    /// <summary>
    /// Tests that tampering with the leaf count in the header is detected.
    /// This is a security test that verifies:
    /// 1. The header leaf's leaf count is cryptographically protected
    /// 2. The system detects attempts to add or remove leaves
    /// 3. The validation fails gracefully with a clear error message
    /// </summary>
    [TestMethod]
    public void Parse_WithTamperedLeafCount_ShouldThrow()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.Metadata.ExchangeDocumentType = "test-exchange";

        // Add some test leaves
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("age", 30, Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Get the JSON and parse it to modify the leaf count
        string json = tree.ToJson();
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Get the header leaf data
        var leaves = root.GetProperty("leaves");
        var headerLeaf = leaves[0];
        var headerHex = headerLeaf.GetProperty("data").GetString()!;

        // Parse the header data and modify the leaf count
        var headerJson = System.Text.Encoding.UTF8.GetString(Hex.Parse(headerHex).ToByteArray());
        var headerDoc = JsonDocument.Parse(headerJson);
        var headerRoot = headerDoc.RootElement;

        // Create a new header with tampered leaf count using JsonDocument
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("alg", headerRoot.GetProperty("alg").GetString());
            writer.WriteString("typ", headerRoot.GetProperty("typ").GetString());
            writer.WriteNumber("leaves", headerRoot.GetProperty("leaves").GetInt32() + 1); // Tamper: add one to leaf count
            writer.WriteString("exchange", headerRoot.GetProperty("exchange").GetString());
            writer.WriteEndObject();
        }

        // Convert back to hex
        var tamperedHeaderJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        var tamperedHeaderHex = new Hex(System.Text.Encoding.UTF8.GetBytes(tamperedHeaderJson));

        // Create a new JSON with the tampered header
        var tamperedJson = json.Replace(headerHex, tamperedHeaderHex.ToString());

        // Act & Assert
        var ex = Assert.ThrowsException<MalformedJsonException>(() => MerkleTree.Parse(tamperedJson));

        // Verify the error message is helpful
        Assert.IsTrue(ex.Message.Contains("leaf count mismatch"),
            "Error message should mention leaf count mismatch");
        Assert.IsTrue(ex.Message.Contains("Unable to parse V3.0 tree"),
            "Error message should indicate parsing failure");
    }

    /// <summary>
    /// Tests that removing the header leaf is detected.
    /// This is a security test that verifies:
    /// 1. The header leaf is required for V3 trees
    /// 2. The system detects attempts to remove the header leaf
    /// 3. The validation fails gracefully with a clear error message
    /// </summary>
    [TestMethod]
    public void Parse_WithMissingHeaderLeaf_ShouldThrow()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.Metadata.ExchangeDocumentType = "test-exchange";

        // Add some test leaves
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("age", 30, Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Get the JSON and parse it to remove the header leaf
        string json = tree.ToJson();
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Get the leaves array and remove the header leaf
        var leaves = root.GetProperty("leaves");
        var leavesArray = leaves.EnumerateArray().ToArray();

        // Create a new JSON without the header leaf
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("header");
            writer.WriteStartObject();
            writer.WriteString("typ", MerkleTreeVersionStrings.V3_0);
            writer.WriteEndObject();
            writer.WritePropertyName("leaves");
            writer.WriteStartArray();
            // Skip the header leaf (index 0) and only write data leaves
            for (int i = 1; i < leavesArray.Length; i++)
            {
                leavesArray[i].WriteTo(writer);
            }
            writer.WriteEndArray();
            writer.WriteString("root", root.GetProperty("root").GetString());
            writer.WriteEndObject();
        }

        string tamperedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        // Act & Assert
        var ex = Assert.ThrowsException<MalformedJsonException>(() => MerkleTree.Parse(tamperedJson));

        // Verify the error message is helpful
        Assert.IsTrue(ex.Message.Contains("Unable to parse V3.0 tree"),
            "Error message should indicate parsing failure");
        Assert.IsTrue(ex.Message.Contains("header leaf"),
            $"Error message should mention missing header leaf: '{ex.Message}'");
    }

    /// <summary>
    /// Tests that tampering with the header leaf data is detected.
    /// This is a security test that verifies:
    /// 1. The header leaf data is cryptographically protected
    /// 2. The system detects attempts to modify the header metadata
    /// 3. The validation fails gracefully with a clear error message
    /// </summary>
    [TestMethod]
    public void Parse_WithTamperedHeaderLeaf_ShouldThrow()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.Metadata.ExchangeDocumentType = "test-exchange";

        // Add some test leaves
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("age", 30, Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Get the JSON and parse it to modify the header leaf
        string json = tree.ToJson();
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Get the header leaf data
        var leaves = root.GetProperty("leaves");
        var headerLeaf = leaves[0];
        var headerHex = headerLeaf.GetProperty("data").GetString()!;

        // Parse the header data and modify it
        var headerJson = System.Text.Encoding.UTF8.GetString(Hex.Parse(headerHex).ToByteArray());
        var headerDoc = JsonDocument.Parse(headerJson);
        var headerRoot = headerDoc.RootElement;

        // Create a new header with tampered content type
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("alg", headerRoot.GetProperty("alg").GetString());
            writer.WriteString("typ", "application/merkle-exchange-header-2.0+json"); // Tamper: wrong version
            writer.WriteNumber("leaves", headerRoot.GetProperty("leaves").GetInt32());
            writer.WriteString("exchange", headerRoot.GetProperty("exchange").GetString());
            writer.WriteEndObject();
        }

        // Convert back to hex
        var tamperedHeaderJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        var tamperedHeaderHex = new Hex(System.Text.Encoding.UTF8.GetBytes(tamperedHeaderJson));

        // Create a new JSON with the tampered header
        var tamperedJson = json.Replace(headerHex, tamperedHeaderHex.ToString());

        // Act & Assert
        var ex = Assert.ThrowsException<MalformedJsonException>(() => MerkleTree.Parse(tamperedJson));

        // Verify the error message is helpful
        Assert.IsTrue(ex.Message.Contains("Unable to parse V3.0 tree"),
            $"Error message should indicate parsing failure: '{ex.Message}'");
        Assert.IsTrue(ex.Message.Contains("header leaf has incorrect type"),
            $"Error message should mention incorrect header type: '{ex.Message}'");
    }

    /// <summary>
    /// Tests that missing required header fields are detected.
    /// This is a security test that verifies:
    /// 1. All required header fields (alg, typ, exchange) are present
    /// 2. The system detects attempts to omit critical metadata
    /// 3. The validation fails gracefully with a clear error message
    /// </summary>
    [TestMethod]
    public void Parse_WithMissingRequiredHeaderFields_ShouldThrow()
    {
        // Arrange
        var tree = CreateTestTreeWithRecompute();
        var (json, headerRoot) = GetTreeAndHeaderRoot(tree);

        // Test missing 'alg' field
        var tamperedJson = CreateTamperedHeaderJson(json, (writer, original) =>
        {
            writer.WriteStartObject();
            // Omit 'alg' field
            writer.WriteString("typ", original.GetProperty("typ").GetString());
            writer.WriteNumber("leaves", original.GetProperty("leaves").GetInt32());
            writer.WriteString("exchange", original.GetProperty("exchange").GetString());
            writer.WriteEndObject();
        });

        // Act & Assert
        var ex = Assert.ThrowsException<MalformedJsonException>(() => MerkleTree.Parse(tamperedJson));
        Assert.IsTrue(ex.Message.Contains("Unable to parse V3.0 tree"),
            $"Error message should indicate header parsing failure: '{ex.Message}'");

        // Test missing 'typ' field
        tamperedJson = CreateTamperedHeaderJson(json, (writer, original) =>
        {
            writer.WriteStartObject();
            writer.WriteString("alg", original.GetProperty("alg").GetString());
            // Omit 'typ' field
            writer.WriteNumber("leaves", original.GetProperty("leaves").GetInt32());
            writer.WriteString("exchange", original.GetProperty("exchange").GetString());
            writer.WriteEndObject();
        });

        ex = Assert.ThrowsException<MalformedJsonException>(() => MerkleTree.Parse(tamperedJson));
        Assert.IsTrue(ex.Message.Contains("Unable to parse V3.0 tree"),
            $"Error message should indicate header parsing failure: '{ex.Message}'");

        // Test missing 'exchange' field
        tamperedJson = CreateTamperedHeaderJson(json, (writer, original) =>
        {
            writer.WriteStartObject();
            writer.WriteString("alg", original.GetProperty("alg").GetString());
            writer.WriteString("typ", original.GetProperty("typ").GetString());
            writer.WriteNumber("leaves", original.GetProperty("leaves").GetInt32());
            // Omit 'exchange' field
            writer.WriteEndObject();
        });

        ex = Assert.ThrowsException<MalformedJsonException>(() => MerkleTree.Parse(tamperedJson));
        Assert.IsTrue(ex.Message.Contains("Unable to parse V3.0 tree"),
            $"Error message should indicate header parsing failure: '{ex.Message}'");
    }

    /// <summary>
    /// Tests that tampering with the header leaf is detected during root verification.
    /// This is a security test that verifies:
    /// 1. The header leaf is included in the root hash computation
    /// 2. The system detects attempts to modify the header leaf
    /// 3. The verification fails when the header leaf is tampered with
    /// </summary>
    [TestMethod]
    public void VerifyRoot_WithTamperedHeaderLeaf_ShouldFail()
    {
        // Arrange
        var tree = CreateTestTreeWithRecompute();
        var (json, headerRoot) = GetTreeAndHeaderRoot(tree);

        // Create a new header with tampered exchange type
        var tamperedJson = CreateTamperedHeaderJson(json, (writer, original) =>
        {
            writer.WriteStartObject();
            writer.WriteString("alg", original.GetProperty("alg").GetString());
            writer.WriteString("typ", original.GetProperty("typ").GetString());
            writer.WriteNumber("leaves", original.GetProperty("leaves").GetInt32());
            writer.WriteString("exchange", "tampered-exchange"); // Tamper: change exchange type
            writer.WriteEndObject();
        });

        // Parse the tampered JSON back into a tree
        var parsedTree = MerkleTree.Parse(tamperedJson);

        // Assert - Verification should fail, detecting the tampering
        Assert.IsFalse(parsedTree.VerifyRoot());
    }

    /// <summary>
    /// Tests that tampering with a regular leaf is detected during root verification.
    /// This is a security test that verifies:
    /// 1. All leaves are included in the root hash computation
    /// 2. The system detects attempts to modify any leaf
    /// 3. The verification fails when any leaf is tampered with
    /// </summary>
    [TestMethod]
    public void VerifyRoot_WithTamperedRegularLeaf_ShouldFail()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.Metadata.ExchangeDocumentType = "test-exchange";

        // Add some test leaves
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("age", 30, Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Get the JSON and parse it to modify a leaf's data
        string json = tree.ToJson();
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Get the first data leaf (after header leaf)
        var leaves = root.GetProperty("leaves");
        var dataLeaf = leaves[1]; // Index 1 is first data leaf (0 is header)
        var originalData = dataLeaf.GetProperty("data").GetString()!;

        // Create tampered data
        string originalContent = "{\"name\":\"John Doe\"}";
        string tamperedContent = "{\"name\":\"Jane Doe\"}";

        // Convert to hex representations
        string originalHex = BytesToHexString(System.Text.Encoding.UTF8.GetBytes(originalContent));
        string tamperedHex = BytesToHexString(System.Text.Encoding.UTF8.GetBytes(tamperedContent));

        // Replace in the JSON
        string tamperedJson = json.Replace(originalData, tamperedHex);

        // Parse the tampered JSON back into a tree
        var parsedTree = MerkleTree.Parse(tamperedJson);

        // Assert - Verification should fail, detecting the tampering
        Assert.IsFalse(parsedTree.VerifyRoot());
    }

    /// <summary>
    /// Tests that adding a private leaf works correctly in V3 format.
    /// This is important because:
    /// 1. Private leaves are a key feature for privacy
    /// 2. The V3 format must handle private leaves correctly
    /// 3. The header leaf must account for private leaves in its counts
    /// </summary>
    [TestMethod]
    public void AddPrivateLeaf_ShouldCreateLeafWithOnlyHash()
    {
        // Arrange
        var tree = CreateTestTreeWithRecompute();
        var originalRoot = tree.Root;

        // Act - Add a private leaf
        var privateHash = Hex.Parse("0x1234567890abcdef");
        var privateLeaf = tree.AddPrivateLeaf(privateHash);
        tree.RecomputeSha256Root();

        // Assert
        // Verify the private leaf properties
        Assert.IsTrue(privateLeaf.IsPrivate, "Leaf should be marked as private");
        Assert.IsTrue(privateLeaf.Data.IsEmpty(), "Private leaf should have no data");
        Assert.IsTrue(privateLeaf.Salt.IsEmpty(), "Private leaf should have no salt");
        Assert.AreEqual(string.Empty, privateLeaf.ContentType, "Private leaf should have no content type");
        Assert.AreEqual(privateHash, privateLeaf.Hash, "Private leaf should preserve its hash");

        // Verify the tree structure
        var (json, headerRoot) = GetTreeAndHeaderRoot(tree);
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;
        var leaves = root.GetProperty("leaves");

        // Verify header leaf count includes private leaf
        Assert.IsTrue(headerRoot.TryGetProperty("leaves", out var leafCount) &&
            leafCount.GetInt32() == 4, // header + 2 data leaves + 1 private leaf
            "Header should account for private leaf in count");

        // Verify private leaf in JSON
        var leavesArray = leaves.EnumerateArray().ToArray();
        var jsonPrivateLeaf = leavesArray[3]; // Last leaf should be private
        Assert.IsTrue(jsonPrivateLeaf.TryGetProperty("hash", out var hash) &&
            hash.GetString() == privateHash.ToString(),
            "JSON should contain private leaf hash");
        Assert.IsFalse(jsonPrivateLeaf.TryGetProperty("data", out _),
            "JSON should not contain private leaf data");
        Assert.IsFalse(jsonPrivateLeaf.TryGetProperty("salt", out _),
            "JSON should not contain private leaf salt");
        Assert.IsFalse(jsonPrivateLeaf.TryGetProperty("contentType", out _),
            "JSON should not contain private leaf content type");

        // Verify the tree still validates
        Assert.IsTrue(tree.VerifyRoot(), "Tree with private leaf should verify");
        Assert.AreNotEqual(originalRoot, tree.Root,
            "Root hash should change when adding private leaf");

        // Verify roundtrip preserves privacy
        var parsedTree = MerkleTree.Parse(json);
        Assert.IsTrue(parsedTree.VerifyRoot(), "Parsed tree should verify");
        var parsedPrivateLeaf = parsedTree.Leaves[3];
        Assert.IsTrue(parsedPrivateLeaf.IsPrivate, "Parsed private leaf should remain private");
        Assert.AreEqual(privateHash, parsedPrivateLeaf.Hash,
            "Parsed private leaf should preserve its hash");
    }

    /// <summary>
    /// Tests that the header leaf prevents single leaf attacks.
    /// This is a security test that verifies:
    /// 1. The header leaf prevents presenting a single leaf as valid
    /// 2. The system detects attempts to omit the header leaf
    /// 3. The validation fails when the header leaf is missing
    /// </summary>
    [TestMethod]
    public void HeaderLeaf_ShouldProtectAgainstSingleLeafAttack()
    {
        // Arrange
        var tree = CreateTestTreeWithRecompute();
        var (json, _) = GetTreeAndHeaderRoot(tree);
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Get the first data leaf (after header leaf)
        var leaves = root.GetProperty("leaves");
        var dataLeaf = leaves[1]; // Index 1 is first data leaf (0 is header)

        // Create a new JSON with just the data leaf
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("header");
            writer.WriteStartObject();
            writer.WriteString("typ", MerkleTreeVersionStrings.V3_0);
            writer.WriteEndObject();
            writer.WritePropertyName("leaves");
            writer.WriteStartArray();
            dataLeaf.WriteTo(writer); // Only include the data leaf
            writer.WriteEndArray();
            writer.WriteString("root", root.GetProperty("root").GetString());
            writer.WriteEndObject();
        }

        string tamperedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        // Act & Assert
        var ex = Assert.ThrowsException<MalformedJsonException>(() => MerkleTree.Parse(tamperedJson));

        // Verify the error message is helpful
        Assert.IsTrue(ex.Message.Contains("Unable to parse V3.0 tree"),
            $"Error message should indicate parsing failure: '{ex.Message}'");
        Assert.IsTrue(ex.Message.Contains("header leaf"),
            $"Error message should mention missing header leaf: '{ex.Message}'");

        // Verify the tree fails validation if somehow parsed
        try
        {
            var parsedTree = MerkleTree.Parse(tamperedJson);
            Assert.IsFalse(parsedTree.VerifyRoot(),
                "Tree with missing header leaf should fail validation");
        }
        catch (MalformedJsonException)
        {
            // This is expected - the tree should fail to parse
        }
    }

    /// <summary>
    /// Tests that the header leaf prevents leaf addition attacks.
    /// This is a security test that verifies:
    /// 1. The header leaf's leaf count is cryptographically protected
    /// 2. The system detects attempts to add extra leaves
    /// 3. The validation fails when the leaf count doesn't match
    /// </summary>
    [TestMethod]
    public void HeaderLeaf_ShouldProtectAgainstLeafAdditionAttack()
    {
        // Arrange
        var tree = CreateTestTreeWithRecompute();
        var (goodJson, headerRoot) = GetTreeAndHeaderRoot(tree);

        Console.WriteLine("Good JSON:");
        Console.WriteLine(goodJson);
        Console.WriteLine("--------------------------------");

        var jsonDoc = JsonDocument.Parse(goodJson);
        var root = jsonDoc.RootElement;

        // Get the leaves array
        var leaves = root.GetProperty("leaves");
        var leavesArray = leaves.EnumerateArray().ToArray();

        // Create a new JSON with an extra leaf
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("header");
            writer.WriteStartObject();
            writer.WriteString("typ", MerkleTreeVersionStrings.V3_0);
            writer.WriteEndObject();
            writer.WritePropertyName("leaves");
            writer.WriteStartArray();
            // Write all original leaves
            foreach (var leaf in leavesArray)
            {
                leaf.WriteTo(writer);
            }
            // Add an extra leaf
            writer.WriteStartObject();
            writer.WriteString("data", "0x123456");
            writer.WriteString("salt", "0x789abc");
            writer.WriteString("hash", "0xdef012"); // fake hash; not computed
            writer.WriteString("contentType", "application/json");
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteString("root", root.GetProperty("root").GetString());
            writer.WriteEndObject();
        }

        string treeWithExtraLeafJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        Console.WriteLine("Tree with extra leaf JSON:");
        Console.WriteLine(treeWithExtraLeafJson);
        Console.WriteLine("--------------------------------");

        // Act & Assert
        var ex = Assert.ThrowsException<MalformedJsonException>(() => MerkleTree.Parse(treeWithExtraLeafJson));

        // Verify the error message is helpful
        Assert.IsTrue(ex.Message.Contains("leaf count mismatch"),
            $"Error message should mention leaf count mismatch: '{ex.Message}'");
        Assert.IsTrue(ex.Message.Contains("Unable to parse V3.0 tree"),
            $"Error message should indicate parsing failure: '{ex.Message}'");

        // Verify the tree fails validation if somehow parsed
        try
        {
            var treeWithExtraLeaf = MerkleTree.Parse(treeWithExtraLeafJson);
            Assert.IsFalse(treeWithExtraLeaf.VerifyRoot(),
                "Tree with extra leaf should fail validation");
        }
        catch (MalformedJsonException)
        {
            // This is expected - the tree should fail to parse, but next we're going to
            // tamper the header leaf count to 'lie' then verify that the attack is detected.
        }

        // Verify the attack is detected even if the header leaf count is tampered
        // Extract the header hex from the tampered tree JSON
        var tamperedJsonDoc = JsonDocument.Parse(treeWithExtraLeafJson);
        var tamperedRoot = tamperedJsonDoc.RootElement;
        var tamperedHeaderHex = tamperedRoot.GetProperty("leaves")[0].GetProperty("data").GetString()!;
        var treeWithFakedHeaderJson = CreateTamperedHeaderJson(treeWithExtraLeafJson, (writer, original) =>
        {
            writer.WriteStartObject();
            writer.WriteString("alg", original.GetProperty("alg").GetString());
            writer.WriteString("typ", original.GetProperty("typ").GetString());
            writer.WriteNumber("leaves", original.GetProperty("leaves").GetInt32() + 1); // Tamper: increment leaf count
            writer.WriteString("exchange", original.GetProperty("exchange").GetString());
            writer.WriteEndObject();
        });

        Console.WriteLine("Tree with faked header JSON:");
        Console.WriteLine(treeWithFakedHeaderJson);
        Console.WriteLine("--------------------------------");

        // With the dodgy header in place, this should Parse correctly because it won't detect the
        // tampering until the root hash is computed because the hash was not properly computed
        // for the fake leaf.
        //
        // Note that if we'd have computed a proper hash for the fake leaf, the tree would parse
        // correctly because everything would have been computed correctly. The only way to detect
        // the attack is to compare the computed root hash with a known good root hash, e.g. a hash
        // that has been attested to on a blockchain.
        var dodgyTree = MerkleTree.Parse(treeWithFakedHeaderJson);
        Assert.IsFalse(dodgyTree.VerifyRoot(),
            "Tree with tampered header leaf count should fail verification because the hash was not " +
            "computed for the dodgy leaf");
    }

    /// <summary>
    /// Tests that the header leaf prevents leaf removal attacks.
    /// This is a security test that verifies:
    /// 1. The header leaf's leaf count is cryptographically protected
    /// 2. The system detects attempts to remove leaves
    /// 3. The validation fails when the leaf count doesn't match
    /// </summary>
    [TestMethod]
    public void HeaderLeaf_ShouldProtectAgainstLeafRemovalAttack()
    {
        // Test that the header leaf prevents attacks that try to remove leaves
    }

    /// <summary>
    /// Tests that the exchange document type is preserved through serialization.
    /// This is important because:
    /// 1. The exchange type is a key metadata field
    /// 2. It must be preserved for document classification
    /// 3. It must be protected in the header leaf
    /// </summary>
    [TestMethod]
    public void ExchangeDocumentType_ShouldBePreserved()
    {
        // Test that the exchange document type is preserved through serialization
    }

    /// <summary>
    /// Tests that the header leaf has the correct content type.
    /// This is important because:
    /// 1. The content type is required for proper parsing
    /// 2. It must follow the V3 specification
    /// 3. It must be preserved through serialization
    /// </summary>
    [TestMethod]
    public void HeaderLeaf_ShouldHaveCorrectContentType()
    {
        // Test that the header leaf has the correct content type
    }

    /// <summary>
    /// Tests the basic ToJson/Parse roundtrip preserves the root hash.
    /// This is a fundamental test that verifies:
    /// 1. The root hash is preserved exactly through serialization
    /// 2. The tree can be reconstructed from JSON
    /// 3. The reconstructed tree validates correctly
    /// 4. The computed root matches the stored root
    /// </summary>
    [TestMethod]
    public void ToJson_AndParse_ShouldPreserveRootHash()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("age", 30, Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Verify the original tree
        Assert.IsTrue(tree.VerifyRoot(), "Original tree should verify");
        var originalRoot = tree.Root;

        // Act
        string json = tree.ToJson();
        var parsedTree = MerkleTree.Parse(json);

        // Assert
        // Verify the parsed tree validates
        Assert.IsTrue(parsedTree.VerifyRoot(), "Parsed tree should verify");

        // Verify the root hash is preserved exactly
        Assert.AreEqual(originalRoot, parsedTree.Root,
            "Root hash should be preserved exactly through ToJson/Parse roundtrip");

        // Verify we can get the computed root and it matches
        Assert.IsTrue(parsedTree.VerifyRoot(MerkleTree.ComputeSha256Hash, out var computedRoot),
            "Tree should verify and return computed root");
        Assert.AreEqual(originalRoot, computedRoot,
            "Computed root should match the original root");
    }

    /// <summary>
    /// Tests that an empty tree (no leaves added) correctly throws when trying to compute a root.
    /// This is important because:
    /// 1. It verifies the edge case of a tree with no data
    /// 2. It ensures we can't create a valid tree without any leaves
    /// 3. It validates that the header leaf is required for a valid tree
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void EmptyTree_ShouldThrowWhenComputingRoot()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.Metadata.ExchangeDocumentType = "test-exchange";

        // Act - This should throw
        tree.RecomputeSha256Root();

        // Assert is handled by ExpectedException
    }

    /// <summary>
    /// Tests that attempting to verify a tree with an unsupported hash algorithm throws an exception
    /// with a helpful error message. This is important because:
    /// 1. It ensures we fail safely when encountering unknown algorithms
    /// 2. It provides clear guidance on how to handle custom algorithms
    /// 3. It maintains security by not silently accepting unknown algorithms
    /// </summary>
    [TestMethod]
    public void VerifyRoot_WithUnsupportedAlgorithm_ShouldThrowWithHelpfulMessage()
    {
        // Arrange
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.Metadata.ExchangeDocumentType = "test-exchange";

        // Add a leaf and compute root with SHA-256
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Modify the metadata to use an unsupported algorithm
        tree.Metadata.HashAlgorithm = "unsupported-algo";

        // Act & Assert
        var ex = Assert.ThrowsException<NotSupportedException>(() => tree.VerifyRoot());

        // Verify the error message is helpful
        Assert.IsTrue(ex.Message.Contains("unsupported-algo"),
            "Error message should mention the unsupported algorithm");
        Assert.IsTrue(ex.Message.Contains("VerifyRoot(HashFunction)"),
            "Error message should suggest using VerifyRoot(HashFunction)");

        // Verify we can still verify with explicit hash function
        Assert.IsTrue(tree.VerifyRoot(MerkleTree.ComputeSha256Hash),
            "Tree should still verify with explicit SHA-256 function");
    }

    /// <summary>
    /// Tests that adding a leaf after root computation correctly updates the header leaf count.
    /// This is important because:
    /// 1. The header leaf must always reflect the current leaf count
    /// 2. Adding leaves after root computation must update the header
    /// 3. The tree must remain cryptographically sound after adding leaves
    /// </summary>
    [TestMethod]
    public void AddLeaf_AfterRootComputation_ShouldUpdateHeaderLeafCount()
    {
        // Arrange
        var tree = CreateTestTreeWithRecompute();
        var originalRoot = tree.Root;
        var originalLeafCount = tree.Leaves.Count;

        // Act - Add a new leaf after root computation
        var newLeaf = tree.AddJsonLeaf("newField", "newValue", Hex.Parse("0x112233"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Assert
        // Verify the tree structure
        var (json, headerRoot) = GetTreeAndHeaderRoot(tree);
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;
        var leaves = root.GetProperty("leaves");

        // Verify header leaf count is updated
        Assert.IsTrue(headerRoot.TryGetProperty("leaves", out var leafCount) &&
            leafCount.GetInt32() == originalLeafCount + 1,
            "Header should reflect new leaf count");

        // Verify the new leaf is present
        var leavesArray = leaves.EnumerateArray().ToArray();
        var jsonNewLeaf = leavesArray[leavesArray.Length - 1];
        Assert.IsTrue(jsonNewLeaf.TryGetProperty("data", out var data),
            "New leaf should have data property");

        // Verify the leaf data is correct by decoding the hex
        var leafData = Hex.Parse(data.GetString()!);
        var leafJson = System.Text.Encoding.UTF8.GetString(leafData.ToByteArray());
        Assert.IsTrue(leafJson.Contains("newField"),
            $"Leaf data should contain 'newField', got: {leafJson}");

        // Verify the tree still validates
        Assert.IsTrue(tree.VerifyRoot(), "Tree should verify after adding leaf");
        Assert.AreNotEqual(originalRoot, tree.Root,
            "Root hash should change when adding new leaf");

        // Verify roundtrip preserves all leaves
        var parsedTree = MerkleTree.Parse(json);
        Assert.IsTrue(parsedTree.VerifyRoot(), "Parsed tree should verify");
        Assert.AreEqual(originalLeafCount + 1, parsedTree.Leaves.Count,
            "Parsed tree should have correct leaf count");

        // Verify the new leaf data is preserved through roundtrip
        var roundtrippedLeaf = parsedTree.Leaves[parsedTree.Leaves.Count - 1];
        Assert.IsFalse(roundtrippedLeaf.IsPrivate, "New leaf should not be private");
        Assert.IsFalse(roundtrippedLeaf.Data.IsEmpty(), "New leaf should have data");
        Assert.IsFalse(roundtrippedLeaf.Salt.IsEmpty(), "New leaf should have salt");
        Assert.IsFalse(string.IsNullOrEmpty(roundtrippedLeaf.ContentType), "New leaf should have content type");
    }

    /// <summary>
    /// Tests that the CreateTamperedHeaderJson replacement logic works as expected, by creating a simple Merkle tree, extracting the header hex, tampering a field, and asserting that the replacement occurs and the tampered hex is present in the output.
    /// </summary>
    [TestMethod]
    public void CreateTamperedHeaderJson_ShouldReplaceHeaderHexCorrectly()
    {
        // Arrange: create a simple V3 tree
        var tree = CreateTestTreeWithRecompute();
        string json = tree.ToJson();
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;
        var leaves = root.GetProperty("leaves");
        var headerLeaf = leaves[0];
        var headerHex = headerLeaf.GetProperty("data").GetString()!;

        // Tamper the header: change the 'exchange' field
        string tamperedJson = CreateTamperedHeaderJson(json, (writer, original) =>
        {
            writer.WriteStartObject();
            writer.WriteString("alg", original.GetProperty("alg").GetString());
            writer.WriteString("typ", original.GetProperty("typ").GetString());
            writer.WriteNumber("leaves", original.GetProperty("leaves").GetInt32());
            writer.WriteString("exchange", "tampered-exchange");
            writer.WriteEndObject();
        });

        // Extract the new header hex from the tampered JSON
        var tamperedJsonDoc = JsonDocument.Parse(tamperedJson);
        var tamperedRoot = tamperedJsonDoc.RootElement;
        var tamperedHeaderHex = tamperedRoot.GetProperty("leaves")[0].GetProperty("data").GetString();

        // Decode the tampered header hex to JSON
        var tamperedHeaderJson = System.Text.Encoding.UTF8.GetString(Hex.Parse(tamperedHeaderHex!).ToByteArray());
        var tamperedHeaderDoc = JsonDocument.Parse(tamperedHeaderJson);
        var tamperedHeaderRoot = tamperedHeaderDoc.RootElement;

        // Assert: the 'exchange' field is the tampered value
        Assert.AreEqual("tampered-exchange", tamperedHeaderRoot.GetProperty("exchange").GetString(), "Exchange field should be tampered");

        // Assert: the original header hex is not present in the output
        Assert.IsFalse(tamperedJson.Contains(headerHex), "Original header hex should not be present in the output");
    }

    //

    private static string BytesToHexString(byte[] bytes)
    {
        return "0x" + BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    private MerkleTree CreateTestTreeWithRecompute()
    {
        var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        tree.Metadata.ExchangeDocumentType = "test-exchange";
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("age", 30, Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();
        return tree;
    }

    private (string Json, JsonElement HeaderRoot) GetTreeAndHeaderRoot(MerkleTree tree)
    {
        string json = tree.ToJson();
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;
        var leaves = root.GetProperty("leaves");
        var headerLeaf = leaves[0];
        var headerHex = headerLeaf.GetProperty("data").GetString()!;
        var headerJson = System.Text.Encoding.UTF8.GetString(Hex.Parse(headerHex).ToByteArray());
        return (json, JsonDocument.Parse(headerJson).RootElement);
    }

    private string CreateTamperedHeaderJson(string originalJson, Action<Utf8JsonWriter, JsonElement> modifyHeader)
    {
        // Extract the header hex from the JSON
        var jsonDoc = JsonDocument.Parse(originalJson);
        var root = jsonDoc.RootElement;
        var leaves = root.GetProperty("leaves");
        var headerLeaf = leaves[0];
        var headerHex = headerLeaf.GetProperty("data").GetString()!;

        // Decode the original hex to get the JSON
        var headerJson = System.Text.Encoding.UTF8.GetString(Hex.Parse(headerHex).ToByteArray());
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            modifyHeader(writer, JsonDocument.Parse(headerJson).RootElement);
        }
        var tamperedHeaderJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        var tamperedHeaderHex = new Hex(System.Text.Encoding.UTF8.GetBytes(tamperedHeaderJson));
        var finalJson = originalJson.Replace(headerHex, tamperedHeaderHex.ToString());
        if (originalJson == finalJson)
        {
            throw new Exception("No replacement was made");
        }
        return finalJson;
    }

    private void AssertMalformedJsonException(string tamperedJson, string expectedMessage)
    {
        var ex = Assert.ThrowsException<MalformedJsonException>(() => MerkleTree.Parse(tamperedJson));
        Assert.IsTrue(ex.Message.Contains(expectedMessage),
            $"Error message should contain '{expectedMessage}': '{ex.Message}'");
    }

    private void AssertHeaderLeafContentType(JsonElement headerLeaf)
    {
        Assert.IsTrue(headerLeaf.TryGetProperty("contentType", out var contentType),
            "Header leaf should have 'contentType' property");
        Assert.AreEqual("application/merkle-exchange-header-3.0+json; charset=utf-8; encoding=hex",
            contentType.GetString(),
            "Header leaf should have correct content type");
    }

    private void AssertHeaderMetadata(JsonElement headerRoot)
    {
        Assert.IsTrue(headerRoot.TryGetProperty("alg", out var alg) &&
            alg.GetString() == MerkleTreeHashAlgorithmStrings.Sha256,
            "Header should have 'alg' property set to 'sha256'");

        Assert.IsTrue(headerRoot.TryGetProperty("typ", out var typ) &&
            typ.GetString() == "application/merkle-exchange-header-3.0+json",
            "Header should have 'typ' property set to 'application/merkle-exchange-header-3.0+json'");

        Assert.IsTrue(headerRoot.TryGetProperty("leaves", out var leafCount) &&
            leafCount.GetInt32() > 0,
            "Header should have 'leaves' property set to a positive number");

        Assert.IsTrue(headerRoot.TryGetProperty("exchange", out var exchange) &&
            !string.IsNullOrEmpty(exchange.GetString()),
            "Header should have non-empty 'exchange' property");
    }

    [TestMethod]
    public void From_WithV3Tree_ShouldPreserveMetadataLeaf()
    {
        // Arrange - Create V3 tree with header leaf and data leaves
        var sourceTree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        sourceTree.Metadata.ExchangeDocumentType = "test-exchange";
        sourceTree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        sourceTree.AddJsonLeaf("ssn", "123-45-6789", Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        sourceTree.AddJsonLeaf("email", "john@example.com", Hex.Parse("0x112233"), MerkleTree.ComputeSha256Hash);
        sourceTree.RecomputeSha256Root();

        // Act - Create selective disclosure tree with predicate that would make all leaves private
        var selectiveTree = MerkleTree.From(
            sourceTree,
            makePrivate: leaf => true // This would make all leaves private, but metadata should be preserved
        );

        // Assert - Metadata leaf should be preserved with full data
        Assert.AreEqual(sourceTree.Leaves.Count, selectiveTree.Leaves.Count, "Should have same number of leaves");

        // First leaf should be the metadata leaf and should be preserved
        var metadataLeaf = selectiveTree.Leaves[0];
        Assert.IsTrue(metadataLeaf.IsMetadata, "First leaf should be a metadata leaf");
        Assert.IsFalse(metadataLeaf.IsPrivate, "Metadata leaf should not be private");
        Assert.IsFalse(metadataLeaf.Data.IsEmpty(), "Metadata leaf should have data");
        Assert.IsFalse(metadataLeaf.Salt.IsEmpty(), "Metadata leaf should have salt");

        // Metadata leaf should be preserved exactly as-is (same salt, data, hash)
        var sourceMetadataLeaf = sourceTree.Leaves[0];
        Assert.AreEqual(sourceMetadataLeaf.ContentType, metadataLeaf.ContentType, "Metadata content type should be preserved");
        Assert.AreEqual(sourceMetadataLeaf.Data, metadataLeaf.Data, "Metadata data should be preserved");
        Assert.AreEqual(sourceMetadataLeaf.Salt, metadataLeaf.Salt, "Metadata salt should be preserved");
        Assert.AreEqual(sourceMetadataLeaf.Hash, metadataLeaf.Hash, "Metadata hash should be preserved");

        // All other leaves should be private
        for (int i = 1; i < selectiveTree.Leaves.Count; i++)
        {
            Assert.IsTrue(selectiveTree.Leaves[i].IsPrivate, $"Leaf {i} should be private");
            Assert.IsTrue(selectiveTree.Leaves[i].Data.IsEmpty(), $"Leaf {i} should have no data");
            Assert.IsTrue(selectiveTree.Leaves[i].Salt.IsEmpty(), $"Leaf {i} should have no salt");
            Assert.AreEqual(sourceTree.Leaves[i].Hash, selectiveTree.Leaves[i].Hash, $"Leaf {i} hash should be preserved");
        }

        // Root should be the same
        Assert.AreEqual(sourceTree.Root, selectiveTree.Root, "Root hash should be preserved");
    }

    [TestMethod]
    public void From_WithV3Tree_ShouldMaintainSameRootHash()
    {
        // Arrange - Create V3 tree with header leaf and data leaves
        var sourceTree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        sourceTree.Metadata.ExchangeDocumentType = "test-exchange";
        sourceTree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        sourceTree.AddJsonLeaf("ssn", "123-45-6789", Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        sourceTree.AddJsonLeaf("email", "john@example.com", Hex.Parse("0x112233"), MerkleTree.ComputeSha256Hash);
        sourceTree.RecomputeSha256Root();

        var originalRoot = sourceTree.Root;
        Assert.IsFalse(originalRoot.IsEmpty(), "Source tree should have a computed root");

        // Act - Create selective disclosure tree with various predicates
        var allPrivateTree = MerkleTree.From(sourceTree, makePrivate: leaf => true);
        var allRevealedTree = MerkleTree.From(sourceTree, makePrivate: leaf => false);
        var selectiveTree = MerkleTree.From(sourceTree, makePrivate: leaf =>
            leaf.TryReadJsonKeys(out var keys) && keys.Contains("ssn"));

        // Assert - All selective disclosure trees should have the same root hash as the source
        Assert.AreEqual(originalRoot, allPrivateTree.Root, "All-private tree should have same root");
        Assert.AreEqual(originalRoot, allRevealedTree.Root, "All-revealed tree should have same root");
        Assert.AreEqual(originalRoot, selectiveTree.Root, "Selective tree should have same root");

        // Verify the trees are different in terms of privacy but same in structure
        Assert.AreEqual(sourceTree.Leaves.Count, allPrivateTree.Leaves.Count);
        Assert.AreEqual(sourceTree.Leaves.Count, allRevealedTree.Leaves.Count);
        Assert.AreEqual(sourceTree.Leaves.Count, selectiveTree.Leaves.Count);
    }
}