# Dual-Purpose Merkle Trees: A Unified Approach to Data Exchange and Selective Disclosure Proofs

Hello Ethereum Magicians community,

I'd like to share an approach I'm developing for Merkle trees that serves a dual purpose: both complete data exchange and selective disclosure proofs in a single structure. This design is particularly relevant for Ethereum Attestation Service (EAS) implementations and other onchain attestation scenarios.

## The Dual-Purpose Pattern

Rather than creating separate structures for data storage and proof verification, my approach uses a single Merkle tree that can:

1. **Exchange Data**: Share complete or partial data between systems (API-to-API exchange)
2. **Create Selective Disclosure Proofs**: Allow end users to reveal only specific parts when sharing with third parties
3. **Verify Against Onchain Attestations**: Match the root hash against attestations stored on the blockchain

The key insight is that all these use cases can be served by the same data structure with appropriate serialization policies.

## Application to EAS and Onchain Attestations

This pattern is especially valuable in the context of Ethereum Attestation Service and other attestation frameworks, where:

1. **Issuers** create attestations with a Merkle root of the complete data
2. **Users** receive the complete Merkle tree with all data
3. **Verifiers** accept selective disclosure proofs derived from the original tree
4. **Everyone** can verify against the same onchain attestation

## Key Design Choice: Data-Agnostic Leaves

A distinctive design decision in this implementation is that each leaf can contain arbitrary data without a predefined schema or structure. Unlike systems that require specific field names at the leaf level, this approach:

1. **Treats Data as Opaque Values**: The Merkle tree doesn't dictate what's in each leaf - it could be a JSON object, a single value, or any other data format
2. **Uses Content Type for Context**: The format is defined by the content type, not by the tree structure
3. **Separates Content from Structure**: The tree structure focuses on cryptographic relationships, not data semantics

This design creates exceptional flexibility, where:

```
// A leaf's data could be a simple JSON key-value pair
Leaf 1: { "fullName": "John Doe" }

// Or a more complex nested object
Leaf 2: { "address": { "street": "123 Main St", "city": "Anytown" } }

// Or even non-JSON data
Leaf 3: <binary biometric data>
```

The benefit of this approach is that developers can organize data in whatever way makes sense for their application. While I typically use one JSON key-value pair per leaf for maximum selective disclosure flexibility, the implementation doesn't enforce this pattern - it's a choice left to the application.

## Implementation Details

My implementation includes several key features:

```csharp
// Create a Merkle tree with all user data
var tree = new MerkleTree();
var userData = new Dictionary<string, object?>
{
    { "fullName", "John Doe" },
    { "dob", "1990-01-15" },
    { "passport", "AB123456" },
    { "address", "123 Main St, Anytown" },
    { "ssn", "123-45-6789" }
};

// Add all fields with automatic per-leaf random salts
tree.AddJsonLeaves(userData);
tree.RecomputeSha256Root();

// The root hash can be stored onchain as an attestation
Hex rootHash = tree.Root;

// OPTION 1: Complete data exchange (API-to-API)
string completeJson = tree.ToJson(); // Contains all fields

// OPTION 2: Selective disclosure (for any scenario)
Predicate<MerkleLeaf> hideSSN = leaf =>
    leaf.TryReadText(out string text) && text.Contains("ssn");
string proofWithoutSSN = tree.ToJson(hideSSN);
```

When the tree is serialized for selective disclosure, it includes:
- All metadata needed for verification
- Full data for disclosed fields
- Only hashes for private fields

### The Role of Content Type

A distinctive feature of this implementation is the `contentType` field for each leaf, which:

1. **Ensures Data Integrity**: Specifies how the data should be interpreted and processed
2. **Supports Multiple Formats**: Handles JSON, plain text, binary data, or hex-encoded content
3. **Standardizes Encoding**: Uses MIME types with charset and encoding information (e.g., "application/json; charset=utf-8; encoding=hex")
4. **Enables Smart Processing**: Allows verifiers to properly decode and use the data without guesswork

For example, the content type `application/json; charset=utf-8; encoding=hex` tells us:
- The data is a JSON object
- It's UTF-8 encoded text
- It's represented in hexadecimal format in the tree

