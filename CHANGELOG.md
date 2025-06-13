# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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