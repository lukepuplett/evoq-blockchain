using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

[TestClass]
public class SimpleJsonParsingTests
{
    [TestMethod]
    public void Parse_JsonNumber_ShouldReturnNumber()
    {
        // Arrange
        string jsonNumber = "29.192892";

        // Act
        var element = JsonSerializer.Deserialize<JsonElement>(jsonNumber);

        // Assert
        Assert.AreEqual(JsonValueKind.Number, element.ValueKind);
        Assert.AreEqual(29.192892, element.GetDouble());
    }

    [TestMethod]
    public void Parse_JsonString_ShouldReturnString()
    {
        // Arrange
        string jsonString = "\"hello world\"";

        // Act
        var parsedValue = JsonSerializer.Deserialize<string>(jsonString);

        // Assert
        Assert.AreEqual("hello world", parsedValue);
    }

    [TestMethod]
    public void Parse_JsonBoolean_ShouldReturnBoolean()
    {
        // Arrange
        string jsonTrue = "true";
        string jsonFalse = "false";

        // Act
        var trueElement = JsonSerializer.Deserialize<JsonElement>(jsonTrue);
        var falseElement = JsonSerializer.Deserialize<JsonElement>(jsonFalse);

        // Assert
        Assert.AreEqual(JsonValueKind.True, trueElement.ValueKind);
        Assert.IsTrue(trueElement.GetBoolean());

        Assert.AreEqual(JsonValueKind.False, falseElement.ValueKind);
        Assert.IsFalse(falseElement.GetBoolean());
    }

    [TestMethod]
    public void Parse_JsonNull_ShouldReturnNull()
    {
        // Arrange
        string jsonNull = "null";

        // Act
        var element = JsonSerializer.Deserialize<JsonElement>(jsonNull);

        // Assert
        Assert.AreEqual(JsonValueKind.Null, element.ValueKind);
    }

    [TestMethod]
    public void Parse_EmptyObject_ShouldReturnEmptyObject()
    {
        // Arrange
        string jsonObject = "{}";

        // Act
        var element = JsonSerializer.Deserialize<JsonElement>(jsonObject);

        // Assert
        Assert.AreEqual(JsonValueKind.Object, element.ValueKind);
        Assert.AreEqual(0, element.EnumerateObject().Count());
    }

    [TestMethod]
    public void Parse_EmptyArray_ShouldReturnEmptyArray()
    {
        // Arrange
        string jsonArray = "[]";

        // Act
        var element = JsonSerializer.Deserialize<JsonElement>(jsonArray);

        // Assert
        Assert.AreEqual(JsonValueKind.Array, element.ValueKind);
        Assert.AreEqual(0, element.GetArrayLength());
    }

    [TestMethod]
    [ExpectedException(typeof(JsonException))]
    public void Parse_UnquotedString_ShouldThrowJsonException()
    {
        // Arrange
        string invalidJson = "hello world"; // Missing quotes

        // Act - this should throw an exception
        JsonSerializer.Deserialize<JsonElement>(invalidJson);
    }

    [TestMethod]
    public void Parse_SingleChar_ShouldReturnString()
    {
        // Arrange
        string jsonChar = "\"a\"";

        // Act
        var element = JsonSerializer.Deserialize<JsonElement>(jsonChar);

        // Assert
        Assert.AreEqual(JsonValueKind.String, element.ValueKind);
        Assert.AreEqual("a", element.GetString());
    }

    [TestMethod]
    public void Parse_IntegerZero_ShouldReturnNumberZero()
    {
        // Arrange
        string jsonZero = "0";

        // Act
        var element = JsonSerializer.Deserialize<JsonElement>(jsonZero);

        // Assert
        Assert.AreEqual(JsonValueKind.Number, element.ValueKind);
        Assert.AreEqual(0, element.GetInt32());
    }

    [TestMethod]
    public void Dictionary_WithSingleKeyValue_SerializesToJsonObject()
    {
        // Arrange
        string fieldName = "name";
        string fieldValue = "John Doe";
        var jsonObject = new Dictionary<string, object>
    {
        { fieldName, fieldValue }
    };

        // Act
        string json = JsonSerializer.Serialize(jsonObject);

        // Assert
        Assert.AreEqual("{\"name\":\"John Doe\"}", json);

        // Verify we can deserialize it back
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.AreEqual(JsonValueKind.Object, element.ValueKind);
        Assert.IsTrue(element.TryGetProperty("name", out var nameProperty));
        Assert.AreEqual(JsonValueKind.String, nameProperty.ValueKind);
        Assert.AreEqual("John Doe", nameProperty.GetString());
    }

}