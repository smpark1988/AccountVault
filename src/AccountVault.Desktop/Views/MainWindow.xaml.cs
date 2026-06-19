using System.Windows;
using AccountVault.Models;
using AccountVault.Services;
using AccountVault.ViewModels;
using Microsoft.Win32;

namespace AccountVault.Views;

public partial class MainWindow : Window
{
    private readonly AutoLockService _autoLockService = new();
    private readonly MainViewModel _viewModel;
    private bool _returningToLogin;

    public MainWindow(VaultSession session)
    {
        InitializeComponent();

        _viewModel = new MainViewModel(
            App.VaultService,
            App.ClipboardService,
            session,
            ShowAccountEditor,
            Confirm,
            ShowMessage,
            LockAndReturnToLogin);

        DataContext = _viewModel;
        Loaded += (_, _) => _autoLockService.Start(this, TimeSpan.FromMinutes(5), LockAndReturnToLogin);
    }

    private AccountItem? ShowAccountEditor(AccountItem? account)
    {
        var window = new AccountEditWindow(account)
        {
            Owner = this
        };

        return window.ShowDialog() == true ? window.ResultAccount : null;
    }

    private bool Confirm(string title, string message)
    {
        return MessageBox.Show(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
               == MessageBoxResult.Yes;
    }

    private void ShowMessage(string title, string message)
    {
        MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "백업 내보내기",
            Filter = "AccountVault 백업 (*.dat)|*.dat|모든 파일 (*.*)|*.*",
            FileName = $"AccountVault-backup-{DateTime.Now:yyyyMMdd-HHmmss}.dat"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.ExportBackup(dialog.FileName);
        }
    }

    private void ImportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "백업 가져오기",
            Filter = "AccountVault 백업 (*.dat)|*.dat|모든 파일 (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var prompt = new PasswordPromptWindow("백업 파일의 마스터 비밀번호를 입력하세요.")
        {
            Owner = this
        };

        if (prompt.ShowDialog() != true)
        {
            return;
        }

        if (_viewModel.ImportBackup(dialog.FileName, prompt.Password))
        {
            MessageBox.Show(this, "백업을 가져왔습니다. 다시 로그인하세요.", "백업 가져오기",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LockAndReturnToLogin();
        }
    }

    private void LockAndReturnToLogin()
    {
        if (_returningToLogin)
        {
            return;
        }

        _returningToLogin = true;
        _autoLockService.Stop();
        _viewModel.ClearSensitiveData();
        App.ReturnToEntryWindow(this);
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _autoLockService.Dispose();
        _viewModel.ClearSensitiveData();

        if (!_returningToLogin && !App.IsNavigating)
        {
            Application.Current.Shutdown();
        }
    }
}
