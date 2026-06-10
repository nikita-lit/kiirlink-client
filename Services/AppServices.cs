namespace KiirLink.Services;

public sealed class ConnectivityService : IConnectivityService
{
    public ConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, e.NetworkAccess == NetworkAccess.Internet);
    }
}

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);
}

public sealed class DialogService : IDialogService
{
    public Task AlertAsync(string title, string message, string cancel = "OK")
    {
        var page = Shell.Current?.CurrentPage
                   ?? Application.Current?.Windows.FirstOrDefault()?.Page;

        return page?.DisplayAlertAsync(title, message, cancel) ?? Task.CompletedTask;
    }
}
