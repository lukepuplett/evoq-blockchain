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

// Compute the root hash
tree.RecomputeSha256Root();

// Create a selective disclosure version (hiding sensitive data)
Predicate<MerkleLeaf> privateSsn = leaf => 
    leaf.TryReadText(out string text) && text.Contains("ssn");

// Convert to JSON with selective disclosure
string json = tree.ToJson(privateSsn);
```

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