using System.Windows;
using System.Windows.Input;
using AccountVault.ViewModels;

namespace AccountVault.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private bool _completed;

    public LoginWindow()
    {
        InitializeComponent();
        _viewModel = new LoginViewModel(App.VaultService);
        DataContext = _viewModel;
        Loaded += (_, _) => MasterPasswordBox.Focus();
    }

    private void UnlockButton_Click(object sender, RoutedEventArgs e)
    {
        TryUnlock();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryUnlock();
        }
    }

    private void TryUnlock()
    {
        if (!_viewModel.Unlock(MasterPasswordBox.Password) || _viewModel.Session is null)
        {
            MasterPasswordBox.Clear();
            MasterPasswordBox.Focus();
            return;
        }

        try
        {
            App.OpenMainWindow(_viewModel.Session);
            _completed = true;
            Close();
        }
        catch (Exception ex)
        {
            _viewModel.Session.Clear();
            App.ShowError("AccountVault 오류", "메인 화면을 여는 중 오류가 발생했습니다.", ex);
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        if (!_completed && !App.IsNavigating)
        {
            Application.Current.Shutdown();
        }
    }
}
