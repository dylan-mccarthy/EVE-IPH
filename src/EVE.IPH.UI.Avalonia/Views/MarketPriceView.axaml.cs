using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class MarketPriceView : UserControl
{
    public MarketPriceView()
    {
        InitializeComponent();
    }

    private MarketPriceViewModel? ViewModel => DataContext as MarketPriceViewModel;

    private async void ReloadDefaults_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }

    private async void BuildSavedCategories_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.BuildWatchlistFromSavedSelectionAsync();
    }

    private async void LoadPrices_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.LoadPricesAsync();
    }
}