using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Evoq.Blockchain.Merkle;

namespace Evoq.Blockchain.Tests.Merkle;

[TestClass]
public class MerkleTreeAddObjectLeavesTests
{
    [TestMethod]
    public void AddObjectLeaves__when__simple_object_with_two_string_properties__then__creates_two_leaves()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var simpleObject = new
        {
            name = "John",
            birthplace = "London"
        };

        // Act
        merkleTree.AddObjectLeaves(simpleObject);

        // Assert - Before root computation, should have 2 data leaves
        Assert.AreEqual(2, merkleTree.Leaves.Count, "Should have 2 data leaves before root computation");

        // Compute root (this adds the header leaf)
        merkleTree.RecomputeSha256Root();

        // Verify we can decode the leaves and find our data
        var json = merkleTree.ToJson();
        var doc = JsonDocument.Parse(json);
        var leaves = doc.RootElement.GetProperty("leaves");

        // After RecomputeSha256Root, V2.0 may or may not add a header leaf
        // Check that we have at least 2 data leaves
        Assert.IsTrue(leaves.GetArrayLength() >= 2, "Should have at least 2 data leaves after root computation");

        // Find the data leaves (start from index 0, as V2.0 may not have header leaf at index 0)
        var foundName = false;
        var foundBirthplace = false;

        for (int i = 0; i < leaves.GetArrayLength(); i++)
        {
            var leaf = leaves[i];
            if (leaf.TryGetProperty("data", out var dataElement))
            {
                var hexData = dataElement.GetString();
                var decoded = DecodeHexToUtf8(hexData!);
                var leafJson = JsonDocument.Parse(decoded);

                if (leafJson.RootElement.TryGetProperty("name", out var nameProp))
                {
                    Assert.AreEqual("John", nameProp.GetString());
                    foundName = true;
                }
                else if (leafJson.RootElement.TryGetProperty("birthplace", out var birthplaceProp))
                {
                    Assert.AreEqual("London", birthplaceProp.GetString());
                    foundBirthplace = true;
                }
            }
        }

