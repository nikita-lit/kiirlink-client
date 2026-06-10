using System.Windows.Input;
using KiirLink.Services;

namespace KiirLink.ViewModels;

public sealed class SignInViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private string _email = string.Empty;
    private string _password = string.Empty;

    public SignInViewModel(IAuthService auth, IConnectivityService connectivity, INavigationService navigation,
        IDialogService dialogs) : base(connectivity)
    {
        _auth = auth;
        _navigation = navigation;
        _dialogs = dialogs;
        SignInCommand = new AsyncCommand(SignInAsync, () => CanInteract);
        CreateAccountCommand = new AsyncCommand(() => navigation.GoToAsync("//CreateAccount"));
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(CanInteract))
                SignInCommand.RaiseCanExecuteChanged();
        };
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public AsyncCommand SignInCommand { get; }
    public ICommand CreateAccountCommand { get; }

    public async Task CheckAuthenticationAsync()
    {
        if (await _auth.IsAuthenticatedAsync())
            await _navigation.GoToAsync("//Home");
    }

    private async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await _dialogs.AlertAsync("Missing details", "Enter an email and password.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            var login = await _auth.LoginAsync(Email.Trim(), Password);
            if (!login.Success)
            {
                await _dialogs.AlertAsync("Sign in failed", login.Error ?? "Could not sign in.");
                return;
            }

            Email = string.Empty;
            Password = string.Empty;
            await _navigation.GoToAsync("//Home");
        });

        if (HasError)
            await _dialogs.AlertAsync("Sign in failed", ErrorMessage!);
    }
}

public sealed class CreateAccountViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    public CreateAccountViewModel(IAuthService auth, IConnectivityService connectivity,
        INavigationService navigation, IDialogService dialogs) : base(connectivity)
    {
        _auth = auth;
        _navigation = navigation;
        _dialogs = dialogs;
        CreateAccountCommand = new AsyncCommand(CreateAccountAsync, () => CanInteract);
        BackCommand = new AsyncCommand(() => navigation.GoToAsync("//SignIn"));
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(CanInteract))
                CreateAccountCommand.RaiseCanExecuteChanged();
        };
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public AsyncCommand CreateAccountCommand { get; }
    public ICommand BackCommand { get; }

    public async Task CheckAuthenticationAsync()
    {
        if (await _auth.IsAuthenticatedAsync())
            await _navigation.GoToAsync("//Home");
    }

    private async Task CreateAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await _dialogs.AlertAsync("Missing details", "Enter an email and password.");
            return;
        }

        if (Password != ConfirmPassword)
        {
            await _dialogs.AlertAsync("Passwords do not match", "Check the confirmation password and try again.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            var register = await _auth.RegisterAsync(Email.Trim(), Password);
            if (!register.Success)
            {
                await _dialogs.AlertAsync("Create account failed",
                    register.Error ?? "Could not create the account.");
                return;
            }

            var login = await _auth.LoginAsync(Email.Trim(), Password);
            if (!login.Success)
            {
                await _dialogs.AlertAsync("Account created",
                    "Your account was created, but automatic sign in failed. Please sign in.");
                await _navigation.GoToAsync("//SignIn");
                return;
            }

            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            await _navigation.GoToAsync("//Home");
        });

        if (HasError)
            await _dialogs.AlertAsync("Create account failed", ErrorMessage!);
    }
}
