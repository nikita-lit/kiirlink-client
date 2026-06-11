using System.Windows.Input;
using KiirLink.Services;

namespace KiirLink.ViewModels;

public abstract class AuthViewModelBase(
    IAuthService auth,
    IConnectivityService connectivity,
    INavigationService navigation,
    IDialogService dialogs) : ViewModelBase(connectivity)
{
    private string _email = string.Empty;
    private string _password = string.Empty;

    protected IAuthService Auth { get; } = auth;
    protected INavigationService Navigation { get; } = navigation;
    protected IDialogService Dialogs { get; } = dialogs;

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

    public async Task CheckAuthenticationAsync()
    {
        if (await Auth.IsAuthenticatedAsync())
            await Navigation.GoToAsync("//Home");
    }

    protected async Task<bool> HasCredentialsAsync()
    {
        if (!string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password))
            return true;

        await Dialogs.AlertAsync(L("MissingDetails"), L("EnterEmailPassword"));
        return false;
    }

    protected async Task RunAuthAsync(string failureTitle, Func<Task> action)
    {
        await RunBusyAsync(action);
        if (HasError)
            await Dialogs.AlertAsync(L(failureTitle), ErrorMessage!);
    }

    protected Task ShowAuthErrorAsync(string title, string? error, string fallback) =>
        Dialogs.AlertAsync(L(title), LocalizationManager.Instance.LocalizeAuthError(error ?? L(fallback)));

    protected static string L(string key) => LocalizationManager.Instance.Get(key);
}

public sealed class SignInViewModel : AuthViewModelBase
{
    public SignInViewModel(IAuthService auth, IConnectivityService connectivity, INavigationService navigation,
        IDialogService dialogs) : base(auth, connectivity, navigation, dialogs)
    {
        SignInCommand = new AsyncCommand(SignInAsync, () => CanInteract);
        CreateAccountCommand = new AsyncCommand(() => navigation.GoToAsync("//CreateAccount"));
        TrackCanInteract(SignInCommand);
    }

    public AsyncCommand SignInCommand { get; }
    public ICommand CreateAccountCommand { get; }

    private async Task SignInAsync()
    {
        if (!await HasCredentialsAsync())
            return;

        await RunAuthAsync("SignInFailed", async () =>
        {
            var login = await Auth.LoginAsync(Email.Trim(), Password);
            if (!login.Success)
            {
                await ShowAuthErrorAsync("SignInFailed", login.Error, "CouldNotSignIn");
                return;
            }

            Email = string.Empty;
            Password = string.Empty;
            await Navigation.GoToAsync("//Home");
        });
    }
}

public sealed class CreateAccountViewModel : AuthViewModelBase
{
    private string _confirmPassword = string.Empty;

    public CreateAccountViewModel(IAuthService auth, IConnectivityService connectivity,
        INavigationService navigation, IDialogService dialogs) : base(auth, connectivity, navigation, dialogs)
    {
        CreateAccountCommand = new AsyncCommand(CreateAccountAsync, () => CanInteract);
        BackCommand = new AsyncCommand(() => navigation.GoToAsync("//SignIn"));
        TrackCanInteract(CreateAccountCommand);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public AsyncCommand CreateAccountCommand { get; }
    public ICommand BackCommand { get; }

    private async Task CreateAccountAsync()
    {
        if (!await HasCredentialsAsync())
            return;

        if (Password != ConfirmPassword)
        {
            await Dialogs.AlertAsync(L("PasswordConfirmationMismatch"), L("CheckConfirmationPassword"));
            return;
        }

        await RunAuthAsync("CreateAccountFailed", async () =>
        {
            var register = await Auth.RegisterAsync(Email.Trim(), Password);
            if (!register.Success)
            {
                await ShowAuthErrorAsync("CreateAccountFailed", register.Error, "CouldNotCreateAccount");
                return;
            }

            var login = await Auth.LoginAsync(Email.Trim(), Password);
            if (!login.Success)
            {
                await Dialogs.AlertAsync(L("AccountCreated"), L("AutomaticSignInFailed"));
                await Navigation.GoToAsync("//SignIn");
                return;
            }

            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            await Navigation.GoToAsync("//Home");
        });
    }
}
