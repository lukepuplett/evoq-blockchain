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
    // Delegates
    /// <summary>
    /// Delegate for hash function implementations to be used with Merkle trees.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>A hex representation of the hash result.</returns>
    public delegate Hex HashFunction(byte[] data);

    // Public Properties
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

    // Constructors
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

    // Public Methods
    /// <summary>
    /// Adds a set of leaves to the Merkle tree, each with their own unique salt using the SHA-256 hash function.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each JsonLeafData entry represents a key-value pair with its own unique salt. The data for each leaf
    /// will be the JSON representation of the key-value pair.
    /// </para>
    /// <para>
    /// For example, if an entry has Key="name" and Value="John", then a leaf will be added with 
    /// the data "{\"name\":\"John\"}" and the leaf will have the content type "application/json; charset=utf-8".
    /// </para>
    /// </remarks>
    /// <param name="keyValues">Dictionary of key-value pairs to add as leaves.</param>
    public void AddJsonLeaves(Dictionary<string, object?> keyValues)
    {
        var leaves = keyValues.Select(kvp => MerkleLeaf.FromJsonValue(kvp.Key, kvp.Value));
        this.AddJsonLeaves(leaves);
    }

    /// <summary>
    /// Adds a set of leaves to the Merkle tree, each with their own unique salt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each JsonLeafData entry represents a key-value pair with its own unique salt. The data for each leaf
    /// will be the JSON representation of the key-value pair.
    /// </para>
    /// <para>
    /// For example, if an entry has Key="name" and Value="John", then a leaf will be added with 
    /// the data "{\"name\":\"John\"}" and the leaf will have the content type "application/json; charset=utf-8".
    /// </para>
    /// </remarks>
    /// <param name="leafEntries">Collection of leaf entries, each with its own salt.</param>
    public void AddJsonLeaves(IEnumerable<MerkleLeaf> leafEntries)
    {
        foreach (var leaf in leafEntries)
        {
            this.AddLeaf(leaf);
        }
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
        this.AddLeaf(leaf);
        return leaf;
    }

    /// <summary>
    /// Adds a new leaf to the Merkle tree.
    /// </summary>
    /// <param name="data">The data to add to the leaf.</param>
    /// <param name="contentType">The content type of the leaf.</param>
    /// <returns>The added leaf.</returns>
    public MerkleLeaf AddLeaf(Hex data, string contentType)
    {
        var leaf = MerkleLeaf.FromData(contentType, data);
        this.AddLeaf(leaf);
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
        this.AddLeaf(leaf);
        return leaf;
    }

    /// <summary>
    /// Adds a new leaf to the Merkle tree.
    /// </summary>
    /// <param name="leaf">The leaf to add.</param>
    public void AddLeaf(MerkleLeaf leaf)
    {
        ((List<MerkleLeaf>)this.Leaves).Add(leaf);
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

        var computedRoot = this.ComputeRootFromLeafHashes(hashFunction);
        return this.Root.Equals(computedRoot);
    }

    /// <summary>
    /// Verifies that the current root matches the computed root from the leaves using the hash function specified in the tree's metadata.
    /// </summary>
    /// <returns>True if the verification passes, false otherwise.</returns>
    /// <exception cref="NotSupportedException">Thrown if the hash algorithm specified in the metadata is not supported.</exception>
    public bool VerifyRoot()
    {
        var hashFunction = GetHashFunctionFromMetadata();
        return VerifyRoot(hashFunction);
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

        if (Leaves.Count == 0)
        {
            throw new InvalidOperationException("Cannot recompute root from empty tree");
        }

        this.Root = ComputeRootFromLeafHashes(hashFunction);
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
        return RecomputeRoot(ComputeSha256Hash, MerkleTreeHashAlgorithmStrings.Sha256);
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

            // First parse as JsonDocument to detect version
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check if this is v2.0 format (has header)
            if (root.TryGetProperty("header", out _))
            {
                var dto = JsonSerializer.Deserialize<MerkleTreeV2Dto>(json, options);
                return FromV2Dto(dto);
            }
            // Otherwise assume v1.0 format (has metadata)
            else if (root.TryGetProperty("metadata", out _))
            {
                var dto = JsonSerializer.Deserialize<MerkleTreeV1Dto>(json, options);
                return FromDto(dto);
            }
            else
            {
                throw new MalformedJsonException("JSON does not contain either 'header' or 'metadata' property");
            }
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
    /// <param name="makePrivate">A predicate to determine if a leaf should be made private.</param>
    /// <param name="options">JSON serializer options to customize the output.</param>
    /// <returns>A JSON string representation of the MerkleTree object.</returns>
    /// <exception cref="InvalidRootException">Thrown if the root verification fails.</exception>
    public string ToJson(HashFunction hashFunction, Predicate<MerkleLeaf>? makePrivate = null, JsonSerializerOptions? options = null)
    {
        if (!VerifyRoot(hashFunction))
        {
            throw new InvalidRootException(
                $"Merkle tree root verification failed. The root hash '{Root}' does not" +
                $" match the computed hash '{ComputeRootFromLeafHashes(hashFunction)}' from leaves.");
        }

        options ??= new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (Metadata.Version == MerkleTreeVersionStrings.V1_0)
        {
            MerkleTreeV1Dto v1 = this.ToV1Dto(makePrivate ?? (leaf => false));
            return JsonSerializer.Serialize(v1, options);
        }
        else if (Metadata.Version == MerkleTreeVersionStrings.V2_0)
        {
            MerkleTreeV2Dto v2 = this.ToV2Dto(makePrivate ?? (leaf => false));
            return JsonSerializer.Serialize(v2, options);
        }
        else
        {
            throw new ArgumentException("Unable to serialize Merkle tree with version " + Metadata.Version);
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random salt.
    /// </summary>
    /// <param name="length">Length of the salt in bytes. Default is 16 bytes (128 bits).</param>
    /// <returns>A hex-encoded random salt.</returns>
    public static Hex GenerateRandomSalt(int length = 16)
    {
        byte[] saltBytes = new byte[length];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return new Hex(saltBytes);
    }

    // Private Methods
    private HashFunction GetHashFunctionFromMetadata()
    {
        return Metadata.HashAlgorithm switch
        {
            MerkleTreeHashAlgorithmStrings.Sha256 => ComputeSha256Hash,
            MerkleTreeHashAlgorithmStrings.Sha256Legacy => ComputeSha256Hash,
            _ => throw new NotSupportedException(
                $"Hash algorithm '{Metadata.HashAlgorithm}' specified in metadata is not supported. " +
                "To use a custom hash algorithm, call VerifyRoot(HashFunction) directly with your implementation.")
        };
    }

    private Hex ComputeRootFromLeafHashes(HashFunction hashFunction)
    {
        if (Leaves.Count == 0)
        {
            throw new InvalidOperationException("Cannot compute root from empty tree");
        }

        // Compute hashes from the leaf data and salt for non-private leaves
        // to ensure that we detect tampered data during verification

        List<Hex> hashes = new List<Hex>();
        foreach (var leaf in Leaves)
        {
            if (leaf.IsPrivate)
            {
                // For private leaves, use the stored hash
                hashes.Add(leaf.Hash);
            }
            else
            {
                // For non-private leaves, recompute the hash from data and salt
                var computedHash = hashFunction(Hex.Concat(leaf.Data, leaf.Salt).ToByteArray());
                hashes.Add(computedHash);
            }
        }

        return ComputeMerkleRoot(hashes, hashFunction);
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

                var combinedBytes = ConcatenateHashes(hashes[i], hashes[i + 1]); // left and right
                nextLevel.Add(hashFunction(combinedBytes));
            }
            else
            {
                // Odd number of hashes, duplicate the last one

                var combinedBytes = ConcatenateHashes(hashes[i], hashes[i]); // duplicate the last one
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

    private MerkleTreeV1Dto ToV1Dto(Predicate<MerkleLeaf> makePrivate)
    {
        return new MerkleTreeV1Dto
        {
            Root = Root.ToString(),
            Leaves = Leaves?.Select(l => makeLeafV1Dto(l, makePrivate)).ToList(),
            Metadata = new MerkleTreeV1MetadataDto
            {
                HashAlgorithm = Metadata?.HashAlgorithm ?? string.Empty,
                Version = Metadata?.Version ?? string.Empty,
            }
        };

        static MerkleTreeV1LeafDto makeLeafV1Dto(MerkleLeaf leaf, Predicate<MerkleLeaf> makePrivate)
        {
            if (makePrivate(leaf))
            {
                return new MerkleTreeV1LeafDto
                {
                    Hash = leaf.Hash.ToString()
                };
            }
            else
            {
                return new MerkleTreeV1LeafDto
                {
                    Data = leaf.ToHexString(),
                    Salt = leaf.Salt.ToString(),
                    Hash = leaf.Hash.ToString(),
                    ContentType = leaf.ContentType
                };
            }
        }
    }

    private MerkleTreeV2Dto ToV2Dto(Predicate<MerkleLeaf> makePrivate)
    {
        return new MerkleTreeV2Dto
        {
            Root = Root.ToString(),
            Leaves = Leaves?.Select(l => makeLeafV2Dto(l, makePrivate)).ToList(),
            Header = new MerkleTreeV2HeaderDto
            {
                Alg = Metadata?.HashAlgorithm ?? string.Empty,
                Typ = Metadata?.Version ?? string.Empty,
            }
        };

        static MerkleTreeV2LeafDto makeLeafV2Dto(MerkleLeaf leaf, Predicate<MerkleLeaf> makePrivate)
        {
            if (makePrivate(leaf))
            {
                return new MerkleTreeV2LeafDto
                {
                    Hash = leaf.Hash.ToString()
                };
            }
            else
            {
                return new MerkleTreeV2LeafDto
                {
                    Data = leaf.ToHexString(),
                    Salt = leaf.Salt.ToString(),
                    Hash = leaf.Hash.ToString(),
                    ContentType = leaf.ContentType
                };
            }
        }
    }

    private static MerkleTree FromDto(MerkleTreeV1Dto dto)
    {
        var hexParseOptions = HexParseOptions.AllowEmptyString | HexParseOptions.AllowNullString;

        Hex readData(MerkleTreeV1LeafDto leafDto)
        {
            if (string.IsNullOrEmpty(leafDto.Data))
            {
                return Hex.Empty;
            }

            if (leafDto.Data.StartsWith("0x"))
            {
                return Hex.Parse(leafDto.Data, hexParseOptions);
            }

            return new Hex(System.Text.Encoding.UTF8.GetBytes(leafDto.Data));
        }

        return new MerkleTree
        {
            Root = Hex.Parse(dto.Root, hexParseOptions),
            Leaves = dto.Leaves?.Select(l => new MerkleLeaf(
                l.ContentType,
                readData(l),
                Hex.Parse(l.Salt, hexParseOptions),
                Hex.Parse(l.Hash, hexParseOptions)
            )).ToList() ?? new List<MerkleLeaf>(),
            Metadata = new MerkleMetadata
            {
                HashAlgorithm = dto.Metadata?.HashAlgorithm ?? string.Empty,
                Version = dto.Metadata?.Version ?? string.Empty,
            }
        };
    }

    private static MerkleTree FromV2Dto(MerkleTreeV2Dto dto)
    {
        var hexParseOptions = HexParseOptions.AllowEmptyString | HexParseOptions.AllowNullString;

        Hex readData(MerkleTreeV2LeafDto leafDto)
        {
            if (string.IsNullOrEmpty(leafDto.Data))
            {
                return Hex.Empty;
            }

            if (leafDto.Data.StartsWith("0x"))
            {
                return Hex.Parse(leafDto.Data, hexParseOptions);
            }

            return new Hex(System.Text.Encoding.UTF8.GetBytes(leafDto.Data));
        }

        return new MerkleTree
        {
            Root = Hex.Parse(dto.Root, hexParseOptions),
            Leaves = dto.Leaves?.Select(l => new MerkleLeaf(
                l.ContentType,
                readData(l),
                Hex.Parse(l.Salt, hexParseOptions),
                Hex.Parse(l.Hash, hexParseOptions)
            )).ToList() ?? new List<MerkleLeaf>(),
            Metadata = new MerkleMetadata
            {
                HashAlgorithm = dto.Header?.Alg ?? MerkleTreeHashAlgorithmStrings.Sha256,
                Version = dto.Header?.Typ ?? MerkleTreeVersionStrings.V1_0,
            }
        };
    }
}