        Assert.IsTrue(foundName, "Should find 'name' property in leaves");
        Assert.IsTrue(foundBirthplace, "Should find 'birthplace' property in leaves");
    }

    [TestMethod]
    public void AddObjectLeaves__when__object_with_mixed_primitive_types__then__creates_leaves_with_correct_types()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var mixedObject = new
        {
            name = "John",
            age = 30,
            active = true,
            score = 95.5,
            metadata = (string?)null
        };

        // Act
        merkleTree.AddObjectLeaves(mixedObject);
        merkleTree.RecomputeSha256Root();

        // Assert
        var json = merkleTree.ToJson();
        var doc = JsonDocument.Parse(json);
        var leaves = doc.RootElement.GetProperty("leaves");

        // Should have at least 5 data leaves (V2.0 doesn't add header leaf)
        Assert.IsTrue(leaves.GetArrayLength() >= 5, "Should have at least 5 data leaves");

        // Verify all properties are present with correct types
        var foundProperties = new HashSet<string>();

        for (int i = 0; i < leaves.GetArrayLength(); i++)
        {
            var leaf = leaves[i];
            if (leaf.TryGetProperty("data", out var dataElement))
            {
                var hexData = dataElement.GetString();
                var decoded = DecodeHexToUtf8(hexData!);
                var leafJson = JsonDocument.Parse(decoded);

                foreach (var prop in leafJson.RootElement.EnumerateObject())
                {
                    foundProperties.Add(prop.Name);

                    switch (prop.Name)
                    {
                        case "name":
                            Assert.AreEqual(JsonValueKind.String, prop.Value.ValueKind);
                            Assert.AreEqual("John", prop.Value.GetString());
                            break;
                        case "age":
                            Assert.AreEqual(JsonValueKind.Number, prop.Value.ValueKind);
                            Assert.AreEqual(30, prop.Value.GetInt32());
                            break;
                        case "active":
                            Assert.AreEqual(JsonValueKind.True, prop.Value.ValueKind);
                            Assert.IsTrue(prop.Value.GetBoolean());
                            break;
                        case "score":
                            Assert.AreEqual(JsonValueKind.Number, prop.Value.ValueKind);
                            Assert.AreEqual(95.5, prop.Value.GetDouble(), 0.01);
                            break;
                        case "metadata":
                            Assert.AreEqual(JsonValueKind.Null, prop.Value.ValueKind);
                            break;
                    }
                }
            }
        }

        Assert.AreEqual(5, foundProperties.Count, "Should find all 5 properties");
        Assert.IsTrue(foundProperties.Contains("name"));
        Assert.IsTrue(foundProperties.Contains("age"));
        Assert.IsTrue(foundProperties.Contains("active"));
        Assert.IsTrue(foundProperties.Contains("score"));
        Assert.IsTrue(foundProperties.Contains("metadata"));
    }

    [TestMethod]
    public void AddObjectLeaves__when__object_with_nested_object__then__creates_leaf_with_serialized_nested_object()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var objectWithNesting = new
        {
            name = "John",
            mother = new
            {
                name = "Jane",
                birthplace = "Paris"
            }
        };

        // Act
        merkleTree.AddObjectLeaves(objectWithNesting);
        merkleTree.RecomputeSha256Root();

        // Assert
        var json = merkleTree.ToJson();
        var doc = JsonDocument.Parse(json);
        var leaves = doc.RootElement.GetProperty("leaves");

        // Should have at least 2 data leaves (name and mother) - V2.0 doesn't add header leaf
        Assert.IsTrue(leaves.GetArrayLength() >= 2, "Should have at least 2 data leaves");

        var foundName = false;
        var foundMother = false;

        for (int i = 0; i < leaves.GetArrayLength(); i++)
        {
            var leaf = leaves[i];
            if (leaf.TryGetProperty("data", out var dataElement))
            {
                var hexData = dataElement.GetString();
                var decoded = DecodeHexToUtf8(hexData!);
                var leafJson = JsonDocument.Parse(decoded);

                if (leafJson.RootElement.TryGetProperty("name", out var nameProp))
                {
                    Assert.AreEqual("John", nameProp.GetString());
                    foundName = true;
                }
                else if (leafJson.RootElement.TryGetProperty("mother", out var motherProp))
                {
                    // Verify the nested object is fully serialized
                    Assert.AreEqual(JsonValueKind.Object, motherProp.ValueKind);
                    Assert.IsTrue(motherProp.TryGetProperty("name", out var motherName));
                    Assert.AreEqual("Jane", motherName.GetString());
                    Assert.IsTrue(motherProp.TryGetProperty("birthplace", out var motherBirthplace));
                    Assert.AreEqual("Paris", motherBirthplace.GetString());
                    foundMother = true;
                }
            }
        }

        Assert.IsTrue(foundName, "Should find 'name' property");
        Assert.IsTrue(foundMother, "Should find 'mother' property with nested object");
    }

    [TestMethod]
    public void AddObjectLeaves__when__object_with_deeply_nested_object__then__creates_leaf_with_full_nested_structure()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var deeplyNestedObject = new
        {
            name = "John",
            mother = new
            {
                name = "Jane",
                mother = new
                {
                    name = "Mary",
                    birthplace = "Rome"
                }
            }
        };

        // Act
        merkleTree.AddObjectLeaves(deeplyNestedObject);
        merkleTree.RecomputeSha256Root();

        // Assert
        var json = merkleTree.ToJson();
        var doc = JsonDocument.Parse(json);
        var leaves = doc.RootElement.GetProperty("leaves");

        // Should have at least 2 data leaves (name and mother) - V2.0 doesn't add header leaf
        Assert.IsTrue(leaves.GetArrayLength() >= 2, "Should have at least 2 data leaves");

        var foundMother = false;

        for (int i = 0; i < leaves.GetArrayLength(); i++)
        {
            var leaf = leaves[i];
            if (leaf.TryGetProperty("data", out var dataElement))
            {
                var hexData = dataElement.GetString();
                var decoded = DecodeHexToUtf8(hexData!);
                var leafJson = JsonDocument.Parse(decoded);

                if (leafJson.RootElement.TryGetProperty("mother", out var motherProp))
                {
                    // Verify the deeply nested structure is fully serialized
                    Assert.AreEqual(JsonValueKind.Object, motherProp.ValueKind);
                    Assert.IsTrue(motherProp.TryGetProperty("name", out var motherName));
                    Assert.AreEqual("Jane", motherName.GetString());

                    // Verify nested mother object
                    Assert.IsTrue(motherProp.TryGetProperty("mother", out var grandmotherProp));
                    Assert.AreEqual(JsonValueKind.Object, grandmotherProp.ValueKind);
                    Assert.IsTrue(grandmotherProp.TryGetProperty("name", out var grandmotherName));
                    Assert.AreEqual("Mary", grandmotherName.GetString());
                    Assert.IsTrue(grandmotherProp.TryGetProperty("birthplace", out var grandmotherBirthplace));
                    Assert.AreEqual("Rome", grandmotherBirthplace.GetString());

                    foundMother = true;
                }
            }
        }

        Assert.IsTrue(foundMother, "Should find 'mother' property with deeply nested structure");
    }

    [TestMethod]
    public void AddObjectLeaves__when__object_with_arrays_and_nested_objects__then__creates_leaves_correctly()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var complexObject = new
        {
            name = "John",
            tags = new[] { "tag1", "tag2", "tag3" },
            addresses = new[]
            {
                new { street = "123 Main", city = "London" },
                new { street = "456 Oak", city = "Paris" }
            }
        };

        // Act
        merkleTree.AddObjectLeaves(complexObject);
        merkleTree.RecomputeSha256Root();

        // Assert
        var json = merkleTree.ToJson();
        var doc = JsonDocument.Parse(json);
        var leaves = doc.RootElement.GetProperty("leaves");

        // Should have at least 3 data leaves (name, tags, addresses) - V2.0 doesn't add header leaf
        Assert.IsTrue(leaves.GetArrayLength() >= 3, "Should have at least 3 data leaves");

        var foundName = false;
        var foundTags = false;
        var foundAddresses = false;

        for (int i = 0; i < leaves.GetArrayLength(); i++)
        {
            var leaf = leaves[i];
            if (leaf.TryGetProperty("data", out var dataElement))
            {
                var hexData = dataElement.GetString();
                var decoded = DecodeHexToUtf8(hexData!);
                var leafJson = JsonDocument.Parse(decoded);

                if (leafJson.RootElement.TryGetProperty("name", out var nameProp))
                {
                    Assert.AreEqual("John", nameProp.GetString());
                    foundName = true;
                }
                else if (leafJson.RootElement.TryGetProperty("tags", out var tagsProp))
                {
                    // Verify array is serialized
                    Assert.AreEqual(JsonValueKind.Array, tagsProp.ValueKind);
                    Assert.AreEqual(3, tagsProp.GetArrayLength());
                    var tagArray = tagsProp.EnumerateArray().ToArray();
                    Assert.AreEqual("tag1", tagArray[0].GetString());
                    Assert.AreEqual("tag2", tagArray[1].GetString());
                    Assert.AreEqual("tag3", tagArray[2].GetString());
                    foundTags = true;
                }
                else if (leafJson.RootElement.TryGetProperty("addresses", out var addressesProp))
                {
                    // Verify array of objects is serialized
                    Assert.AreEqual(JsonValueKind.Array, addressesProp.ValueKind);
                    Assert.AreEqual(2, addressesProp.GetArrayLength());
                    var addressArray = addressesProp.EnumerateArray().ToArray();

                    // First address
                    Assert.IsTrue(addressArray[0].TryGetProperty("street", out var street1));
                    Assert.AreEqual("123 Main", street1.GetString());
                    Assert.IsTrue(addressArray[0].TryGetProperty("city", out var city1));
                    Assert.AreEqual("London", city1.GetString());

                    // Second address
                    Assert.IsTrue(addressArray[1].TryGetProperty("street", out var street2));
                    Assert.AreEqual("456 Oak", street2.GetString());
                    Assert.IsTrue(addressArray[1].TryGetProperty("city", out var city2));
                    Assert.AreEqual("Paris", city2.GetString());

                    foundAddresses = true;
                }
            }
        }

        Assert.IsTrue(foundName, "Should find 'name' property");
        Assert.IsTrue(foundTags, "Should find 'tags' array property");
        Assert.IsTrue(foundAddresses, "Should find 'addresses' array property");
    }

    [TestMethod]
    public void AddObjectLeaves__when__object_with_integers__then__preserves_integer_types_not_doubles()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var objectWithIntegers = new
        {
            smallInt = 42,           // int
            largeInt = 999999L,      // long
            negativeInt = -100,      // negative int
            zero = 0,                // zero
            maxInt = int.MaxValue,   // max int
            minInt = int.MinValue    // min int
        };

        // Act
        merkleTree.AddObjectLeaves(objectWithIntegers);
        merkleTree.RecomputeSha256Root();

        // Assert
        var json = merkleTree.ToJson();
        var doc = JsonDocument.Parse(json);
        var leaves = doc.RootElement.GetProperty("leaves");

        // Find and verify each integer property
        var foundProperties = new Dictionary<string, JsonElement>();

        for (int i = 0; i < leaves.GetArrayLength(); i++)
        {
            var leaf = leaves[i];
            if (leaf.TryGetProperty("data", out var dataElement))
            {
                var hexData = dataElement.GetString();
                var decoded = DecodeHexToUtf8(hexData!);
                var leafJson = JsonDocument.Parse(decoded);

                foreach (var prop in leafJson.RootElement.EnumerateObject())
                {
                    foundProperties[prop.Name] = prop.Value;
                }
            }
        }

        // Verify all integer properties are present
        Assert.AreEqual(6, foundProperties.Count, "Should find all 6 integer properties");

        // Verify each integer can be read as an integer (not just as a double)
        // The key test: TryGetInt32/Int64 should succeed, and the JSON should not have decimal points
        Assert.IsTrue(foundProperties["smallInt"].TryGetInt32(out var smallIntValue));
        Assert.AreEqual(42, smallIntValue);
        Assert.IsFalse(foundProperties["smallInt"].ToString().Contains("."), "Integer should not have decimal point in JSON");

        Assert.IsTrue(foundProperties["largeInt"].TryGetInt64(out var largeIntValue));
        Assert.AreEqual(999999L, largeIntValue);
        Assert.IsFalse(foundProperties["largeInt"].ToString().Contains("."), "Integer should not have decimal point in JSON");

        Assert.IsTrue(foundProperties["negativeInt"].TryGetInt32(out var negativeIntValue));
        Assert.AreEqual(-100, negativeIntValue);
        Assert.IsFalse(foundProperties["negativeInt"].ToString().Contains("."), "Negative integer should not have decimal point in JSON");

        Assert.IsTrue(foundProperties["zero"].TryGetInt32(out var zeroValue));
        Assert.AreEqual(0, zeroValue);
        Assert.IsFalse(foundProperties["zero"].ToString().Contains("."), "Zero should not have decimal point in JSON");

        Assert.IsTrue(foundProperties["maxInt"].TryGetInt32(out var maxIntValue));
        Assert.AreEqual(int.MaxValue, maxIntValue);
        Assert.IsFalse(foundProperties["maxInt"].ToString().Contains("."), "Max int should not have decimal point in JSON");

        Assert.IsTrue(foundProperties["minInt"].TryGetInt32(out var minIntValue));
        Assert.AreEqual(int.MinValue, minIntValue);
        Assert.IsFalse(foundProperties["minInt"].ToString().Contains("."), "Min int should not have decimal point in JSON");

        // Additional verification: The JSON representation should be pure integers (no .0 suffix)
        // This ensures our ConvertJsonValue method preserved integers correctly
        var smallIntJson = foundProperties["smallInt"].GetRawText();
        Assert.IsFalse(smallIntJson.Contains("."), $"Integer JSON should not contain decimal: {smallIntJson}");
        Assert.AreEqual("42", smallIntJson, "Integer should serialize as '42' not '42.0'");
    }

    [TestMethod]
    public void AddObjectLeaves__when__v3_0_simple_object__then__creates_header_leaf_and_data_leaves()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        merkleTree.Metadata.ExchangeDocumentType = "test-exchange";
        var simpleObject = new
        {
            name = "John",
            birthplace = "London"
        };

        // Act
        merkleTree.AddObjectLeaves(simpleObject);

        // Assert - Before root computation, should have 2 data leaves (no header yet)
        Assert.AreEqual(2, merkleTree.Leaves.Count, "Should have 2 data leaves before root computation");

        // Compute root (this adds the header leaf at index 0 for V3.0)
        merkleTree.RecomputeSha256Root();

        // After RecomputeSha256Root, V3.0 adds a header leaf at index 0
        // So we should have: 1 header leaf + 2 data leaves = 3 total leaves
        Assert.AreEqual(3, merkleTree.Leaves.Count, "V3.0 should have 3 leaves total (1 header + 2 data) after root computation");

        // Verify the header leaf is at index 0
        var headerLeaf = merkleTree.Leaves[0];
        Assert.IsTrue(headerLeaf.IsMetadata, "First leaf should be the V3.0 header leaf");
        Assert.AreEqual(
            "application/merkle-exchange-header-3.0+json; charset=utf-8; encoding=hex",
            headerLeaf.ContentType,
            "Header leaf should have correct content type");

        // Verify we can decode the data leaves and find our data
        var json = merkleTree.ToJson();
        var doc = JsonDocument.Parse(json);
        var leaves = doc.RootElement.GetProperty("leaves");

        // Should have exactly 3 leaves total
        Assert.AreEqual(3, leaves.GetArrayLength(), "Should have exactly 3 leaves (1 header + 2 data)");

        // Find the data leaves (skip header leaf at index 0)
        var foundName = false;
        var foundBirthplace = false;

        for (int i = 0; i < leaves.GetArrayLength(); i++)
        {
            var leaf = leaves[i];

            // Skip header leaf - check contentType to identify it
            if (leaf.TryGetProperty("contentType", out var contentTypeElement))
            {
                var contentType = contentTypeElement.GetString();
                if (contentType != null && contentType.Contains("merkle-exchange-header"))
                {
                    // This is the header leaf, skip it
                    Assert.AreEqual(0, i, "Header leaf should be at index 0");
                    continue;
                }
            }

            // Process data leaves
            if (leaf.TryGetProperty("data", out var dataElement))
            {
                var hexData = dataElement.GetString();
                var decoded = DecodeHexToUtf8(hexData!);
                var leafJson = JsonDocument.Parse(decoded);

                if (leafJson.RootElement.TryGetProperty("name", out var nameProp))
                {
                    Assert.AreEqual("John", nameProp.GetString());
                    foundName = true;
                }
                else if (leafJson.RootElement.TryGetProperty("birthplace", out var birthplaceProp))
                {
                    Assert.AreEqual("London", birthplaceProp.GetString());
                    foundBirthplace = true;
                }
            }
        }

        Assert.IsTrue(foundName, "Should find 'name' property in data leaves");
        Assert.IsTrue(foundBirthplace, "Should find 'birthplace' property in data leaves");

        // Verify header leaf data contains correct metadata
        var headerLeafData = merkleTree.Leaves[0].Data;
        var headerJson = System.Text.Encoding.UTF8.GetString(headerLeafData.ToByteArray());
        var headerDoc = JsonDocument.Parse(headerJson);
        var headerRoot = headerDoc.RootElement;

        Assert.AreEqual("SHA256", headerRoot.GetProperty("alg").GetString(), "Header should specify SHA256 algorithm");
        Assert.AreEqual(3, headerRoot.GetProperty("leaves").GetInt32(), "Header should report 3 total leaves");
        Assert.AreEqual("test-exchange", headerRoot.GetProperty("exchange").GetString(), "Header should preserve exchange document type");
    }

    private static string DecodeHexToUtf8(string hexData)
    {
        if (string.IsNullOrEmpty(hexData))
            throw new ArgumentException("Hex data cannot be null or empty");

        // Remove 0x prefix if present
        if (hexData.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hexData = hexData.Substring(2);

        // Convert hex to bytes
        var bytes = new byte[hexData.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexData.Substring(i * 2, 2), 16);
        }

        // Convert bytes to UTF-8 string
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}

