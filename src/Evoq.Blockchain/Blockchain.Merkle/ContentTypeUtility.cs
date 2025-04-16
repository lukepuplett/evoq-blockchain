namespace Evoq.Blockchain.Merkle;

using System;

/// <summary>
/// Utility for working with MIME content types.
/// </summary>
public static class ContentTypeUtility
{
    /// <summary>
    /// Checks if the content type indicates UTF-8 encoding.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>True if the content is UTF-8 encoded, false otherwise.</returns>
    public static bool IsUtf8(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.Contains("charset=utf-8", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the content type indicates Base64 encoding.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>True if the content is Base64 encoded, false otherwise.</returns>
    public static bool IsBase64(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.Contains("base64", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the content type is JSON.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>True if the content is JSON, false otherwise.</returns>
    public static bool IsJson(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the content type is plain text.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>True if the content is plain text, false otherwise.</returns>
    public static bool IsPlainText(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the content type is XML.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>True if the content is XML, false otherwise.</returns>
    public static bool IsXml(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the content type is binary data.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>True if the content is binary data, false otherwise.</returns>
    public static bool IsBinary(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("application/binary", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the content type is hex-encoded.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>True if the content is hex-encoded, false otherwise.</returns>
    public static bool IsHex(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.Contains("hex", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a content type string for JSON with UTF-8 encoding.
    /// </summary>
    /// <returns>A standardized JSON content type string.</returns>
    public static string CreateJsonUtf8()
    {
        return "application/json; charset=utf-8";
    }

    /// <summary>
    /// Creates a content type string for plain text with UTF-8 encoding.
    /// </summary>
    /// <returns>A standardized plain text content type string.</returns>
    public static string CreatePlainTextUtf8()
    {
        return "text/plain; charset=utf-8";
    }

    /// <summary>
    /// Creates a content type string for binary data.
    /// </summary>
    /// <returns>A standardized binary content type string.</returns>
    public static string CreateBinary()
    {
        return "application/octet-stream";
    }

    /// <summary>
    /// Creates a content type string for hex-encoded binary data.
    /// </summary>
    /// <returns>A standardized hex-encoded content type string.</returns>
    public static string CreateHex()
    {
        return "application/octet-stream; encoding=hex";
    }

    /// <summary>
    /// Creates a content type string for Base64-encoded binary data.
    /// </summary>
    /// <returns>A standardized Base64-encoded content type string.</returns>
    public static string CreateBase64()
    {
        return "application/octet-stream; encoding=base64";
    }

    /// <summary>
    /// Creates a content type string for JSON with UTF-8 encoding and hex-encoded representation.
    /// </summary>
    /// <returns>A standardized JSON content type string for hex-encoded JSON.</returns>
    public static string CreateJsonUtf8Hex()
    {
        return "application/json; charset=utf-8; encoding=hex";
    }

    /// <summary>
    /// Checks if the content type is JSON with UTF-8 encoding and hex representation.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>True if the content is JSON with UTF-8 encoding and hex representation, false otherwise.</returns>
    public static bool IsJsonUtf8Hex(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return IsJson(contentType) && IsUtf8(contentType) && IsHex(contentType);
    }
}