using System.Windows.Input;
using TeaShopManager.DataAccess;
using TeaShopManager.Models;

namespace TeaShopManager.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly AuthenticationRepository _authenticationRepository;
    private readonly RelayCommand _loginCommand;
    private readonly RelayCommand _closeCommand;

    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public LoginViewModel(AuthenticationRepository authenticationRepository)
    {
        _authenticationRepository = authenticationRepository;
        _loginCommand = new RelayCommand(_ => ExecuteLogin(), _ => CanLogin());
        _closeCommand = new RelayCommand(_ => CloseRequested?.Invoke());
    }

    public event Action<UserSession>? AuthenticationSucceeded;

    public event Action? CloseRequested;

    public string Login
    {
        get => _login;
        set
        {
            if (SetProperty(ref _login, value))
            {
                ErrorMessage = string.Empty;
                _loginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ErrorMessage = string.Empty;
                _loginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _loginCommand.RaiseCanExecuteChanged();
                _closeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand LoginCommand => _loginCommand;

    public ICommand CloseCommand => _closeCommand;

    private bool CanLogin()
    {
        return !IsBusy
               && !string.IsNullOrWhiteSpace(Login)
               && !string.IsNullOrWhiteSpace(Password);
    }

    private void ExecuteLogin()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            var session = _authenticationRepository.Authenticate(Login.Trim(), Password);
            if (session is null)
            {
                ErrorMessage = "Неверный логин или пароль.";
                return;
            }

            AuthenticationSucceeded?.Invoke(session);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Не удалось выполнить вход. {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
