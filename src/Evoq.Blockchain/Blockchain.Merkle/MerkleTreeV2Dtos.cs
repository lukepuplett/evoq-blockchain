namespace Evoq.Blockchain.Merkle;

using System.Collections.Generic;
using System.Text.Json.Serialization;

internal record struct MerkleTreeV2Dto
{
    [JsonPropertyName("leaves")]
    public List<MerkleTreeV2LeafDto>? Leaves { get; set; }

    [JsonPropertyName("root")]
    public string Root { get; set; }

    [JsonPropertyName("header")]
    public MerkleTreeV2HeaderDto? Header { get; set; }
}

internal record struct MerkleTreeV2LeafDto
{
    [JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonPropertyName("salt")]
    public string Salt { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }
}

internal record struct MerkleTreeV2HeaderDto
{
    [JsonPropertyName("alg")]
    public string Alg { get; set; }

    [JsonPropertyName("typ")]
    public string Typ { get; set; }
}