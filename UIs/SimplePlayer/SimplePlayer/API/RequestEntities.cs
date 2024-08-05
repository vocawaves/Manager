using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SimplePlayer.API;

public class PlayByIndexRequest
{
    [JsonPropertyName("p")]
    public int PlaylistIndex { get; set; }
    [JsonPropertyName("t")]
    public int TrackIndex { get; set; }
}

public class SfxByIndexRequest
{
    [JsonPropertyName("s")]
    public int BoardIndex { get; set; }
    [JsonPropertyName("b")]
    public int ButtonIndex { get; set; }
}

public class SfxByGridIndexRequest
{
    [JsonPropertyName("b")]
    public int BoardIndex { get; set; }
    [JsonPropertyName("r")]
    public int ButtonRow { get; set; }
    [JsonPropertyName("c")]
    public int ButtonColum { get; set; }
}

public class PlayByIndexNameRequest
{
    [JsonPropertyName("p")]
    public int PlaylistIndex { get; set; }
    [JsonPropertyName("t")]
    public string? TrackName { get; set; }
}

public class PlayByNameRequest
{
    [JsonPropertyName("p")]
    public string? PlaylistName { get; set; }
    [JsonPropertyName("t")]
    public string? TrackName { get; set; }
}

public class PlayByNameIndexRequest
{
    [JsonPropertyName("p")]
    public string? PlaylistName { get; set; }
    [JsonPropertyName("t")]
    public int TrackIndex { get; set; }
}

public class PlayPathRequest
{
    [JsonPropertyName("p")]
    public string? Path { get; set; }
}

public class SetPositionRequest
{
    [JsonPropertyName("p")]
    public double Position { get; set; }
}

[JsonSerializable(typeof(PlayByIndexRequest))]
[JsonSerializable(typeof(PlayByIndexNameRequest))]
[JsonSerializable(typeof(PlayByNameRequest))]
[JsonSerializable(typeof(PlayByNameIndexRequest))]
[JsonSerializable(typeof(PlayPathRequest))]
[JsonSerializable(typeof(SetPositionRequest))]
[JsonSerializable(typeof(SfxByIndexRequest))]
[JsonSerializable(typeof(SfxByGridIndexRequest))]
[JsonSerializable(typeof(TimeSpan?))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
public partial class ApiJsonSerializerContext : JsonSerializerContext
{
}