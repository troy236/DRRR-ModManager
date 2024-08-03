using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace RingRacersModManager.UI;

public enum AddonType {
    Map,
    Character,
    Follower,
    Multi_Category,
    Lua,
    Miscellaneous,
    Character_Port,
    Lua_Port,
    Miscellaneous_Port,
    Unknown
}

public class Addon {
    private bool _isLoadedAtStartup;
    public bool IsLoadedAtStartup {
        get {
            return _isLoadedAtStartup;
        }
        set {
            _isLoadedAtStartup = value;
            try {
                //Check if init done. Will be null while populated by JSON
                if (Program.Addons == null) return;
                //this.InstallPath shouldn't be null if startup box is checked? but check anyway
                if (string.IsNullOrEmpty(this.InstallPath)) return;
                if (string.IsNullOrEmpty(Program.Config.RingRacersAddonsPath)) return;
                //Set in ringexec.cfg
                List<string> ringexecLines = new();
                string ringExecPath = Path.Combine(Path.GetDirectoryName(Program.Config.RingRacersAddonsPath), "ringexec.cfg");
                if (File.Exists(ringExecPath)) {
                    ringexecLines = File.ReadAllLines(ringExecPath).ToList();
                }
                if (_isLoadedAtStartup) {
                    //Check if ringexec.cfg already has the addon specified
                    if (ringexecLines.Contains($"addfile {Path.GetFileName(this.InstallPath)}", StringComparer.OrdinalIgnoreCase)) return;
                    ringexecLines.Add($"addfile {Path.GetFileName(this.InstallPath)}");
                }
                else {
                    //Remove the addon from ringexec.cfg
                    int index = ringexecLines.FindIndex(line => line.Equals($"addfile {Path.GetFileName(this.InstallPath)}", StringComparison.OrdinalIgnoreCase));
                    if (index == -1) return;
                    ringexecLines.RemoveAt(index);
                }
                File.WriteAllLines(ringExecPath, ringexecLines);
            }
            catch (Exception ex) {
                Console.WriteLine($"Failed to update ringexec.cfg for addon {this.Name}");
                Program.HandleError(ex);
            }
        }
    }
    public bool LoadInGame { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string InstalledVersion { get; set; }
    public AddonType Type { get; set; }
    //Non-UI variables below
    public string URL { get; set; }
    public int Site { get; set; } //0 for Local, 1 for Message Board, 2 for Gamebanana
    public ulong EpochTimestampAdded { get; set; } //Gamebanana only
    public ulong EpochTimestampModified { get; set; } //Gamebanana only
    public string LatestVersion { get; set; }
    public string[] MD5FileHashes { get; set; }
    public string InstallPath { get; set; }
    public bool HasUpdate { get; set; }

    public Addon() {
        this.Name = string.Empty;
        this.Author = string.Empty;
        this.InstalledVersion = string.Empty;
        this.URL = string.Empty;
        this.LatestVersion = string.Empty;
        this.MD5FileHashes = Array.Empty<string>();
        this.InstallPath = string.Empty;
    }

    public override bool Equals(object obj) {
        //Used by HashSet/ObservableCollection.Remove
        if (obj is not Addon addon) return false;
        return this.URL == addon.URL;
    }

    public override int GetHashCode() {
        //Used by HashSet
        return this.URL.GetHashCode();
    }

    public string GetUpdateText() {
        var document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}{this.URL}updates");
        var updateListNode = document.QuerySelector("#top > div.p-body > div > div.p-body-main > div.p-body-content > div > div.block.block--messages > div > div");
        if (updateListNode == null) return string.Empty;
        List<string> updateList = new List<string>();
        foreach (var childNode in updateListNode.ChildNodes) {
            if (childNode.NodeType == HtmlNodeType.Text) continue;
            var updateVersion = childNode.QuerySelector("div > div > div > div > div.message-attribution.message-attribution--split");
            var updateContent = childNode.QuerySelector("div > div > div > div > div.message-userContent.lbContainer.js-lbContainer > blockquote");
            if (updateVersion == null) continue;
            if (updateContent == null) continue;
            StringBuilder sb = new();
            sb.Append(updateVersion.ChildNodes[1].ChildNodes[1].InnerText);
            sb.AppendLine(":");
            sb.AppendLine();
            sb.Append(updateContent.ChildNodes[1].InnerText);
            return sb.ToString();
        }
        return string.Empty;
    }

    public async Task Download() {
        this.InstallPath ??= "";
        if (this.Site == 1) {
            await this.DownloadMessageBoard();
        }
        else if (this.Site == 2) {
            await this.DownloadGamebanana();
        }
    }

