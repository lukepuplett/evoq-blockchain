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

When preparing a new release, follow these steps to ensure quality and consistency:

1. **Version Management**
   - Update version in `src/Evoq.Blockchain/Evoq.Blockchain.csproj`
   - Follow semantic versioning (MAJOR.MINOR.PATCH)
   - Create git tag matching version (e.g., `v1.4.0`)

2. **Code Quality**
   - Run `./build.sh` to verify all tests pass
   - Check for and address any critical warnings
   - Ensure all public APIs are documented
   - Remove any TODO comments

3. **Documentation**
   - Update README.md with new features/examples
   - Verify API documentation is complete
   - Document any breaking changes
   - Add examples for new functionality

4. **Git Hygiene**
   - Commit all changes with conventional commit messages
   - Create and push version tag
   - Verify no sensitive data in commits
   - Ensure clean working directory

5. **Build & Test**
   - Run full build in Release mode
   - Verify all tests pass
   - Check NuGet package builds successfully
   - Address any build warnings

6. **Release Artifacts**
   - Verify NuGet package in `./artifacts`
   - Check package version matches project
   - Validate package contents
   - Review package metadata

7. **Pre-Release Checklist**
   - Review changes since last release
   - Verify backward compatibility
   - Check security implications
   - Review performance impact

8. **Post-Release Tasks**
   - Create GitHub release
   - Write release notes
   - Publish to NuGet
   - Update documentation if needed

Example release workflow:
```bash
# 1. Update version in .csproj
# 2. Build and test
./build.sh

# 3. Create and push tag
git tag -a v1.4.0 -m "Version 1.4.0 - Add private leaves support"
git push origin v1.4.0

# 4. Create GitHub release and publish
export NUGET_API_KEY="your-nuget-api-key"
./publish.sh
```

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