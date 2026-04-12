using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class StructureFacilityManagementView : UserControl
{
    public StructureFacilityManagementView()
    {
        InitializeComponent();
    }

    private StructureFacilityManagementViewModel? ViewModel => DataContext as StructureFacilityManagementViewModel;

    private async void Reload_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }

    private async void SaveStructure_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.SaveStructureAsync();
    }

    private async void DeleteStructure_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.DeleteSelectedStructureAsync();
    }

    private void UseSelectedStructure_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel?.UseSelectedStructureForFacility();
    }

    private async void SaveFacility_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.SaveFacilityAsync();
    }

    private async void DeleteFacility_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.DeleteFacilityAsync();
    }
}