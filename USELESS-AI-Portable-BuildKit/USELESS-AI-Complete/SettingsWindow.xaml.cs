using System.Windows;
namespace UselessAI;
public partial class SettingsWindow : Window
{
    public bool SpeakEnabled => SpeakCheck.IsChecked == true;
    public SettingsWindow() { InitializeComponent(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
    private void Save_Click(object sender, RoutedEventArgs e) { DialogResult = true; }
}
