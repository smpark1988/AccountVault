using System.IO;
using System.Windows;
using System.Windows.Threading;
using AccountVault.Services;
using AccountVault.Views;

namespace AccountVault;

public partial class App : Application
{
    public static CryptoService CryptoService { get; } = new();
    public static VaultService VaultService { get; } = new(CryptoService);
    public static ClipboardService ClipboardService { get; } = new();

    public static bool IsNavigating { get; private set; }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        ShowEntryWindow();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        ClipboardService.Dispose();
    }

    public static void ShowEntryWindow()
    {
        Window window = VaultService.VaultExists()
            ? new LoginWindow()
            : new SetupWindow();

        Current.MainWindow = window;
        window.Show();
    }

    public static void OpenMainWindow(VaultSession session)
    {
        var window = new MainWindow(session);
        Current.MainWindow = window;
        window.Show();
    }

    public static void ReturnToEntryWindow(Window currentWindow)
    {
        IsNavigating = true;

        Window nextWindow = VaultService.VaultExists()
            ? new LoginWindow()
            : new SetupWindow();

        Current.MainWindow = nextWindow;
        nextWindow.Show();
        currentWindow.Close();

        IsNavigating = false;
    }

    public static void ShowError(string title, string message, Exception? exception = null)
    {
        if (exception is not null)
        {
            WriteErrorLog(exception);
        }

        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteErrorLog(e.Exception);
        e.Handled = true;
        MessageBox.Show(
            "예상하지 못한 오류가 발생했습니다. 자세한 내용은 data/error.log 파일에 기록했습니다.",
            "AccountVault 오류",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            WriteErrorLog(exception);
        }
    }

    private static void WriteErrorLog(Exception exception)
    {
        try
        {
            var dataDirectory = VaultService.GetDataDirectory();
            Directory.CreateDirectory(dataDirectory);
            var logPath = Path.Combine(dataDirectory, "error.log");
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(logPath, message);
        }
        catch
        {
            // Logging must never cause another UI failure.
        }
    }
}
