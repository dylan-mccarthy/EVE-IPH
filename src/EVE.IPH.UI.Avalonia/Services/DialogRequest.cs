namespace EVE.IPH.UI.Avalonia.Services;

public sealed record DialogRequest(
    string Title,
    string Message,
    IReadOnlyList<string>? Details = null,
    string PrimaryButtonText = "OK",
    string SecondaryButtonText = "Cancel");