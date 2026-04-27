using System.Windows;

namespace Xapper.TestApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        var username = txtUsername.Text;
        var remember = chkRemember.IsChecked == true;

        if (string.IsNullOrWhiteSpace(username))
        {
            txtStatus.Text = "Please enter a username.";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
        }
        else
        {
            txtStatus.Text = $"Welcome, {username}!" + (remember ? " (remembered)" : "");
            txtStatus.Foreground = System.Windows.Media.Brushes.Green;
        }
    }
}
