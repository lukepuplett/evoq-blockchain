# Selective Disclosure Example: Digital Passport

This example demonstrates how to implement selective disclosure with a digital passport stored as a Merkle tree.

## Creating a Digital Passport as a Merkle Tree

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

// Create a unique salt for this passport
var salt = Hex.Parse("0x7f8e7d6c5b4a3210");

// Create the Merkle tree
var merkleTree = new MerkleTree("1.0");

// Add each field as a leaf in the tree
foreach (var pair in passportData)
{
    // Convert the key-value pair to a JSON object
    var jsonObject = new Dictionary<string, object?>
    {
        { pair.Key, pair.Value }
    };

    string json = JsonSerializer.Serialize(jsonObject);
    Hex jsonHex = new Hex(System.Text.Encoding.UTF8.GetBytes(json));

    // Use a content type that indicates it's JSON in UTF-8 encoded as hex
    string contentType = "application/json; charset=utf-8; encoding=hex";

    merkleTree.AddLeaf(jsonHex, salt, contentType, MerkleTree.ComputeSha256Hash);
}

// Compute the root hash
merkleTree.RecomputeSha256Root();

// Verify the root
bool isValid = merkleTree.VerifySha256Root(); // Should be true
```

## Creating a Passport with Selective Disclosure

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

// Create JSON options that omit null values
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// Serialize the tree with selective disclosure
string jsonWithPrivacy = merkleTree.ToJson(MerkleTree.ComputeSha256Hash, makePrivate, options);
```

## JSON Output with Selective Disclosure

The resulting JSON would look like this, with private fields only showing their hash:

```json
{
  "leaves": [
    {
      "data": "0x7b22646f63756d656e7454797065223a2270617373706f7274227d",
      "salt": "0x7f8e7d6c5b4a3210",
      "hash": "0x05e7faf4a47104a39003db687c19c25b1d8178a00573340fa11e93235229e096",
      "contentType": "application/json; charset=utf-8; encoding=hex"
    },
    {
      "hash": "0xa384d43e17939399e28563ac9abb24239d1337bc2251bb4abc92322c31be0ca0"
    },
    {
      "data": "0x7b22697373756544617465223a22323032302d30312d3031227d",
      "salt": "0x7f8e7d6c5b4a3210",
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

## Verifying a Tree with Private Leaves

The verification process works the same way with private leaves:

```csharp
// Parse the selectively disclosed JSON
var parsedTree = MerkleTree.Parse(jsonWithPrivacy);

// Verify the root - this will still work even with private leaves!
bool isStillValid = parsedTree.VerifySha256Root(); // Should be true
```

The verification works because when a leaf is private (has no data or salt), the verification algorithm uses the provided hash value directly.

## Creating a Proof for a Specific Claim

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

// Create the proof
string ageProof = merkleTree.ToJson(MerkleTree.ComputeSha256Hash, revealOnlyDateOfBirth, options);
```

## Conclusion

This example demonstrates how our selective disclosure approach provides:

1. Fine-grained control over which information is revealed
2. Clean JSON representation with only the necessary data
3. Maintained cryptographic verifiability
4. Simple implementation using predicates

This approach is powerful for real-world applications where selective disclosure balances privacy with verifiability needs. 