using System.Windows;
using AccountVault.Models;
using AccountVault.ViewModels;

namespace AccountVault.Views;

public partial class AccountEditWindow : Window
{
    private readonly AccountEditViewModel _viewModel;

    public AccountEditWindow(AccountItem? account)
    {
        InitializeComponent();
        _viewModel = new AccountEditViewModel(account);
        DataContext = _viewModel;
        PasswordBox.Password = _viewModel.Password;
        Loaded += (_, _) => FocusFirstTextBox();
    }

    public AccountItem? ResultAccount { get; private set; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBox.Password;

        if (!_viewModel.Validate())
        {
            return;
        }

        ResultAccount = _viewModel.ToAccount();
        DialogResult = true;
        Close();
    }

    private void FocusFirstTextBox()
    {
        MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
    }
}
