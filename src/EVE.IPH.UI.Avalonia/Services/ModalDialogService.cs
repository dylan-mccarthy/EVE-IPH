using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace EVE.IPH.UI.Avalonia.Services;

public sealed class ModalDialogService : IModalDialogService
{
    public async Task<bool> ShowConfirmationAsync(Window owner, DialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(request);

        Window dialog = CreateDialogWindow(request.Title, BuildConfirmationContent(request));

        Button primaryButton = dialog.FindControl<Button>("DialogPrimaryButton")
            ?? throw new InvalidOperationException("Dialog primary button was not created.");
        Button secondaryButton = dialog.FindControl<Button>("DialogSecondaryButton")
            ?? throw new InvalidOperationException("Dialog secondary button was not created.");

        primaryButton.Click += (_, _) => dialog.Close(true);
        secondaryButton.Click += (_, _) => dialog.Close(false);

        return await dialog.ShowDialog<bool>(owner);
    }

    public async Task ShowMessageAsync(Window owner, DialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(request);

        Window dialog = CreateDialogWindow(request.Title, BuildMessageContent(request));

        Button primaryButton = dialog.FindControl<Button>("DialogPrimaryButton")
            ?? throw new InvalidOperationException("Dialog primary button was not created.");

        primaryButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
    }

    private static Window CreateDialogWindow(string title, Control content) => new()
    {
        Title = title,
        Width = 560,
        Height = 280,
        CanResize = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Content = content,
    };

    private static Control BuildConfirmationContent(DialogRequest request)
    {
        StackPanel buttonPanel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 12,
            Children =
            {
                new Button
                {
                    Name = "DialogSecondaryButton",
                    Content = request.SecondaryButtonText,
                    MinWidth = 96,
                },
                new Button
                {
                    Name = "DialogPrimaryButton",
                    Content = request.PrimaryButtonText,
                    MinWidth = 140,
                },
            },
        };

        return BuildDialogContent(request, buttonPanel);
    }

    private static Control BuildMessageContent(DialogRequest request)
    {
        StackPanel buttonPanel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children =
            {
                new Button
                {
                    Name = "DialogPrimaryButton",
                    Content = request.PrimaryButtonText,
                    MinWidth = 96,
                },
            },
        };

        return BuildDialogContent(request, buttonPanel);
    }

    private static Control BuildDialogContent(DialogRequest request, Control buttonPanel)
    {
        StackPanel contentPanel = new()
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = request.Message,
                    FontWeight = FontWeight.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                },
            },
        };

        if (request.Details is not null)
        {
            foreach (string detail in request.Details.Where(static detail => !string.IsNullOrWhiteSpace(detail)))
            {
                contentPanel.Children.Add(new TextBlock
                {
                    Text = detail,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.85,
                });
            }
        }

        contentPanel.Children.Add(buttonPanel);

        return new Border
        {
            Padding = new Thickness(20),
            Child = contentPanel,
        };
    }
}