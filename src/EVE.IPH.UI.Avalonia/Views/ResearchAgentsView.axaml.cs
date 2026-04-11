using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class ResearchAgentsView : UserControl
{
    public ResearchAgentsView()
    {
        InitializeComponent();
    }

    private ResearchAgentsViewModel? ViewModel => DataContext as ResearchAgentsViewModel;

    private async void RefreshResearchAgents_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }
}