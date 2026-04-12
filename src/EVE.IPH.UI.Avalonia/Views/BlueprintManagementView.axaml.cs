using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class BlueprintManagementView : UserControl
{
    public BlueprintManagementView()
    {
        InitializeComponent();
    }

    private BlueprintManagementViewModel? ViewModel => DataContext as BlueprintManagementViewModel;

    private async void ReloadBlueprints_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }

    private async void SaveBlueprint_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.SaveSelectedBlueprintAsync();
    }

    private async void DeleteBlueprint_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.DeleteSelectedBlueprintAsync();
    }
}