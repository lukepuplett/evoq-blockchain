# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.10.0] - 2026-01-07

### Added
- **Object Serialization to Merkle Trees**: New `AddObjectLeaves<T>()` method to serialize any object directly to Merkle tree leaves
- **Object Reconstruction from Merkle Trees**: New `GetObjectFromLeaves<T>()` method to reconstruct objects from Merkle tree leaves (inverse operation)
- **MissingLeafDataException**: New exception type for handling missing required properties during object reconstruction
- **Comprehensive Test Coverage**: Over 1,000 lines of new tests covering object serialization/deserialization scenarios
  - Simple objects, nested objects, arrays, and mixed types
  - V2.0 and V3.0 format support
  - V3.0 header leaf handling and metadata preservation
  - Private leaves and selective disclosure scenarios
  - Root validation and empty string vs null distinction

### Technical Details
- `AddObjectLeaves<T>()` automatically serializes objects to JSON and creates individual leaves for each property
- `GetObjectFromLeaves<T>()` reconstructs objects by combining leaf data, automatically skipping header leaves and private leaves
- Proper null validation: empty strings are treated as legitimate values, only nullable types are validated for missing properties
- Full support for V3.0 format with automatic header leaf handling
- Works seamlessly with selective disclosure - private leaves are automatically skipped during reconstruction

### Usage Examples
```csharp
// Serialize an object to Merkle tree leaves
var invoice = new { 
    invoiceNumber = "INV-001", 
    amount = 1000.00,
    customer = new { name = "John", email = "john@example.com" }
};
merkleTree.AddObjectLeaves(invoice);
merkleTree.RecomputeSha256Root();

// Reconstruct the object from leaves
var reconstructed = merkleTree.GetObjectFromLeaves<Dictionary<string, object>>();

// Works with selective disclosure - private leaves are automatically skipped
var selectiveTree = MerkleTree.From(merkleTree, leaf => 
    leaf.TryReadJsonKeys(out var keys) && keys.Contains("customer"));
var partial = selectiveTree.GetObjectFromLeaves<Dictionary<string, object>>();
```

## [1.9.0] - 2024-12-19

### Added
- **Native JSON Serialization Support**: Added automatic JSON serialization/deserialization for `Hex` type
- **HexJsonConverter**: System.Text.Json converter for seamless `Hex` property handling in DTOs
- **NullableHexJsonConverter**: Support for nullable `Hex?` properties in JSON serialization
- **JsonSerializerOptionsExtensions**: Easy configuration with `ConfigureForHex()` extension method
- **Comprehensive Test Coverage**: 36 new tests covering all serialization scenarios and edge cases

### Changed
- **Storage DTOs Enhancement**: Consumers can now use `Hex` properties directly in storage DTOs without manual conversion
- **Developer Experience**: Simplified JSON API development with automatic hex string handling

### Technical Details
- Hex values serialize to/from hex strings (e.g., "0x1234abcd") in JSON automatically
- Supports both direct `Hex` properties and nullable `Hex?` properties
- Provides clear error messages for invalid hex strings during deserialization
- Maintains backward compatibility - no breaking changes to existing APIs
- Easy setup: single `ConfigureForHex()` call enables all functionality

### Usage Examples
```csharp
// Configure JSON options (one-time setup)
var options = new JsonSerializerOptions().ConfigureForHex();

// Use Hex properties naturally in DTOs
public class TransactionDto 
{
    public Hex Hash { get; set; }
    public Hex From { get; set; }
    public Hex? BlockHash { get; set; } // Nullable support
}

// Serialization/deserialization works automatically
var dto = new TransactionDto { Hash = "0x1234abcd" }; // String assignment
string json = JsonSerializer.Serialize(dto, options);   // Automatic
var result = JsonSerializer.Deserialize<TransactionDto>(json, options); // Automatic
```

## [1.8.0] - 2024-12-19

### Added
- **Selective Disclosure with V3.0 Metadata Preservation**: Enhanced `From` methods now properly preserve V3.0 header leaf during selective disclosure
- **Flexible Metadata Detection**: New `IsMetadata` property on `MerkleLeaf` for flexible header detection
- **Content Type Constants**: Added `V3_0_HEADER_CONTENT_TYPE` and `V3_0_HEADER_MIME_TYPE` constants to eliminate magic strings
- **Root Hash Consistency**: Ensures root hash remains identical between source and selective disclosure trees

