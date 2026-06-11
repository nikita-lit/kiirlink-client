namespace KiirLink.Services;

public interface IConnectivityService
{
    bool IsOnline { get; }
    event EventHandler<bool>? ConnectivityChanged;
}

public interface INavigationService
{
    Task GoToAsync(string route);
}

public interface IDialogService
{
    Task AlertAsync(string title, string message, string cancel = "OK");
}
