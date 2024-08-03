using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using RingRacersModManager.Gamebanana;
using RingRacersModManager.Github;
using RingRacersModManager.UI.ViewModels;

namespace RingRacersModManager.UI;
public partial class ModManagerWindow : Window {

    public ModManagerWindowViewModel ViewModel;
    public static bool MessageBoardCheckInProgress { get; set; }
    public static DateTime LastMessageBoardCheck { get; set; } = DateTime.MinValue;

    public ModManagerWindow() {
        InitializeComponent();
        this.Title += Program.VERSION;
        this.DataContext = new ModManagerWindowViewModel();
        ViewModel = (ModManagerWindowViewModel)this.DataContext;
        if (Design.IsDesignMode) {
            Program.Config = new();
            Program.Config.Theme = 1;
        }
        if (Program.Config.Theme == 1) this.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        else this.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        if (Design.IsDesignMode) return;
        foreach (var addon in Program.Addons.Where(a => !string.IsNullOrEmpty(a.InstallPath))) {
            ViewModel.Addons.Add(addon);
        }
        if (!OperatingSystem.IsWindows()) this.LoadAddonsButton.IsVisible = false;
    }

    private async void ModManagerWindow_Loaded(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "mmconfig.json"))) {
            if (!await this.FirstLaunchSetup()) {
                //Kinda need the Ring Racers addon path for the app to be useful to so just close if can't find it and user didn't provide a path
                (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown();
                return;
            }
            //We should have the Ring Racers path now. Start auto addon detection
            StartAddonDetection();
        }
        else {
            if (Program.Config.StartupAppUpdateCheck) {
                await CheckForAppUpdate();
            }
            if (Program.Config.StartupAddonsUpdateCheck) {
                //Clone addon list as we are running this on a background thread
                UpdateAddons(ViewModel.Addons.ToArray());
            }
        }
    }

    private async Task<bool> FirstLaunchSetup() {
        Console.WriteLine();
        Console.WriteLine("1st launch setup start");
        //Try and auto detect the executable path
        if (!FindRingRacersPath()) {
            if (OperatingSystem.IsWindows()) {
                if (!await this.SetRingRacersPath()) return false;
            }
            //If on Linux/Mac check if we at least can auto detect the addon path
            else if (string.IsNullOrEmpty(Program.Config.RingRacersAddonsPath)) {
                Console.WriteLine("Failed to find Ring Racers addon folder");
                if (!await this.SetRingRacersPath()) return false;
            }
        }
        if (string.IsNullOrEmpty(Program.Config.RingRacersAddonsPath)) {
            if (!await this.SetRingRacersPath()) return false;
        }
        else Console.WriteLine($"Found Ring Racers addon path: {Program.Config.RingRacersAddonsPath}");
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "mmconfig.json"), JsonSerializer.SerializeToUtf8Bytes(Program.Config, ModManagerJsonContext.Default.Config));
        return true;
    }

    private bool FindRingRacersPath() {
        Console.WriteLine("Looking for Ring Racers path...");
        if (OperatingSystem.IsLinux()) {
            //Please tell me its not under 'main'...
            //Linux is weird. Change my mind

            /*var mainProcesses = Process.GetProcessesByName("main");
            if (mainProcesses.Length != 0) {
                Console.WriteLine("Are you serious? And why is there 2 of them?");
                Console.WriteLine(mainProcesses.Length);
                try {
                    foreach (var process in mainProcesses) {
                        Console.WriteLine(process.ProcessName);
                        Console.WriteLine(process.MainModule.FileName);
                    }
                }
                catch { }
            }*/
            // path returned: /app/bin/ringracers
            //The path isn't even the full path from /var
            //Useless since the path isn't the 'actual' path unless I can use it? I'm not a Linux guy so not a clue
        }
        else {
            var ringRacerProcesses = Process.GetProcessesByName("ringracers");
            if (ringRacerProcesses.Length != 0) {
                if (OperatingSystem.IsMacOS()) {
                    if (ringRacerProcesses[0].MainModule.FileName.StartsWith("/private/var")) {
                        //Warn if path is a temp path
                        Console.WriteLine("Detected Ring Racers open from a temp folder");
                        Console.WriteLine("When opening the DMG file drag the Ring Racers icon to the Applications folder shown and launch it from launchpad");
                    }
                    else {
                        Console.WriteLine($"Found Ring Racers path from game process: {ringRacerProcesses[0].MainModule.FileName}");
                        Program.Config.RingRacersPath = Path.GetDirectoryName(ringRacerProcesses[0].MainModule.FileName);
                        return true;
                    }
                }
                else {
                    Console.WriteLine($"Found Ring Racers path from game process: {ringRacerProcesses[0].MainModule.FileName}");
                    Program.Config.RingRacersPath = Path.GetDirectoryName(ringRacerProcesses[0].MainModule.FileName);
                    return true;
                }
            }
        }
        if (OperatingSystem.IsWindows() && File.Exists(Path.Combine(AppContext.BaseDirectory, "ringracers.exe"))) {
            Console.WriteLine($"Found Ring Racers path in current directory: {AppContext.BaseDirectory}");
            Program.Config.RingRacersPath = AppContext.BaseDirectory;
        }
        //Assume the mod manager will never be in the folder of the game on Linux/Mac
        /*else if (File.Exists(Path.Combine(AppContext.BaseDirectory, "ringracers"))) {
            Console.WriteLine($"Found Ring Racers path in current directory: {AppContext.BaseDirectory}");
            Program.Config.RingRacersPath = AppContext.BaseDirectory;
        }*/
        if (OperatingSystem.IsWindows() &&
            File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RingRacers", "ringracers.exe"))) {

            Console.WriteLine($"Found Ring Racers path in default install path: {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RingRacers", "ringracers.exe")}");
            Program.Config.RingRacersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RingRacers");
            return true;
        }
        else if (OperatingSystem.IsLinux() &&
            File.Exists(@"/var/lib/flatpak/app/org.kartkrew.RingRacers/current/active/files/bin/ringracers")) {

            Console.WriteLine("Found Ring Racers path in default install path: /var/lib/flatpak/app/org.kartkrew.RingRacers/current/active/files/bin/ringracers");
            Program.Config.RingRacersPath = "/var/lib/flatpak/app/org.kartkrew.RingRacers/current/active/files/bin";
            return true;
        }
        else if (OperatingSystem.IsMacOS() &&
            File.Exists(@"/Applications/Dr. Robotnik's Ring Racers.app/Contents/MacOS/ringracers")) {

            Console.WriteLine("Found Ring Racers path in default install path: /Applications/Dr. Robotnik's Ring Racers.app/Contents/MacOS/ringracers");
            Program.Config.RingRacersPath = "/Applications/Dr. Robotnik's Ring Racers.app/Contents/MacOS";
            return true;
        }
        return false;
    }

    private async Task<bool> SetRingRacersPath() {
        if (!StorageProvider.CanOpen) return false;
        var filePickerOpenOptions = new FilePickerOpenOptions() {
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.All },
            Title = "Find Ring Racers executable (optional)"
        };
        var files = await StorageProvider.OpenFilePickerAsync(filePickerOpenOptions);
        if (files.Count != 0) {
            if (files[0].Name is "ringracers.exe" or "ringracers") {
                Program.Config.RingRacersPath = Path.GetDirectoryName(files[0].Path.AbsolutePath);
                Program.Config.RingRacersManualExecutablePath = files[0].Path.AbsolutePath;
            }
            else {
                Console.WriteLine("Invalid file selected. Expecting ringracers");
            }
        }
        var folderPickerOpenOptions = new FolderPickerOpenOptions() {
            AllowMultiple = false,
            Title = "Find Ring Racers addons folder"
        };
        var folders = await StorageProvider.OpenFolderPickerAsync(folderPickerOpenOptions);
        if (folders.Count != 0) {
            if (folders[0].Name == "addons") {
                Program.Config.RingRacersManualAddonPath = folders[0].Path.AbsolutePath;
            }
            else return false;
        }
        else return false;
        return true;
    }

    private void StartAddonDetection() {
        string addonPath = Program.Config.RingRacersAddonsPath;
        if (!Directory.Exists(addonPath)) return;
        Console.WriteLine("Checking addons folder...");
        var addons = Program.Addons;
        int count = this.ViewModel.Addons.Count;
        foreach (var file in Directory.GetFiles(Program.Config.RingRacersAddonsPath, "*", SearchOption.AllDirectories)) {
            var installPath = file.Substring(addonPath.Length + 1);
            var md5Hash = BitConverter.ToString(MD5.HashData(File.ReadAllBytes(file))).Replace("-", string.Empty);
            var addon = addons.FirstOrDefault(a => {
                if (a.MD5FileHashes == null || a.MD5FileHashes.Length == 0) return false;
                return a.MD5FileHashes.Any(md5 => md5.Equals(md5Hash, StringComparison.OrdinalIgnoreCase));
            });
            if (addon == null) {
                Console.WriteLine($"Unknown addon: {Path.GetFileName(file)}");
                continue;
            }
            addon.InstallPath = installPath;
            addon.InstalledVersion = addon.LatestVersion;
            if (!this.ViewModel.Addons.Contains(addon)) {
                this.ViewModel.Addons.Add(addon);
            }
        }
        count = this.ViewModel.Addons.Count - count;
        Console.WriteLine($"Finished addons auto detection. Found {count} addons");
    }

    private void ModManagerWindow_SizeChanged(object sender, SizeChangedEventArgs e) {
        if (!e.HeightChanged) return;
        this.AddonsGrid.MaxHeight = e.NewSize.Height - 100;
    }

    private void NewAddon_Click(object sender, RoutedEventArgs e) {
        new NewAddonWindow().ShowDialog(this);
    }

    private async void EditAddon_Click(object sender, RoutedEventArgs e) {
        int rowCount = this.AddonsGrid.RowSelection.Count;
        if (rowCount == 0) return;
        else if (rowCount == 1) {
            var addon = (Addon)this.AddonsGrid.RowSelection.SelectedItem;
            var window = new NewAddonWindow(addon);
            await window.ShowDialog(this);
            if (!window.DoneHit) return;
            int index = ViewModel.Addons.IndexOf(addon);
            //There must be some better way of refreshing the grid... This moves the addon to the bottom
            ViewModel.Addons.RemoveAt(index);
            ViewModel.Addons.Add(addon);
        }
    }

    private async void DeleteAddon_Click(object sender, RoutedEventArgs e) {
        int rowCount = this.AddonsGrid.RowSelection.Count;
        if (rowCount == 0) return;
        else if (rowCount == 1) {
            var addon = (Addon)this.AddonsGrid.RowSelection.SelectedItem;
            var box = MessageBoxManager.GetMessageBoxStandard("Delete?", $"Delete addon {addon.Name} from disk?",
                    ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Info);
            var result = await box.ShowWindowDialogAsync(this);
            ViewModel.Addons.Remove(addon);
            addon.InstalledVersion = string.Empty;
            addon.IsLoadedAtStartup = false;
            addon.LoadInGame = false;
            if (result == ButtonResult.Yes) {
                try {
                    File.Delete(Path.Combine(Program.Config.RingRacersAddonsPath, addon.InstallPath));
                }
                catch {
                    Console.WriteLine($"Failed to delete addon {addon.Name} from disk");
                }
            }
            addon.InstallPath = string.Empty;
        }
        else {
            for (int i = 0; i < rowCount; i++) {
                var addon = (Addon)this.AddonsGrid.RowSelection.SelectedItems[i];
                var box = MessageBoxManager.GetMessageBoxStandard("Delete?", $"Delete addon {addon.Name} from disk?",
                        ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Info);
                var result = await box.ShowWindowDialogAsync(this);
                ViewModel.Addons.Remove(addon);
                if (result == ButtonResult.Yes) {
                    try {
                        File.Delete(Path.Combine(Program.Config.RingRacersAddonsPath, addon.InstallPath));
                        addon.InstallPath = string.Empty;
                    }
                    catch {
                        Console.WriteLine($"Failed to delete addon {addon.Name} from disk");
                    }
                }
                else addon.InstallPath = string.Empty;
            }
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        try {
            var options = new JsonSerializerOptions {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                TypeInfoResolver = ModManagerJsonContext.Default,
                WriteIndented = true
            };
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "mmconfig.json"), JsonSerializer.SerializeToUtf8Bytes(Program.Config, ModManagerJsonContext.Default.Config));
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "mmaddons.json"), JsonSerializer.SerializeToUtf8Bytes(Program.Addons, options));
        }
        catch (Exception ex) {
            Program.HandleError(ex);
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to save: {ex.Message}",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowWindowDialogAsync(this);
        }
    }

    private async void UpdateMessageBoardButton_Click(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        if (MessageBoardCheckInProgress) {
            var box = MessageBoxManager.GetMessageBoxStandard("", "Already in progress",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);
            return;
        }
        UpdateMessageBoardData();
        var box2 = MessageBoxManager.GetMessageBoxStandard("", "Started getting Message Board data",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
        await box2.ShowWindowDialogAsync(this);
    }

    private void UpdateAddonsButton_Click(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        //Clone addon list as we are running this on a background thread
        UpdateAddons(ViewModel.Addons.ToArray());
    }

    private async void LoadAddonsButton_Click(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        await ModLoader.LoadAddons(ViewModel.Addons.Where(a => a.LoadInGame).ToArray());
    }

    private void LaunchGame_Click(object sender, RoutedEventArgs e) {
        if (File.Exists(Program.Config.RingRacersExecutablePath)) {
            //Does this work on Linux/Mac?
            Process.Start(Program.Config.RingRacersExecutablePath);
            //(Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown();
        }
        else {
            Console.WriteLine($"Could not find ringracers in {Program.Config.RingRacersExecutablePath}");
        }
    }

    private void DarkModeButton_Click(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        this.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        Program.Config.Theme = 1;
    }

    private void LightModeButton_Click(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        this.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        Program.Config.Theme = 2;
    }

    private void AppStartup_IsCheckedChanged(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        Program.Config.StartupAppUpdateCheck = (bool)(sender as CheckBox).IsChecked;
    }

    private void AddonStartup_IsCheckedChanged(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        Program.Config.StartupAddonsUpdateCheck = (bool)(sender as CheckBox).IsChecked;
    }

    private async void SetRingRacersPath_Click(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        if (await SetRingRacersPath()) {
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "mmconfig.json"), JsonSerializer.SerializeToUtf8Bytes(Program.Config, ModManagerJsonContext.Default.Config));
        }
    }

    private async void AppUpdateCheck_Click(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        await CheckForAppUpdate();
    }

    private void RunAddonDetection_Click(object sender, RoutedEventArgs e) {
        if (Design.IsDesignMode) return;
        StartAddonDetection();
    }

    private async Task CheckForAppUpdate() {
        if (await ModManagerUpdateCheck.CheckForUpdates()) {
            try {
                //Update requested and zip should be downloaded. Close application and launch Updater
                Console.WriteLine("Update downloaded. Opening Updater...");
                if (OperatingSystem.IsWindows()) Process.Start(Path.Combine(AppContext.BaseDirectory, "Updater.exe"));
                else Process.Start(Path.Combine(AppContext.BaseDirectory, "Updater"));
                (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown();
                return;
            }
            catch (Exception ex) {
                Console.WriteLine("Failed to launch Updater");
                Program.HandleError(ex);
            }
        }
    }

    private static async Task UpdateMessageBoardData(bool newThread = true) {
        if (MessageBoardCheckInProgress) return;
        MessageBoardCheckInProgress = true;
        if (newThread) {
            //Do this away from the UI thread if called from it
            new Thread(() => GetData()).Start();
        }
        else await GetData(); //Caller is in non-UI thread so it needs awaiting so this doesn't return before addon check is done
        static async Task GetData() {
            try {
                //API please Kart Krew?
                Console.WriteLine("Getting Maps...");
                var document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}/addons/categories/ring-racers-maps.32/");
                ParseMessageBoardPage(document, AddonType.Map);
                Console.WriteLine("Getting Characters...");
                document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}/addons/categories/ring-racers-characters.31/");
                ParseMessageBoardPage(document, AddonType.Character);
                Console.WriteLine("Getting Followers...");
                document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}/addons/categories/ring-racers-followers.36/");
                ParseMessageBoardPage(document, AddonType.Follower);
                Console.WriteLine("Getting Multi-Category...");
                document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}/addons/categories/ring-racers-multi-category.34/");
                ParseMessageBoardPage(document, AddonType.Multi_Category);
                Console.WriteLine("Getting Lua...");
                document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}/addons/categories/ring-racers-lua.35/");
                ParseMessageBoardPage(document, AddonType.Lua);
                Console.WriteLine("Getting Miscellaneous...");
                document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}/addons/categories/ring-racers-miscellaneous.33/");
                ParseMessageBoardPage(document, AddonType.Miscellaneous);
                Console.WriteLine("Getting Character ports...");
                document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}/addons/categories/rr-char-ports.38/");
                ParseMessageBoardPage(document, AddonType.Character_Port);
                Console.WriteLine("Getting Lua ports...");
                document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}/addons/categories/rr-lua-ports.39/");
                ParseMessageBoardPage(document, AddonType.Miscellaneous);
                Console.WriteLine("Getting Miscellaneous ports...");
                document = Program.WebLoader.Load($"{Program.MessageBoardBaseURL}/addons/categories/rr-misc-ports.40/");
                ParseMessageBoardPage(document, AddonType.Miscellaneous_Port);
                Console.WriteLine("Finished Message Board check.");
                Console.WriteLine("Start Gamebanana check");
                var gamebananaMod = await GamebananaAPI.GetMods(20201, 1, 50);
                if (!string.IsNullOrEmpty(gamebananaMod.ErrorCode)) {
                    return;
                }
                foreach (var mod in gamebananaMod.Records) {
                    if (!mod.HasFiles) continue;
                    Addon addon = new();
                    addon.Type = AddonType.Unknown;
                    addon.Site = 2;
                    addon.Name = mod.Name;
                    addon.Author = mod.Submitter.Name;
                    addon.URL = $"/{mod.ID}";
                    addon.LatestVersion = mod.Version;
                    addon.EpochTimestampAdded = mod.EpochTimestampAdded;
                    addon.EpochTimestampModified = mod.EpochTimestampModified;
                    if (Program.Addons.TryGetValue(addon, out var localAddon)) {
                        if (localAddon.EpochTimestampModified == addon.EpochTimestampModified) continue;
                        //Update time has changed but make sure a file has as well
                        var files = await mod.GetFiles();
                        var hashes = files.Select(f => f.MD5Checksum).ToArray();
                        addon.MD5FileHashes = hashes;
                        if (Enumerable.SequenceEqual(localAddon.MD5FileHashes, addon.MD5FileHashes)) continue;
                        Console.WriteLine($"Updating cached addon {localAddon.Name}");
                        localAddon.Name = addon.Name;
                        localAddon.LatestVersion = addon.LatestVersion;
                        localAddon.EpochTimestampModified = addon.EpochTimestampModified;
                        localAddon.MD5FileHashes = addon.MD5FileHashes;
                        localAddon.HasUpdate = true;
                    }
                    else {
                        var files = await mod.GetFiles();
                        var hashes = files.Select(f => f.MD5Checksum).ToArray();
                        addon.MD5FileHashes = hashes;
                        Program.Addons.Add(addon);
                    }
                }
                Console.WriteLine(value: "Finished Gamebanana check");
            }
            catch (Exception ex) {
                Console.WriteLine("Update Message Board data encountered a error:");
                Program.HandleError(ex);
            }
            finally {
                MessageBoardCheckInProgress = false;
                var options = new JsonSerializerOptions {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    TypeInfoResolver = ModManagerJsonContext.Default,
                    WriteIndented = true
                };
                File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "mmaddons.json"), JsonSerializer.SerializeToUtf8Bytes(Program.Addons, options));
                LastMessageBoardCheck = DateTime.UtcNow;
            }
        }
    }

    private static bool ParseMessageBoardPage(HtmlDocument document, AddonType type) {
        bool errored = false;
        var addonListNode = document.QuerySelector("#top > div.p-body > div > div.p-body-main.p-body-main--withSideNav > div.p-body-content > div > div > div.block-container > div.block-body");
        if (addonListNode.ChildNodes[1].ChildNodes[0].InnerText == "No resources have been created yet.") return true;

        foreach (var childNode in addonListNode.ChildNodes[1].ChildNodes) {
            Addon addon = new();
            try {
                if (childNode.NodeType == HtmlNodeType.Text) continue;
                addon.Type = type;
                addon.Site = 1;
                var titleNode = childNode.QuerySelector("div.structItem-cell.structItem-cell--main > div.structItem-title");
                if (titleNode.ChildNodes.Count > 5) {
                    addon.Name = System.Net.WebUtility.HtmlDecode(titleNode.ChildNodes[1].ChildNodes[0].InnerText + " " + titleNode.ChildNodes[3].InnerText);
                    addon.URL = titleNode.ChildNodes[3].GetAttributeValue("href", "");
                    addon.LatestVersion = System.Net.WebUtility.HtmlDecode(titleNode.ChildNodes[5].InnerText);
                }
                else {
                    addon.Name = System.Net.WebUtility.HtmlDecode(titleNode.ChildNodes[1].InnerText);
                    addon.URL = titleNode.ChildNodes[1].GetAttributeValue("href", "");
                    addon.LatestVersion = System.Net.WebUtility.HtmlDecode(titleNode.ChildNodes[3].InnerText);
                }
                if (string.IsNullOrEmpty(addon.URL)) continue;
                addon.Author = System.Net.WebUtility.HtmlDecode(childNode.GetAttributeValue("data-author", ""));
                //addon.GetMD5().ConfigureAwait(false).GetAwaiter().GetResult();
                if (Program.Addons.TryGetValue(addon, out var localAddon)) {
                    //Skip if version is same as cached
                    if (localAddon.LatestVersion == addon.LatestVersion) continue;
                    localAddon.Name = addon.Name;
                    localAddon.LatestVersion = addon.LatestVersion;
                    //Don't bother with getting MD5 hashes on public builds since no API and would require downloading the mod
                    //which is pointless to do if the user doesn't want the mod or has limited bandwidth
                    localAddon.MD5FileHashes = Array.Empty<string>();
                    localAddon.HasUpdate = true;
                }
                else Program.Addons.Add(addon);
            }
            catch (Exception ex) {
                if (!errored) {
                    Program.HandleError(ex);
                    Console.WriteLine("Failed to locate message board data. Addon data will be incomplete");
                    errored = true;
                }
                if (string.IsNullOrEmpty(addon.URL)) continue;
                Program.Addons.Add(addon);
            }
        }
        return !errored;
    }

    private void UpdateAddons(Addon[] addons) {
        new Thread(async () => {
            try {
                //If message board was just checked don't do it again
                if (DateTime.UtcNow - LastMessageBoardCheck >= TimeSpan.FromMinutes(5)) {
                    //Already on a non-UI thread
                    await UpdateMessageBoardData(false);
                }
                if (addons.Length == 0) {
                    Console.WriteLine("No installed addons found");
                    return;
                }
                foreach (var installedAddon in addons) {
                    if (installedAddon.Site == 0) continue;
                    if (Program.Addons.TryGetValue(installedAddon, out Addon addon)) {
                        if (addon.HasUpdate) {
                            bool update = false;
                            await Dispatcher.UIThread.Invoke(async () => {
                                var box = MessageBoxManager.GetMessageBoxStandard("Update?", $"Install Update for {addon.Name} ({addon.LatestVersion})?",
                                        ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Info);
                                var result = await box.ShowWindowDialogAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                                update = result == ButtonResult.Yes;
                            });
                            if (!update) continue;
                            var oldAddonPath = installedAddon.InstallPath;
                            var addonStartup = installedAddon.IsLoadedAtStartup;
                            //Clear old addon from ringexec.cfg
                            if (addonStartup) installedAddon.IsLoadedAtStartup = false;
                            await addon.Download();
                            if (oldAddonPath != installedAddon.InstallPath) {
                                if (File.Exists(Path.Combine(Program.Config.RingRacersAddonsPath, oldAddonPath))) {
                                    File.Delete(Path.Combine(Program.Config.RingRacersAddonsPath, oldAddonPath));
                                }
                            }
                            //Add it back to ringexec.cfg
                            if (addonStartup) installedAddon.IsLoadedAtStartup = true;
                        }
                    }
                }
                //Force a save cause we don't want to lose track of updates saved to disk
                var options = new JsonSerializerOptions {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    TypeInfoResolver = ModManagerJsonContext.Default,
                    WriteIndented = true
                };
                File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "mmaddons.json"), JsonSerializer.SerializeToUtf8Bytes(Program.Addons, options));
            }
            catch (Exception ex) {
                Console.WriteLine("Addon update check encountered a error:");
                Program.HandleError(ex);
            }
            await Dispatcher.UIThread.Invoke(async () => {
                var box = MessageBoxManager.GetMessageBoxStandard("Done", "Finished Addon Update check",
                        ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
                var result = await box.ShowWindowDialogAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
            });
        }).Start();

    }
}