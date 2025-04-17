# Merkle Tree Implementation

This directory contains documentation for our Merkle tree implementation, focusing on features that enhance its utility for privacy-preserving verification.

## Contents

### [Selective Disclosure](./selective-disclosure.md)

Documentation on our approach to selective disclosure in Merkle trees, allowing for privacy-preserving verification where only specific leaves are revealed while maintaining the cryptographic integrity of the tree. Includes a complete example with digital passport data.

## Key Features

- **Standard Merkle Tree Implementation**: Binary tree of hashes with efficient verification
- **Automatic Random Salts**: Each leaf gets its own cryptographically secure random salt by default
- **Selective Disclosure**: Ability to reveal only specific leaves while maintaining verifiability
- **Flexible Serialization**: JSON representation with options for privacy and null handling
- **Clean API Design**: Intuitive interfaces for creating, modifying, and verifying trees

## Security Best Practices

For optimal security, we've designed our Merkle tree implementation with these best practices:

- **Unique Random Salts**: Each leaf in the tree automatically gets its own secure random salt
- **Salt Length**: 16 bytes (128 bits) of randomness by default
- **Correlation Protection**: Different salt for each leaf prevents correlation attacks on identical data

## Implementation Details

The Merkle tree implementation is available in the `Evoq.Blockchain.Merkle` namespace:

- `MerkleTree`: The main class for creating and managing Merkle trees
- `MerkleLeaf`: Represents a leaf node in the tree, containing data, salt, hash, and content type
- `MerkleMetadata`: Contains metadata about the tree, such as version and hash algorithm

## Usage

```csharp
// Create a new tree
var tree = new MerkleTree();

// Add JSON key-value pairs with automatic random salts (preferred)
var data = new Dictionary<string, object?> { { "key1", "value1" }, { "key2", 123 } };
tree.AddJsonLeaves(data);

// Compute the root hash
tree.RecomputeSha256Root();

// Verify the tree
bool isValid = tree.VerifySha256Root();
```

For a complete usage example, see the [Complete Example section in the Selective Disclosure documentation](./selective-disclosure.md#complete-example-digital-passport).

For more detailed implementation information, examine the source code in:
`src/Evoq.Blockchain/Blockchain.Merkle/MerkleTree.cs` 