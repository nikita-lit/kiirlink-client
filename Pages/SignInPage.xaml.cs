using KiirLink.ViewModels;

namespace KiirLink.Pages;

public partial class SignInPage
{
    private readonly SignInViewModel _viewModel;

    public SignInPage(SignInViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.CheckAuthenticationAsync();
    }
}
