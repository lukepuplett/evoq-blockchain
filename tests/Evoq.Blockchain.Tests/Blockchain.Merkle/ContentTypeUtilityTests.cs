using Evoq.Blockchain.Merkle;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Blockchain.Tests.Merkle;

[TestClass]
public class ContentTypeUtilityTests
{
    [TestMethod]
    public void IsUtf8_WithUtf8CharsetString_ReturnsTrue()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsUtf8(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsUtf8_WithUppercaseUtf8CharsetString_ReturnsTrue()
    {
        // Arrange
        var contentType = "application/json; charset=UTF-8";

        // Act
        bool result = ContentTypeUtility.IsUtf8(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsUtf8_WithNonUtf8CharsetString_ReturnsFalse()
    {
        // Arrange
        var contentType = "application/json; charset=iso-8859-1";

        // Act
        bool result = ContentTypeUtility.IsUtf8(contentType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsUtf8_WithNullString_ReturnsFalse()
    {
        // Act
        bool result = ContentTypeUtility.IsUtf8(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsUtf8_WithEmptyString_ReturnsFalse()
    {
        // Act
        bool result = ContentTypeUtility.IsUtf8(string.Empty);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsBase64_WithBase64EncodingString_ReturnsTrue()
    {
        // Arrange
        var contentType = "application/octet-stream; encoding=base64";

        // Act
        bool result = ContentTypeUtility.IsBase64(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsBase64_WithNonBase64EncodingString_ReturnsFalse()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsBase64(contentType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsBase64_WithNullString_ReturnsFalse()
    {
        // Act
        bool result = ContentTypeUtility.IsBase64(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsJson_WithApplicationJsonString_ReturnsTrue()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsJson(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsJson_WithNonJsonString_ReturnsFalse()
    {
        // Arrange
        var contentType = "text/plain; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsJson(contentType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsPlainText_WithTextPlainString_ReturnsTrue()
    {
        // Arrange
        var contentType = "text/plain; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsPlainText(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPlainText_WithNonTextPlainString_ReturnsFalse()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsPlainText(contentType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsXml_WithApplicationXmlString_ReturnsTrue()
    {
        // Arrange
        var contentType = "application/xml; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsXml(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsXml_WithTextXmlString_ReturnsTrue()
    {
        // Arrange
        var contentType = "text/xml; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsXml(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsXml_WithNonXmlString_ReturnsFalse()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsXml(contentType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsBinary_WithOctetStreamString_ReturnsTrue()
    {
        // Arrange
        var contentType = "application/octet-stream";

        // Act
        bool result = ContentTypeUtility.IsBinary(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsBinary_WithApplicationBinaryString_ReturnsTrue()
    {
        // Arrange
        var contentType = "application/binary";

        // Act
        bool result = ContentTypeUtility.IsBinary(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsBinary_WithNonBinaryString_ReturnsFalse()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsBinary(contentType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsHex_WithHexEncodingString_ReturnsTrue()
    {
        // Arrange
        var contentType = "application/octet-stream; encoding=hex";

        // Act
        bool result = ContentTypeUtility.IsHex(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsHex_WithNonHexEncodingString_ReturnsFalse()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsHex(contentType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CreateJsonUtf8_ReturnsCorrectContentType()
    {
        // Act
        string result = ContentTypeUtility.CreateJsonUtf8();

        // Assert
        Assert.AreEqual("application/json; charset=utf-8", result);
    }

    [TestMethod]
    public void CreatePlainTextUtf8_ReturnsCorrectContentType()
    {
        // Act
        string result = ContentTypeUtility.CreatePlainTextUtf8();

        // Assert
        Assert.AreEqual("text/plain; charset=utf-8", result);
    }

    [TestMethod]
    public void CreateBinary_ReturnsCorrectContentType()
    {
        // Act
        string result = ContentTypeUtility.CreateBinary();

        // Assert
        Assert.AreEqual("application/octet-stream", result);
    }

    [TestMethod]
    public void CreateHex_ReturnsCorrectContentType()
    {
        // Act
        string result = ContentTypeUtility.CreateHex();

        // Assert
        Assert.AreEqual("application/octet-stream; encoding=hex", result);
    }

    [TestMethod]
    public void CreateBase64_ReturnsCorrectContentType()
    {
        // Act
        string result = ContentTypeUtility.CreateBase64();

        // Assert
        Assert.AreEqual("application/octet-stream; encoding=base64", result);
    }

    [TestMethod]
    public void IsJsonUtf8Hex_WithValidContentType_ReturnsTrue()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8; encoding=hex";

        // Act
        bool result = ContentTypeUtility.IsJsonUtf8Hex(contentType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsJsonUtf8Hex_WithMissingHexEncoding_ReturnsFalse()
    {
        // Arrange
        var contentType = "application/json; charset=utf-8";

        // Act
        bool result = ContentTypeUtility.IsJsonUtf8Hex(contentType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsJsonUtf8Hex_WithMissingUtf8Charset_ReturnsFalse()
    {
        // Arrange
        var contentType = "application/json; encoding=hex";

        // Act
        bool result = ContentTypeUtility.IsJsonUtf8Hex(contentType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CreateJsonUtf8Hex_ReturnsCorrectContentType()
    {
        // Act
        string result = ContentTypeUtility.CreateJsonUtf8Hex();

        // Assert
        Assert.AreEqual("application/json; charset=utf-8; encoding=hex", result);
    }
}