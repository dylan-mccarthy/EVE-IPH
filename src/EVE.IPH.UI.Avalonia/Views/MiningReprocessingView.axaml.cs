using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class MiningReprocessingView : UserControl
{
    public MiningReprocessingView()
    {
        InitializeComponent();
    }

    private MiningReprocessingViewModel? ViewModel => DataContext as MiningReprocessingViewModel;

    private async void Reload_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }

    private async void Calculate_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.CalculateAsync();
    }
}