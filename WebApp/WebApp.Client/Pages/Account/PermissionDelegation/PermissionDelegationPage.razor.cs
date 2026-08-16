using BlazorBootstrap;
using Hgs.Share.Requests.PermissionDelegation;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.PermissionDelegation;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Network;
using CustomToastService = WebApp.Client.Services.Components.ToastService;
using AssignRoleDialog = WebApp.Client.Pages.Account.PermissionDelegation.AssignRoleDialog;
using RevokeRoleDialog = WebApp.Client.Pages.Account.PermissionDelegation.RevokeRoleDialog;

namespace WebApp.Client.Pages.Account.PermissionDelegation;

public partial class PermissionDelegationPage : ComponentBase
{
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    private AssignRoleDialog assignRoleDialog = default!;
    private RevokeRoleDialog revokeRoleDialog = default!;
    private IEnumerable<ManageableUserResponse>? manageableUsers;
    private IEnumerable<AssignableRoleResponse>? assignableRoles;
    public List<RoleInfo> userRoles = new();
    private int selectedUserId = 0;
    private string selectedUsername = string.Empty;
    private bool isLoading = true;
    private string? errorMessage;
    private bool isPermissionsViewVisible = false;

    protected override async Task OnInitializedAsync()
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
            }
            else
            {
                userRoles = new List<RoleInfo>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user roles: {ex.Message}");
            userRoles = new List<RoleInfo>();
        }
    }

    private void ShowPermissionsView(ManageableUserResponse user)
    {
        selectedUserId = user.Id;
        selectedUsername = user.Username;
        isPermissionsViewVisible = true;
        _ = LoadUserRoles(user.Id);
    }

    private void ClosePermissionsView()
    {
        isPermissionsViewVisible = false;
        selectedUserId = 0;
        selectedUsername = string.Empty;
        userRoles = new List<RoleInfo>();
    }

    private async Task ShowAssignRoleDialog()
    {
        await assignRoleDialog.ShowAsync();
    }

    private async Task CloseAssignRoleDialog()
    {
        await assignRoleDialog.HideAsync();
    }

    private async Task HandleAssignRole(int roleId)
    {
        var request = new AssignRoleRequest
        {
            TargetUserId = selectedUserId,
            RoleId = roleId
        };

        var success = await ApiClient.PostAsync("api/permissiondelegation/assign-role", request);
        if (success)
        {
            ToastService.ShowSuccess("Đã gán quyền");
            await LoadUserRoles(selectedUserId);
            await assignRoleDialog.HideAsync();
        }
        else
        {
            var errorMessage = !string.IsNullOrEmpty(ApiClient.LastError) 
                ? $"Không thể gán quyền: {ApiClient.LastError}" 
                : "Không thể gán quyền";
            ToastService.ShowError(errorMessage);
        }
    }

    private async Task ShowRevokeRoleDialog()
    {
        await revokeRoleDialog.ShowAsync();
    }

    private async Task CloseRevokeRoleDialog()
    {
        await revokeRoleDialog.HideAsync();
    }

    private async Task HandleRevokeRole(int roleId)
    {
        var request = new RevokeRoleRequest
        {
            TargetUserId = selectedUserId,
            RoleId = roleId
        };

        var success = await ApiClient.PostAsync("api/permissiondelegation/revoke-role", request);
        if (success)
        {
            ToastService.ShowSuccess("Đã gỡ quyền");
            await LoadUserRoles(selectedUserId);
            await revokeRoleDialog.HideAsync();
        }
        else
        {
            var errorMessage = !string.IsNullOrEmpty(ApiClient.LastError) 
                ? $"Không thể gỡ quyền: {ApiClient.LastError}" 
                : "Không thể gỡ quyền";
            ToastService.ShowError(errorMessage);
        }
    }
}
