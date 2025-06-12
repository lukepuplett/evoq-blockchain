namespace Evoq.Blockchain.Merkle;

/// <summary>
/// Contains the version strings for the Merkle tree.
/// </summary>
public static class MerkleTreeVersionStrings
{
    /// <summary>
    /// The version string for the Merkle tree version 1.0.
    /// </summary>
    public const string V1_0 = "1.0";

    /// <summary>
    /// The version string for the Merkle tree version 2.0.
    /// </summary>
    public const string V2_0 = "MerkleTree+2.0";

    /// <summary>
    /// The version string for the Merkle tree version 3.0.
    /// </summary>
    public const string V3_0 = "application/merkle-exchange-3.0+json";
}

/// <summary>
/// Contains the hash algorithm strings for the Merkle tree.
/// </summary>
public static class MerkleTreeHashAlgorithmStrings
{
    /// <summary>
    /// The hash algorithm string for the SHA-256 hash algorithm.
    /// </summary>
    public const string Sha256Legacy = "sha256";

    /// <summary>
    /// The hash algorithm string for the SHA-256 hash algorithm using JWT style.
    /// </summary>
    public const string Sha256 = "SHA256";
}

/// <summary>
/// Contains metadata about a Merkle tree.
/// </summary>
public class MerkleMetadata
{
    /// <summary>
    /// Gets or sets the hash algorithm used in the Merkle tree.
    /// </summary>
    public string HashAlgorithm { get; set; } = MerkleTreeHashAlgorithmStrings.Sha256;

    /// <summary>
    /// Gets or sets the version of the Merkle tree implementation.
    /// </summary>
    public string Version { get; set; } = MerkleTreeVersionStrings.V1_0;

    /// <summary>
    /// Gets or sets the type of the document or record being exchanged in the leaf data.
    /// </summary>
    public string? ExchangeDocumentType { get; set; }
}