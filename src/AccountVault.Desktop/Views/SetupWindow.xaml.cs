using System.Windows;
using System.Windows.Input;
using AccountVault.ViewModels;

namespace AccountVault.Views;

public partial class SetupWindow : Window
{
    private readonly SetupViewModel _viewModel;
    private bool _completed;

    public SetupWindow()
    {
        InitializeComponent();
        _viewModel = new SetupViewModel(App.VaultService);
        DataContext = _viewModel;
        Loaded += (_, _) => MasterPasswordBox.Focus();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        TryCreate();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryCreate();
        }
    }

    private void TryCreate()
    {
        if (!_viewModel.Create(MasterPasswordBox.Password, ConfirmPasswordBox.Password) ||
            _viewModel.Session is null)
        {
            MasterPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
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
