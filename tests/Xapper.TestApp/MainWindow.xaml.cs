using System.Windows;

namespace Xapper.TestApp;

/// <summary>
/// Xapper E2E 테스트용 샘플 WPF 로그인 폼.
/// 사용자 이름 입력, 기억하기 체크박스, 로그인 버튼, 상태 텍스트를 제공.
/// </summary>
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
