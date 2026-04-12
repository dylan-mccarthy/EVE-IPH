using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class ShoppingListView : UserControl
{
    public ShoppingListView()
    {
        InitializeComponent();
    }

    private ShoppingListViewModel? ViewModel => DataContext as ShoppingListViewModel;

    private async void Reload_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }

    private async void Clear_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.ClearAsync();
    }

    private async void RemoveItem_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not Button button)
        {
            return;
        }

        long? typeId = button.Tag switch
        {
            long longValue => longValue,
            int intValue => intValue,
            string text when long.TryParse(text, out long parsed) => parsed,
            _ => null,
        };

        if (!typeId.HasValue)
        {
            return;
        }

        await ViewModel.RemoveItemAsync(typeId.Value);
    }
}