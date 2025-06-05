namespace Evoq.Blockchain.Merkle;

using System.Collections.Generic;

/// <summary>
/// DTOs (Data Transfer Objects) for Merkle tree serialization and deserialization.
/// </summary>
internal record struct MerkleTreeV2Dto
{
    /// <summary>
    /// Gets or sets the collection of leaf nodes in the Merkle tree.
    /// </summary>
    public List<MerkleTreeV2LeafDto>? Leaves { get; set; }

    /// <summary>
    /// Gets or sets the root hash of the Merkle tree.
    /// </summary>
    public string Root { get; set; }

    /// <summary>
    /// Gets or sets metadata about the Merkle tree.
    /// </summary>
    public MerkleTreeV2HeaderDto? Header { get; set; }
}

/// <summary>
/// DTO (Data Transfer Object) for MerkleLeaf serialization and deserialization.
/// </summary>
internal record struct MerkleTreeV2LeafDto
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
internal record struct MerkleTreeV2HeaderDto
{
    /// <summary>
    /// Gets or sets the hash algorithm used in the Merkle tree.
    /// </summary>
    public string Alg { get; set; }

    /// <summary>
    /// Gets or sets the type/version of the Merkle tree implementation.
    /// </summary>
    public string Typ { get; set; }
}