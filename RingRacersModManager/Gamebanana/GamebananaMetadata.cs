using System.Text.Json.Serialization;

namespace RingRacersModManager.Gamebanana;
public class GamebananaMetadata {

    [JsonPropertyName("_nRecordCount")]
    public uint RecordCount { get; set; }
    [JsonPropertyName("_bIsComplete")]
    public bool IsComplete { get; set; }
    [JsonPropertyName("_nPerpage")]
    public uint Perpage { get; set; }
}
