using KiirLink.ViewModels;

namespace KiirLink;

public partial class MainPage : ContentPage
{
    public MainPage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
