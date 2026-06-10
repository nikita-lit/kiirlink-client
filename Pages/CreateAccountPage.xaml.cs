using KiirLink.ViewModels;

namespace KiirLink.Pages;

public partial class CreateAccountPage
{
    private readonly CreateAccountViewModel _viewModel;

    public CreateAccountPage(CreateAccountViewModel viewModel)
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
