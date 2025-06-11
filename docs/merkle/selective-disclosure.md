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

## Important: Security Through Unique Salts

> **SECURITY BEST PRACTICE**: Each leaf in a Merkle tree should have its own unique random salt. 
> This is critical for preventing correlation attacks where identical data would otherwise produce identical hashes.

Our API now defaults to automatically generating secure random salts for each leaf. Always use the methods that automatically generate unique salts unless you have a specific reason not to.

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
string json = merkleTree.ToJson(makePrivate);
```

This expressive approach lets you use any boolean logic to determine which leaves should be private, whether based on content, metadata, or other criteria.

### JSON Representation

When serialized, private leaves contain only their hash value, with data, salt, and content type completely omitted:

```json
// Regular leaf with all fields
{
  "data": "0x7b22646f63756d656e7454797065223a2270617373706f7274227d",
  "salt": "0xa48c12f5e7b943de67c8901f",
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

### Salt Security

For optimal security, each leaf should use its own unique random salt:

- **Unique Salts**: Generate a cryptographically secure random salt for each leaf
- **Salt Length**: Use at least 16 bytes (128 bits) of randomness for each salt
- **Prevent Correlation**: Different salts ensure that identical data in different leaves produces different hashes

Using unique random salts for each leaf is critical for preventing correlation attacks where an attacker might identify patterns in the hashed data.

## Complete Example: Digital Passport

Let's walk through a complete example of implementing selective disclosure with a digital passport stored as a Merkle tree.

### Creating a Digital Passport as a Merkle Tree

First, we'll create a Merkle tree containing various passport data fields:

```csharp
// Passport data fields
var passportData = new Dictionary<string, object?>
{
    { "documentType", "passport" },
    { "documentNumber", "AB123456" },
    { "issueDate", "2020-01-01" },
    { "expiryDate", "2030-01-01" },
    { "issuingCountry", "United Kingdom" },
    { "givenName", "John" },
    { "surname", "Doe" },
    { "dateOfBirth", "1980-05-15" },
    { "placeOfBirth", "London" },
    { "gender", "M" },
    { "nationality", "British" },
    { "address", new Dictionary<string, object?>
        {
            { "streetAddress", "123 Main Street" },
            { "city", "London" },
            { "postalCode", "SW1A 1AA" },
            { "country", "United Kingdom" }
        }
    },
    { "biometricData", new Dictionary<string, object?>
        {
            { "facialFeatures", "0xABCDEF1234567890" },
            { "fingerprints", new[]
                {
                    "0x1122334455667788",
                    "0x99AABBCCDDEEFF00"
                }
            }
        }
    }
};

// Create the Merkle tree and add all fields as leaves with automatically generated random salts
var merkleTree = new MerkleTree();
merkleTree.AddJsonLeaves(passportData);  // Uses random salts automatically

// Compute the root hash
merkleTree.RecomputeSha256Root();

// Verify the root
bool isValid = merkleTree.VerifySha256Root(); // Should be true
```

### Alternative approaches

The API provides multiple ways to add leaves with secure random salts:

```csharp
// Option 1: Add all leaves at once from a dictionary (preferred)
merkleTree.AddJsonLeaves(passportData);

// Option 2: Add individual leaves one by one
foreach (var field in passportData)
{
    merkleTree.AddJsonLeaf(field.Key, field.Value);
}

// Option 3: Create custom leaves with explicit random salts if needed
foreach (var field in passportData)
{
    var salt = MerkleTree.GenerateRandomSalt();
    merkleTree.AddJsonLeaf(field.Key, field.Value, salt);  
}
```

### Creating a Passport with Selective Disclosure

Now, let's demonstrate how to create a version of the passport with selective disclosure, where the document number and address are kept private:

```csharp
// Create a predicate that makes sensitive fields private
Predicate<MerkleLeaf> makePrivate = leaf =>
{
    if (leaf.TryReadText(out string text))
    {
        // Make the document number and address private
        return text.Contains("documentNumber") || text.Contains("address");
    }
    return false;
};

// Serialize the tree with selective disclosure
string jsonWithPrivacy = merkleTree.ToJson(makePrivate);
```

### JSON Output with Selective Disclosure

The resulting JSON would look like this, with private fields only showing their hash:

```json
{
  "leaves": [
    {
      "data": "0x7b22646f63756d656e7454797065223a2270617373706f7274227d",
      "salt": "0xa48c12f5e7b943de67c8901f",
      "hash": "0x05e7faf4a47104a39003db687c19c25b1d8178a00573340fa11e93235229e096",
      "contentType": "application/json; charset=utf-8; encoding=hex"
    },
    {
      "hash": "0xa384d43e17939399e28563ac9abb24239d1337bc2251bb4abc92322c31be0ca0"
    },
    {
      "data": "0x7b22697373756544617465223a22323032302d30312d3031227d",
      "salt": "0xdf87a62c31f0e49b5a3c0b42",
      "hash": "0x5495300bdcc7db5422bd9f058affcda66160650cf246a7c3b36f15da8670d5c6",
      "contentType": "application/json; charset=utf-8; encoding=hex"
    },
    // Additional fields...
    {
      "hash": "0x2b43afa0eac0e42474a610410ced4cfab267b0a4b920cbd44ed9c214dd77e3df"
    }
  ],
  "root": "0x42b0557fd2578668da8218367ef9f8f0e233a2a928a979f66c8331fda5d81af8",
  "metadata": {
    "hashAlgorithm": "sha256",
    "version": "1.0"
  }
}
```

### Verifying a Tree with Private Leaves

The verification process works the same way with private leaves:

```csharp
// Parse the selectively disclosed JSON
var parsedTree = MerkleTree.Parse(jsonWithPrivacy);

// Verify the root - this will still work even with private leaves!
bool isStillValid = parsedTree.VerifySha256Root(); // Should be true
```

The verification works because when a leaf is private (has no data or salt), the verification algorithm uses the provided hash directly.

### Creating a Proof for a Specific Claim

Let's say we want to prove the person's age (date of birth) without revealing other information:

```csharp
// Create a predicate that makes everything EXCEPT date of birth private
Predicate<MerkleLeaf> revealOnlyDateOfBirth = leaf =>
{
    if (leaf.TryReadText(out string text))
    {
        // Only reveal date of birth
        return !text.Contains("dateOfBirth");
    }
    return true; // Make everything else private
};

// Create the proof with selective disclosure
string ageProof = merkleTree.ToJson(revealOnlyDateOfBirth);
```

### Creating Private Leaves

There are two ways to create private leaves in a Merkle tree:

1. **Using AddPrivateLeaf**: Create a leaf that is private from the start:
```csharp
// Create a tree with a private leaf
var tree = new MerkleTree();
var hash = Hex.Parse("0x1234567890abcdef");
var privateLeaf = tree.AddPrivateLeaf(hash);
```

2. **Using Selective Disclosure**: Make existing leaves private during serialization:
```csharp
// Create a predicate that makes certain leaves private
Predicate<MerkleLeaf> makePrivate = leaf =>
    leaf.TryReadText(out string text) && text.Contains("documentNumber");

// Serialize with selective disclosure
string json = tree.ToJson(makePrivate);
```

Both approaches result in leaves that:
- Only contain their hash in the JSON output
- Maintain the tree's verifiability
- Preserve privacy of the leaf's data

## Conclusion

Our selective disclosure implementation for Merkle trees offers a pragmatic balance between privacy and verifiability. By using a simple predicate-based approach and clean JSON serialization, we've created a solution that's easy to use, efficient to implement, and powerful in its applications.

The approach provides:

1. Fine-grained control over which information is revealed
2. Clean JSON representation with only the necessary data
3. Maintained cryptographic verifiability
4. Simple implementation using predicates

This is particularly valuable for real-world applications where selective disclosure balances privacy with verifiability needs. 