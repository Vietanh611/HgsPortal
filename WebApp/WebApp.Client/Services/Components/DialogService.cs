namespace WebApp.Client.Services.Components;

using BlazorBootstrap;

/// <summary>
/// Facade over BlazorBootstrap's ConfirmDialog: the dialog instance is registered once
/// (typically from a layout) and components then use the typed confirm helpers.
/// </summary>
public class DialogService
{
    private ConfirmDialog? _confirmDialog;

    /// <summary>
    /// Registers the ConfirmDialog instance (typically from a layout) before any confirm
    /// can be shown.
    /// </summary>
    public void SetConfirmDialog(ConfirmDialog confirmDialog)
    {
        _confirmDialog = confirmDialog;
    }

    /// <summary>
    /// Shows a confirmation dialog and returns true if the user confirmed. Throws
    /// InvalidOperationException if no dialog was registered via SetConfirmDialog.
    /// </summary>
    public async Task<bool> ShowConfirmAsync(string title, string message, ConfirmDialogOptions? options = null)
    {
        if (_confirmDialog == null)
            throw new InvalidOperationException("ConfirmDialog is not initialized. Call SetConfirmDialog first.");
        
        return await _confirmDialog.ShowAsync(title, message, options);
    }

    /// <summary>
    /// Shows a two-message confirmation dialog and returns true if the user confirmed.
    /// Throws InvalidOperationException if no dialog was registered via SetConfirmDialog.
    /// </summary>
    public async Task<bool> ShowConfirmAsync(string title, string message1, string message2, ConfirmDialogOptions? options = null)
    {
        if (_confirmDialog == null)
            throw new InvalidOperationException("ConfirmDialog is not initialized. Call SetConfirmDialog first.");
        
        return await _confirmDialog.ShowAsync(title, message1, message2, options);
    }

    /// <summary>
    /// Shows a delete confirmation dialog with a danger-styled "Delete" button for
    /// destructive actions.
    /// </summary>
    public async Task<bool> ShowDeleteConfirmAsync(string itemName)
    {
        var options = new ConfirmDialogOptions
        {
            YesButtonText = "Delete",
            NoButtonText = "Cancel",
            YesButtonColor = ButtonColor.Danger
        };
        return await ShowConfirmAsync(
            title: "Confirm Delete",
            message: $"Are you sure you want to delete {itemName}?",
            options: options
        );
    }
}
