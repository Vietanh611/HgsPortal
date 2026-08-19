using Hgs.Share.Requests.Menus;
using Hgs.Share.Requests.RoleMenus;
using Hgs.Share.Requests.Roles;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Menus;
using Hgs.Share.Responses.OrganizationUnits;
using Hgs.Share.Responses.Roles;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Components;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Network;
using BlazorBootstrap;
using CustomToastService = WebApp.Client.Services.Components.ToastService;

namespace WebApp.Client.Pages.Account.Roles;

public partial class Roles : AuthorizedPageBase
{
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    private RoleFormModal roleFormModal = default!;
    private AssignMenuModal assignMenuModal = default!;
    private IEnumerable<RolesGetAllResponse>? roles;
    private IEnumerable<OrganizationUnitsGetAllResponse>? organizationUnits;
    private IEnumerable<MenusGetByUserIdResponse>? menus;
    private List<int> selectedMenuIds = new();
    private HashSet<int> expandedMenuIds = new();
    private RolesCreateRequest roleForm = new();
    private string assignMenuRoleName = string.Empty;
    private int assignMenuRoleId = 0;
    private bool isLoading = true;
    private string? errorMessage;
    private bool isLoadingMenus = false;
    private bool isEditMode = false;
    private bool isSubmitting = false;
    private bool isAssigningMenus = false;
    private int editingRoleId = 0;

    protected override async Task OnInitializedAuthorizedAsync()
    {
        await Task.WhenAll(LoadRoles(), LoadOrganizationUnits());
    }

    private async Task LoadRoles()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<RolesGetAllResponse>>>("api/roles");
            if (response != null && response.Success && response.Data != null)
            {
                roles = response.Data;
            }
            else
            {
                roles = null;
                errorMessage = !string.IsNullOrWhiteSpace(ApiClient.LastError)
                    ? ApiClient.LastError
                    : "Không thể tải danh sách vai trò.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading roles: {ex.Message}");
            roles = null;
            errorMessage = "Không thể tải danh sách vai trò. Vui lòng thử lại.";
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
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<OrganizationUnitsGetAllResponse>>>("api/organizationunits", silent: true);
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
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<MenusGetByUserIdResponse>>>("api/my/menus", silent: true);
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

    private async Task ShowCreateModal()
    {
        isEditMode = false;
        roleForm = new RolesCreateRequest();
        await roleFormModal.ShowAsync();
    }

    private async Task ShowEditModal(RolesGetAllResponse role)
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
        await roleFormModal.ShowAsync();
    }

    private async Task CloseRoleFormModal()
    {
        await roleFormModal.HideAsync();
    }

    private async Task ShowAssignMenuModal(RolesGetAllResponse role)
    {
        assignMenuRoleId = role.Id;
        assignMenuRoleName = role.Name;
        selectedMenuIds = new List<int>();
        expandedMenuIds = new HashSet<int>();
        await Task.WhenAll(LoadMenus(), LoadRoleMenus(role.Id));
        await assignMenuModal.ShowAsync();
    }

    private async Task CloseAssignMenuModal()
    {
        await assignMenuModal.HideAsync();
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
                    ToastService.ShowSuccess("Đã cập nhật vai trò");
                    await LoadRoles();
                    await CloseRoleFormModal();
                }
                else
                {
                    var errorMessage = !string.IsNullOrEmpty(ApiClient.LastError) 
                        ? $"Không thể cập nhật vai trò: {ApiClient.LastError}" 
                        : "Không thể cập nhật vai trò";
                    ToastService.ShowError(errorMessage);
                }
            }
            else
            {
                var success = await ApiClient.PostAsync("api/roles", roleForm);
                if (success)
                {
                    ToastService.ShowSuccess("Đã tạo vai trò");
                    await LoadRoles();
                    await CloseRoleFormModal();
                }
                else
                {
                    var errorMessage = !string.IsNullOrEmpty(ApiClient.LastError) 
                        ? $"Không thể tạo vai trò: {ApiClient.LastError}" 
                        : "Không thể tạo vai trò";
                    ToastService.ShowError(errorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Lỗi khi lưu vai trò: {ex.Message}");
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
                ToastService.ShowSuccess("Đã gán menu");
                await CloseAssignMenuModal();
            }
            else
            {
                var errorMessage = !string.IsNullOrEmpty(ApiClient.LastError) 
                    ? $"Không thể gán menu: {ApiClient.LastError}" 
                    : "Không thể gán menu";
                ToastService.ShowError(errorMessage);
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Lỗi khi gán menu: {ex.Message}");
        }
        finally
        {
            isAssigningMenus = false;
        }
    }

    private async Task DeleteRole(int id)
    {
        var confirmation = await DialogService.ShowDeleteConfirmAsync("vai trò này");
        
        if (confirmation)
        {
            try
            {
                var success = await ApiClient.DeleteAsync($"api/roles/{id}");
                if (success)
                {
                    ToastService.ShowSuccess("Đã xóa vai trò");
                    await LoadRoles();
                }
                else
                {
                    var errorMessage = !string.IsNullOrEmpty(ApiClient.LastError) 
                        ? $"Không thể xóa vai trò: {ApiClient.LastError}" 
                        : "Không thể xóa vai trò";
                    ToastService.ShowError(errorMessage);
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Lỗi khi xóa vai trò: {ex.Message}");
            }
        }
    }
}
