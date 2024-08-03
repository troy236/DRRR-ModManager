using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

namespace RingRacersModManager.UI.ViewModels;
public class ModManagerWindowViewModel {
    public ObservableCollection<Addon> Addons { get; }
    public FlatTreeDataGridSource<Addon> GridSource { get; }

    public ModManagerWindowViewModel() {
        Addons = new ObservableCollection<Addon>();
        if (OperatingSystem.IsWindows()) {
            GridSource = new FlatTreeDataGridSource<Addon>(Addons) {
                Columns = {
                    new TextColumn<Addon, string>("Name", x => x.Name),
                    new TextColumn<Addon, string>("Author", x => x.Author),
                    new TextColumn<Addon, AddonType>("Type", x => x.Type),
                    new TextColumn<Addon, string>("Version", x => x.InstalledVersion),
                    new CheckBoxColumn<Addon>("Startup", x => x.IsLoadedAtStartup, (o, v) => o.IsLoadedAtStartup = v),
                    new CheckBoxColumn<Addon>("Load", x => x.LoadInGame, (o, v) => o.LoadInGame = v)
                },
            };
        }
        else {
            GridSource = new FlatTreeDataGridSource<Addon>(Addons) {
                Columns = {
                    new TextColumn<Addon, string>("Name", x => x.Name),
                    new TextColumn<Addon, string>("Author", x => x.Author),
                    new TextColumn<Addon, AddonType>("Type", x => x.Type),
                    new TextColumn<Addon, string>("Version", x => x.InstalledVersion),
                    new CheckBoxColumn<Addon>("Startup", x => x.IsLoadedAtStartup, (o, v) => o.IsLoadedAtStartup = v)
                },
            };
        }
    }
}
