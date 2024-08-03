using System.Text.Json.Serialization;

namespace RingRacersModManager.Github;
public class GitHubRelease {
    [JsonPropertyName("html_url")]
    public string URL { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; }
    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; }
    [JsonPropertyName("body")]
    public string Body { get; set; }
}