### Fixed
- **V3.0 Header Leaf Preservation**: Fixed issue where V3.0 metadata leaf salt was being regenerated during selective disclosure
- **Cryptographic Integrity**: Selective disclosure trees now maintain exact same root hash as source tree
- **Metadata Leaf Detection**: Improved header leaf detection to work with any version containing 'merkle-exchange-header'

### Changed
- **Enhanced Test Coverage**: Added comprehensive tests for V3.0 selective disclosure scenarios
- **Improved Error Handling**: Better validation for empty trees without root
- **Code Organization**: Moved content type constants to appropriate utility class

### Technical Details
- V3.0 header leaf is now preserved exactly as-is during selective disclosure
- Root hash computation uses preserved header leaf instead of recreating it
- Flexible metadata detection supports future version formats
- Comprehensive test coverage ensures reliability across all scenarios

## [1.7.0] - 2024-12-19

### Added
- **Selective Disclosure Factory Methods**: New `From` static methods for creating selective disclosure trees
  - `From(sourceTree, makePrivate)` - Create tree with custom predicate for privacy decisions
  - `From(sourceTree, preserveKeys)` - Convenience method using key names to preserve
- **JSON Key Extraction**: New `TryReadJsonKeys` method on `MerkleLeaf` for extracting keys from JSON data
- **Custom Exception**: `NonJsonLeafException` for handling non-JSON leaves in selective disclosure
- **Comprehensive Test Coverage**: Full test suite for all selective disclosure functionality

### Technical Details
- Factory methods create new tree instances with mixed public/private leaves
- Maintains cryptographic integrity with same root hash as source tree
- Supports both predicate-based and key-based selective disclosure
- Handles empty trees and edge cases gracefully
- Throws descriptive exceptions for invalid operations

### Usage Examples
```csharp
// Create selective disclosure tree with predicate
var selectiveTree = MerkleTree.From(sourceTree, leaf => 
    leaf.TryReadJsonKeys(out var keys) && keys.Contains("ssn"));

// Create selective disclosure tree with key names
var preserveKeys = new HashSet<string> { "name", "email" };
var selectiveTree = MerkleTree.From(sourceTree, preserveKeys);
```

## [1.5.0] - 2024-03-19

### Added
- Merkle tree v3.0 format with enhanced security features
- Protected header leaf for cryptographic protection
- Blockchain attestation support
- Strict validation of header leaf during parsing
- Leaf count verification
- Exchange document type protection

### Security
- Protected header leaf prevents:
  - Leaf addition/removal attacks
  - Single leaf presentation attacks
  - Algorithm substitution attacks
- Strict validation of header leaf during parsing
- Leaf count verification
- Exchange document type protection

### Documentation
- Updated Merkle Tree Implementation documentation
- Added Selective Disclosure Guide
- Enhanced security documentation
- Added blockchain attestation examples

### Technical Details
- Uses standard MIME types for structured data exchange
- Implements JWT-style headers for version 3.0
- Supports selective disclosure through private leaves
- Enables efficient proof generation with O(log n) hashes
- Maintains backward compatibility with v1.0 and v2.0 formats

## [1.4.0] - 2024-03-12

### Added
- Support for private leaves in Merkle trees
- Enhanced selective disclosure capabilities
- Improved JSON serialization options

### Changed
- Updated documentation with private leaves examples
- Enhanced error messages for better debugging

## [1.3.0] - 2024-03-05

### Added
- Merkle tree v2.0 format with JWT-style headers
- Enhanced JSON serialization
- Improved error handling

### Changed
- Updated documentation with v2.0 examples
- Enhanced test coverage

## [1.2.0] - 2024-02-27

### Added
- Initial Merkle tree implementation
- Basic selective disclosure support
- JSON serialization

### Changed
- Updated documentation
- Added examples

## [1.1.0] - 2024-02-20

### Added
- Basic blockchain address support
- Hex encoding utilities
- Initial documentation

## [1.0.0] - 2024-02-13

### Added
- Initial release
- Basic blockchain utilities
- Core types and helpers 