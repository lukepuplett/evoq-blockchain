using System.Text.Json;

namespace Evoq.Blockchain;

/// <summary>
/// Extension methods for <see cref="JsonSerializerOptions"/> to simplify Hex serialization configuration.
/// </summary>
public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Configures the <see cref="JsonSerializerOptions"/> to automatically handle <see cref="Hex"/> serialization and deserialization.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to configure.</param>
    /// <returns>The same <see cref="JsonSerializerOptions"/> instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method adds the necessary JSON converters to enable seamless serialization and deserialization of <see cref="Hex"/>
    /// values in your DTOs. After calling this method, <see cref="Hex"/> properties will be automatically converted to/from
    /// hex strings in JSON.
    /// </para>
    /// 
    /// <para>
    /// <strong>Usage:</strong>
    /// </para>
    /// 
    /// <code>
    /// var options = new JsonSerializerOptions().ConfigureForHex();
    /// 
    /// // Or chain with other configurations
    /// var options = new JsonSerializerOptions
    /// {
    ///     PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    /// }.ConfigureForHex();
    /// 
    /// // Now you can serialize/deserialize objects with Hex properties
    /// string json = JsonSerializer.Serialize(myObject, options);
    /// var result = JsonSerializer.Deserialize&lt;MyType&gt;(json, options);
    /// </code>
    /// 
    /// <para>
    /// <strong>What it configures:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="HexJsonConverter"/> for <see cref="Hex"/> properties</description></item>
    /// <item><description><see cref="NullableHexJsonConverter"/> for nullable <see cref="Hex"/> properties</description></item>
    /// </list>
    /// </remarks>
    public static JsonSerializerOptions ConfigureForHex(this JsonSerializerOptions options)
    {
        options.Converters.Add(new HexJsonConverter());
        options.Converters.Add(new NullableHexJsonConverter());
        return options;
    }
}