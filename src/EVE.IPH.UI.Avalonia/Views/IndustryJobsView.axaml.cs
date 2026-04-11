using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class IndustryJobsView : UserControl
{
    public IndustryJobsView()
    {
        InitializeComponent();
    }

    private IndustryJobsViewModel? ViewModel => DataContext as IndustryJobsViewModel;

    private async void RefreshIndustryJobs_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }
}