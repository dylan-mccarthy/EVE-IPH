using Avalonia.Controls;

namespace EVE.IPH.UI.Avalonia.Services;

public interface IModalDialogService
{
    Task<bool> ShowConfirmationAsync(Window owner, DialogRequest request);

    Task ShowMessageAsync(Window owner, DialogRequest request);
}