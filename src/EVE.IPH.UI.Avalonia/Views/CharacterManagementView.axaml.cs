using Avalonia.Controls;
using EVE.IPH.UI.Avalonia.ViewModels;

namespace EVE.IPH.UI.Avalonia.Views;

public partial class CharacterManagementView : UserControl
{
    public CharacterManagementView()
    {
        InitializeComponent();
    }

    private CharacterManagementViewModel? ViewModel => DataContext as CharacterManagementViewModel;

    private async void ConnectCharacter_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.ConnectCharacterAsync();
    }

    private async void RefreshSelectedCharacter_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshSelectedCharacterAsync();
    }

    private async void ConnectCorporation_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.ConnectCorporationFromSelectedCharacterAsync();
    }

    private async void SetDefaultCharacter_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.SetSelectedCharacterDefaultAsync();
    }

    private async void DeleteSelectedCharacter_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.DeleteSelectedCharacterAsync();
    }

    private async void RefreshSelectedCorporation_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RefreshSelectedCorporationAsync();
    }

    private async void DeleteSelectedCorporation_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.DeleteSelectedCorporationAsync();
    }
}