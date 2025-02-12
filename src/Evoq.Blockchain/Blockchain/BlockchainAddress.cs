using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Evoq.Blockchain;

/// <summary>
/// Exception thrown when a blockchain address format is invalid.
/// </summary>
public class InvalidBlockchainAddressException : FormatException
{
    public InvalidBlockchainAddressException(string message) : base(message) { }
    public InvalidBlockchainAddressException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Represents a blockchain address using CAIP-10 format.
/// https://github.com/ChainAgnostic/CAIPs/blob/master/CAIPs/caip-10.md
/// </summary>
public readonly struct BlockchainAddress : IEquatable<BlockchainAddress>
{
    private const string Delimiter = ":";
    private static readonly Regex EthereumAddressRegex = new("^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled);

    // Cached uppercase versions for GetHashCode
    private readonly string normalizedNamespace;
    private readonly string normalizedReference;
    private readonly string normalizedAddress;

    /// <summary>
    /// Creates a new BlockchainAddress from its components
    /// </summary>
    /// <param name="namespace">The namespace for the blockchain (e.g., "eip155" for EVM chains)</param>
    /// <param name="reference">The reference ID for the chain (e.g., "1" for Ethereum mainnet)</param>
    /// <param name="address">The account address on the chain</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    /// <exception cref="InvalidBlockchainAddressException">Thrown when any parameter is invalid</exception>
    public BlockchainAddress(string @namespace, string reference, string address)
    {
        if (@namespace == null) throw new ArgumentNullException(nameof(@namespace));
        if (reference == null) throw new ArgumentNullException(nameof(reference));
        if (address == null) throw new ArgumentNullException(nameof(address));

        if (string.IsNullOrWhiteSpace(@namespace))
            throw new InvalidBlockchainAddressException("Namespace cannot be empty");
        if (string.IsNullOrWhiteSpace(reference))
            throw new InvalidBlockchainAddressException("Reference cannot be empty");
        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidBlockchainAddressException("Address cannot be empty");

        // Validate format based on namespace
        if (@namespace == BlockchainNamespaces.Evm && !EthereumAddressRegex.IsMatch(address))
            throw new InvalidBlockchainAddressException("Invalid Ethereum address format");

        this.Namespace = @namespace;
        this.Reference = reference;
        this.Address = address;

        // Cache normalized versions for GetHashCode
        this.normalizedNamespace = @namespace.ToUpperInvariant();
        this.normalizedReference = reference.ToUpperInvariant();
        this.normalizedAddress = address.ToUpperInvariant();
    }

    //

    /// <summary>
    /// The namespace for the blockchain (e.g., "eip155" for EVM chains)
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// The reference ID for the chain (e.g., "1" for Ethereum mainnet)
    /// </summary>
    public string Reference { get; }

    /// <summary>
    /// The account address on the chain
    /// </summary>
    public string Address { get; }

    public bool IsEthereum => Namespace == BlockchainNamespaces.Evm;

    //

    /// <summary>
    /// Creates a new BlockchainAddress from a CAIP-10 string
    /// </summary>
    /// <param name="caip10Address">The CAIP-10 formatted address string</param>
    /// <returns>A new BlockchainAddress instance</returns>
    /// <exception cref="InvalidBlockchainAddressException">Thrown when the input string is invalid</exception>
    public static BlockchainAddress Parse(string caip10Address)
    {
        if (string.IsNullOrWhiteSpace(caip10Address))
            throw new InvalidBlockchainAddressException("Address cannot be empty");

        var parts = caip10Address.Split(Delimiter);
        if (parts.Length != 3)
            throw new InvalidBlockchainAddressException(
                $"Invalid CAIP-10 address format. Expected format: namespace{Delimiter}reference{Delimiter}address");

        return new BlockchainAddress(parts[0], parts[1], parts[2]);
    }

    /// <summary>
    /// Tries to parse a CAIP-10 string into a BlockchainAddress
    /// </summary>
    /// <param name="caip10Address">The CAIP-10 formatted address string to parse</param>
    /// <param name="result">When this method returns, contains the parsed BlockchainAddress if successful</param>
    /// <returns>true if parsing was successful; otherwise, false</returns>
    public static bool TryParse(string? caip10Address, [NotNullWhen(true)] out BlockchainAddress? result)
    {
        try
        {
            result = Parse(caip10Address!);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Creates a new BlockchainAddress for an Ethereum address
    /// </summary>
    /// <param name="address">The Ethereum address (must start with 0x and be 40 hex characters)</param>
    /// <param name="chainId">The EVM chain ID (must be provided explicitly)</param>
    /// <returns>A new BlockchainAddress instance</returns>
    /// <exception cref="InvalidBlockchainAddressException">Thrown when the address format is invalid</exception>
    public static BlockchainAddress FromEthereum(string address, string chainId)
    {
        return new BlockchainAddress(BlockchainNamespaces.Evm, chainId, address);
    }

    /// <summary>
    /// Returns the CAIP-10 string representation
    /// </summary>
    public override string ToString() =>
        $"{Namespace}{Delimiter}{Reference}{Delimiter}{Address}";

    /// <summary>
    /// Determines whether this address equals another BlockchainAddress
    /// </summary>
    /// <remarks>
    /// Comparison is case-sensitive for the address component and case-insensitive for namespace and reference
    /// </remarks>
    public bool Equals(BlockchainAddress other)
    {
        return string.Equals(Namespace, other.Namespace, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Reference, other.Reference, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Address, other.Address, StringComparison.Ordinal); // Case-sensitive for addresses
    }

    /// <summary>
    /// Determines whether this address equals another object
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is BlockchainAddress other && Equals(other);

    /// <summary>
    /// Returns a hash code for this address
    /// </summary>
    public override int GetHashCode() =>
        HashCode.Combine(normalizedNamespace, normalizedReference, normalizedAddress);

    public static bool operator ==(BlockchainAddress left, BlockchainAddress right) =>
        left.Equals(right);

    public static bool operator !=(BlockchainAddress left, BlockchainAddress right) =>
        !left.Equals(right);
}

/// <summary>
/// Registry of known blockchain namespaces
/// </summary>
public static class BlockchainNamespaces
{
    public const string Evm = "eip155";      // Ethereum and EVM-compatible chains
    public const string Solana = "solana";   // Solana
    public const string Bitcoin = "bip122";  // Bitcoin

    // Add new namespaces here
}

/// <summary>
/// Registry of known chain references
/// </summary>
public static class ChainReferences
{
    public const string EthereumMainnet = "1";
    public const string Polygon = "137";
    public const string Arbitrum = "42161";
    public const string Base = "8453";
    public const string SolanaMainnet = "5eykt4UsFv8P8NJdTREpY1vzqKqZKvdp";
    public const string BitcoinMainnet = "000000000019d6689c085ae165831e93";

    // Add new references here
}