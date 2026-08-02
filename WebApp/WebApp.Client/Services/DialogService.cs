namespace WebApp.Client.Services;

using BlazorBootstrap;

public class DialogService
{
    private ConfirmDialog? _confirmDialog;

    public void SetConfirmDialog(ConfirmDialog confirmDialog)
    {
        _confirmDialog = confirmDialog;
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, ConfirmDialogOptions? options = null)
    {
        if (_confirmDialog == null)
            throw new InvalidOperationException("ConfirmDialog is not initialized. Call SetConfirmDialog first.");
        
        return await _confirmDialog.ShowAsync(title, message, options);
    }

    public async Task<bool> ShowConfirmAsync(string title, string message1, string message2, ConfirmDialogOptions? options = null)
    {
        if (_confirmDialog == null)
            throw new InvalidOperationException("ConfirmDialog is not initialized. Call SetConfirmDialog first.");
        
        return await _confirmDialog.ShowAsync(title, message1, message2, options);
    }

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
