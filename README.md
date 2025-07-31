# Evoq.Blockchain

A lightweight .NET library providing utilities for blockchain integration. This package contains common types and helpers that are useful when working with any blockchain from .NET applications.

## Installation

```
dotnet add package Evoq.Blockchain
```

## Features

- Type-safe blockchain primitives
- Common blockchain data structures
- Utility methods for blockchain operations
- Framework-agnostic design (works with any blockchain implementation)
- [Merkle trees with selective disclosure and automatic random salts](./docs/merkle/selective-disclosure.md)

## Future Vision

The library is designed to support advanced verification capabilities in the future, in collaboration with wrapper libraries:

- **Complete Chain of Trust Verification**
  - Decode and validate JWS structures
  - Parse and verify Merkle tree JSON payloads
  - Validate individual leaf data and hashes
  - Verify root hashes against blockchain attestations
  - Support for Merkle Commitment Protocol (MCP) verification

- **Intelligent Verification**
  - Automatic detection of private leaves
  - Validation of header leaf metadata
  - Cryptographic proof verification
  - Blockchain attestation checking
  - Selective disclosure analysis

This vision enables a complete end-to-end verification system that can validate the entire chain of trust from JWS signatures down to individual leaf data and back up to blockchain attestations. Work is in progress with wrapper libraries like Zipwire.ProofPack which adds attestation locators and JWS outer wrappers to create complete proof packages.

## Documentation

Comprehensive documentation is available in the [docs directory](./docs/):

- [Merkle Tree Implementation](./docs/merkle/README.md)
  - [Selective Disclosure in Merkle Trees](./docs/merkle/selective-disclosure.md) (includes complete examples)

## Quick Start

```csharp
// Create a Merkle tree
var tree = new MerkleTree();

// Add leaves with automatic random salts
var data = new Dictionary<string, object?>
{
    { "name", "John" },
    { "age", 30 },
    { "ssn", "123-45-6789" }
};
tree.AddJsonLeaves(data);

// Compute the root hash (required before serialization)
tree.RecomputeSha256Root();

// Create a selective disclosure version (hiding sensitive data)
Predicate<MerkleLeaf> privateSsn = leaf => 
    leaf.TryReadText(out string text) && text.Contains("ssn");

// Convert to JSON with selective disclosure
string json = tree.ToJson(privateSsn);

// Parse and verify the tree
var parsedTree = MerkleTree.Parse(json);
bool isValid = parsedTree.VerifyRoot(); // Automatically uses hash function from metadata
```

### Merkle Tree Features

#### Automatic Hash Function Selection
The tree automatically selects the appropriate hash function based on the metadata:
```csharp
// Verify using the hash function specified in metadata
bool isValid = tree.VerifyRoot();

// Or explicitly specify a hash function
bool isValid = tree.VerifyRoot(myCustomHashFunction);
```

#### Root Hash Computation
The root hash must be computed before serialization:
```csharp
// This will throw InvalidRootException if root hasn't been computed
tree.ToJson();

// Always compute the root first
tree.RecomputeSha256Root();
tree.ToJson(); // Now works
```

#### Error Handling
The library provides clear error messages for common issues:
```csharp
try {
    tree.VerifyRoot();
} catch (NotSupportedException ex) {
    // Error message explains how to use custom hash functions
    Console.WriteLine(ex.Message);
}
```

#### Custom Hash Functions
You can implement custom hash functions and select them based on the tree's metadata:
```csharp
// Define a custom hash function
Hex ComputeReverseSha256Hash(byte[] data)
{
    // Reverse the input bytes
    byte[] reversed = new byte[data.Length];
    Array.Copy(data, reversed, data.Length);
    Array.Reverse(reversed);

    // Hash the reversed bytes
    using var sha256 = SHA256.Create();
    return new Hex(sha256.ComputeHash(reversed));
}

// Create a hash function selector
HashFunction SelectHashFunction(MerkleTree tree)
{
    return tree.Metadata.HashAlgorithm switch
    {
        "sha256" => MerkleTree.ComputeSha256Hash,
        "sha256-reverse" => ComputeReverseSha256Hash,
        _ => throw new NotSupportedException(
            $"Hash algorithm '{tree.Metadata.HashAlgorithm}' is not supported. " +
            "Please implement a custom hash function for this algorithm.")
    };
}

// Use the selector to get the right hash function
var hashFunction = SelectHashFunction(tree);
bool isValid = tree.VerifyRoot(hashFunction);
```

