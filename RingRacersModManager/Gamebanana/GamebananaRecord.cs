using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RingRacersModManager.Gamebanana;
public class GamebananaRecord {

    [JsonPropertyName("_idRow")]
    public uint ID { get; set; }
    [JsonPropertyName("_sName")]
    public string Name { get; set; }
    [JsonPropertyName("_tsDateAdded")]
    public ulong EpochTimestampAdded { get; set; }
    [JsonPropertyName("_tsDateModified")]
    public ulong EpochTimestampModified { get; set; }
    [JsonPropertyName("_bHasFiles")]
    public bool HasFiles { get; set; }
    [JsonPropertyName("_aSubmitter")]
    public GamebananaUser Submitter { get; set; }
    [JsonPropertyName("_sVersion")]
    public string Version { get; set; }
    [JsonPropertyName("_nLikeCount")]
    public uint LikeCount { get; set; }
    [JsonPropertyName("_nViewCount")]
    public uint ViewCount { get; set; }

    public async Task<GamebananaFile[]> GetFiles() {
        using var httpResponse = await Program.HttpClient.GetAsync($"https://gamebanana.com/apiv11/Mod/{this.ID}?_csvProperties=_aFiles");
        byte[] fileBytes = await httpResponse.Content.ReadAsByteArrayAsync();
        return JsonSerializer.Deserialize(fileBytes, ModManagerJsonContext.Default.GamebananaAPIFileGet).Files;

    }
}
