using AccountVault.Mobile.Services;
using AccountVault.Models;
using AccountVault.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AccountVault.Mobile.Pages;

public sealed class AccountListPage : ContentPage
{
    private readonly IServiceProvider _services;
    private readonly VaultService _vaultService;
    private readonly SearchBar _searchBar;
    private readonly CollectionView _collectionView;

    public AccountListPage(IServiceProvider services)
    {
        _services = services;
        _vaultService = services.GetRequiredService<VaultService>();
        Title = "AccountVault";

        _searchBar = new SearchBar { Placeholder = "사이트명, URL, 아이디 검색" };
        _searchBar.TextChanged += (_, _) => RefreshAccounts();

        var addButton = new Button
        {
            Text = "계정 추가",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White
        };
        addButton.Clicked += async (_, _) => await Navigation.PushAsync(new AccountEditPage(_services, null));

        var exportButton = new Button { Text = "백업 내보내기" };
        exportButton.Clicked += ExportButton_Clicked;

        _collectionView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(CreateAccountTemplate)
        };
        _collectionView.SelectionChanged += CollectionView_SelectionChanged;

        var commandBar = new HorizontalStackLayout
        {
            Spacing = 8,
            Margin = new Thickness(0, 10, 0, 12),
            Children = { addButton, exportButton }
        };
        Grid.SetRow(commandBar, 1);
        Grid.SetRow(_collectionView, 2);

        Content = new Grid
        {
            Padding = new Thickness(18, 12),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            },
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        new Label
                        {
                            Text = "AccountVault",
                            FontSize = 26,
                            FontAttributes = FontAttributes.Bold
                        },
                        _searchBar
                    }
                },
                commandBar,
                _collectionView
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshAccounts();
    }

    private View CreateAccountTemplate()
    {
        var siteLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
        siteLabel.SetBinding(Label.TextProperty, nameof(AccountItem.SiteName));

        var userLabel = new Label { TextColor = Color.FromArgb("#667085"), LineBreakMode = LineBreakMode.TailTruncation };
        userLabel.SetBinding(Label.TextProperty, nameof(AccountItem.UserId));

        var urlLabel = new Label { TextColor = Color.FromArgb("#667085"), FontSize = 12, LineBreakMode = LineBreakMode.TailTruncation };
        urlLabel.SetBinding(Label.TextProperty, nameof(AccountItem.Url));

        return new Border
        {
            Stroke = Color.FromArgb("#DDE3EA"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = Colors.White,
            Padding = new Thickness(14, 12),
            Margin = new Thickness(0, 0, 0, 10),
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children = { siteLabel, userLabel, urlLabel }
            }
        };
    }

    private async void CollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not AccountItem account)
        {
            return;
        }

        _collectionView.SelectedItem = null;
        await Navigation.PushAsync(new AccountDetailPage(_services, account.Id));
    }

    private void RefreshAccounts()
    {
        var session = ((App)Application.Current!).Session;
        var accounts = session?.Data?.Accounts ?? [];
        var searchText = _searchBar.Text ?? string.Empty;

        _collectionView.ItemsSource = string.IsNullOrWhiteSpace(searchText)
            ? accounts.OrderBy(account => account.SiteName).ToList()
            : accounts
                .Where(account =>
                    Contains(account.SiteName, searchText) ||
                    Contains(account.Url, searchText) ||
                    Contains(account.UserId, searchText))
                .OrderBy(account => account.SiteName)
                .ToList();
    }

    private async void ExportButton_Clicked(object? sender, EventArgs e)
    {
        try
        {
            var sourcePath = _vaultService.GetVaultFilePath();
            var targetPath = System.IO.Path.Combine(FileSystem.CacheDirectory, $"AccountVault-backup-{DateTime.Now:yyyyMMdd-HHmmss}.dat");
            File.Copy(sourcePath, targetPath, overwrite: true);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "AccountVault 백업 내보내기",
                File = new ShareFile(targetPath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("오류", $"백업을 내보낼 수 없습니다.\n{ex.Message}", "확인");
        }
    }

    private static bool Contains(string value, string searchText)
    {
        return value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
    }
}