This approach creates a self-describing data structure that can be safely exchanged between different systems while preserving the original data format and semantics.

### Example JSON Output

Here's what a selective disclosure structure looks like in JSON (with document number redacted):

```json
{
  "leaves": [
    {
      "data": "0x7b22646f63756d656e7454797065223a2270617373706f7274227d",
      "salt": "0x3d29e942cc77a7e77dad43bfbcbd5be3",
      "hash": "0xe77007d7627eb3eb334a556343a8ef0b5c9582061195441b2d9e18b32501897f",
      "contentType": "application/json; charset=utf-8; encoding=hex"
    },
    {
      "hash": "0xf4d2c8036badd107e396d4f05c7c6fc174957e4d2107cc3f4aa805f92deeeb63"
    },
    {
      "data": "0x7b22697373756544617465223a22323032302d30312d3031227d",
      "salt": "0x24c29488605b00e641326f6100284241",
      "hash": "0x1b3bccc577633c54c0aead00bae2d7ddb8a25fd93e4ac2e2e0b36b9d154f30b9",
      "contentType": "application/json; charset=utf-8; encoding=hex"
    },
    {
      "data": "0x7b2265787069727944617465223a22323033302d30312d3031227d",
      "salt": "0x5d3cd91a0211ed1deb5988a58066cacd",
      "hash": "0xce04b9b0455d7b1ac202f0981429000c9f9c06665b64d6d02ee1299a0502b121",
      "contentType": "application/json; charset=utf-8; encoding=hex"
    },
    {
      "data": "0x7b2269737375696e67436f756e747279223a22556e69746564204b696e67646f6d227d",
      "salt": "0xc59f9924118917267ebc7e6bb69ec354",
      "hash": "0xf06f970de5b098300a7731b9c419fc007fdfcd85d476bc28bb5356d15aff2bbc",
      "contentType": "application/json; charset=utf-8; encoding=hex"
    }
  ],
  "root": "0x1316fc0f3d76988cb4f660bdf97fff70df7bf90a5ff342ffc3baa09ed3c280e5",
  "metadata": {
    "hashAlgorithm": "sha256",
    "version": "1.0"
  }
}
```

In this example, you can see:
1. The document number leaf (second position) has only its hash preserved
2. All other leaves include their full data, salt, and content type
3. The root hash allows verification against the original attestation
4. Each leaf has its own unique random salt
5. The data in each leaf is a hex-encoded JSON object (in this case, each with a single key-value pair)
6. The structure allows for any data format, as specified by the content type

## Real-World Flows

### Identity Verification Flow

1. **Issuance**: An identity provider creates a Merkle tree with all user data, stores the root hash onchain as an attestation, and gives the user the complete tree
2. **Selective Disclosure**: The user creates a proof revealing identity information without sensitive data like SSN
3. **Verification**: A third-party (like a KYC service) verifies the proof against the onchain attestation

### API-to-API Exchange Flow

1. **Selective Data Exchange**: Two services exchange a data structure with only the necessary fields (e.g., a KYC service might omit certain sensitive fields even in API-to-API communication)
2. **Verification**: The receiving service verifies the root hash against the onchain attestation
3. **Further Filtering**: The receiving service can apply additional filtering when sharing with other parties

## Security Considerations

I've implemented several security features:
- Each leaf has its own cryptographically secure random salt (16 bytes/128 bits)
- Correlation attacks are prevented as identical data produces different hashes 
- JSON serialization supports both complete and selective disclosure modes

## Looking for Feedback

I'd appreciate the community's thoughts on:

1. How this pattern could integrate with existing EAS implementations and standards
2. Whether having a unified structure for both complete data and selective disclosure makes sense
3. Additional use cases where this approach could be valuable
4. Security considerations we should address

This approach aims to simplify the developer experience while maintaining security, providing a seamless path from data sharing to selective disclosure without requiring separate data structures.

My implementation is available in [our open-source repository](https://github.com/lukepuplett/evoq-blockchain), with detailed documentation on both the complete data exchange and selective disclosure capabilities.

Looking forward to your insights!
