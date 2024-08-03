using System.Text.Json.Serialization;

namespace RingRacersModManager.Gamebanana;
public class GamebananaUser {

    [JsonPropertyName("_idRow")]
    public uint ID { get; set; }
    [JsonPropertyName("_sName")]
    public string Name { get; set; }
}
