using Hgs.Share.Requests.Menus;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Menus;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services;
using BlazorBootstrap;
using CustomToastService = WebApp.Client.Services.ToastService;

namespace WebApp.Client.Pages.Account.Menus;

public partial class Menus
{
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    private IEnumerable<MenusGetAllResponse>? menus;
    private IEnumerable<MenusGetAllResponse>? parentMenus;
    private MenusCreateRequest menuForm = new();
    private bool isLoading = true;
    private bool isEditMode = false;
    private bool isSubmitting = false;
    private int editingMenuId = 0;
    private bool isMenuFormModalVisible = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadMenus();
    }

    private async Task LoadMenus()
    {
        isLoading = true;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<MenusGetAllResponse>>>("api/menus");
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

    private void ShowCreateModal()
    {
        isEditMode = false;
        menuForm = new MenusCreateRequest();
        isMenuFormModalVisible = true;
    }

    private void ShowEditModal(MenusGetAllResponse menu)
    {
        isEditMode = true;
        editingMenuId = menu.Id;
        menuForm = new MenusCreateRequest
        {
            ModuleId = menu.ModuleId,
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
        isMenuFormModalVisible = true;
    }

    private void CloseMenuFormModal()
    {
        isMenuFormModalVisible = false;
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
                    ModuleId = menuForm.ModuleId,
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
                    CloseMenuFormModal();
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
                    CloseMenuFormModal();
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
