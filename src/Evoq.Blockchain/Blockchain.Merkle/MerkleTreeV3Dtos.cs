namespace Evoq.Blockchain.Merkle;

using System.Collections.Generic;
using System.Text.Json.Serialization;

internal record struct MerkleTreeV3Dto
{
    [JsonPropertyName("leaves")]
    public List<MerkleTreeV3LeafDto>? Leaves { get; set; }

    [JsonPropertyName("root")]
    public string Root { get; set; }

    [JsonPropertyName("header")]
    public MerkleTreeV3HeaderDto? Header { get; set; }
}

internal record struct MerkleTreeV3LeafDto
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

internal record struct MerkleTreeV3HeaderDto
{
    [JsonPropertyName("typ")]
    public string Typ { get; set; }
}

internal record struct MerkleTreeV3LeafHeaderDto
{
    [JsonPropertyName("alg")]
    public string Alg { get; set; }

    [JsonPropertyName("typ")]
    public string Typ { get; set; }

    [JsonPropertyName("leaves")]
    public int Leaves { get; set; }

    [JsonPropertyName("exchange")]
    public string Exchange { get; set; }
}