#### Version Support
The parser automatically detects the version format of the JSON. The library currently defaults to v1.0 format, but also supports the newer v2.0 format which uses JWT-style headers:

```csharp
// v1.0 format (current default, uses "metadata" property)
var v1Json = @"{
    ""metadata"": { ""hashAlgorithm"": ""sha256"", ""version"": ""1.0"" },
    ""leaves"": [...],
    ""root"": ""...""
}";

// v2.0 format (JWT-style, uses "header" property with standardized values)
var v2Json = @"{
    ""header"": { 
        ""alg"": ""SHA256"",           // Standardized algorithm name
        ""typ"": ""MerkleTree+2.0""    // JWT-style type identifier
    },
    ""leaves"": [...],
    ""root"": ""...""
}";

// Both formats are automatically detected
var tree = MerkleTree.Parse(v1Json); // Works with v1.0
var tree2 = MerkleTree.Parse(v2Json); // Works with v2.0
```

> **Note:** The library uses `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` to ensure special characters in version strings (like '+' in "MerkleTree+2.0") are not escaped in the JSON output. This is particularly important for JWT-style type identifiers in v2.0 format.

## Target Frameworks

This package targets .NET Standard 2.0 for maximum compatibility across:
- .NET 6.0+
- .NET Framework 4.6.1+
- .NET Core 2.0+
- Xamarin
- Unity

## Building

```
dotnet build
dotnet test
```

The repository includes shell scripts to simplify the build and publishing process:

### build.sh

This script automates the build process:
- Cleans previous artifacts
- Builds the project in Release configuration
- Runs all tests
- Creates a NuGet package in the ./artifacts directory

```bash
# Make the script executable
chmod +x build.sh

# Run the build script
./build.sh
```

### publish.sh

This script publishes the NuGet package to NuGet.org:
- Requires the NUGET_API_KEY environment variable to be set
- Finds the .nupkg file in the artifacts directory
- Pushes the package to NuGet.org

```bash
# Make the script executable
chmod +x publish.sh

# Set your NuGet API key
export NUGET_API_KEY="your-nuget-api-key"

# Run the publish script
./publish.sh
```

## Shipping a Release

**IMPORTANT**: Follow the comprehensive [Shipping Guide](./docs/SHIPPING.md) for detailed release procedures.

The release process includes:
- Version management and validation
- GitHub releases with proper documentation
- NuGet publishing
- Verification and testing

Quick reference:
```bash
# Check current versions
grep '<Version>' src/Evoq.Blockchain/Evoq.Blockchain.csproj
git tag --list --sort=-version:refname | head -1
curl -s "https://api.nuget.org/v3/registration5-semver1/evoq.blockchain/index.json" | grep -o '"version":"[^"]*"' | tail -1

# Build and test
./build.sh

# Create tag and release
git tag -a vX.Y.Z -m "Version X.Y.Z - Description"
git push origin vX.Y.Z

# Publish to NuGet (manual upload)
# Upload artifacts/Evoq.Blockchain.X.Y.Z.nupkg to https://www.nuget.org/packages/manage/upload
```

**See [docs/SHIPPING.md](./docs/SHIPPING.md) for the complete process.**

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

Luke Puplett

## Project Links

- [GitHub Repository](https://github.com/lukepuplett/evoq-blockchain)
- [NuGet Package](https://www.nuget.org/packages/Evoq.Blockchain)