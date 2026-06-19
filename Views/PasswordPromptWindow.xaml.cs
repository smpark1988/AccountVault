using System.Windows;
using System.Windows.Input;

namespace AccountVault.Views;

public partial class PasswordPromptWindow : Window
{
    public PasswordPromptWindow(string message)
    {
        InitializeComponent();
        MessageTextBlock.Text = message;
        Loaded += (_, _) => PasswordBox.Focus();
    }

    public string Password { get; private set; } = string.Empty;

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Complete();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Complete();
        }
    }

    private void Complete()
    {
        Password = PasswordBox.Password;
        DialogResult = true;
        Close();
    }
}
