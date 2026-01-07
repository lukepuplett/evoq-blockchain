using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Evoq.Blockchain.Merkle;

namespace Evoq.Blockchain.Tests.Merkle;

[TestClass]
public class MerkleTreeGetObjectFromLeavesTests
{
    /// <summary>
    /// Test 1: Simple round-trip with basic string properties.
    /// This is the most basic case - add an object with simple properties and reconstruct it.
    /// </summary>
    [TestMethod]
    public void GetObjectFromLeaves__when__simple_object_with_string_properties__then__reconstructs_correctly()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new
        {
            name = "John",
            birthplace = "London"
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Act
        var reconstructed = merkleTree.GetObjectFromLeaves<Dictionary<string, object>>();

        // Assert
        Assert.IsNotNull(reconstructed);
        Assert.AreEqual(2, reconstructed.Count);
        Assert.AreEqual("John", reconstructed["name"].ToString());
        Assert.AreEqual("London", reconstructed["birthplace"].ToString());
    }

    /// <summary>
    /// Test 2: Mixed primitive types (string, int, bool, double, null).
    /// This tests type preservation through the round-trip process.
    /// </summary>
    [TestMethod]
    public void GetObjectFromLeaves__when__object_with_mixed_primitive_types__then__preserves_types()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new
        {
            name = "John",
            age = 30,
            active = true,
            score = 95.5,
            metadata = (string?)null
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Act
        var reconstructed = merkleTree.GetObjectFromLeaves<Dictionary<string, object>>();

        // Assert
        Assert.IsNotNull(reconstructed);
        Assert.AreEqual(5, reconstructed.Count);
        Assert.AreEqual("John", reconstructed["name"].ToString());
        Assert.AreEqual(30L, reconstructed["age"]); // JSON numbers become long/int64
        Assert.AreEqual(true, reconstructed["active"]);
        Assert.AreEqual(95.5, (double)reconstructed["score"], 0.01);
        Assert.IsNull(reconstructed["metadata"]);
    }

    /// <summary>
    /// Test 3: Nested objects.
    /// This tests that nested structures are properly preserved and reconstructed.
    /// </summary>
    [TestMethod]
    public void GetObjectFromLeaves__when__object_with_nested_object__then__reconstructs_nested_structure()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new
        {
            name = "John",
            mother = new
            {
                name = "Jane",
                birthplace = "Paris"
            }
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Act
        var reconstructed = merkleTree.GetObjectFromLeaves<Dictionary<string, object>>();

        // Assert
        Assert.IsNotNull(reconstructed);
        Assert.AreEqual(2, reconstructed.Count);
        Assert.AreEqual("John", reconstructed["name"].ToString());

        var mother = (Dictionary<string, object>)reconstructed["mother"];
        Assert.IsNotNull(mother);
        Assert.AreEqual("Jane", mother["name"].ToString());
        Assert.AreEqual("Paris", mother["birthplace"].ToString());
    }

