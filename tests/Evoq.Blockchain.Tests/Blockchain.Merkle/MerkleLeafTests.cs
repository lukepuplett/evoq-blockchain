using System.Collections.Generic;
using System.Text;
using Evoq.Blockchain.Merkle;

namespace Evoq.Blockchain.Tests.Merkle;

[TestClass]
public class MerkleLeafTests
{
    [TestMethod]
    public void Constructor_WithContentMetadata_ShouldSetProperties()
    {
        // Arrange
        var contentType = "text/plain; charset=utf-8";
        var data = new Hex(Encoding.UTF8.GetBytes("test data"));
        var salt = new Hex(new byte[] { 1, 2, 3, 4 });
        var hash = new Hex(new byte[] { 5, 6, 7, 8 });

        // Act
        var leaf = new MerkleLeaf(contentType, data, salt, hash);

        // Assert
        Assert.AreEqual(contentType, leaf.ContentType);
        Assert.AreEqual(data, leaf.Data);
        Assert.AreEqual(salt, leaf.Salt);
        Assert.AreEqual(hash, leaf.Hash);
        Assert.IsFalse(leaf.IsPrivate);
    }

    [TestMethod]
    public void Constructor_WithHashOnly_ShouldCreatePrivateLeaf()
    {
        // Arrange
        var hash = new Hex(new byte[] { 5, 6, 7, 8 });

        // Act
        var leaf = new MerkleLeaf(hash);

        // Assert
        Assert.AreEqual(string.Empty, leaf.ContentType);
        Assert.IsTrue(leaf.Data.IsEmpty());
        Assert.IsTrue(leaf.Salt.IsEmpty());
        Assert.AreEqual(hash, leaf.Hash);
        Assert.IsTrue(leaf.IsPrivate);
    }

    [TestMethod]
    public void FromData_WithContentTypeAndData_ShouldCreateLeafWithRandomSalt()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8";
        var data = new Hex(Encoding.UTF8.GetBytes("{\"key\":\"value\"}"));

        // Act
        var leaf = MerkleLeaf.FromData(contentType, data);

