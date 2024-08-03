using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace RingRacersModManager.Github;
public static class ModManagerUpdateCheck {

    public static async Task<bool> CheckForUpdates() {
        try {
            using var httpResponse = await Program.HttpClient.GetAsync("https://api.github.com/repos/troy236/DRRR-ModManager/releases/latest");
            byte[] bytes = await httpResponse.Content.ReadAsByteArrayAsync();
            var latestRelease = JsonSerializer.Deserialize(bytes, ModManagerJsonContext.Default.GitHubRelease);
            if (latestRelease?.Assets == null) return false;
            string architecture = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "x64" : "arm64";
            string operatingSystem = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : "mac";
            var asset = latestRelease.Assets.FirstOrDefault(asset => asset.Name.Contains(operatingSystem) && asset.Name.Contains(architecture) && !asset.Name.Contains("symbols"));
            if (asset == null) return false;
            Version releaseVersion = new(latestRelease.TagName);
            Version currentVersion = new(Program.VERSION);
            if (currentVersion == releaseVersion) return false;
            var box = MessageBoxManager.GetMessageBoxStandard("Update?", $"Mod Manager update: {latestRelease.TagName}{Environment.NewLine}{Environment.NewLine}{latestRelease.Body}{Environment.NewLine}{Environment.NewLine}Would you like to update?",
                    ButtonEnum.YesNo, Icon.Info);
            var result = await box.ShowWindowDialogAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
            if (result != ButtonResult.Yes) return false;
            Console.WriteLine($"Downloading {latestRelease.TagName}");
            //Download the zip, return true prompting the program to close. Updater waits half a second to assume enough time passes and then replaces files
            using var httpResponse2 = await Program.HttpClient.GetAsync(asset.DownloadURL);
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, asset.Name), await httpResponse2.Content.ReadAsByteArrayAsync());
            //Update symbols if user has them too
            if (OperatingSystem.IsWindows() && File.Exists(Path.Combine(AppContext.BaseDirectory, "DRRRModManager.pdb"))) {
                Console.WriteLine("Downloading symbols");
                asset = latestRelease.Assets.FirstOrDefault(asset => asset.Name.Contains(operatingSystem) && asset.Name.Contains(architecture) && asset.Name.Contains("symbols"));
                using var httpResponse3 = await Program.HttpClient.GetAsync(asset.DownloadURL);
                File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "DRRRModManager-symbols.zip"), await httpResponse3.Content.ReadAsByteArrayAsync());
            }
            else if (OperatingSystem.IsLinux() && File.Exists(Path.Combine(AppContext.BaseDirectory, "DRRRModManager.dbg"))) {
                Console.WriteLine("Downloading symbols");
                asset = latestRelease.Assets.FirstOrDefault(asset => asset.Name.Contains(operatingSystem) && asset.Name.Contains(architecture) && asset.Name.Contains("symbols"));
                using var httpResponse3 = await Program.HttpClient.GetAsync(asset.DownloadURL);
                File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "DRRRModManager-symbols.zip"), await httpResponse3.Content.ReadAsByteArrayAsync());
            }
            else if (OperatingSystem.IsMacOS() && File.Exists(Path.Combine(AppContext.BaseDirectory, "DRRRModManager.dsym"))) {
                Console.WriteLine("Downloading symbols");
                asset = latestRelease.Assets.FirstOrDefault(asset => asset.Name.Contains(operatingSystem) && asset.Name.Contains(architecture) && asset.Name.Contains("symbols"));
                using var httpResponse3 = await Program.HttpClient.GetAsync(asset.DownloadURL);
                File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "DRRRModManager-symbols.zip"), await httpResponse3.Content.ReadAsByteArrayAsync());
            }
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine("Mod Manager update check encountered a error:");
            Program.HandleError(ex);
        }
        return false;
    }
}
