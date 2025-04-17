# Selective Disclosure in Merkle Trees

## A Privacy-Preserving Approach to Verification

Merkle trees are a powerful cryptographic data structure that allows efficient and secure verification of content within a larger dataset. However, traditional Merkle proof implementations focus on proving the inclusion of specific leaves without addressing a critical need in many real-world applications: **selective disclosure**.

This document explores our implementation of selective disclosure in Merkle trees - an approach that allows revealing only specific leaves while maintaining the cryptographic verifiability of the entire tree.

## The Challenge

Consider a digital passport or identification document stored as a Merkle tree where each leaf represents a different piece of personal information:

- Document number
- Name
- Date of birth
- Address
- Biometric data
- Nationality

In many verification scenarios, you need to prove only specific attributes (like age or nationality) without revealing other sensitive information (like your address or full document number).

Traditional approaches require either:
1. Revealing the entire document (compromising privacy)
2. Creating separate trees for each attribute (sacrificing the unified cryptographic binding)
3. Implementing complex zero-knowledge proofs (adding significant complexity)

Our implementation provides a clean, elegant solution to this problem through a technique we call "private leaves" - leaves whose content is withheld while their cryptographic presence is maintained.

## Our Approach

We've implemented a simple yet powerful solution that enables any subset of leaves to be marked as private, while maintaining the tree's verifiability. This approach has several key advantages:

1. **Simple API**: Using a predicate function to determine which leaves should be private
2. **Clean Serialization**: Private leaves contain only their hash, omitting data, salt, and content type
3. **Seamless Verification**: Standard verification algorithms still work with private leaves
4. **Flexible Privacy Control**: Any combination of leaves can be made private or public

## Implementation Details

### The Predicate-Based Approach

The core of our solution is a simple predicate function that determines which leaves should be private:

```csharp
// Create a predicate that makes the document number private
Predicate<MerkleLeaf> makePrivate = leaf =>
    leaf.TryReadText(out string text) && text.Contains("documentNumber");

// Serialize to JSON with selective privacy
string json = merkleTree.ToJson(
    MerkleTree.ComputeSha256Hash, 
    makePrivate,
    new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }
);
```

This expressive approach lets you use any boolean logic to determine which leaves should be private, whether based on content, metadata, or other criteria.

### JSON Representation

When serialized, private leaves contain only their hash value, with data, salt, and content type completely omitted:

```json
// Regular leaf with all fields
{
  "data": "0x7b22646f63756d656e7454797065223a2270617373706f7274227d",
  "salt": "0x7f8e7d6c5b4a3210",
  "hash": "0x05e7faf4a47104a39003db687c19c25b1d8178a00573340fa11e93235229e096",
  "contentType": "application/json; charset=utf-8; encoding=hex"
},
// Private leaf with only the hash
{
  "hash": "0xa384d43e17939399e28563ac9abb24239d1337bc2251bb4abc92322c31be0ca0"
}
```

### Verification Process

Verification remains straightforward. When a leaf is private:

1. The verification process detects the missing data and salt
2. It uses the provided hash directly instead of computing a hash from data and salt
3. The Merkle tree is constructed normally, and the root hash can be verified

This approach maintains the same verification code path while seamlessly handling private leaves.

## Use Cases

This selective disclosure approach is particularly valuable for:

1. **Identity Verification**: Prove age without revealing full identity
2. **Credential Validation**: Verify qualifications without exposing all credential details
3. **Document Attestation**: Cryptographically verify specific document attributes
4. **Blockchain Applications**: Reduce on-chain data while maintaining verifiability

## Security Considerations

While this approach provides practical privacy, it's important to understand its limitations:

- The hash of private leaves is still visible
- The structure of the tree (number of leaves) remains visible
- It doesn't provide cryptographic hiding properties like zero-knowledge proofs

For most practical applications, these limitations are acceptable given the simplicity and efficiency of the approach.

## Conclusion

Our selective disclosure implementation for Merkle trees offers a pragmatic balance between privacy and verifiability. By using a simple predicate-based approach and clean JSON serialization, we've created a solution that's easy to use, efficient to implement, and powerful in its applications.

For code examples and usage patterns, see the [examples directory](./examples/). 