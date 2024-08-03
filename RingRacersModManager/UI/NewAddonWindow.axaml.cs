using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace RingRacersModManager.UI;

public partial class NewAddonWindow : Window {

    public Addon EditAddon { get; set; }
    public string EditAddonOldURL { get; set; }
    public bool DoneHit { get; set; }
    public NewAddonWindow() {
        InitializeComponent();
    }

    public NewAddonWindow(Addon addon) {
        InitializeComponent();
        EditAddon = addon;
        //Need to keep the old URL to fix the dictionary if URL changes
        EditAddonOldURL = addon.URL;
        if (addon.Site == 1) this.AddonURL.Text = Program.MessageBoardBaseURL + addon.URL;
        else if (addon.Site == 2) this.AddonURL.Text = Program.GamebananaBaseURL + addon.URL;
        this.AddonAuthor.Text = addon.Author;
        this.AddonCategory.SelectedIndex = (int)addon.Type;
        this.AddonName.Text = addon.Name;
        this.AddonPath.Text = addon.InstallPath;
        this.AddonVersion.Text = addon.InstalledVersion;
        this.AddButton.IsVisible = false;
        this.DoneButton.IsVisible = true;
    }

    private async void LocateAddon_Click(object sender, RoutedEventArgs e) {
        if (StorageProvider.CanOpen) {
            string addonPath = Program.Config.RingRacersAddonsPath;
            var startFolder = await StorageProvider.TryGetFolderFromPathAsync(addonPath);
            var filePickerOpenOptions = new FilePickerOpenOptions() {
                AllowMultiple = false,
                SuggestedStartLocation = startFolder,
                FileTypeFilter = new[] { FilePickerFileTypes.All },
                Title = "Select addon"
            };
            var files = await StorageProvider.OpenFilePickerAsync(filePickerOpenOptions);
            if (files.Count != 0) {
                this.AddonPath.Text = files[0].Path.AbsolutePath.Substring(addonPath.Length + 1);
            }
        }
        else {
            Console.WriteLine("Unable to open file dialog. Using console input as fallback");
            while (true) {
                Console.Write("Enter addon path (Leave empty to exit): ");
                string ringRacersPath = Console.ReadLine();
                if (string.IsNullOrEmpty(ringRacersPath)) return;
                if (!File.Exists(ringRacersPath)) {
                    Console.WriteLine("File does not exist");
                    continue;
                }
                this.AddonPath.Text = ringRacersPath;
            }
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e) {
        if (string.IsNullOrEmpty(this.AddonName.Text)) {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Addon Name is required",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }
        if (string.IsNullOrEmpty(this.AddonPath.Text)) {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Addon Path is required. Click Locate Addon",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }
        Addon addon = new();
        addon.Name = this.AddonName.Text;
        addon.Author = this.AddonAuthor.Text ?? string.Empty;
        addon.InstallPath = this.AddonPath.Text;
        addon.InstalledVersion = this.AddonVersion.Text ?? string.Empty;
        addon.LatestVersion = this.AddonVersion.Text ?? string.Empty;
        if (string.IsNullOrEmpty(this.AddonURL.Text)) {
            //Need a unique URL for dictionary
            addon.URL = $"local:{addon.Name}";
            if (Program.Addons.Contains(addon)) {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Addon name already used by a local addon",
                        ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await box.ShowWindowDialogAsync(this);
                return;
            }
        }
        else if (this.AddonURL.Text.StartsWith(Program.MessageBoardBaseURL)) {
            addon.URL = this.AddonURL.Text.Substring(Program.MessageBoardBaseURL.Length);
            addon.Site = 1;
        }
        else if (this.AddonURL.Text.StartsWith(Program.GamebananaBaseURL)) {
            addon.URL = this.AddonURL.Text.Substring(Program.GamebananaBaseURL.Length);
            addon.Site = 2;
        }
        else {
            //Unknown site but has a URL
            addon.URL = this.AddonURL.Text;
            if (Program.Addons.Contains(addon)) {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Addon URL already used by a addon",
                        ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await box.ShowWindowDialogAsync(this);
                return;
            }
        }
        if (this.AddonCategory.SelectedItem == null) {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Missing addon category",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }
        int category = this.AddonCategory.SelectedIndex;
        if (category == 0) {
            addon.Type = AddonType.Map;
        }
        else if (category == 1) {
            addon.Type = AddonType.Character;
        }
        else if (category == 2) {
            addon.Type = AddonType.Follower;
        }
        else if (category == 3) {
            addon.Type = AddonType.Multi_Category;
        }
        else if (category == 4) {
            addon.Type = AddonType.Lua;
        }
        else if (category == 5) {
            addon.Type = AddonType.Miscellaneous;
        }
        else if (category == 6) {
            addon.Type = AddonType.Character_Port;
        }
        else if (category == 7) {
            addon.Type = AddonType.Lua_Port;
        }
        else if (category == 8) {
            addon.Type = AddonType.Miscellaneous_Port;
        }
        else if (category == 9) {
            EditAddon.Type = AddonType.Unknown;
        }
        if (Program.Addons.TryGetValue(addon, out var cachedAddon)) {
            //Addon exists in cache already
            if (string.IsNullOrEmpty(cachedAddon.InstallPath)) {
                //Addon isn't installed so update cache and add to UI
                cachedAddon.InstallPath = addon.InstallPath;
                cachedAddon.InstalledVersion = addon.InstalledVersion;
                //Assume user has latest. Painful to check MD5 from Message Board if it isn't already added
                cachedAddon.HasUpdate = false;
                (this.Owner as ModManagerWindow).ViewModel.Addons.Add(cachedAddon);
            }
            else {
                //Addon already installed and should be a recognised site. Let user know to use 'Update Addons' instead
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Addon already installed and linked to Mod Manager. Use Update Addons instead",
                        ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await box.ShowWindowDialogAsync(this);
                return;
            }
        }
        else {
            //Addon not found in cache. Add to cache and UI
            Program.Addons.Add(addon);
            (this.Owner as ModManagerWindow).ViewModel.Addons.Add(addon);
        }
        this.Close();
    }

    private async void Done_Click(object sender, RoutedEventArgs e) {
        if (string.IsNullOrEmpty(this.AddonName.Text)) {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Addon Name is required",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }
        if (string.IsNullOrEmpty(this.AddonPath.Text)) {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Addon Path is required. Click Locate Addon",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }
        EditAddon.Name = this.AddonName.Text;
        EditAddon.Author = this.AddonAuthor.Text ?? string.Empty;
        EditAddon.InstallPath = this.AddonPath.Text;
        EditAddon.InstalledVersion = this.AddonVersion.Text ?? string.Empty;
        Program.Addons.Remove(EditAddon);
        if (string.IsNullOrEmpty(this.AddonURL.Text)) {
            //Need a unique URL for dictionary
            EditAddon.URL = $"local:{EditAddon.Name}";
        }
        else if (this.AddonURL.Text.StartsWith(Program.MessageBoardBaseURL)) {
            EditAddon.URL = this.AddonURL.Text.Substring(Program.MessageBoardBaseURL.Length);
            EditAddon.Site = 1;
        }
        else if (this.AddonURL.Text.StartsWith(Program.GamebananaBaseURL)) {
            EditAddon.URL = this.AddonURL.Text.Substring(Program.GamebananaBaseURL.Length);
            EditAddon.Site = 2;
        }
        else {
            //Unknown site but has a URL
            EditAddon.URL = this.AddonURL.Text;
        }
        int category = this.AddonCategory.SelectedIndex;
        if (category == 0) {
            EditAddon.Type = AddonType.Map;
        }
        else if (category == 1) {
            EditAddon.Type = AddonType.Character;
        }
        else if (category == 2) {
            EditAddon.Type = AddonType.Follower;
        }
        else if (category == 3) {
            EditAddon.Type = AddonType.Multi_Category;
        }
        else if (category == 4) {
            EditAddon.Type = AddonType.Lua;
        }
        else if (category == 5) {
            EditAddon.Type = AddonType.Miscellaneous;
        }
        else if (category == 6) {
            EditAddon.Type = AddonType.Character_Port;
        }
        else if (category == 7) {
            EditAddon.Type = AddonType.Lua_Port;
        }
        else if (category == 8) {
            EditAddon.Type = AddonType.Miscellaneous_Port;
        }
        else if (category == 9) {
            EditAddon.Type = AddonType.Unknown;
        }
        Program.Addons.Add(EditAddon);
        this.DoneHit = true;
        this.Close();
    }
}