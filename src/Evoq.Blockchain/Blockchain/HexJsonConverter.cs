using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Evoq.Blockchain;

/// <summary>
/// JSON converter for the <see cref="Hex"/> type that enables automatic serialization and deserialization.
/// </summary>
/// <remarks>
/// <para>
/// This converter enables <see cref="Hex"/> values to be seamlessly serialized to and from JSON as hex strings.
/// During serialization, <see cref="Hex"/> values are converted to their string representation (e.g., "0x1234abcd").
/// During deserialization, hex strings are parsed back into <see cref="Hex"/> values.
/// </para>
/// 
/// <para>
/// <strong>Usage:</strong> Register this converter with your <see cref="JsonSerializerOptions"/>:
/// </para>
/// 
/// <code>
/// var options = new JsonSerializerOptions
/// {
///     Converters = { new HexJsonConverter() }
/// };
/// 
/// // Now Hex properties in your DTOs will be automatically serialized/deserialized
/// string json = JsonSerializer.Serialize(myObject, options);
/// var result = JsonSerializer.Deserialize&lt;MyType&gt;(json, options);
/// </code>
/// 
/// <para>
/// <strong>Supported Formats:</strong> The converter accepts hex strings with or without the "0x" prefix during
/// deserialization, but always serializes with the "0x" prefix for consistency.
/// </para>
/// 
/// <para>
/// <strong>Error Handling:</strong> Invalid hex strings during deserialization will throw a <see cref="JsonException"/>
/// with a descriptive error message. This includes strings with invalid hex characters or odd-length hex strings
/// (unless using lenient parsing options).
/// </para>
/// </remarks>
public class HexJsonConverter : JsonConverter<Hex>
{
    /// <summary>
    /// Reads a JSON token and converts it to a <see cref="Hex"/> value.
    /// </summary>
    /// <param name="reader">The JSON reader to read from.</param>
    /// <param name="typeToConvert">The type being converted (should be <see cref="Hex"/>).</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>A <see cref="Hex"/> value parsed from the JSON string.</returns>
    /// <exception cref="JsonException">Thrown when the JSON token is not a string or when the hex string is invalid.</exception>
    public override Hex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token for Hex deserialization, but got {reader.TokenType}");
        }

        string? hexString = reader.GetString();
        
        if (hexString == null)
        {
            throw new JsonException("Cannot deserialize null string to Hex");
        }

        try
        {
            return Hex.Parse(hexString);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"Invalid hex string '{hexString}': {ex.Message}", ex);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"Invalid hex string format '{hexString}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes a <see cref="Hex"/> value to JSON as a hex string.
    /// </summary>
    /// <param name="writer">The JSON writer to write to.</param>
    /// <param name="value">The <see cref="Hex"/> value to serialize.</param>
    /// <param name="options">The JSON serializer options.</param>
    public override void Write(Utf8JsonWriter writer, Hex value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// JSON converter for nullable <see cref="Hex"/> types that enables automatic serialization and deserialization.
/// </summary>
/// <remarks>
/// <para>
/// This converter handles nullable <see cref="Hex"/> properties in DTOs, serializing null values as JSON null
/// and deserializing JSON null back to null <see cref="Hex"/> values.
/// </para>
/// 
/// <para>
/// <strong>Usage:</strong> This converter is typically registered alongside <see cref="HexJsonConverter"/>:
/// </para>
/// 
/// <code>
/// var options = new JsonSerializerOptions
/// {
///     Converters = { 
///         new HexJsonConverter(),
///         new NullableHexJsonConverter()
///     }
/// };
/// </code>
/// 
/// <para>
/// <strong>Note:</strong> In most cases, you only need to register <see cref="HexJsonConverter"/> as System.Text.Json
/// will automatically handle nullable conversions. This converter is provided for explicit nullable handling scenarios.
/// </para>
/// </remarks>
public class NullableHexJsonConverter : JsonConverter<Hex?>
{
    /// <summary>
    /// Reads a JSON token and converts it to a nullable <see cref="Hex"/> value.
    /// </summary>
    /// <param name="reader">The JSON reader to read from.</param>
    /// <param name="typeToConvert">The type being converted (should be <see cref="Hex"/>?).</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>A nullable <see cref="Hex"/> value parsed from the JSON, or null if the JSON token is null.</returns>
    /// <exception cref="JsonException">Thrown when the JSON token is not a string or null, or when the hex string is invalid.</exception>
    public override Hex? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string or null token for nullable Hex deserialization, but got {reader.TokenType}");
        }

        string? hexString = reader.GetString();
        
        if (hexString == null)
        {
            return null;
        }

        try
        {
            return Hex.Parse(hexString);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"Invalid hex string '{hexString}': {ex.Message}", ex);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"Invalid hex string format '{hexString}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes a nullable <see cref="Hex"/> value to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer to write to.</param>
    /// <param name="value">The nullable <see cref="Hex"/> value to serialize.</param>
    /// <param name="options">The JSON serializer options.</param>
    public override void Write(Utf8JsonWriter writer, Hex? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}