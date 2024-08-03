using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Updater;

internal class Program {
    static void Main(string[] args) {
        try {
            string architecture = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "x64" : "arm64";
            string operatingSystem = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : "mac";
            if (!File.Exists(Path.Combine(AppContext.BaseDirectory, $"DRRRModManager-{operatingSystem}-{architecture}.zip"))) {
                Console.WriteLine("Update file not found. Closing");
                return;
            }
            //Wait for Mod Manager to close
            Thread.Sleep(500);
            var modManagerProcesses = Process.GetProcessesByName("DRRRModManager");
            foreach (var process in modManagerProcesses) {
                Console.WriteLine("Waiting for Mod Manager to close...");
                try {
                    process.WaitForExit();
                }
                catch { }
            }
            Console.WriteLine("Extracting update...");
            var zipArchive = ZipFile.Open(Path.Combine(AppContext.BaseDirectory, $"DRRRModManager-{operatingSystem}-{architecture}.zip"), ZipArchiveMode.Read);
            foreach (var entry in zipArchive.Entries) {
                if (entry.Name == "mmaddons.json" || entry.Name.Contains("Updater")) continue;
                entry.ExtractToFile(Path.Combine(AppContext.BaseDirectory, entry.Name), true);
            }
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, $"DRRRModManager-{operatingSystem}-{architecture}-symbols.zip"))) {
                Console.WriteLine("Extracting symbols...");
                var zipArchiveSymbols = ZipFile.Open(Path.Combine(AppContext.BaseDirectory, $"DRRRModManager-{operatingSystem}-{architecture}-symbols.zip"), ZipArchiveMode.Read);
                zipArchiveSymbols.ExtractToDirectory(AppContext.BaseDirectory, true);
                zipArchiveSymbols.Dispose();
                File.Delete(Path.Combine(AppContext.BaseDirectory, $"DRRRModManager-{operatingSystem}-{architecture}-symbols.zip"));
            }
            zipArchive.Dispose();
            File.Delete(Path.Combine(AppContext.BaseDirectory, $"DRRRModManager-{operatingSystem}-{architecture}.zip"));
            Console.WriteLine("Done");
        }
        catch (Exception ex) {
            Console.WriteLine("Failed to update. You can find the zip in the mod manager folder to manually update");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }
    }
}
