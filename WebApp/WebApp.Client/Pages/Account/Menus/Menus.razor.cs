using BlazorBootstrap;
using Hgs.Share.Requests.Menus;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Menus;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Network;
using CustomToastService = WebApp.Client.Services.Components.ToastService;

namespace WebApp.Client.Pages.Account.Menus;

public partial class Menus
{
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    private MenuFormModal menuFormModal = default!;
    private IEnumerable<MenusGetAllResponse>? menus;
    private IEnumerable<MenusGetAllResponse>? parentMenus;
    private MenusCreateRequest menuForm = new();
    private bool isLoading = true;
    private bool isEditMode = false;
    private bool isSubmitting = false;
    private int editingMenuId = 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadMenus();
    }

    private async Task LoadMenus()
    {
        isLoading = true;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<MenusGetAllResponse>>>("api/menus/all");
            if (response != null && response.Success && response.Data != null)
            {
                menus = response.Data;
                parentMenus = response.Data.Where(m => m.ParentId == null).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading menus: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task ShowCreateModal()
    {
        isEditMode = false;
        menuForm = new MenusCreateRequest();
        await menuFormModal.ShowAsync();
    }

    private async Task ShowEditModal(MenusGetAllResponse menu)
    {
        isEditMode = true;
        editingMenuId = menu.Id;
        menuForm = new MenusCreateRequest
        {
            ParentId = menu.ParentId,
            Code = menu.Code,
            Name = menu.Name,
            Route = menu.Route,
            Component = menu.Component,
            Icon = menu.Icon,
            SortOrder = menu.SortOrder,
            IsVisible = menu.IsVisible,
            IsActive = menu.IsActive
        };
        await menuFormModal.ShowAsync();
    }

    private async Task CloseMenuFormModal()
    {
        await menuFormModal.HideAsync();
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        try
        {
            if (isEditMode)
            {
                var updateRequest = new MenusUpdateRequest
                {
                    ParentId = menuForm.ParentId,
                    Code = menuForm.Code,
                    Name = menuForm.Name,
                    Route = menuForm.Route,
                    Component = menuForm.Component,
                    Icon = menuForm.Icon,
                    SortOrder = menuForm.SortOrder,
                    IsVisible = menuForm.IsVisible,
                    IsActive = menuForm.IsActive
                };
                var success = await ApiClient.PutAsync($"api/menus/{editingMenuId}", updateRequest);
                if (success)
                {
                    ToastService.ShowSuccess("Menu updated successfully");
                    await LoadMenus();
                    await CloseMenuFormModal();
                }
                else
                {
                    ToastService.ShowError("Failed to update menu");
                }
            }
            else
            {
                var success = await ApiClient.PostAsync("api/menus", menuForm);
                if (success)
                {
                    ToastService.ShowSuccess("Menu created successfully");
                    await LoadMenus();
                    await CloseMenuFormModal();
                }
                else
                {
                    ToastService.ShowError("Failed to create menu");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error saving menu: {ex.Message}");
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task DeleteMenu(int id)
    {
        var confirmation = await DialogService.ShowDeleteConfirmAsync("this menu");

        if (confirmation)
        {
            try
            {
                var success = await ApiClient.DeleteAsync($"api/menus/{id}");
                if (success)
                {
                    ToastService.ShowSuccess("Menu deleted successfully");
                    await LoadMenus();
                }
                else
                {
                    ToastService.ShowError("Failed to delete menu");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Error deleting menu: {ex.Message}");
            }
        }
    }
}
