using Hgs.Share.Requests.Menus;
using Hgs.Share.Requests.RoleMenus;
using Hgs.Share.Requests.Roles;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Menus;
using Hgs.Share.Responses.OrganizationUnits;
using Hgs.Share.Responses.Roles;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services;
using BlazorBootstrap;
using CustomToastService = WebApp.Client.Services.ToastService;

namespace WebApp.Client.Pages.Account.Roles;

public partial class Roles
{
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    private IEnumerable<RolesGetAllResponse>? roles;
    private IEnumerable<OrganizationUnitsGetAllResponse>? organizationUnits;
    private IEnumerable<MenusGetAllResponse>? menus;
    private List<int> selectedMenuIds = new();
    private HashSet<int> expandedMenuIds = new();
    private RolesCreateRequest roleForm = new();
    private string assignMenuRoleName = string.Empty;
    private int assignMenuRoleId = 0;
    private bool isLoading = true;
    private bool isLoadingMenus = false;
    private bool isEditMode = false;
    private bool isSubmitting = false;
    private bool isAssigningMenus = false;
    private int editingRoleId = 0;
    private bool isRoleFormModalVisible = false;
    private bool isAssignMenuModalVisible = false;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadRoles(), LoadOrganizationUnits());
    }

    private async Task LoadRoles()
    {
        isLoading = true;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<RolesGetAllResponse>>>("api/roles");
            if (response != null && response.Success && response.Data != null)
            {
                roles = response.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading roles: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadOrganizationUnits()
    {
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<OrganizationUnitsGetAllResponse>>>("api/organizationunits");
            if (response != null && response.Success && response.Data != null)
            {
                organizationUnits = response.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading organization units: {ex.Message}");
        }
    }

    private async Task LoadMenus()
    {
        isLoadingMenus = true;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<MenusGetAllResponse>>>("api/menus");
            if (response != null && response.Success && response.Data != null)
            {
                menus = response.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading menus: {ex.Message}");
        }
        finally
        {
            isLoadingMenus = false;
        }
    }

    private async Task LoadRoleMenus(int roleId)
    {
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<Hgs.Share.Responses.RoleMenus.RoleMenusGetAllResponse>>>($"api/rolemenus/by-role/{roleId}");
            if (response != null && response.Success && response.Data != null)
            {
                selectedMenuIds = response.Data.Select(rm => rm.MenuId).ToList();
            }
            else
            {
                selectedMenuIds = new List<int>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading role menus: {ex.Message}");
            selectedMenuIds = new List<int>();
        }
    }

    private void ShowCreateModal()
    {
        isEditMode = false;
        roleForm = new RolesCreateRequest();
        isRoleFormModalVisible = true;
    }

    private void ShowEditModal(RolesGetAllResponse role)
    {
        isEditMode = true;
        editingRoleId = role.Id;
        roleForm = new RolesCreateRequest
        {
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            OrganizationUnitId = role.OrganizationUnitId,
            DataScope = role.DataScope,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive
        };
        isRoleFormModalVisible = true;
    }

    private void CloseRoleFormModal()
    {
        isRoleFormModalVisible = false;
    }

    private async Task ShowAssignMenuModal(RolesGetAllResponse role)
    {
        assignMenuRoleId = role.Id;
        assignMenuRoleName = role.Name;
        selectedMenuIds = new List<int>();
        await Task.WhenAll(LoadMenus(), LoadRoleMenus(role.Id));
        isAssignMenuModalVisible = true;
    }

    private void CloseAssignMenuModal()
    {
        isAssignMenuModalVisible = false;
    }

    private void OnMenuCheckboxChanged(int menuId, string? value)
    {
        if (bool.TryParse(value, out var isChecked))
        {
            if (isChecked && !selectedMenuIds.Contains(menuId))
            {
                selectedMenuIds.Add(menuId);
            }
            else if (!isChecked && selectedMenuIds.Contains(menuId))
            {
                selectedMenuIds.Remove(menuId);
            }
        }
    }

    private void HandleMenuCheckboxChanged(int menuId, string? value)
    {
        OnMenuCheckboxChanged(menuId, value);
    }

    private void ToggleMenu(int menuId)
    {
        if (expandedMenuIds.Contains(menuId))
        {
            expandedMenuIds.Remove(menuId);
        }
        else
        {
            expandedMenuIds.Add(menuId);
        }
    }

    private void HandleToggleMenu(int menuId)
    {
        ToggleMenu(menuId);
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        try
        {
            if (isEditMode)
            {
                var updateRequest = new RolesUpdateRequest
                {
                    Code = roleForm.Code,
                    Name = roleForm.Name,
                    Description = roleForm.Description,
                    OrganizationUnitId = roleForm.OrganizationUnitId,
                    DataScope = roleForm.DataScope,
                    IsSystemRole = roleForm.IsSystemRole,
                    IsActive = roleForm.IsActive
                };
                var success = await ApiClient.PutAsync($"api/roles/{editingRoleId}", updateRequest);
                if (success)
                {
                    ToastService.ShowSuccess("Role updated successfully");
                    await LoadRoles();
                    CloseRoleFormModal();
                }
                else
                {
                    ToastService.ShowError("Failed to update role");
                }
            }
            else
            {
                var success = await ApiClient.PostAsync("api/roles", roleForm);
                if (success)
                {
                    ToastService.ShowSuccess("Role created successfully");
                    await LoadRoles();
                    CloseRoleFormModal();
                }
                else
                {
                    ToastService.ShowError("Failed to create role");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error saving role: {ex.Message}");
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task HandleAssignMenus()
    {
        isAssigningMenus = true;
        try
        {
            var request = new RoleMenusAssignMultipleRequest
            {
                RoleId = assignMenuRoleId,
                MenuIds = selectedMenuIds
            };
            var success = await ApiClient.PostAsync("api/rolemenus/assign-multiple", request);
            if (success)
            {
                ToastService.ShowSuccess("Menus assigned successfully");
                CloseAssignMenuModal();
            }
            else
            {
                ToastService.ShowError("Failed to assign menus");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error assigning menus: {ex.Message}");
        }
        finally
        {
            isAssigningMenus = false;
        }
    }

    private async Task DeleteRole(int id)
    {
        var confirmation = await DialogService.ShowDeleteConfirmAsync("this role");
        
        if (confirmation)
        {
            try
            {
                var success = await ApiClient.DeleteAsync($"api/roles/{id}");
                if (success)
                {
                    ToastService.ShowSuccess("Role deleted successfully");
                    await LoadRoles();
                }
                else
                {
                    ToastService.ShowError("Failed to delete role");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Error deleting role: {ex.Message}");
            }
        }
    }
}