    private async Task DownloadMessageBoard() {
        var document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}{this.URL}");
        var downloadButton = document.QuerySelector("#top > div.p-body > div > div.p-body-header > div > div > div.p-title > div > a");
        if (downloadButton == null) {
            Console.WriteLine($"{this.Name} - Could not find download button");
            await Dispatcher.UIThread.Invoke(async () => {
                var box2 = MessageBoxManager.GetMessageBoxStandard("", $"{this.Name} Could not find download button",
                        ButtonEnum.Ok, Icon.Error);
                await box2.ShowAsync();
            });
            return;
        }
        List<(string Link, string Filename)> fileList = new();
        string installPath = Path.GetDirectoryName(this.InstallPath);
        if (downloadButton.Attributes.Count == 4) {
            document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}{this.URL}download");
            var fileListNode = document.QuerySelector("#top > div.p-body > div > div.p-body-main > div.p-body-content > div > div > div > ul");
            for (int i = 0; i < fileListNode.ChildNodes.Count; i++) {
                if (fileListNode.ChildNodes[i].NodeType == HtmlNodeType.Text) continue;
                var node = fileListNode.ChildNodes[i].QuerySelector("div > div");
                string downloadLink = node.ChildNodes[1].ChildNodes[1].GetAttributeValue("href", "");
                string fileName = System.Net.WebUtility.HtmlDecode(node.ChildNodes[3].InnerText);
                fileList.Add((downloadLink, fileName));
            }
            int fileIndex = -1;
            await Dispatcher.UIThread.Invoke(async () => {
                var window = new AddonDownloadSelect(fileList.Select(fl => fl.Filename).ToArray());
                var index = await window.ShowDialog<int?>((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                if (index.HasValue) fileIndex = index.Value;
            });
            if (fileIndex == -1) return;
            using var httpResponse = await Program.HttpClient.GetAsync($"{Program.MessageBoardBaseURL}{fileList[fileIndex].Link}");
            byte[] fileBytes = await httpResponse.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(Path.Combine(Program.Config.RingRacersAddonsPath, installPath == null ? "" : installPath, fileList[fileIndex].Filename), fileBytes);
            this.InstallPath = Path.Combine(installPath == null ? "" : installPath, fileList[fileIndex].Filename);
            this.InstalledVersion = this.LatestVersion;
            this.MD5FileHashes = [BitConverter.ToString(MD5.HashData(fileBytes)).Replace("-", string.Empty)];
            this.HasUpdate = false;
        }
        else {
            using var httpResponse = await Program.HttpClient.GetAsync($"{Program.MessageBoardBaseURL}{this.URL}download");
            string fileName = System.Net.WebUtility.HtmlDecode(httpResponse.Content.Headers.ContentDisposition.FileName.Replace("\"", ""));
            byte[] fileBytes = await httpResponse.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(Path.Combine(Program.Config.RingRacersAddonsPath, installPath == null ? "" : installPath, fileName), fileBytes);
            this.InstallPath = Path.Combine(installPath == null ? "" : installPath, fileName);
            this.InstalledVersion = this.LatestVersion;
            this.MD5FileHashes = [BitConverter.ToString(MD5.HashData(fileBytes)).Replace("-", string.Empty)];
            this.HasUpdate = false;
        }
    }

    private async Task DownloadGamebanana() {
        string installPath = Path.GetDirectoryName(this.InstallPath);
        using var httpResponse = await Program.HttpClient.GetAsync($"https://gamebanana.com/apiv11/Mod{this.URL}?_csvProperties=_aFiles");
        byte[] fileBytes = await httpResponse.Content.ReadAsByteArrayAsync();
        var gamebananaFiles = JsonSerializer.Deserialize(fileBytes, ModManagerJsonContext.Default.GamebananaAPIFileGet).Files;
        if (gamebananaFiles.Length == 0) throw new Exception("No Files found");
        else if (gamebananaFiles.Length == 1) {
            using var httpResponse2 = await Program.HttpClient.GetAsync(gamebananaFiles[0].DownloadURL);
            fileBytes = await httpResponse2.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(Path.Combine(Program.Config.RingRacersAddonsPath, installPath == null ? "" : installPath, gamebananaFiles[0].FileName), fileBytes);
            this.InstallPath = Path.Combine(installPath == null ? "" : installPath, gamebananaFiles[0].FileName);
            this.InstalledVersion = this.LatestVersion;
            this.MD5FileHashes = [gamebananaFiles[0].MD5Checksum];
            this.HasUpdate = false;
        }
        else if (gamebananaFiles.Length > 1) {
            int fileIndex = -1;
            await Dispatcher.UIThread.Invoke(async () => {
                var window = new AddonDownloadSelect(gamebananaFiles.Select(gb => gb.FileName).ToArray());
                var index = await window.ShowDialog<int?>((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                if (index.HasValue) fileIndex = index.Value;
            });
            if (fileIndex == -1) return;
            using var httpResponse2 = await Program.HttpClient.GetAsync(gamebananaFiles[fileIndex].DownloadURL);
            fileBytes = await httpResponse2.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(Path.Combine(Program.Config.RingRacersAddonsPath, installPath == null ? "" : installPath, gamebananaFiles[fileIndex].FileName), fileBytes);
            this.InstallPath = Path.Combine(installPath == null ? "" : installPath, gamebananaFiles[fileIndex].FileName);
            this.InstalledVersion = this.LatestVersion;
            var hashes = gamebananaFiles.Select(f => f.MD5Checksum).ToArray();
            this.MD5FileHashes = hashes;
            this.HasUpdate = false;
        }
    }
}
