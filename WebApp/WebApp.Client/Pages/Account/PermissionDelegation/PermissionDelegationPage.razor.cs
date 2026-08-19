using BlazorBootstrap;
using Hgs.Share.Requests.PermissionDelegation;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.PermissionDelegation;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Components;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Network;
using CustomToastService = WebApp.Client.Services.Components.ToastService;

namespace WebApp.Client.Pages.Account.PermissionDelegation;

public partial class PermissionDelegationPage : AuthorizedPageBase
{
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    private AssignPermissionModal assignPermissionModal = default!;
    private IEnumerable<ManageableUserResponse>? manageableUsers;
    private IEnumerable<AssignableRoleResponse>? assignableRoles;
    public List<RoleInfo> userRoles = new();
    public List<MenuInfo> userMenus = new();
    private List<int> selectedRoleIds = new();
    private int selectedUserId = 0;
    private string selectedUsername = string.Empty;
    private bool isLoading = true;
    private bool isLoadingPermissions = false;
    private bool isSavingPermissions = false;
    private string? errorMessage;

    protected override async Task OnInitializedAuthorizedAsync()
    {
        await LoadManageableUsers();
        await LoadAssignableRoles();
    }

    private async Task LoadManageableUsers()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<ManageableUserResponse>>>("api/permissiondelegation/manageable-users");
            if (response != null && response.Success && response.Data != null)
            {
                manageableUsers = response.Data;
            }
            else
            {
                manageableUsers = Enumerable.Empty<ManageableUserResponse>();
                errorMessage = !string.IsNullOrWhiteSpace(ApiClient.LastError)
                    ? ApiClient.LastError
                    : "Không thể tải danh sách nhân viên.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading manageable users: {ex.Message}");
            manageableUsers = Enumerable.Empty<ManageableUserResponse>();
            errorMessage = "Không thể tải danh sách nhân viên. Vui lòng thử lại.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadAssignableRoles()
    {
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<AssignableRoleResponse>>>("api/permissiondelegation/assignable-roles");
            if (response != null && response.Success && response.Data != null)
            {
                assignableRoles = response.Data;
            }
            else
            {
                assignableRoles = Enumerable.Empty<AssignableRoleResponse>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading assignable roles: {ex.Message}");
            assignableRoles = Enumerable.Empty<AssignableRoleResponse>();
        }
    }

    private async Task LoadUserRoles(int userId)
    {
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<UserEffectivePermissionsResponse>>($"api/permissiondelegation/user/{userId}/effective-permissions");
            if (response != null && response.Success && response.Data != null)
            {
                userRoles = response.Data.Roles;
                userMenus = response.Data.Menus;
            }
            else
            {
                userRoles = new List<RoleInfo>();
                userMenus = new List<MenuInfo>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user roles: {ex.Message}");
            userRoles = new List<RoleInfo>();
            userMenus = new List<MenuInfo>();
        }
    }

    private async Task ShowAssignPermissionModal(ManageableUserResponse user)
    {
        selectedUserId = user.Id;
        selectedUsername = user.Username;
        isLoadingPermissions = true;
        try
        {
            await LoadUserRoles(user.Id);
            var assignableRoleIds = (assignableRoles ?? Enumerable.Empty<AssignableRoleResponse>())
                .Select(r => r.Id)
                .ToHashSet();
            selectedRoleIds = userRoles
                .Where(r => assignableRoleIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToList();
            await assignPermissionModal.ShowAsync();
        }
        finally
        {
            isLoadingPermissions = false;
        }
    }

    private void OnPermissionCheckboxChanged(int roleId, string? value)
    {
        if (bool.TryParse(value, out var isChecked))
        {
            if (isChecked)
            {
                if (!selectedRoleIds.Contains(roleId))
                {
                    selectedRoleIds.Add(roleId);
                }
            }
            else
            {
                selectedRoleIds.Remove(roleId);
            }
            StateHasChanged();
        }
    }

    private async Task HandleAssignPermissions()
    {
        var request = new AssignRolesRequest
        {
            TargetUserId = selectedUserId,
            RoleIds = selectedRoleIds
        };

        isSavingPermissions = true;
        try
        {
            var success = await ApiClient.PostAsync("api/permissiondelegation/assign-roles", request);
            if (success)
            {
                ToastService.ShowSuccess("Đã cập nhật phân quyền");
                await LoadUserRoles(selectedUserId);
                await assignPermissionModal.HideAsync();
            }
            else
            {
                var message = !string.IsNullOrEmpty(ApiClient.LastError)
                    ? $"Không thể cập nhật phân quyền: {ApiClient.LastError}"
                    : "Không thể cập nhật phân quyền";
                ToastService.ShowError(message);
            }
        }
        finally
        {
            isSavingPermissions = false;
        }
    }

    private async Task CloseAssignPermissionModal()
    {
        await assignPermissionModal.HideAsync();
        selectedUserId = 0;
        selectedUsername = string.Empty;
        selectedRoleIds = new List<int>();
        userRoles = new List<RoleInfo>();
        userMenus = new List<MenuInfo>();
    }
}