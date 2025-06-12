# Merkle Tree Implementation

This directory contains documentation for our Merkle tree implementation, focusing on features that enhance its utility for privacy-preserving verification.

## Contents

### [Selective Disclosure](./selective-disclosure.md)

Documentation on our approach to selective disclosure in Merkle trees, allowing for privacy-preserving verification where only specific leaves are revealed while maintaining the cryptographic integrity of the tree. Includes a complete example with digital passport data.

## Key Features

- **Merkle-Inspired Hash Set**: Optimized for small records (less than 20 items) where most data will be revealed
- **Automatic Random Salts**: Each leaf gets its own cryptographically secure random salt by default
- **Selective Disclosure**: Ability to reveal only specific leaves while maintaining verifiability
- **Flexible Serialization**: JSON representation with options for privacy and null handling
- **Clean API Design**: Intuitive interfaces for creating, modifying, and verifying trees
- **Version 3.0 Support**: Enhanced security with protected header leaf and JOSE-inspired format

## Security Best Practices

For optimal security, we've designed our Merkle tree implementation with these best practices:

- **Unique Random Salts**: Each leaf in the tree automatically gets its own secure random salt
- **Salt Length**: 16 bytes (128 bits) of randomness by default
- **Correlation Protection**: Different salt for each leaf prevents correlation attacks on identical data
- **Protected Header**: First leaf contains cryptographically protected metadata (algorithm, leaf count, document type)
- **Minimum Leaf Count**: All valid trees must have at least two leaves to prevent single-leaf attacks
- **Algorithm Protection**: Hash algorithm is cryptographically bound to the tree structure
- **Document Type Safety**: Exchange field prevents mixing different types of records

## Implementation Details

The Merkle tree implementation is available in the `Evoq.Blockchain.Merkle` namespace:

- `MerkleTree`: The main class for creating and managing Merkle trees
- `MerkleLeaf`: Represents a leaf node in the tree, containing data, salt, hash, and content type
- `MerkleMetadata`: Contains metadata about the tree, such as version and hash algorithm

## Version 3.0 Improvements

Version 3.0 introduces significant security enhancements:

1. **Security Improvements**:
   - Protected header leaf with cryptographically secured metadata
   - Prevention of single leaf, leaf addition/removal, and algorithm substitution attacks
   - Document type safety through the exchange field
   - Strict validation of header leaf and tree structure

2. **Interoperability Features**:
   - JOSE-inspired header format (alg, typ fields)
   - Standard MIME types for structured data exchange
   - Support for selective disclosure through private leaves
   - Efficient proof generation with O(log n) hashes

3. **Use Cases**:
   - Selective Disclosure: Reveal specific leaves while keeping others private
   - Document Exchange: Exchange structured data with type safety and integrity
   - Proof Generation: Generate compact proofs for verification
   - Private Storage: Store full structure for quick proof reissuance

## Usage

```csharp
// Create a new v3.0 tree
var tree = new MerkleTree(MerkleTreeVersionStrings.V3_0);

// Add JSON key-value pairs with automatic random salts (preferred)
var data = new Dictionary<string, object?> { { "key1", "value1" }, { "key2", 123 } };
tree.AddJsonLeaves(data);

// Compute the root hash
tree.RecomputeSha256Root();

// Verify the tree
bool isValid = tree.VerifySha256Root();
```

## Blockchain Attestation

The Merkle tree implementation is designed to work seamlessly with blockchain attestations:

1. **Root Hash Attestation**: The root hash of a Merkle tree can be attested on a blockchain, providing a tamper-proof record of the tree's state at a specific point in time.

2. **Verification Flow**:
   - A Merkle tree is created and its root hash is attested on the blockchain
   - The complete tree can be shared with users or stored privately
   - When verification is needed, the tree is parsed from JSON
   - The parsed tree's root is verified against the attested hash on the blockchain

3. **Selective Disclosure with Attestation**:
   - Users can create selectively disclosed versions of the tree
   - The disclosed tree maintains the same root hash
   - Verifiers can confirm the disclosed tree matches the attested root

This pattern is particularly valuable for:
- Digital identity documents
- Credential verification
- Document attestation
- Supply chain tracking
- Any scenario requiring both privacy and verifiability

For a complete usage example, see the [Complete Example section in the Selective Disclosure documentation](./selective-disclosure.md#complete-example-digital-passport).

For more detailed implementation information, examine the source code in:
`src/Evoq.Blockchain/Blockchain.Merkle/MerkleTree.cs` 