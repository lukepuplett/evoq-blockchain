namespace Evoq.Blockchain.Merkle;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Evoq.Blockchain;

/// <summary>
/// A Merkle tree is a binary tree of hashes.
/// </summary>
/// <remarks>
/// A simple Merkle tree which can be assembled manually or parsed from a JSON string. The design forces the user
/// to verify the root before serializing to JSON, ensuring that the tree is valid before it is saved or exchanged.
/// If the tree fails verification, an exception can be caught and the tree root can be recomputed from the leaves.
/// </remarks>
public class MerkleTree
{
    /// <summary>
    /// Delegate for hash function implementations to be used with Merkle trees.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>A hex representation of the hash result.</returns>
    public delegate Hex HashFunction(byte[] data);

    //

    /// <summary>
    /// Initializes a new instance of the MerkleTree class.
    /// </summary>
    public MerkleTree(string version = "1.0", params MerkleLeaf[] leaves)
    {
        this.Metadata = new MerkleMetadata
        {
            Version = version
        };

        this.Leaves = leaves.ToList();
    }

    //

    /// <summary>
    /// Gets the collection of leaf nodes in the Merkle tree.
    /// </summary>
    public IReadOnlyList<MerkleLeaf> Leaves { get; private set; } = new List<MerkleLeaf>();

    /// <summary>
    /// Gets the root hash of the Merkle tree.
    /// </summary>
    public Hex Root { get; private set; } = Hex.Empty;

    /// <summary>
    /// Gets metadata about the Merkle tree.
    /// </summary>
    public MerkleMetadata Metadata { get; private set; } = new MerkleMetadata();

    //

    /// <summary>
    /// Adds a set of leaves to the Merkle tree using the same salt and hash function.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each key-value pair in the dictionary is added as a leaf with the data being the JSON representation
    /// of the key-value pair. For example, if the dictionary contains a single key-value pair {"name": "John"},
    /// then a single leaf will be added with the data "{\"name\":\"John\"}" and the leaf will have the content type
    /// "application/json; charset=utf-8".
    /// </para>
    /// <para>
    /// If the dictionary contains multiple key-value pairs, each pair will be added as a separate leaf.
    /// </para>
    /// <para>
    /// This method is especially useful for turning a dictionary into a Merkle tree containing versatile JSON
    /// key-value pairs.
    /// </para>
    /// </remarks>
    /// <param name="keyValues">The JSON object to add to the leaves.</param>
    /// <param name="salt">The salt to add to the leaves.</param>
    /// <param name="hashFunction">The hash function to use for the leaves.</param>
    /// <returns>The added leaves.</returns>
    public IReadOnlyList<MerkleLeaf> AddJsonLeaves(IDictionary<string, object?> keyValues, Hex salt, HashFunction hashFunction)
    {
        var leaves = new List<MerkleLeaf>();

        foreach (var kvp in keyValues)
        {
            leaves.Add(AddJsonLeaf(kvp.Key, kvp.Value, salt, hashFunction));
        }

        return leaves;
    }

    /// <summary>
    /// Adds a new leaf to the Merkle tree.
    /// </summary>
    /// <param name="fieldName">The name of the field to store in the leaf.</param>
    /// <param name="fieldValue">The value of the field to store in the leaf.</param>
    /// <param name="salt">The salt to add to the leaf.</param>
    /// <param name="hashFunction">The hash function to use for the leaf.</param>
    /// <returns>The added leaf.</returns>
    public MerkleLeaf AddJsonLeaf(string fieldName, object? fieldValue, Hex salt, HashFunction hashFunction)
    {
        var leaf = MerkleLeaf.FromJsonValue(fieldName, fieldValue, salt, hashFunction);
        ((List<MerkleLeaf>)this.Leaves).Add(leaf);

        return leaf;
    }

