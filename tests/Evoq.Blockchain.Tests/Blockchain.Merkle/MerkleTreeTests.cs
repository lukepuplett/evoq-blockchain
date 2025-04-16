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
        // Arrange
        string originalJson = @"{
            ""leaves"": [
                {
                    ""data"": ""data1"",
                    ""salt"": ""0xaabbcc"",
                    ""hash"": ""0x1234567890abcdef"",
                    ""contentType"": ""text/plain; charset=utf-8""
                },
                {
                    ""data"": ""data2"",
                    ""salt"": ""0xddeeff"",
                    ""hash"": ""0xabcdef1234567890"",
                    ""contentType"": ""application/json; charset=utf-8""
                }
            ],
            ""root"": ""0xc5185f0d2dfb9f5b4079ecaaac77a48cbe6758197333528d02f070e006420c79"",
            ""metadata"": {
                ""hashAlgorithm"": ""sha256"",
                ""version"": ""1.0""
            }
        }";

        // Act
        var originalTree = MerkleTree.Parse(originalJson);
        string roundtrippedJson = originalTree.ToJson();
        var roundtrippedTree = MerkleTree.Parse(roundtrippedJson);

        // Assert - Compare trees rather than exact JSON
        Assert.AreEqual(originalTree.Root, roundtrippedTree.Root);
        Assert.AreEqual(originalTree.Metadata.HashAlgorithm, roundtrippedTree.Metadata.HashAlgorithm);
        Assert.AreEqual(originalTree.Metadata.Version, roundtrippedTree.Metadata.Version);
        Assert.AreEqual(originalTree.Leaves.Count, roundtrippedTree.Leaves.Count);

        // Compare leaves
        for (int i = 0; i < originalTree.Leaves.Count; i++)
        {
            Assert.AreEqual(originalTree.Leaves[i].Hash, roundtrippedTree.Leaves[i].Hash);
            Assert.AreEqual(originalTree.Leaves[i].Salt, roundtrippedTree.Leaves[i].Salt);
            Assert.AreEqual(originalTree.Leaves[i].ContentType, roundtrippedTree.Leaves[i].ContentType);

            // Compare actual data bytes rather than serialized format
            CollectionAssert.AreEqual(
                originalTree.Leaves[i].Data.ToByteArray(),
                roundtrippedTree.Leaves[i].Data.ToByteArray());

            // Verify that TryReadText still works correctly
            bool originalCanReadText = originalTree.Leaves[i].TryReadText(out string originalText);
            bool roundtrippedCanReadText = roundtrippedTree.Leaves[i].TryReadText(out string roundtrippedText);

            Assert.AreEqual(originalCanReadText, roundtrippedCanReadText);
            if (originalCanReadText)
            {
                Assert.AreEqual(originalText, roundtrippedText);
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
        merkleTree.AddJsonLeaves(keyValues, Hex.Empty, MerkleTree.ComputeSha256Hash);

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
        var salt = Hex.Parse("0x98765432");
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
        merkleTree.AddJsonLeaves(keyValues, salt, MerkleTree.ComputeSha256Hash);
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
            Assert.AreEqual(salt, leaf.Salt);
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
    public void CreatePassportDataTree_SaveToFile_PrintsLocation()
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

        // Create a unique salt for this passport
        var salt = Hex.Parse("0x7f8e7d6c5b4a3210");

        // Act - Create the Merkle tree
        var merkleTree = new MerkleTree("1.0");

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

            merkleTree.AddLeaf(jsonHex, salt, contentType, MerkleTree.ComputeSha256Hash);
        }

        merkleTree.RecomputeSha256Root();

        // Verify the root
        Assert.IsTrue(merkleTree.VerifySha256Root());

        // Save to a temporary file
        string tempFilePath = Path.Combine(Path.GetTempPath(), $"passport_merkle_tree_{Guid.NewGuid()}.json");
        File.WriteAllText(tempFilePath, merkleTree.ToJson());

        // Print the file location for manual inspection (with attention-grabbing markers)
        Console.WriteLine("==================================================================");
        Console.WriteLine($"MERKLE TREE FILE LOCATION: {tempFilePath}");

        // Print the first few lines of the file for quick inspection
        string fileContent = File.ReadAllText(tempFilePath);
        string firstFewLines = string.Join(Environment.NewLine,
            fileContent.Split(Environment.NewLine).Take(15));
        Console.WriteLine("First few lines of the file:");
        Console.WriteLine(firstFewLines);
        Console.WriteLine("==================================================================");

        // Ensure the file exists (for the test)
        Assert.IsTrue(File.Exists(tempFilePath));

        // Cleanup (optional, comment this out if you want to keep the file)
        // File.Delete(tempFilePath);
    }

    /// <summary>
    /// Converts a JSON string to a hex-encoded byte array.
    /// </summary>
    /// <param name="json">The JSON string to convert.</param>
    /// <returns>A hex representation of the JSON string.</returns>
    private static Hex JsonToHex(string json)
    {
        // Convert the JSON string to UTF-8 encoded bytes
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Create a hex from those bytes
        return new Hex(jsonBytes);
    }
}