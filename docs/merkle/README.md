# Merkle Tree Implementation

This directory contains documentation for our Merkle tree implementation, focusing on features that enhance its utility for privacy-preserving verification.

## Contents

### [Selective Disclosure](./selective-disclosure.md)

Documentation on our approach to selective disclosure in Merkle trees, allowing for privacy-preserving verification where only specific leaves are revealed while maintaining the cryptographic integrity of the tree. Includes a complete example with digital passport data.

## Key Features

- **Standard Merkle Tree Implementation**: Binary tree of hashes with efficient verification
- **Selective Disclosure**: Ability to reveal only specific leaves while maintaining verifiability
- **Flexible Serialization**: JSON representation with options for privacy and null handling
- **Clean API Design**: Intuitive interfaces for creating, modifying, and verifying trees

## Implementation Details

The Merkle tree implementation is available in the `Evoq.Blockchain.Merkle` namespace:

- `MerkleTree`: The main class for creating and managing Merkle trees
- `MerkleLeaf`: Represents a leaf node in the tree, containing data, salt, hash, and content type
- `MerkleMetadata`: Contains metadata about the tree, such as version and hash algorithm

## Usage

For a complete usage example, see the [Complete Example section in the Selective Disclosure documentation](./selective-disclosure.md#complete-example-digital-passport).

For more detailed implementation information, examine the source code in:
`src/Evoq.Blockchain/Blockchain.Merkle/MerkleTree.cs` 