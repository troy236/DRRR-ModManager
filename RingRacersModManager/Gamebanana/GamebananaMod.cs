using System.Text.Json.Serialization;

namespace RingRacersModManager.Gamebanana;
public class GamebananaMod {

    [JsonPropertyName("_sErrorCode")]
    public string ErrorCode { get; set; }

    [JsonPropertyName("_aMetadata")]
    public GamebananaMetadata Metadata { get; set; }
    [JsonPropertyName("_aRecords")]
    public GamebananaRecord[] Records { get; set; }
}
