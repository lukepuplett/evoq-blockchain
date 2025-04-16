namespace Evoq.Blockchain.Merkle;

/// <summary>
/// Contains metadata about a Merkle tree.
/// </summary>
public class MerkleMetadata
{
    /// <summary>
    /// Gets or sets the hash algorithm used in the Merkle tree.
    /// </summary>
    public string HashAlgorithm { get; set; } = "none";

    /// <summary>
    /// Gets or sets the version of the Merkle tree implementation.
    /// </summary>
    public string Version { get; set; } = "1.0";
}