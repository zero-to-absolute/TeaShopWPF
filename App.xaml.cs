using System.Configuration;
using System.Windows;
using TeaShopManager.DataAccess;
using TeaShopManager.Models;
using TeaShopManager.ViewModels;
using TeaShopManager.Views;

namespace TeaShopManager;

public partial class App : Application
{
    private AuthenticationRepository? _authenticationRepository;
    private TeaShopRepository? _teaShopRepository;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show(
                "В App.config не найдена строка подключения DefaultConnection.",
                "TeaShopManager",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Shutdown();
            return;
        }

        _authenticationRepository = new AuthenticationRepository(connectionString);
        _teaShopRepository = new TeaShopRepository(connectionString);
        ShowLoginWindow();
    }

    private void ShowLoginWindow()
    {
        if (_authenticationRepository is null)
        {
            Shutdown();
            return;
        }

        var loginWindow = CreateLoginWindow();
        MainWindow = loginWindow;
        loginWindow.Show();
    }

    private LoginWindow CreateLoginWindow()
    {
        var loginViewModel = new LoginViewModel(_authenticationRepository!);
        var loginWindow = new LoginWindow
        {
            DataContext = loginViewModel
        };

        loginViewModel.AuthenticationSucceeded += session => OpenMainWindow(loginWindow, session);
        loginViewModel.CloseRequested += () => loginWindow.Close();

        return loginWindow;
    }

    private void OpenMainWindow(Window loginWindow, UserSession session)
    {
        if (_teaShopRepository is null)
        {
            MessageBox.Show("Не удалось инициализировать рабочий репозиторий.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var mainViewModel = new ShellViewModel(session, _teaShopRepository);
        try
        {
            mainViewModel.LoadData();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Не удалось загрузить данные приложения. {exception.Message}", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        mainViewModel.SignOutRequested += () => ReturnToLogin(mainWindow);

        MainWindow = mainWindow;
        mainWindow.Show();
        loginWindow.Close();
    }

    private void ReturnToLogin(Window currentWindow)
    {
        var loginWindow = CreateLoginWindow();
        MainWindow = loginWindow;
        loginWindow.Show();
        currentWindow.Close();
    }
}