    /// <summary>
    /// Test 4: V3.0 with header leaf.
    /// This tests that header leaves are properly skipped during reconstruction.
    /// </summary>
    [TestMethod]
    public void GetObjectFromLeaves__when__v3_0_with_header_leaf__then__skips_header_and_reconstructs_data()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V3_0);
        merkleTree.Metadata.ExchangeDocumentType = "test-exchange";
        var original = new
        {
            name = "John",
            city = "London"
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root(); // This adds the header leaf

        // Verify header leaf exists
        Assert.IsTrue(merkleTree.Leaves[0].IsMetadata, "First leaf should be header leaf");

        // Act
        var reconstructed = merkleTree.GetObjectFromLeaves<Dictionary<string, object>>();

        // Assert - should only have the 2 data properties, not the header
        Assert.IsNotNull(reconstructed);
        Assert.AreEqual(2, reconstructed.Count, "Should only have data properties, not header properties");
        Assert.AreEqual("John", reconstructed["name"].ToString());
        Assert.AreEqual("London", reconstructed["city"].ToString());

        // Verify header properties are not present
        Assert.IsFalse(reconstructed.ContainsKey("alg"));
        Assert.IsFalse(reconstructed.ContainsKey("typ"));
        Assert.IsFalse(reconstructed.ContainsKey("leaves"));
        Assert.IsFalse(reconstructed.ContainsKey("exchange"));
    }

    /// <summary>
    /// Test 5: Private leaves causing missing required data.
    /// This tests that when required properties are in private leaves, an appropriate exception is thrown.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(MissingLeafDataException))]
    public void GetObjectFromLeaves__when__required_property_is_private__then__throws_MissingLeafDataException()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new PersonWithRequiredName
        {
            Name = "John",  // This will be required
            Secret = "hidden"
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Make the 'name' leaf private (which is required for reconstruction)
        var nameLeaf = merkleTree.Leaves.FirstOrDefault(l =>
        {
            var data = System.Text.Encoding.UTF8.GetString(l.Data.ToByteArray());
            return data.Contains("\"name\"") || data.Contains("\"Name\"");
        });

        Assert.IsNotNull(nameLeaf, "Should find the name leaf");

        // Create a selective disclosure version where 'name' is private
        var privateTree = MerkleTree.From(merkleTree, leaf =>
            leaf == nameLeaf // Make only the name leaf private
        );

        // Act - This should throw MissingLeafDataException because 'Name' is required but private
        var reconstructed = privateTree.GetObjectFromLeaves<PersonWithRequiredName>();

        // If we get here, the test failed (should have thrown)
        Assert.Fail("Expected MissingLeafDataException was not thrown");
    }

    /// <summary>
    /// Test 6: Arrays (primitives and objects).
    /// This tests that arrays are properly preserved and reconstructed.
    /// </summary>
    [TestMethod]
    public void GetObjectFromLeaves__when__object_with_arrays__then__reconstructs_arrays_correctly()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new
        {
            name = "John",
            tags = new[] { "tag1", "tag2", "tag3" },
            addresses = new[]
            {
                new { street = "123 Main", city = "London" },
                new { street = "456 Oak", city = "Paris" }
            }
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Act
        var reconstructed = merkleTree.GetObjectFromLeaves<Dictionary<string, object>>();

        // Assert
        Assert.IsNotNull(reconstructed);
        Assert.AreEqual(3, reconstructed.Count);
        Assert.AreEqual("John", reconstructed["name"].ToString());

        // Verify array of primitives
        var tags = (object[])reconstructed["tags"];
        Assert.IsNotNull(tags);
        Assert.AreEqual(3, tags.Length);
        Assert.AreEqual("tag1", tags[0].ToString());
        Assert.AreEqual("tag2", tags[1].ToString());
        Assert.AreEqual("tag3", tags[2].ToString());

        // Verify array of objects
        var addresses = (object[])reconstructed["addresses"];
        Assert.IsNotNull(addresses);
        Assert.AreEqual(2, addresses.Length);

        var addr1 = (Dictionary<string, object>)addresses[0];
        Assert.AreEqual("123 Main", addr1["street"].ToString());
        Assert.AreEqual("London", addr1["city"].ToString());

        var addr2 = (Dictionary<string, object>)addresses[1];
        Assert.AreEqual("456 Oak", addr2["street"].ToString());
        Assert.AreEqual("Paris", addr2["city"].ToString());
    }

    /// <summary>
    /// Test 7: Empty tree.
    /// This tests that an empty tree throws InvalidOperationException.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void GetObjectFromLeaves__when__empty_tree__then__throws_InvalidOperationException()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);

        // Act - This should throw InvalidOperationException
        var reconstructed = merkleTree.GetObjectFromLeaves<Dictionary<string, object>>();

        // If we get here, the test failed (should have thrown)
        Assert.Fail("Expected InvalidOperationException was not thrown");
    }

    /// <summary>
    /// Test 8: All private leaves.
    /// This tests that when all leaves are private, an InvalidOperationException is thrown.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void GetObjectFromLeaves__when__all_leaves_are_private__then__throws_InvalidOperationException()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new
        {
            name = "John",
            secret = "hidden"
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Create a selective disclosure version where all leaves are private
        var privateTree = MerkleTree.From(merkleTree, leaf => true); // Make all leaves private

        // Act - This should throw InvalidOperationException because no data leaves are available
        var reconstructed = privateTree.GetObjectFromLeaves<Dictionary<string, object>>();

        // If we get here, the test failed (should have thrown)
        Assert.Fail("Expected InvalidOperationException was not thrown");
    }

    /// <summary>
    /// Test 9: Root validation with validateRoot=true and invalid root.
    /// This tests that when validateRoot is true and the root is invalid, InvalidRootException is thrown.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(InvalidRootException))]
    public void GetObjectFromLeaves__when__validateRoot_true_and_invalid_root__then__throws_InvalidRootException()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new
        {
            name = "John",
            city = "London"
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Create a tree with an invalid root by manually constructing JSON with a corrupted root
        // We'll get the valid JSON first, then replace the root with a corrupted value
        var validJson = merkleTree.ToJson();

        // Create corrupted JSON by replacing the root value
        var corruptedRoot = "0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
        var corruptedJson = validJson.Replace(
            $"\"root\": \"{merkleTree.Root}\"",
            $"\"root\": \"{corruptedRoot}\""
        );

        var treeWithInvalidRoot = MerkleTree.Parse(corruptedJson);

        // Verify the root is indeed invalid
        Assert.IsFalse(treeWithInvalidRoot.VerifyRoot(MerkleTree.ComputeSha256Hash),
            "Tree should have invalid root");

        // Act - This should throw InvalidRootException when validateRoot=true
        var reconstructed = treeWithInvalidRoot.GetObjectFromLeaves<Dictionary<string, object>>(validateRoot: true);

        // If we get here, the test failed (should have thrown)
        Assert.Fail("Expected InvalidRootException was not thrown");
    }

    /// <summary>
    /// Test 10: Round-trip with strongly-typed object.
    /// This tests that we can reconstruct a strongly-typed object, not just a Dictionary.
    /// </summary>
    [TestMethod]
    public void GetObjectFromLeaves__when__strongly_typed_object__then__reconstructs_to_typed_object()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new Person
        {
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            Address = new Address
            {
                Street = "123 Main St",
                City = "London",
                Country = "UK"
            }
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Act
        var reconstructed = merkleTree.GetObjectFromLeaves<Person>();

        // Assert
        Assert.IsNotNull(reconstructed);
        Assert.AreEqual(original.Name, reconstructed.Name);
        Assert.AreEqual(original.Age, reconstructed.Age);
        Assert.AreEqual(original.Email, reconstructed.Email);
        Assert.IsNotNull(reconstructed.Address);
        Assert.AreEqual(original.Address.Street, reconstructed.Address.Street);
        Assert.AreEqual(original.Address.City, reconstructed.Address.City);
        Assert.AreEqual(original.Address.Country, reconstructed.Address.Country);
    }

    /// <summary>
    /// Test 11: Empty strings are legitimate values.
    /// This tests that empty strings are treated as valid values and don't trigger MissingLeafDataException.
    /// </summary>
    [TestMethod]
    public void GetObjectFromLeaves__when__property_has_empty_string_value__then__treats_as_legitimate_value()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new PersonWithNullableProperties
        {
            Name = "",           // Empty string is a legitimate value
            Description = "",    // Empty string is a legitimate value
            Secret = "hidden"
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Act - Should not throw, empty strings are valid
        var reconstructed = merkleTree.GetObjectFromLeaves<PersonWithNullableProperties>();

        // Assert
        Assert.IsNotNull(reconstructed);
        Assert.AreEqual("", reconstructed.Name, "Empty string should be preserved");
        Assert.AreEqual("", reconstructed.Description, "Empty string should be preserved");
        Assert.AreEqual("hidden", reconstructed.Secret);
    }

    /// <summary>
    /// Test 12: Null values that exist in data are legitimate.
    /// This tests that if a property is explicitly set to null in the data, it's not treated as missing.
    /// </summary>
    [TestMethod]
    public void GetObjectFromLeaves__when__property_is_explicitly_null_in_data__then__does_not_throw()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new PersonWithNullableProperties
        {
            Name = null,        // Explicitly null
            Description = null,  // Explicitly null
            Secret = "hidden"
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Act - Should not throw, null values that exist in data are valid
        var reconstructed = merkleTree.GetObjectFromLeaves<PersonWithNullableProperties>();

        // Assert
        Assert.IsNotNull(reconstructed);
        Assert.IsNull(reconstructed.Name, "Null value should be preserved");
        Assert.IsNull(reconstructed.Description, "Null value should be preserved");
        Assert.AreEqual("hidden", reconstructed.Secret);
    }

    /// <summary>
    /// Test 13: Empty string vs missing property distinction.
    /// This tests that when a property with empty string value is in a private leaf,
    /// it's treated as missing (null) and throws MissingLeafDataException.
    /// This proves we can distinguish between empty string (legitimate value) and missing property (null).
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(MissingLeafDataException))]
    public void GetObjectFromLeaves__when__property_with_empty_string_is_private__then__throws_MissingLeafDataException()
    {
        // Arrange
        var merkleTree = new MerkleTree(MerkleTreeVersionStrings.V2_0);
        var original = new PersonWithNullableProperties
        {
            Name = "",           // Empty string - legitimate value
            Description = "test",
            Secret = "hidden"
        };

        merkleTree.AddObjectLeaves(original);
        merkleTree.RecomputeSha256Root();

        // Make the 'name' leaf private (which contains empty string)
        var nameLeaf = merkleTree.Leaves.FirstOrDefault(l =>
        {
            var data = System.Text.Encoding.UTF8.GetString(l.Data.ToByteArray());
            return data.Contains("\"name\"") || data.Contains("\"Name\"");
        });

        Assert.IsNotNull(nameLeaf, "Should find the name leaf");

        // Create a selective disclosure version where 'name' is private
        var privateTree = MerkleTree.From(merkleTree, leaf =>
            leaf == nameLeaf // Make only the name leaf private
        );

        // Act - Since Name is nullable and missing (was in private leaf), this should throw MissingLeafDataException
        // This proves that empty string (legitimate value) is different from missing property (null)
        var reconstructed = privateTree.GetObjectFromLeaves<PersonWithNullableProperties>();

        // If we get here, the test failed (should have thrown)
        Assert.Fail("Expected MissingLeafDataException was not thrown");
    }

    // Test helper classes
    private class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
        public Address Address { get; set; } = new Address();
    }

    private class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    private class PersonWithRequiredName
    {
        public string? Name { get; set; }    // Required (nullable - null means missing)
        public string? Secret { get; set; }  // Optional (nullable)
    }

    private class PersonWithNullableProperties
    {
        public string? Name { get; set; }        // Nullable - null means missing
        public string? Description { get; set; } // Nullable - null means missing
        public string? Secret { get; set; }      // Nullable - null means missing
    }
}

