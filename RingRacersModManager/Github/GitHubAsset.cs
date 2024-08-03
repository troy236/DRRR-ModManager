using System.Text.Json.Serialization;

namespace RingRacersModManager.Github;
public class GitHubAsset {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("browser_download_url")]
    public string DownloadURL { get; set; }
}