        // Assert
        Assert.AreEqual(contentType, leaf.ContentType);
        Assert.AreEqual(data, leaf.Data);
        Assert.IsFalse(leaf.Salt.IsEmpty());
        Assert.IsFalse(leaf.Hash.IsEmpty());
        Assert.IsFalse(leaf.IsPrivate);
    }

    [TestMethod]
    public void FromData_WithCustomSaltAndHashFunction_ShouldCreateLeaf()
    {
        // Arrange
        var contentType = "text/plain; charset=utf-8";
        var data = new Hex(Encoding.UTF8.GetBytes("test"));
        var salt = new Hex(new byte[] { 1, 2, 3, 4 });

        // Act
        var leaf = MerkleLeaf.FromData(contentType, data, salt, MerkleTree.ComputeSha256Hash);

        // Assert
        Assert.AreEqual(contentType, leaf.ContentType);
        Assert.AreEqual(data, leaf.Data);
        Assert.AreEqual(salt, leaf.Salt);
        Assert.IsFalse(leaf.Hash.IsEmpty());
        Assert.IsFalse(leaf.IsPrivate);
    }

    [TestMethod]
    public void FromJsonValue_WithFieldNameAndValue_ShouldCreateJsonLeaf()
    {
        // Arrange
        var fieldName = "name";
        var fieldValue = "John Doe";

        // Act
        var leaf = MerkleLeaf.FromJsonValue(fieldName, fieldValue);

        // Assert
        Assert.AreEqual("application/json; charset=utf-8; encoding=hex", leaf.ContentType);
        Assert.IsFalse(leaf.Data.IsEmpty());
        Assert.IsFalse(leaf.Salt.IsEmpty());
        Assert.IsFalse(leaf.Hash.IsEmpty());
        Assert.IsFalse(leaf.IsPrivate);
        Assert.IsTrue(leaf.IsUtf8);
    }

    [TestMethod]
    public void FromJsonValue_WithCustomSaltAndHashFunction_ShouldCreateJsonLeaf()
    {
        // Arrange
        var fieldName = "age";
        var fieldValue = 30;
        var salt = new Hex(new byte[] { 1, 2, 3, 4 });

        // Act
        var leaf = MerkleLeaf.FromJsonValue(fieldName, fieldValue, salt, MerkleTree.ComputeSha256Hash);

        // Assert
        Assert.AreEqual("application/json; charset=utf-8; encoding=hex", leaf.ContentType);
        Assert.IsFalse(leaf.Data.IsEmpty());
        Assert.AreEqual(salt, leaf.Salt);
        Assert.IsFalse(leaf.Hash.IsEmpty());
        Assert.IsFalse(leaf.IsPrivate);
    }

    [TestMethod]
    public void TryReadText_WithUtf8Content_ShouldReturnTrueAndText()
    {
        // Arrange
        var text = "Hello, World!";
        var data = new Hex(Encoding.UTF8.GetBytes(text));
        var leaf = new MerkleLeaf("text/plain; charset=utf-8", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadText(out var readText);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(text, readText);
    }

    [TestMethod]
    public void TryReadText_WithNonUtf8Content_ShouldReturnFalse()
    {
        // Arrange
        var data = new Hex(new byte[] { 0xFF, 0xFE, 0xFD });
        var leaf = new MerkleLeaf("application/octet-stream", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadText(out var readText);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(string.Empty, readText);
    }

    [TestMethod]
    public void IsUtf8_WithUtf8ContentType_ShouldReturnTrue()
    {
        // Arrange
        var leaf = new MerkleLeaf("text/plain; charset=utf-8", Hex.Empty, Hex.Empty, Hex.Empty);

        // Act & Assert
        Assert.IsTrue(leaf.IsUtf8);
    }

    [TestMethod]
    public void IsUtf8_WithNonUtf8ContentType_ShouldReturnFalse()
    {
        // Arrange
        var leaf = new MerkleLeaf("application/octet-stream", Hex.Empty, Hex.Empty, Hex.Empty);

        // Act & Assert
        Assert.IsFalse(leaf.IsUtf8);
    }

    [TestMethod]
    public void IsBase64_WithBase64ContentType_ShouldReturnTrue()
    {
        // Arrange
        var leaf = new MerkleLeaf("text/plain; charset=utf-8; encoding=base64", Hex.Empty, Hex.Empty, Hex.Empty);

        // Act & Assert
        Assert.IsTrue(leaf.IsBase64);
    }

    [TestMethod]
    public void IsBase64_WithNonBase64ContentType_ShouldReturnFalse()
    {
        // Arrange
        var leaf = new MerkleLeaf("text/plain; charset=utf-8", Hex.Empty, Hex.Empty, Hex.Empty);

        // Act & Assert
        Assert.IsFalse(leaf.IsBase64);
    }

    [TestMethod]
    public void ToString_WithUtf8Text_ShouldReturnText()
    {
        // Arrange
        var text = "Hello, World!";
        var data = new Hex(Encoding.UTF8.GetBytes(text));
        var leaf = new MerkleLeaf("text/plain; charset=utf-8", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.ToString();

        // Assert
        Assert.AreEqual(text, result);
    }

    [TestMethod]
    public void ToString_WithNonUtf8Data_ShouldReturnHexString()
    {
        // Arrange
        var data = new Hex(new byte[] { 0xFF, 0xFE, 0xFD });
        var leaf = new MerkleLeaf("application/octet-stream", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.ToString();

        // Assert
        Assert.AreEqual(data.ToString(), result);
    }

    [TestMethod]
    public void TryReadJson_WithValidJsonContent_ShouldReturnTrueAndDictionary()
    {
        // Arrange
        var jsonText = "{\"name\":\"John Doe\",\"age\":30,\"active\":true}";
        var data = new Hex(Encoding.UTF8.GetBytes(jsonText));
        var leaf = new MerkleLeaf("application/json; charset=utf-8", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadJson(out var jsonObject);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(jsonObject);
        Assert.AreEqual(3, jsonObject!.Count);
        Assert.AreEqual("John Doe", jsonObject["name"]!.ToString());
        Assert.AreEqual(30, ((System.Text.Json.JsonElement)jsonObject["age"]!).GetInt32());
        Assert.AreEqual(true, ((System.Text.Json.JsonElement)jsonObject["active"]!).GetBoolean());
    }

    [TestMethod]
    public void TryReadJson_WithJsonFromFromJsonValue_ShouldReturnTrueAndDictionary()
    {
        // Arrange
        var leaf = MerkleLeaf.FromJsonValue("name", "John Doe");

        // Act
        var result = leaf.TryReadJson(out var jsonObject);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(jsonObject);
        Assert.AreEqual(1, jsonObject!.Count);
        Assert.AreEqual("John Doe", jsonObject["name"]!.ToString());
    }

    [TestMethod]
    public void TryReadJson_WithInvalidJsonContent_ShouldReturnFalse()
    {
        // Arrange
        var invalidJson = "{\"name\":\"John Doe\",\"age\":30,"; // Missing closing brace
        var data = new Hex(Encoding.UTF8.GetBytes(invalidJson));
        var leaf = new MerkleLeaf("application/json; charset=utf-8", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadJson(out var jsonObject);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(jsonObject);
    }

    [TestMethod]
    public void TryReadJson_WithNonUtf8Content_ShouldReturnFalse()
    {
        // Arrange
        var data = new Hex(new byte[] { 0xFF, 0xFE, 0xFD });
        var leaf = new MerkleLeaf("application/octet-stream", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadJson(out var jsonObject);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(jsonObject);
    }

    [TestMethod]
    public void TryReadJson_WithEmptyData_ShouldReturnFalse()
    {
        // Arrange
        var leaf = new MerkleLeaf("application/json; charset=utf-8", Hex.Empty, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadJson(out var jsonObject);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(jsonObject);
    }

    [TestMethod]
    public void TryReadJson_WithPrivateLeaf_ShouldReturnFalse()
    {
        // Arrange
        var hash = new Hex(new byte[] { 1, 2, 3, 4 });
        var leaf = new MerkleLeaf(hash);

        // Act
        var result = leaf.TryReadJson(out var jsonObject);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(jsonObject);
    }

    [TestMethod]
    public void TryReadObject_WithValidJsonContent_ShouldReturnTrueAndDeserializedObject()
    {
        // Arrange
        var jsonText = "{\"name\":\"John Doe\",\"age\":30,\"active\":true}";
        var data = new Hex(Encoding.UTF8.GetBytes(jsonText));
        var leaf = new MerkleLeaf("application/json; charset=utf-8", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadObject<TestPerson>(out var person);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(person);
        Assert.AreEqual("John Doe", person!.Name);
        Assert.AreEqual(30, person.Age);
        Assert.IsTrue(person.Active);
    }

    [TestMethod]
    public void TryReadObject_WithJsonFromFromJsonValue_ShouldReturnTrueAndDeserializedObject()
    {
        // Arrange
        var leaf = MerkleLeaf.FromJsonValue("name", "John Doe");

        // Act
        var result = leaf.TryReadObject<Dictionary<string, string>>(out var dict);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(dict);
        Assert.AreEqual(1, dict!.Count);
        Assert.AreEqual("John Doe", dict["name"]);
    }

    [TestMethod]
    public void TryReadObject_WithInvalidJsonContent_ShouldReturnFalse()
    {
        // Arrange
        var invalidJson = "{\"name\":\"John Doe\",\"age\":30,"; // Missing closing brace
        var data = new Hex(Encoding.UTF8.GetBytes(invalidJson));
        var leaf = new MerkleLeaf("application/json; charset=utf-8", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadObject<TestPerson>(out var person);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(person);
    }

    [TestMethod]
    public void TryReadObject_WithNonUtf8Content_ShouldReturnFalse()
    {
        // Arrange
        var data = new Hex(new byte[] { 0xFF, 0xFE, 0xFD });
        var leaf = new MerkleLeaf("application/octet-stream", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadObject<TestPerson>(out var person);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(person);
    }

    [TestMethod]
    public void TryReadObject_WithEmptyData_ShouldReturnFalse()
    {
        // Arrange
        var leaf = new MerkleLeaf("application/json; charset=utf-8", Hex.Empty, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadObject<TestPerson>(out var person);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(person);
    }

    [TestMethod]
    public void TryReadObject_WithPrivateLeaf_ShouldReturnFalse()
    {
        // Arrange
        var hash = new Hex(new byte[] { 1, 2, 3, 4 });
        var leaf = new MerkleLeaf(hash);

        // Act
        var result = leaf.TryReadObject<TestPerson>(out var person);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(person);
    }

    [TestMethod]
    public void TryReadObject_WithPrimitiveType_ShouldReturnTrueAndDeserializedValue()
    {
        // Arrange
        var jsonText = "\"Hello, World!\"";
        var data = new Hex(Encoding.UTF8.GetBytes(jsonText));
        var leaf = new MerkleLeaf("application/json; charset=utf-8", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadObject<string>(out var text);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual("Hello, World!", text);
    }

    [TestMethod]
    public void TryReadObject_WithArrayType_ShouldReturnTrueAndDeserializedArray()
    {
        // Arrange
        var jsonText = "[1, 2, 3, 4, 5]";
        var data = new Hex(Encoding.UTF8.GetBytes(jsonText));
        var leaf = new MerkleLeaf("application/json; charset=utf-8", data, Hex.Empty, Hex.Empty);

        // Act
        var result = leaf.TryReadObject<int[]>(out var numbers);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(numbers);
        Assert.AreEqual(5, numbers!.Length);
        Assert.AreEqual(1, numbers[0]);
        Assert.AreEqual(5, numbers[4]);
    }

    // Test class for TryReadObject tests
    private class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool Active { get; set; }
    }
}