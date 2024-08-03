using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RingRacersModManager.UI;

public partial class AddonDownloadSelect : Window {
    public AddonDownloadSelect() {
        InitializeComponent();
    }

    public AddonDownloadSelect(string[] files) {
        InitializeComponent();
        foreach (string file in files) {
            this.FileComboBox.Items.Add(file);
        }
    }

    private void Download_Click(object sender, RoutedEventArgs e) {
        int index = -1;
        if (this.FileComboBox.SelectedItem != null) index = this.FileComboBox.SelectedIndex;
        this.Close(index);
    }
}