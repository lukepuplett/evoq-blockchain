namespace Evoq.Blockchain.Merkle;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using Evoq.Blockchain;

/// <summary>
/// Exception thrown when a leaf cannot be read as JSON and therefore cannot be processed for selective disclosure.
/// </summary>
public class NonJsonLeafException : Exception
{
    /// <summary>
    /// Initializes a new instance of the NonJsonLeafException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NonJsonLeafException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the NonJsonLeafException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NonJsonLeafException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// A Merkle tree is a binary tree of hashes.
/// </summary>
/// <remarks>
/// A simple Merkle tree which can be assembled manually or parsed from a JSON string. The design forces the user
/// to verify the root before serializing to JSON, ensuring that the tree is valid before it is saved or exchanged.
/// If the tree fails verification, an exception can be caught and the tree root can be recomputed from the leaves.
/// 
/// Version 3.0 introduces a protected header leaf that provides enhanced security and interoperability:
/// 
/// Security Improvements:
/// - The header leaf is part of the Merkle tree itself, making its contents (algorithm, leaf count, exchange type) 
///   cryptographically protected by the tree's structure
/// - Protects against leaf addition/removal attacks by including the exact leaf count
/// - Prevents single leaf attacks by requiring a header leaf
/// - Protects against algorithm substitution by including the hash algorithm in the protected header
/// - Includes the type of data/record being exchanged (e.g., "invoice", "contract", "certificate") to prevent 
///   mixing different types of records in the same tree
/// - Uses proper MIME types for structured exchange format
/// - Performs strict validation of the header leaf during parsing
/// 
/// Interoperability Features:
/// - Uses standard MIME types (application/merkle-exchange-3.0+json) for structured data exchange
/// - Supports selective disclosure through private leaves and makePrivate predicates
/// - Enables efficient proof generation with O(log n) hashes for classical Merkle proofs
/// - Maintains backward compatibility with v1.0 and v2.0 formats
/// 
/// Use Cases:
/// - Selective Disclosure: Reveal specific leaves while keeping others private
/// - Document Exchange: Exchange structured data with type safety and integrity
/// - Proof Generation: Generate compact proofs for verification
/// - Private Storage: Store full structure (data, salts, hashes) for quick proof reissuance
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

    // Private Properties

