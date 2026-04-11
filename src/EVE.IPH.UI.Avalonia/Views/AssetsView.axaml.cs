using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class AssetsView : UserControl
{
    public AssetsView()
    {
        InitializeComponent();
    }

    private AssetsViewModel? ViewModel => DataContext as AssetsViewModel;

    private async void RefreshAssets_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }
}