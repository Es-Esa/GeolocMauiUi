using ClientApp.ViewModels;

namespace ClientApp.Views;

public partial class PictureDetectionPage : ContentPage
{
    private readonly PictureDetectionViewModel _viewModel;

    public PictureDetectionPage(PictureDetectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
