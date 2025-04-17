namespace Evoq.Blockchain.Merkle;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Evoq.Blockchain;

/// <summary>
/// Represents a leaf node in a Merkle tree.
/// </summary>
public class MerkleLeaf
{
    /// <summary>
    /// Initializes a new instance of the MerkleLeaf class with content metadata.
    /// </summary>
    /// <param name="data">The data contained in the leaf.</param>
    /// <param name="salt">The salt used for hashing the leaf data.</param>
    /// <param name="hash">The hash of the leaf.</param>
    /// <param name="contentType">The MIME content type of the data, including encoding information if applicable.</param>
    public MerkleLeaf(string contentType, Hex data, Hex salt, Hex hash)
    {
        this.ContentType = contentType;
        this.Data = data;
        this.Salt = salt;
        this.Hash = hash;
    }

    //

    /// <summary>
    /// Gets or sets the MIME content type of the data (e.g., "text/plain; charset=utf-8", "application/json; charset=utf-8").
    /// </summary>
    public string ContentType { get; set; } = ContentTypeUtility.CreateJsonUtf8();

    /// <summary>
    /// Gets a value indicating whether the content is UTF-8 encoded.
    /// </summary>
    public bool IsUtf8 => ContentTypeUtility.IsUtf8(ContentType);

    /// <summary>
    /// Gets a value indicating whether the content is Base64 encoded.
    /// </summary>
    public bool IsBase64 => ContentTypeUtility.IsBase64(ContentType);

    /// <summary>
    /// Gets a value indicating whether this leaf is private (data and salt are not provided,
    /// but hash is available for verification).
    /// </summary>
    public bool IsPrivate => this.Data.IsEmpty() && this.Salt.IsEmpty() && !this.Hash.IsEmpty();

    /// <summary>
    /// Gets or sets the data contained in the leaf.
    /// </summary>
    public Hex Data { get; } = Hex.Empty;

    /// <summary>
    /// Gets or sets the salt used for hashing the leaf data.
    /// </summary>
    public Hex Salt { get; } = Hex.Empty;

    /// <summary>
    /// Gets or sets the hash of the leaf.
    /// </summary>
    public Hex Hash { get; } = Hex.Empty;

    //

    /// <summary>
    /// Creates a new MerkleLeaf from the given data using a random salt and SHA-256 hash function.
    /// </summary>
    /// <param name="contentType">The MIME content type of the data, including encoding information if applicable.</param>
    /// <param name="data">The data contained in the leaf.</param>
    /// <returns>A new MerkleLeaf with the specified content type.</returns>
    public static MerkleLeaf FromData(string contentType, Hex data)
    {
        return FromData(contentType, data, MerkleTree.GenerateRandomSalt(), MerkleTree.ComputeSha256Hash);
    }

    /// <summary>
    /// Creates a new MerkleLeaf from the given data, salt, hash function, and content metadata.
    /// </summary>
    /// <param name="contentType">The MIME content type of the data, including encoding information if applicable.</param>
    /// <param name="data">The data contained in the leaf.</param>
    /// <param name="salt">The salt used for hashing the leaf data.</param>
    /// <param name="hashFunction">The hash function to use for hashing the leaf data.</param>
    /// <returns>A new MerkleLeaf with the specified content type.</returns>
    public static MerkleLeaf FromData(string contentType, Hex data, Hex salt, MerkleTree.HashFunction hashFunction)
    {
        var hash = hashFunction(Hex.Concat(data, salt).ToByteArray());

        return new MerkleLeaf(contentType, data, salt, hash);
    }

    /// <summary>
    /// Creates a new MerkleLeaf from the given field name and field value using a random salt and SHA-256 hash function.
    /// </summary>
    /// <param name="fieldName">The name of the field to store in the leaf.</param>
    /// <param name="fieldValue">The value of the field to store in the leaf.</param>
    /// <returns>A new MerkleLeaf with "application/json; charset=utf-8" content type.</returns>
    public static MerkleLeaf FromJsonValue(string fieldName, object? fieldValue)
    {
        return FromJsonValue(fieldName, fieldValue, MerkleTree.GenerateRandomSalt(), MerkleTree.ComputeSha256Hash);
    }

    /// <summary>
    /// Creates a new MerkleLeaf with data encoded as JSON.
    /// </summary>
    /// <param name="fieldName">The name of the field to store in the leaf.</param>
    /// <param name="fieldValue">The value of the field to store in the leaf.</param>
    /// <param name="salt">The salt used for hashing the leaf data.</param>
    /// <param name="hashFunction">The hash function to use for hashing the leaf data.</param>
    /// <returns>A new MerkleLeaf with "application/json; charset=utf-8" content type.</returns>
    public static MerkleLeaf FromJsonValue(string fieldName, object? fieldValue, Hex salt, MerkleTree.HashFunction hashFunction)
    {
        var jsonObject = new Dictionary<string, object?>
        {
            { fieldName, fieldValue }
        };

        var json = JsonSerializer.Serialize(jsonObject);
        var data = new Hex(System.Text.Encoding.UTF8.GetBytes(json));
        var hash = hashFunction(Hex.Concat(data, salt).ToByteArray());

        return new MerkleLeaf(ContentTypeUtility.CreateJsonUtf8(), data, salt, hash);
    }

    /// <summary>
    /// Attempts to read the data as a UTF-8 encoded text string or JSON object or simple value.
    /// </summary>
    /// <param name="value">When this method returns, contains the text value if successful; otherwise, empty string.</param>
    /// <returns>true if the data was successfully read as UTF-8 text or JSON object or simple value; otherwise, false.</returns>
    public bool TryReadText(out string value)
    {
        if (IsUtf8)
        {
            try
            {
                value = System.Text.Encoding.UTF8.GetString(Data.ToByteArray());
                return true;
            }
            catch
            {
                value = string.Empty;
                return false;
            }
        }

        value = string.Empty;
        return false;
    }

    //

    internal string ToHexString()
    {
        return this.Data.ToString();
    }

    /// <summary>
    /// Returns a string representation of the MerkleLeaf.
    /// </summary>
    /// <returns>A string representation of the MerkleLeaf.</returns>
    public override string ToString()
    {
        if (this.TryReadText(out var text))
        {
            return text;
        }

        return this.Data.ToString();
    }
}