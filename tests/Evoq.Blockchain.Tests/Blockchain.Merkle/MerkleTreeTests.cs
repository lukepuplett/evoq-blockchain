using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Evoq.Blockchain;
using Evoq.Blockchain.Merkle;

namespace Evoq.Blockchain.Tests.Merkle;

[TestClass]
public class MerkleTreeTests
{
    [TestMethod]
    public void Parse_ValidJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        string jsonInput = @"{
            ""leaves"": [
                {
                    ""data"": ""0x646174613100"",
                    ""salt"": ""0xaabbcc"",
                    ""hash"": ""0x1234567890abcdef"",
                    ""contentType"": ""text/plain; charset=utf-8""
                },
                {
                    ""data"": ""0x646174613200"",
                    ""salt"": ""0xddeeff"",
                    ""hash"": ""0xabcdef1234567890"",
                    ""contentType"": ""application/json; charset=utf-8""
                }
            ],
            ""root"": ""0xabcdef0123456789"",
            ""metadata"": {
                ""hashAlgorithm"": ""sha256"",
                ""version"": ""1.0""
            }
        }";

        // Act
        var merkleTree = MerkleTree.Parse(jsonInput);

        // Assert
        Assert.IsNotNull(merkleTree);
        Assert.AreEqual(2, merkleTree.Leaves.Count);
        Assert.AreEqual(Hex.Parse("0x646174613100"), merkleTree.Leaves[0].Data); // Hex for "data1\0"
        Assert.AreEqual(Hex.Parse("0xaabbcc"), merkleTree.Leaves[0].Salt);
        Assert.AreEqual(Hex.Parse("0x1234567890abcdef"), merkleTree.Leaves[0].Hash);
        Assert.AreEqual("text/plain; charset=utf-8", merkleTree.Leaves[0].ContentType);
        Assert.IsTrue(merkleTree.Leaves[0].IsUtf8);
        Assert.AreEqual(Hex.Parse("0x646174613200"), merkleTree.Leaves[1].Data); // Hex for "data2\0"
        Assert.AreEqual(Hex.Parse("0xddeeff"), merkleTree.Leaves[1].Salt);
        Assert.AreEqual(Hex.Parse("0xabcdef1234567890"), merkleTree.Leaves[1].Hash);
        Assert.AreEqual("application/json; charset=utf-8", merkleTree.Leaves[1].ContentType);
        Assert.IsTrue(merkleTree.Leaves[1].IsUtf8);
        Assert.AreEqual(Hex.Parse("0xabcdef0123456789"), merkleTree.Root);
        Assert.AreEqual("sha256", merkleTree.Metadata.HashAlgorithm);
        Assert.AreEqual("1.0", merkleTree.Metadata.Version);
    }

    [TestMethod]
    public void Parse_AndToJson_ShouldRoundtripCorrectly()
    {
        // Create a tree with valid leaves and hashes
        var tree = new MerkleTree("1.0");

        // Add two leaves with predefined salts
        var leaf1 = tree.AddLeaf(
            new Hex(System.Text.Encoding.UTF8.GetBytes("data1")),
            Hex.Parse("0xaabbcc"),
            "text/plain; charset=utf-8",
            MerkleTree.ComputeSha256Hash);

        var leaf2 = tree.AddLeaf(
            new Hex(System.Text.Encoding.UTF8.GetBytes("data2")),
            Hex.Parse("0xddeeff"),
            "application/json; charset=utf-8",
            MerkleTree.ComputeSha256Hash);

        // Compute the root hash
        tree.RecomputeSha256Root();

        // Convert to JSON
        string json = tree.ToJson();

        // Parse the JSON back to a tree
        var parsedTree = MerkleTree.Parse(json);

        // Convert the parsed tree back to JSON (should verify and succeed)
        string roundtrippedJson = parsedTree.ToJson();

        // Parse the roundtripped JSON
        var roundtrippedTree = MerkleTree.Parse(roundtrippedJson);

        // Assert - Compare trees
        Assert.AreEqual(tree.Root, parsedTree.Root);
        Assert.AreEqual(parsedTree.Root, roundtrippedTree.Root);
        Assert.AreEqual(tree.Metadata.HashAlgorithm, parsedTree.Metadata.HashAlgorithm);
        Assert.AreEqual(tree.Metadata.Version, parsedTree.Metadata.Version);
        Assert.AreEqual(tree.Leaves.Count, parsedTree.Leaves.Count);

        // Compare leaves
        for (int i = 0; i < tree.Leaves.Count; i++)
        {
            Assert.AreEqual(tree.Leaves[i].Hash, parsedTree.Leaves[i].Hash);
            Assert.AreEqual(tree.Leaves[i].Salt, parsedTree.Leaves[i].Salt);
            Assert.AreEqual(tree.Leaves[i].ContentType, parsedTree.Leaves[i].ContentType);

            // Compare actual data bytes
            CollectionAssert.AreEqual(
                tree.Leaves[i].Data.ToByteArray(),
                parsedTree.Leaves[i].Data.ToByteArray());

            // Verify that TryReadText still works correctly
            bool originalCanReadText = tree.Leaves[i].TryReadText(out string originalText);
            bool parsedCanReadText = parsedTree.Leaves[i].TryReadText(out string parsedText);

            Assert.AreEqual(originalCanReadText, parsedCanReadText);
            if (originalCanReadText)
            {
                Assert.AreEqual(originalText, parsedText);
            }
        }
    }

    [TestMethod]
    [ExpectedException(typeof(MalformedJsonException))]
    public void Parse_JsonWithInvalidHexString_ShouldThrowMalformedJsonException()
    {
        // Arrange - JSON with an invalid hex string (contains 'g' which is not a valid hex character)
        string jsonWithInvalidHex = @"{
            ""leaves"": [
                {
                    ""data"": ""0x646174613100"",
                    ""salt"": ""0xaabbcc"",
                    ""hash"": ""0x123g567890abcdef"",
                    ""contentType"": ""text/plain; charset=utf-8""
                }
            ],
            ""root"": ""0xc5185f0d2dfb9f5b4079ecaaac77a48cbe6758197333528d02f070e006420c79"",
            ""metadata"": {
                ""hashAlgorithm"": ""sha256"",
                ""version"": ""1.0""
            }
        }";

        // Act
        MerkleTree.Parse(jsonWithInvalidHex);

        // Assert is handled by ExpectedException
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Parse_NullJson_ShouldThrowArgumentNullException()
    {
        // Act
        MerkleTree.Parse(null);

        // Assert is handled by ExpectedException
    }

    [TestMethod]
    [ExpectedException(typeof(MalformedJsonException))]
    public void Parse_InvalidJson_ShouldThrowCustomJsonException()
    {
        // Arrange
        string invalidJson = "{this is not valid json}";

        // Act
        MerkleTree.Parse(invalidJson);

        // Assert is handled by ExpectedException
    }

    [TestMethod]
    public void MerkleLeaf_FromJsonValue_ShouldCreateCorrectLeaf()
    {
        // Act
        var leaf = MerkleLeaf.FromJsonValue("name", "John", Hex.Empty, MerkleTree.ComputeSha256Hash);

        // Assert
        Assert.AreEqual("application/json; charset=utf-8", leaf.ContentType);
        Assert.IsTrue(leaf.IsUtf8);
        Assert.AreEqual(Hex.Empty, leaf.Salt);

        // Verify the text can be retrieved
        bool success = leaf.TryReadText(out string retrievedText);
        Assert.IsTrue(success);

        var expectedText = "{\"name\":\"John\"}";
        Assert.AreEqual(expectedText, retrievedText);

        // Verify the data is correctly stored as UTF-8 bytes
        byte[] expectedBytes = System.Text.Encoding.UTF8.GetBytes(expectedText);
        byte[] actualBytes = leaf.Data.ToByteArray();
        CollectionAssert.AreEqual(expectedBytes, actualBytes);
    }

    [TestMethod]
    public void TryReadText_WithNonUtf8Encoding_ShouldReturnFalse()
    {
        // Arrange
        byte[] binaryData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }; // Some binary data
        var data = new Hex(binaryData);
        var salt = Hex.Parse("0x12345678");
        var hash = MerkleTree.ComputeSha256Hash(data.ToByteArray());

        // Create a leaf with binary data (non-UTF-8)
        var leaf = new MerkleLeaf("application/octet-stream", data, salt, hash);

        // Act
        bool success = leaf.TryReadText(out string result);

        // Assert
        Assert.IsFalse(success);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void AddJsonLeaves_ShouldAddLeavesWithCorrectData()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?>
        {
            { "name", "John" },
            { "age", 30 },
            { "city", "New York" }
        };

        // Act
        var merkleTree = new MerkleTree();
        merkleTree.AddJsonLeaves(keyValues);

        // Assert
        Assert.AreEqual(3, merkleTree.Leaves.Count);
    }

    [TestMethod]
    public void FromJsonValue_WithComplexObject_StoresCorrectJson()
    {
        // Arrange
        string fieldName = "person";
        var fieldValue = new Dictionary<string, object?>
        {
            { "name", "Alice" },
            { "age", 28 },
            { "address", new Dictionary<string, string>
              {
                  { "street", "123 Main St" },
                  { "city", "Wonderland" }
              }
            }
        };

        var salt = Hex.Parse("0x12345678");

        // Act
        var leaf = MerkleLeaf.FromJsonValue(fieldName, fieldValue, salt, MerkleTree.ComputeSha256Hash);

        // Assert
        Assert.IsTrue(ContentTypeUtility.IsJson(leaf.ContentType));
        Assert.IsTrue(leaf.IsUtf8);

        // Verify the stored JSON data can be retrieved and parsed
        bool success = leaf.TryReadText(out string jsonText);
        Assert.IsTrue(success);

        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonText);
        Assert.AreEqual(JsonValueKind.Object, jsonElement.ValueKind);

        // Verify the person object exists
        Assert.IsTrue(jsonElement.TryGetProperty("person", out var personElement));
        Assert.AreEqual(JsonValueKind.Object, personElement.ValueKind);

        // Verify nested properties
        Assert.IsTrue(personElement.TryGetProperty("name", out var nameElement));
        Assert.AreEqual("Alice", nameElement.GetString());

        Assert.IsTrue(personElement.TryGetProperty("age", out var ageElement));
        Assert.AreEqual(28, ageElement.GetInt32());

        // Verify nested address object
        Assert.IsTrue(personElement.TryGetProperty("address", out var addressElement));
        Assert.AreEqual(JsonValueKind.Object, addressElement.ValueKind);
        Assert.AreEqual("123 Main St", addressElement.GetProperty("street").GetString());
        Assert.AreEqual("Wonderland", addressElement.GetProperty("city").GetString());
    }

    [TestMethod]
    public void AddJsonLeaves_WithMixedValues_ShouldCreateProperJsonLeaves()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?>
        {
            { "name", "Sarah" },
            { "age", 35 },
            { "isVerified", true },
            { "balance", 1234.56 },
            { "tags", new[] { "developer", "designer" } },
            { "address", new Dictionary<string, object?>
              {
                  { "street", "456 Oak Avenue" },
                  { "city", "San Francisco" },
                  { "zipCode", 94103 },
                  { "coordinates", new[] { 37.7749, -122.4194 } }
              }
            },
            { "lastLogin", null }
        };

        // Act
        var merkleTree = new MerkleTree();
        merkleTree.AddJsonLeaves(keyValues);
        merkleTree.RecomputeSha256Root();

        // Assert
        Assert.AreEqual(7, merkleTree.Leaves.Count); // One leaf per key-value pair

        // Verify the root can be validated
        Assert.IsTrue(merkleTree.VerifySha256Root());

        // Verify each leaf has the correct content type and is UTF-8
        foreach (var leaf in merkleTree.Leaves)
        {
            Assert.IsTrue(ContentTypeUtility.IsJson(leaf.ContentType));
            Assert.IsTrue(leaf.IsUtf8);
            Assert.IsFalse(leaf.Salt.IsEmpty());
        }

        // Verify individual leaves have correct JSON content
        bool foundNameLeaf = false;
        bool foundAddressLeaf = false;
        bool foundTagsLeaf = false;
        bool foundNullLeaf = false;

        foreach (var leaf in merkleTree.Leaves)
        {
            Assert.IsTrue(leaf.TryReadText(out string jsonText));
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonText);

            // Check for the name leaf
            if (jsonElement.TryGetProperty("name", out var nameValue) &&
                nameValue.ValueKind == JsonValueKind.String &&
                nameValue.GetString() == "Sarah")
            {
                foundNameLeaf = true;
            }

            // Check for the address leaf with nested properties
            if (jsonElement.TryGetProperty("address", out var addressValue) &&
                addressValue.ValueKind == JsonValueKind.Object)
            {
                foundAddressLeaf = true;

                // Verify nested properties
                Assert.AreEqual("456 Oak Avenue", addressValue.GetProperty("street").GetString());
                Assert.AreEqual("San Francisco", addressValue.GetProperty("city").GetString());
                Assert.AreEqual(94103, addressValue.GetProperty("zipCode").GetInt32());

                // Verify array of coordinates
                var coordinates = addressValue.GetProperty("coordinates");
                Assert.AreEqual(JsonValueKind.Array, coordinates.ValueKind);
                Assert.AreEqual(2, coordinates.GetArrayLength());
                Assert.AreEqual(37.7749, coordinates[0].GetDouble());
                Assert.AreEqual(-122.4194, coordinates[1].GetDouble());
            }

            // Check for the tags array leaf
            if (jsonElement.TryGetProperty("tags", out var tagsValue) &&
                tagsValue.ValueKind == JsonValueKind.Array)
            {
                foundTagsLeaf = true;

                Assert.AreEqual(2, tagsValue.GetArrayLength());
                Assert.AreEqual("developer", tagsValue[0].GetString());
                Assert.AreEqual("designer", tagsValue[1].GetString());
            }

            // Check for null value leaf
            if (jsonElement.TryGetProperty("lastLogin", out var lastLoginValue) &&
                lastLoginValue.ValueKind == JsonValueKind.Null)
            {
                foundNullLeaf = true;
            }
        }

        // Ensure we found all the expected leaves
        Assert.IsTrue(foundNameLeaf, "Leaf with 'name' field not found");
        Assert.IsTrue(foundAddressLeaf, "Leaf with 'address' field not found");
        Assert.IsTrue(foundTagsLeaf, "Leaf with 'tags' field not found");
        Assert.IsTrue(foundNullLeaf, "Leaf with null 'lastLogin' field not found");
    }

    [TestMethod]
    public void CreatePassportDataTree_SaveToFile_WithPrivateLeaf_PrintsLocation()
    {
        // Arrange - Create passport data with a flatter structure
        var passportData = new Dictionary<string, object?>
        {
            { "documentType", "passport" },
            { "documentNumber", "AB123456" },
            { "issueDate", "2020-01-01" },
            { "expiryDate", "2030-01-01" },
            { "issuingCountry", "United Kingdom" },
            { "givenName", "John" },
            { "surname", "Doe" },
            { "dateOfBirth", "1980-05-15" },
            { "placeOfBirth", "London" },
            { "gender", "M" },
            { "nationality", "British" },
            { "address", new Dictionary<string, object?>
                {
                    { "streetAddress", "123 Main Street" },
                    { "city", "London" },
                    { "postalCode", "SW1A 1AA" },
                    { "country", "United Kingdom" }
                }
            },
            { "biometricData", new Dictionary<string, object?>
                {
                    { "facialFeatures", "0xABCDEF1234567890" },
                    { "fingerprints", new[]
                        {
                            "0x1122334455667788",
                            "0x99AABBCCDDEEFF00"
                        }
                    }
                }
            }
        };

        // Act - Create the Merkle tree
        var merkleTree = new MerkleTree("1.0");

        // Dictionary to keep track of the leaf for each field
        var leafMap = new Dictionary<string, MerkleLeaf>();

        // Instead of using AddJsonLeaves, we'll manually add each leaf with hex-encoded JSON
        foreach (var pair in passportData)
        {
            // Convert the key-value pair to a JSON object
            var jsonObject = new Dictionary<string, object?>
            {
                { pair.Key, pair.Value }
            };

            string json = JsonSerializer.Serialize(jsonObject);
            Hex jsonHex = JsonToHex(json);

            // Use a content type that indicates it's JSON in UTF-8 encoded as hex
            string contentType = ContentTypeUtility.CreateJsonUtf8Hex();

            var leaf = merkleTree.AddLeaf(jsonHex, contentType);
            leafMap[pair.Key] = leaf;
        }

        merkleTree.RecomputeSha256Root();

        // Verify the root
        Assert.IsTrue(merkleTree.VerifySha256Root());

        // Create a predicate that makes the document number private
        Predicate<MerkleLeaf> makePrivate = leaf =>
            leaf.TryReadText(out string text) && text.Contains("documentNumber");

        // Create JSON options to omit null values
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Save to a temporary file with one private leaf
        string tempFilePath = Path.Combine(Path.GetTempPath(), $"passport_merkle_tree_with_private_{Guid.NewGuid()}.json");
        File.WriteAllText(tempFilePath, merkleTree.ToJson(MerkleTree.ComputeSha256Hash, makePrivate, jsonOptions));

        // Print the file location for manual inspection (with attention-grabbing markers)
        Console.WriteLine("==================================================================");
        Console.WriteLine($"MERKLE TREE WITH PRIVATE DOCUMENT NUMBER: {tempFilePath}");

        // Print the first few lines of the file for quick inspection
        string fileContent = File.ReadAllText(tempFilePath);
        Console.WriteLine("File content:");
        Console.WriteLine(fileContent);
        Console.WriteLine("==================================================================");

        // Ensure the file exists (for the test)
        Assert.IsTrue(File.Exists(tempFilePath));

        // Verify that we can still parse and verify the tree with the private leaf
        var parsedTree = MerkleTree.Parse(fileContent);
        Assert.IsTrue(parsedTree.VerifySha256Root(), "Tree with private document number should verify");

        // Optionally cleanup
        // File.Delete(tempFilePath);
    }

    [TestMethod]
    public void ToJson_WithPrivateLeaves_ShouldOmitDataAndSaltForPrivateLeaves()
    {
        // Arrange
        var tree = new MerkleTree();

        // Add three leaves - one for each data type
        var leafA = tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        var leafB = tree.AddJsonLeaf("dob", "1980-01-15", Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        var leafC = tree.AddJsonLeaf("ssn", "123-45-6789", Hex.Parse("0x112233"), MerkleTree.ComputeSha256Hash);

        tree.RecomputeSha256Root();

        // Create a predicate that only makes the SSN private
        Predicate<MerkleLeaf> makePrivate = leaf =>
            leaf.TryReadText(out string text) && text.Contains("ssn");

        // Act
        string json = tree.ToJson(MerkleTree.ComputeSha256Hash, makePrivate);

        // Assert
        // Parse the JSON for inspection
        var jsonDoc = JsonDocument.Parse(json);
        var leavesArray = jsonDoc.RootElement.GetProperty("leaves").EnumerateArray().ToArray();

        // Should have 3 leaves
        Assert.AreEqual(3, leavesArray.Length);

        // Check each leaf
        foreach (var leafElement in leavesArray)
        {
            // All leaves should have hash property
            Assert.IsTrue(leafElement.TryGetProperty("hash", out _), "All leaves should have a hash");

            bool hasData = leafElement.TryGetProperty("data", out var dataProperty);
            bool hasSalt = leafElement.TryGetProperty("salt", out _);
            bool hasContentType = leafElement.TryGetProperty("contentType", out _);

            // If this is the private leaf (SSN)
            if (hasData && dataProperty.ValueKind == JsonValueKind.String)
            {
                string dataValue = dataProperty.GetString() ?? string.Empty;

                if (dataValue.Contains("ssn"))
                {
                    Assert.Fail("Found SSN data in JSON output when it should be private");
                }
            }

            // Private leaves should have only hash property
            if (!hasData && !hasSalt && !hasContentType)
            {
                // This should be our private leaf - verify it has the hash
                Assert.IsTrue(leafElement.TryGetProperty("hash", out _),
                    "Private leaf should have a hash");
            }
            else
            {
                // Non-private leaves should have all properties
                Assert.IsTrue(hasData, "Non-private leaf should have data");
                Assert.IsTrue(hasSalt, "Non-private leaf should have salt");
                Assert.IsTrue(hasContentType, "Non-private leaf should have contentType");
            }
        }

        // Verify we can parse it back into a valid tree
        var parsedTree = MerkleTree.Parse(json);
        Assert.IsTrue(parsedTree.VerifySha256Root(), "Parsed tree from JSON with private leaves should verify");
    }

    [TestMethod]
    public void VerifyRoot_ShouldDetectTamperedLeafData()
    {
        // Arrange - Create a merkle tree with some data
        var tree = new MerkleTree();
        tree.AddJsonLeaf("name", "John Doe", Hex.Parse("0xaabbcc"), MerkleTree.ComputeSha256Hash);
        tree.AddJsonLeaf("age", 30, Hex.Parse("0xddeeff"), MerkleTree.ComputeSha256Hash);
        tree.RecomputeSha256Root();

        // Serialize to JSON
        string json = tree.ToJson();

        // Act - Create tampered JSON by replacing the hex-encoded leaf data
        // First, let's create the tampered data
        string originalContent = "{\"name\":\"John Doe\"}";
        string tamperedContent = "{\"name\":\"Jane Doe\"}";

        // Convert to hex representations
        string originalHex = BytesToHexString(System.Text.Encoding.UTF8.GetBytes(originalContent));
        string tamperedHex = BytesToHexString(System.Text.Encoding.UTF8.GetBytes(tamperedContent));

        // Replace in the JSON
        string tamperedJson = json.Replace(originalHex, tamperedHex);

        // Parse the tampered JSON back into a tree
        var parsedTree = MerkleTree.Parse(tamperedJson);

        // Assert - Verification should fail, detecting the tampering
        Assert.IsFalse(parsedTree.VerifySha256Root(), "Tree verification should detect tampered leaf data");
    }

    //

    private static string BytesToHexString(byte[] bytes)
    {
        return "0x" + BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    private static Hex JsonToHex(string json)
    {
        // Convert the JSON string to UTF-8 encoded bytes
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Create a hex from those bytes
        return new Hex(jsonBytes);
    }
}