    private IList<MerkleLeaf> WritableLeaves => (IList<MerkleLeaf>)this.Leaves;

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
        this.WritableLeaves.Add(leaf);
    }

    /// <summary>
    /// Adds a new private leaf to the Merkle tree that only contains a hash.
    /// </summary>
    /// <param name="hash">The hash of the leaf.</param>
    /// <returns>The added leaf.</returns>
    public MerkleLeaf AddPrivateLeaf(Hex hash)
    {
        var leaf = new MerkleLeaf(hash);
        this.AddLeaf(leaf);
        return leaf;
    }

    /// <summary>
    /// Verifies that the current root matches the computed root from the leaves.
    /// </summary>
    /// <param name="hashFunction">The hash function to use for verification.</param>
    /// <param name="computedRoot">The computed root hash.</param>
    /// <returns>True if the verification passes, false otherwise.</returns>
    public bool VerifyRoot(HashFunction hashFunction, out Hex computedRoot)
    {
        if (this.Leaves.Count == 0)
        {
            computedRoot = Hex.Empty;
            return this.Root.Equals(Hex.Empty);
        }

        try
        {
            computedRoot = this.ComputeRootFromLeafHashes(hashFunction, false);
            return this.Root.Equals(computedRoot);
        }
        catch (InvalidLeafHashException)
        {
            computedRoot = Hex.Empty;
            return false;
        }
    }

    /// <summary>
    /// Verifies that the current root matches the computed root from the leaves.
    /// </summary>
    /// <param name="hashFunction">The hash function to use for verification.</param>
    /// <returns>True if the verification passes, false otherwise.</returns>
    public bool VerifyRoot(HashFunction hashFunction)
    {
        return this.VerifyRoot(hashFunction, out _);
    }

    /// <summary>
    /// Verifies that the current root matches the computed root from the leaves using the hash function specified in the tree's metadata.
    /// </summary>
    /// <returns>True if the verification passes, false otherwise.</returns>
    /// <exception cref="NotSupportedException">Thrown if the hash algorithm specified in the metadata is not supported.</exception>
    public bool VerifyRoot()
    {
        var hashFunction = GetHashFunctionFromMetadata(this.Metadata.HashAlgorithm);
        return this.VerifyRoot(hashFunction);
    }

    /// <summary>
    /// Verifies the current root using the SHA-256 algorithm.
    /// </summary>
    /// <returns>True if the verification passes, false otherwise.</returns>
    public bool VerifySha256Root()
    {
        return this.VerifyRoot(ComputeSha256Hash);
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

        this.Metadata.HashAlgorithm = hashAlgorithmName;
        this.Root = this.ComputeRootFromLeafHashes(hashFunction, true);

        return this.Root;
    }

    /// <summary>
    /// Computes and updates the root hash using SHA-256 algorithm.
    /// </summary>
    /// <returns>The computed root hash.</returns>
    public Hex RecomputeSha256Root()
    {
        return this.RecomputeRoot(ComputeSha256Hash, MerkleTreeHashAlgorithmStrings.Sha256);
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
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // First parse as JsonDocument to detect version
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check if this is greater than v1.0 format (has header)
            if (root.TryGetProperty("header", out var header))
            {
                if (header.TryGetProperty("typ", out var typ1) && typ1.GetString() == MerkleTreeVersionStrings.V2_0)
                {
                    var dto = JsonSerializer.Deserialize<MerkleTreeV2Dto>(json, options);
                    return FromV2Dto(dto);
                }
                else if (header.TryGetProperty("typ", out var typ2) && typ2.GetString() == MerkleTreeVersionStrings.V3_0)
                {
                    var dto = JsonSerializer.Deserialize<MerkleTreeV3Dto>(json, options);
                    return FromV3Dto(dto);
                }
                else
                {
                    throw new MalformedJsonException("JSON does not contain a valid header with version " + MerkleTreeVersionStrings.V2_0 + " or " + MerkleTreeVersionStrings.V3_0);
                }
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
    /// Converts the MerkleTree object to its JSON string representation after verifying the root.
    /// </summary>
    /// <param name="hashFunction">The hash function to use for verification.</param>
    /// <param name="makePrivate">A predicate to determine if a leaf should be made private, in addition to the leaf's own IsPrivate property.</param>
    /// <param name="options">JSON serializer options to customize the output.</param>
    /// <returns>A JSON string representation of the MerkleTree object.</returns>
    /// <exception cref="InvalidRootException">Thrown if the root verification fails.</exception>
    public string ToJson(HashFunction hashFunction, Predicate<MerkleLeaf>? makePrivate = null, JsonSerializerOptions? options = null)
    {
        if (!this.VerifyRoot(hashFunction, out var computedRoot))
        {
            throw new InvalidRootException(
                $"Merkle tree root verification failed. The root hash '{Root}' does not" +
                $" match the computed hash '{computedRoot}' from leaves.");
        }

        options ??= new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Combine the leaf's IsPrivate property with any additional predicate
        Predicate<MerkleLeaf> combinedPredicate = leaf => leaf.IsPrivate || (makePrivate?.Invoke(leaf) ?? false);

        if (Metadata.Version == MerkleTreeVersionStrings.V1_0)
        {
            MerkleTreeV1Dto v1 = this.ToV1Dto(combinedPredicate);
            return JsonSerializer.Serialize(v1, options);
        }
        else if (Metadata.Version == MerkleTreeVersionStrings.V2_0)
        {
            MerkleTreeV2Dto v2 = this.ToV2Dto(combinedPredicate);
            return JsonSerializer.Serialize(v2, options);
        }
        else if (Metadata.Version == MerkleTreeVersionStrings.V3_0)
        {
            MerkleTreeV3Dto v3 = this.ToV3Dto(combinedPredicate);
            return JsonSerializer.Serialize(v3, options);
        }
        else
        {
            throw new ArgumentException("Unable to serialize Merkle tree with version " + Metadata.Version);
        }
    }

    /// <summary>
    /// Converts the MerkleTree object to its JSON string representation after verifying the root using SHA-256.
    /// </summary>
    /// <returns>A JSON string representation of the MerkleTree object.</returns>
    /// <exception cref="InvalidRootException">Thrown if the root verification fails.</exception>
    public string ToJson()
    {
        return this.ToJson(ComputeSha256Hash);
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

    /// <summary>
    /// Creates a new MerkleTree with selective disclosure based on the source tree and a predicate.
    /// </summary>
    /// <param name="sourceTree">The source MerkleTree to create a selective disclosure version from.</param>
    /// <param name="makePrivate">A predicate that determines which leaves should be made private.</param>
    /// <returns>A new MerkleTree with the specified selective disclosure applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when sourceTree or makePrivate is null.</exception>
    public static MerkleTree From(MerkleTree sourceTree, Predicate<MerkleLeaf> makePrivate)
    {
        if (sourceTree == null)
        {
            throw new ArgumentNullException(nameof(sourceTree));
        }

        if (makePrivate == null)
        {
            throw new ArgumentNullException(nameof(makePrivate));
        }

        if (sourceTree.Root.IsEmpty())
        {
            throw new InvalidOperationException("Unable to create selective disclosure version of a tree with no root");
        }

        var newLeaves = new List<MerkleLeaf>();

        foreach (var leaf in sourceTree.Leaves)
        {
            bool shouldBePrivate = makePrivate(leaf);

            if (!shouldBePrivate || leaf.IsMetadata)
            {
                // Create a new leaf with full data (copy the original)
                newLeaves.Add(new MerkleLeaf(leaf.ContentType, leaf.Data, leaf.Salt, leaf.Hash));
            }
            else
            {
                // Create a private leaf with just the hash
                newLeaves.Add(new MerkleLeaf(leaf.Hash));
            }
        }

        var newTree = new MerkleTree(sourceTree.Metadata.Version, newLeaves.ToArray());

        newTree.Metadata.HashAlgorithm = sourceTree.Metadata.HashAlgorithm;
        newTree.Metadata.ExchangeDocumentType = sourceTree.Metadata.ExchangeDocumentType;
        newTree.Root = newTree.ComputeRootFromLeafHashes(GetHashFunctionFromMetadata(newTree.Metadata.HashAlgorithm), false);

        return newTree;
    }

    /// <summary>
    /// Creates a new MerkleTree with selective disclosure based on the source tree and a set of keys to preserve.
    /// Leaves containing any of the specified keys will be revealed, all others will be made private.
    /// </summary>
    /// <param name="sourceTree">The source MerkleTree to create a selective disclosure version from.</param>
    /// <param name="preserveKeys">A set of keys to preserve (reveal) in the new tree. Leaves containing any of these keys will be revealed.</param>
    /// <returns>A new MerkleTree with the specified selective disclosure applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when sourceTree or preserveKeys is null.</exception>
    /// <exception cref="NonJsonLeafException">Thrown when one or more leaves cannot be read as JSON and therefore cannot be processed for selective disclosure.</exception>
    public static MerkleTree From(MerkleTree sourceTree, ISet<string> preserveKeys)
    {
        if (sourceTree == null)
        {
            throw new ArgumentNullException(nameof(sourceTree));
        }

        if (preserveKeys == null)
        {
            throw new ArgumentNullException(nameof(preserveKeys));
        }

        // Create a predicate that uses TryReadJsonKeys to determine which leaves to preserve
        Predicate<MerkleLeaf> makePrivate = leaf =>
        {
            if (!leaf.TryReadJsonKeys(out var keys))
            {
                throw new NonJsonLeafException($"Leaf cannot be read as JSON and therefore cannot be processed for selective disclosure.");
            }

            // If the leaf has any of the keys we want to preserve, don't make it private
            return !keys.Any(key => preserveKeys.Contains(key));
        };

        return From(sourceTree, makePrivate);
    }

    // Private Methods
    private static HashFunction GetHashFunctionFromMetadata(string hashAlgorithmName)
    {
        return hashAlgorithmName switch
        {
            MerkleTreeHashAlgorithmStrings.Sha256 => ComputeSha256Hash,
            MerkleTreeHashAlgorithmStrings.Sha256Legacy => ComputeSha256Hash,
            _ => throw new NotSupportedException(
                $"Hash algorithm '{hashAlgorithmName}' specified in metadata is not supported. " +
                "To use a custom hash algorithm, call VerifyRoot(HashFunction) directly with your implementation.")
        };
    }

    private Hex ComputeRootFromLeafHashes(HashFunction hashFunction, bool forceNewHeader)
    {
        if (this.Leaves.Count == 0)
        {
            throw new InvalidOperationException("Cannot compute root from empty tree");
        }

        if (this.Metadata.Version == MerkleTreeVersionStrings.V3_0)
        {
            // v3.0 has its first leaf as a header leaf which protects the algorithm
            // and attacks which add or remove leaves, and single leaf presentation
            // attacks.
            //
            // The first time we compute the root, we need to add a header leaf to
            // the tree, if it is not already present.

            var hasHeaderLeaf = this.Leaves.First().IsMetadata;

            if (forceNewHeader && hasHeaderLeaf)
            {
                this.WritableLeaves.RemoveAt(0);
            }

            if (forceNewHeader || !hasHeaderLeaf)
            {
                var leaves = new List<MerkleLeaf>(this.Leaves.Count + 1);

                MerkleTreeV3LeafHeaderDto header = new()
                {
                    Alg = this.Metadata.HashAlgorithm, // assumes Metadata is configured before calling this method
                    Typ = ContentTypeUtility.V3_0_HEADER_MIME_TYPE,
                    Leaves = this.Leaves.Count + 1,
                    Exchange = this.Metadata.ExchangeDocumentType ?? "unspecified",
                };

                var headerJson = JsonSerializer.Serialize(header);
                var headerHex = new Hex(System.Text.Encoding.UTF8.GetBytes(headerJson));
                var headerLeaf = MerkleLeaf.FromData(ContentTypeUtility.V3_0_HEADER_CONTENT_TYPE, headerHex);

                leaves.Add(headerLeaf);
                leaves.AddRange(this.Leaves);

                this.Leaves = leaves;
            }
        }

        // Compute hashes from the leaf data and salt for non-private leaves
        // to ensure that we detect tampered data during verification

        var hashes = new List<Hex>(this.Leaves.Count);

        foreach (var leaf in this.Leaves)
        {
            if (leaf.IsPrivate)
            {
                // For private leaves, use the stored hash
                hashes.Add(leaf.Hash);
            }
            else
            {
                // Recompute the hash from data and salt
                var computedHash = MerkleLeaf.UseHashFunction(hashFunction, leaf.Data, leaf.Salt);

                if (!leaf.Hash.IsEmpty())
                {
                    if (computedHash != leaf.Hash)
                    {
                        throw new InvalidLeafHashException("Leaf hash does not match computed hash");
                    }
                }

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
            // Verified: For a single leaf, we return its hash directly as the root.
            // This is correct because the hash parameter already contains the hashed leaf value.

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

    private MerkleTreeV3Dto ToV3Dto(Predicate<MerkleLeaf> makePrivate)
    {
        return new MerkleTreeV3Dto
        {
            Root = Root.ToString(),
            Leaves = Leaves?.Select(l => makeLeafV3Dto(l, makePrivate)).ToList(),
            Header = new MerkleTreeV3HeaderDto
            {
                Typ = Metadata?.Version ?? string.Empty,
            }
        };

        static MerkleTreeV3LeafDto makeLeafV3Dto(MerkleLeaf leaf, Predicate<MerkleLeaf> makePrivate)
        {
            if (makePrivate(leaf))
            {
                return new MerkleTreeV3LeafDto
                {
                    Hash = leaf.Hash.ToString()
                };
            }
            else
            {
                return new MerkleTreeV3LeafDto
                {
                    Data = leaf.ToHexString(),
                    Salt = leaf.Salt.ToString(),
                    Hash = leaf.Hash.ToString(),
                    ContentType = leaf.ContentType
                };
            }
        }
    }

    private static MerkleTree FromDto(MerkleTreeV1Dto inboundDto)
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
            Root = Hex.Parse(inboundDto.Root, hexParseOptions),
            Leaves = inboundDto.Leaves?.Select(leafDto => new MerkleLeaf(
                leafDto.ContentType,
                readData(leafDto),
                Hex.Parse(leafDto.Salt, hexParseOptions),
                Hex.Parse(leafDto.Hash, hexParseOptions)
            )).ToList() ?? new List<MerkleLeaf>(),
            Metadata = new MerkleMetadata
            {
                HashAlgorithm = inboundDto.Metadata?.HashAlgorithm ?? string.Empty,
                Version = inboundDto.Metadata?.Version ?? string.Empty,
            }
        };
    }

    private static MerkleTree FromV2Dto(MerkleTreeV2Dto inboundDto)
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
            Root = Hex.Parse(inboundDto.Root, hexParseOptions),
            Leaves = inboundDto.Leaves?.Select(l => new MerkleLeaf(
                l.ContentType,
                readData(l),
                Hex.Parse(l.Salt, hexParseOptions),
                Hex.Parse(l.Hash, hexParseOptions)
            )).ToList() ?? new List<MerkleLeaf>(),
            Metadata = new MerkleMetadata
            {
                HashAlgorithm = inboundDto.Header?.Alg ?? MerkleTreeHashAlgorithmStrings.Sha256,
                Version = inboundDto.Header?.Typ ?? MerkleTreeVersionStrings.V1_0,
            }
        };
    }

    private static MerkleTree FromV3Dto(MerkleTreeV3Dto inboundDto)
    {
        var hexParseOptions = HexParseOptions.AllowEmptyString | HexParseOptions.AllowNullString;

        Hex readData(MerkleTreeV3LeafDto leafDto)
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

        MerkleTreeV3LeafHeaderDto readLeafHeader(Hex leafData)
        {
            var headerJson = System.Text.Encoding.UTF8.GetString(leafData.ToByteArray());

            try
            {
                var header = JsonSerializer.Deserialize<MerkleTreeV3LeafHeaderDto>(headerJson);

                // Verify this is actually a header leaf by checking required fields
                if (string.IsNullOrEmpty(header.Alg) ||
                    string.IsNullOrEmpty(header.Typ) ||
                    string.IsNullOrEmpty(header.Exchange) ||
                    header.Leaves <= 0)
                {
                    throw new MalformedJsonException("Unable to parse V3.0 tree: first leaf is not a valid header leaf");
                }

                // Verify the content type matches V3 header format
                if (header.Typ != "application/merkle-exchange-header-3.0+json")
                {
                    throw new MalformedJsonException("Unable to parse V3.0 tree: header leaf has incorrect type");
                }

                return header;
            }
            catch (JsonException)
            {
                throw new MalformedJsonException("Unable to parse V3.0 tree: header leaf is not valid JSON");
            }
        }

        var leaves = inboundDto.Leaves?.Select(leafDto => new MerkleLeaf(
                leafDto.ContentType,
                readData(leafDto),
                Hex.Parse(leafDto.Salt, hexParseOptions),
                Hex.Parse(leafDto.Hash, hexParseOptions)
            )).ToList() ?? new List<MerkleLeaf>();

        if (leaves.Count == 0)
        {
            throw new MalformedJsonException("Unable to parse V3.0 tree: no leaves found");
        }

        var protectedHeader = readLeafHeader(leaves[0].Data);

        if (leaves.Count != protectedHeader.Leaves)
        {
            throw new MalformedJsonException("Unable to parse V3.0 tree: leaf count mismatch");
        }

        return new MerkleTree
        {
            Root = Hex.Parse(inboundDto.Root, hexParseOptions),
            Leaves = leaves,
            Metadata = new MerkleMetadata
            {
                HashAlgorithm = protectedHeader.Alg ?? throw new MalformedJsonException("Unable to parse V3.0 header: 'alg' field is missing"),
                Version = inboundDto.Header?.Typ ?? throw new MalformedJsonException("Unable to parse V3.0 header: 'typ' field is missing"),
                ExchangeDocumentType = protectedHeader.Exchange ?? throw new MalformedJsonException("Unable to parse V3.0 header: 'exchange' field is missing"),
            }
        };
    }
}
