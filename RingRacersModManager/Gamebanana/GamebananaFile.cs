using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RingRacersModManager.Gamebanana;

public class GamebananaAPIFileGet {
    [JsonPropertyName("_aFiles")]
    public GamebananaFile[] Files { get; set; }
}
public class GamebananaFile {

    [JsonPropertyName("_sErrorCode")]
    public string ErrorCode { get; set; }

    [JsonPropertyName("_idRow")]
    public uint ID { get; set; }
    [JsonPropertyName("_sFile")]
    public string FileName { get; set; }
    [JsonPropertyName("_nFilesize")]
    public uint FileSize { get; set; }
    [JsonPropertyName("_sDescription")]
    public string Description { get; set; }
    [JsonPropertyName("_tsDateAdded")]
    public ulong EpochTimestampAdded { get; set; }
    [JsonPropertyName("_nDownloadCount")]
    public uint DownloadCount { get; set; }
    [JsonPropertyName("_sDownloadUrl")]
    public string DownloadURL { get; set; }
    [JsonPropertyName("_sMd5Checksum")]
    public string MD5Checksum { get; set; }

    public async Task Download(string installPath) {
        using var httpResponse = await Program.HttpClient.GetAsync(this.DownloadURL);
        byte[] fileBytes = await httpResponse.Content.ReadAsByteArrayAsync();
        File.WriteAllBytes(Path.Combine(Program.Config.RingRacersAddonsPath, installPath == null ? "" : installPath, FileName), fileBytes);
        //addon.InstallPath = Path.Combine(installPath == null ? "" : installPath, fileName);
        //addon.InstalledVersion = this.LatestVersion;
        //addon.MD5FileHashes = [BitConverter.ToString(MD5.HashData(fileBytes)).Replace("-", string.Empty)];
    }
}
