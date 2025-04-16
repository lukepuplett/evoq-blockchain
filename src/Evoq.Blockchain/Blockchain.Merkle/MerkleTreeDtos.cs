namespace Evoq.Blockchain.Merkle;

using System.Collections.Generic;

/// <summary>
/// DTOs (Data Transfer Objects) for Merkle tree serialization and deserialization.
/// </summary>
internal record struct MerkleTreeDto
{
    /// <summary>
    /// Gets or sets the collection of leaf nodes in the Merkle tree.
    /// </summary>
    public List<MerkleLeafDto>? Leaves { get; set; }

    /// <summary>
    /// Gets or sets the root hash of the Merkle tree.
    /// </summary>
    public string Root { get; set; }

    /// <summary>
    /// Gets or sets metadata about the Merkle tree.
    /// </summary>
    public MerkleMetadataDto? Metadata { get; set; }
}

/// <summary>
/// DTO (Data Transfer Object) for MerkleLeaf serialization and deserialization.
/// </summary>
internal record struct MerkleLeafDto
{
    /// <summary>
    /// Gets or sets the data contained in the leaf.
    /// This should be a hex string (e.g., "0x1234abcd").
    /// </summary>
    public string Data { get; set; }

    /// <summary>
    /// Gets or sets the salt used for hashing the leaf data.
    /// </summary>
    public string Salt { get; set; }

    /// <summary>
    /// Gets or sets the hash of the leaf.
    /// </summary>
    public string Hash { get; set; }

    /// <summary>
    /// Gets or sets the MIME content type of the data including encoding information 
    /// (e.g., "text/plain; charset=utf-8", "application/json; charset=utf-8").
    /// </summary>
    public string ContentType { get; set; }
}

/// <summary>
/// DTO (Data Transfer Object) for MerkleMetadata serialization and deserialization.
/// </summary>
internal record struct MerkleMetadataDto
{
    /// <summary>
    /// Gets or sets the hash algorithm used in the Merkle tree.
    /// </summary>
    public string HashAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the version of the Merkle tree implementation.
    /// </summary>
    public string Version { get; set; }
}