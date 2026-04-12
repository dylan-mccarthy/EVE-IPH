using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class ManufacturingWorkspaceView : UserControl
{
    public ManufacturingWorkspaceView()
    {
        InitializeComponent();
    }

    private ManufacturingWorkspaceViewModel? ViewModel => DataContext as ManufacturingWorkspaceViewModel;

    private async void ReloadWorkspace_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }

    private async void RunAnalysis_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.AnalyzeAsync();
    }
}