    /// <summary>
    /// Adds a new leaf to the Merkle tree.
    /// </summary>
    /// <param name="data">The data to add to the leaf.</param>
    /// <param name="salt">The salt to add to the leaf.</param>
    /// <param name="contentType">The content type of the leaf.</param>
    /// <param name="hashFunction">The hash function to use for the leaf.</param>
    /// <returns>The added leaf.</returns>
    public MerkleLeaf AddLeaf(Hex data, Hex salt, string contentType, HashFunction hashFunction)
    {
        var leaf = MerkleLeaf.FromData(contentType, data, salt, hashFunction);

        ((List<MerkleLeaf>)this.Leaves).Add(leaf);

        return leaf;
    }

    /// <summary>
    /// Verifies that the current root matches the computed root from the leaves.
    /// </summary>
    /// <param name="hashFunction">The hash function to use for verification.</param>
    /// <returns>True if the verification passes, false otherwise.</returns>
    public bool VerifyRoot(HashFunction hashFunction)
    {
        if (Leaves.Count == 0)
        {
            return this.Root.Equals(Hex.Empty);
        }

        var computedRoot = ComputeMerkleRoot(Leaves.Select(l => l.Hash).ToList(), hashFunction);

        return this.Root.Equals(computedRoot);
    }

    /// <summary>
    /// Verifies the current root using the SHA-256 algorithm.
    /// </summary>
    /// <returns>True if the verification passes, false otherwise.</returns>
    public bool VerifySha256Root()
    {
        return VerifyRoot(ComputeSha256Hash);
    }

    /// <summary>
    /// Computes and updates the root hash based on the current leaves using the specified hash function.
    /// Also updates the metadata with the hash algorithm name and leaf count.
    /// </summary>
    /// <param name="hashFunction">The hash function to use for computing the root.</param>
    /// <param name="hashAlgorithmName">The name of the hash algorithm used.</param>
    /// <returns>The computed root hash.</returns>
    public Hex RecomputeRoot(HashFunction hashFunction, string hashAlgorithmName)
    {
        if (string.IsNullOrWhiteSpace(hashAlgorithmName))
        {
            throw new ArgumentException("Hash algorithm name cannot be empty", nameof(hashAlgorithmName));
        }

        // Compute the root from leaves
        this.Root = ComputeRoot(hashFunction);

        // Update metadata
        this.Metadata = new MerkleMetadata
        {
            HashAlgorithm = hashAlgorithmName,
            Version = this.Metadata.Version
        };

        return this.Root;
    }

    /// <summary>
    /// Computes and updates the root hash using SHA-256 algorithm.
    /// </summary>
    /// <returns>The computed root hash.</returns>
    public Hex RecomputeSha256Root()
    {
        return RecomputeRoot(ComputeSha256Hash, "sha256");
    }

    /// <summary>
    /// Computes a SHA-256 hash of the input data.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The hash as a Hex.</returns>
    public static Hex ComputeSha256Hash(byte[] data)
    {
        using var sha256 = SHA256.Create();

        return new Hex(sha256.ComputeHash(data));
    }

