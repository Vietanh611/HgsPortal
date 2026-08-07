namespace WebApp.Client.Services.Components;

using BlazorBootstrap;
using BlazorBootstrapToastService = BlazorBootstrap.ToastService;

public class ToastService
{
    private readonly BlazorBootstrapToastService _toastService;

    public ToastService(BlazorBootstrapToastService toastService)
    {
        _toastService = toastService;
    }

    public void ShowSuccess(string message, string title = "Success")
    {
        _toastService.Notify(new(ToastType.Success, message, title));
    }

    public void ShowError(string message, string title = "Error")
    {
        _toastService.Notify(new(ToastType.Danger, message, title));
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        _toastService.Notify(new(ToastType.Warning, message, title));
    }

    public void ShowInfo(string message, string title = "Info")
    {
        _toastService.Notify(new(ToastType.Info, message, title));
    }
}