    /// <summary>
    /// Parses a JSON string into a MerkleTree object.
    /// </summary>
    /// <param name="json">The JSON string representation of a Merkle tree.</param>
    /// <returns>A MerkleTree object parsed from the JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if json is null.</exception>
    /// <exception cref="MalformedJsonException">Thrown if json is not valid or cannot be deserialized.</exception>
    public static MerkleTree Parse(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var dto = JsonSerializer.Deserialize<MerkleTreeDto>(json, options);
            return FromDto(dto);
        }
        catch (JsonException ex)
        {
            throw new MalformedJsonException("Failed to deserialize Merkle tree from JSON", ex);
        }
        catch (Exception ex) when (ex is not MalformedJsonException)
        {
            throw new MalformedJsonException("Failed to deserialize Merkle tree from JSON", ex);
        }
    }

    /// <summary>
    /// Converts the MerkleTree object to its JSON string representation after verifying the root using SHA-256.
    /// </summary>
    /// <returns>A JSON string representation of the MerkleTree object.</returns>
    /// <exception cref="InvalidRootException">Thrown if the root verification fails.</exception>
    public string ToJson()
    {
        return ToJson(ComputeSha256Hash);
    }

    /// <summary>
    /// Converts the MerkleTree object to its JSON string representation after verifying the root.
    /// </summary>
    /// <param name="hashFunction">The hash function to use for verification.</param>
    /// <returns>A JSON string representation of the MerkleTree object.</returns>
    /// <exception cref="InvalidRootException">Thrown if the root verification fails.</exception>
    public string ToJson(HashFunction hashFunction)
    {
        if (!VerifyRoot(hashFunction))
        {
            throw new InvalidRootException(
                $"Merkle tree root verification failed. The root hash '{Root}' does not" +
                $" match the computed hash '{ComputeRoot(hashFunction)}' from leaves.");
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        MerkleTreeDto dto = ToDto();
        return JsonSerializer.Serialize(dto, options);
    }

    //

    private Hex ComputeRoot(HashFunction hashFunction)
    {
        return ComputeMerkleRoot(Leaves.Select(l => l.Hash).ToList(), hashFunction);
    }

    private static Hex ComputeMerkleRoot(List<Hex> hashes, HashFunction hashFunction)
    {
        if (hashes.Count == 0)
        {
            return Hex.Empty;
        }

        if (hashes.Count == 1)
        {
            return hashes[0];
        }

        var nextLevel = new List<Hex>();

        for (int i = 0; i < hashes.Count; i += 2)
        {
            if (i + 1 < hashes.Count)
            {
                // Concatenate the two hashes and hash them together
                var combinedBytes = ConcatenateHashes(hashes[i], hashes[i + 1]);
                nextLevel.Add(hashFunction(combinedBytes));
            }
            else
            {
                // Odd number of hashes, duplicate the last one
                var combinedBytes = ConcatenateHashes(hashes[i], hashes[i]);
                nextLevel.Add(hashFunction(combinedBytes));
            }
        }

        // Recursively build the tree
        return ComputeMerkleRoot(nextLevel, hashFunction);
    }

    private static byte[] ConcatenateHashes(Hex first, Hex second)
    {
        byte[] firstBytes = first.ToByteArray();
        byte[] secondBytes = second.ToByteArray();
        byte[] result = new byte[firstBytes.Length + secondBytes.Length];

        Buffer.BlockCopy(firstBytes, 0, result, 0, firstBytes.Length);
        Buffer.BlockCopy(secondBytes, 0, result, firstBytes.Length, secondBytes.Length);

        return result;
    }

    private MerkleTreeDto ToDto()
    {
        return new MerkleTreeDto
        {
            Root = Root.ToString(),
            Leaves = Leaves?.Select(l => new MerkleLeafDto
            {
                // Always use hex representation for data
                Data = l.ToHexString(),
                Salt = l.Salt.ToString(),
                Hash = l.Hash.ToString(),
                ContentType = l.ContentType
            }).ToList(),
            Metadata = new MerkleMetadataDto
            {
                HashAlgorithm = Metadata?.HashAlgorithm ?? string.Empty,
                Version = Metadata?.Version ?? string.Empty,
            }
        };
    }

    private static MerkleTree FromDto(MerkleTreeDto dto)
    {
        static Hex readData(MerkleLeafDto leafDto)
        {
            if (string.IsNullOrEmpty(leafDto.Data))
            {
                return Hex.Empty;
            }

            if (leafDto.Data.StartsWith("0x"))
            {
                return Hex.Parse(leafDto.Data, HexParseOptions.AllowEmptyString);
            }

            return new Hex(System.Text.Encoding.UTF8.GetBytes(leafDto.Data));
        }

        return new MerkleTree
        {
            Root = Hex.Parse(dto.Root, HexParseOptions.AllowEmptyString),
            Leaves = dto.Leaves?.Select(l => new MerkleLeaf(
                l.ContentType,
                readData(l),
                Hex.Parse(l.Salt, HexParseOptions.AllowEmptyString),
                Hex.Parse(l.Hash, HexParseOptions.AllowEmptyString)
            )).ToList() ?? new List<MerkleLeaf>(),
            Metadata = new MerkleMetadata
            {
                HashAlgorithm = dto.Metadata?.HashAlgorithm ?? string.Empty,
                Version = dto.Metadata?.Version ?? string.Empty,
            }
        };
    }
}
