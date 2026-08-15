using BlazorBootstrap;
using Hgs.Share.Requests.UserMenus;
using Hgs.Share.Requests.UserRoles;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.OrganizationUnits;
using Hgs.Share.Responses.Roles;
using Hgs.Share.Responses.UserRoles;
using Hgs.Share.Responses.Users;
using Hgs.Share.Responses.Menus;
using Hgs.Share.Responses.UserMenus;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Network;
using CustomToastService = WebApp.Client.Services.Components.ToastService;

namespace WebApp.Client.Pages.Account.Users;

public partial class Users
{
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    private UserFormModal userFormModal = default!;
    private ChangePasswordModal changePasswordModal = default!;
    private AssignMenuModal assignMenuModal = default!;
    private AssignRoleModal assignRoleModal = default!;
    private IEnumerable<UsersGetAllResponse>? users;
    private IEnumerable<OrganizationUnitsGetAllResponse>? organizationUnits;
    private IEnumerable<MenusGetAllResponse>? menus;
    private IEnumerable<RolesGetAllResponse>? roles;
    private List<int> assignedMenuIds = new();
    private List<int> selectedMenuIds = new();
    private List<int> roleMenuIds = new();
    private List<int> assignedRoleIds = new();
    private List<int> selectedRoleIds = new();
    private HashSet<int> expandedMenuIds = new();
    private UsersCreateRequest userForm = new();
    private UsersChangePasswordRequest changePasswordForm = new();
    private string changePasswordUsername = string.Empty;
    private int changePasswordUserId = 0;
    private string assignMenuUsername = string.Empty;
    private int assignMenuUserId = 0;
    private string assignRoleUsername = string.Empty;
    private int assignRoleUserId = 0;
    private bool isLoading = true;
    private string? errorMessage;
    private bool isLoadingMenus = false;
    private bool isLoadingRoles = false;
    private bool isEditMode = false;
    private bool isSubmitting = false;
    private bool isChangingPassword = false;
    private bool isAssigningMenus = false;
    private bool isAssigningRoles = false;
    private int editingUserId = 0;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadUsers(), LoadOrganizationUnits());
    }

    private async Task LoadUsers()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<UsersGetAllResponse>>>("api/users");
            if (response != null && response.Success && response.Data != null)
            {
                users = response.Data;
            }
            else
            {
                users = null;
                errorMessage = !string.IsNullOrWhiteSpace(ApiClient.LastError)
                    ? ApiClient.LastError
                    : "Unable to load users.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading users: {ex.Message}");
            users = null;
            errorMessage = "Unable to load users. Please try again.";
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

    private async Task LoadUserMenus(int userId)
    {
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<UserMenuAssignmentDetailsResponse>>($"api/usermenus/user/{userId}/details");
            if (response != null && response.Success && response.Data != null)
            {
                roleMenuIds = response.Data.RoleMenuIds ?? new List<int>();
                assignedMenuIds = response.Data.UserMenuIds ?? new List<int>();
                selectedMenuIds = new List<int>(assignedMenuIds);
            }
            else
            {
                roleMenuIds = new List<int>();
                assignedMenuIds = new List<int>();
                selectedMenuIds = new List<int>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user menus: {ex.Message}");
            roleMenuIds = new List<int>();
            assignedMenuIds = new List<int>();
            selectedMenuIds = new List<int>();
        }
    }

    private async Task LoadRoles()
    {
        isLoadingRoles = true;
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
            isLoadingRoles = false;
        }
    }

    private async Task LoadUserRoles(int userId)
    {
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<UserRolesGetAllResponse>>>($"api/userroles/by-user/{userId}");
            if (response != null && response.Success && response.Data != null)
            {
                assignedRoleIds = response.Data.Select(ur => ur.RoleId).ToList();
                selectedRoleIds = new List<int>(assignedRoleIds);
            }
            else
            {
                assignedRoleIds = new List<int>();
                selectedRoleIds = new List<int>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user roles: {ex.Message}");
            assignedRoleIds = new List<int>();
            selectedRoleIds = new List<int>();
        }
    }

    private async Task ShowCreateModal()
    {
        isEditMode = false;
        userForm = new UsersCreateRequest();
        await userFormModal.ShowAsync();
    }

    private async Task ShowEditModal(UsersGetAllResponse user)
    {
        isEditMode = true;
        editingUserId = user.Id;
        userForm = new UsersCreateRequest
        {
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            BravoId = user.BravoId,
            PhoneNumber = user.PhoneNumber,
            OrganizationUnitId = user.OrganizationUnitId,
            IsActive = user.IsActive
        };
        await userFormModal.ShowAsync();
    }

    private async Task ShowChangePasswordModal(UsersGetAllResponse user)
    {
        changePasswordUserId = user.Id;
        changePasswordUsername = user.Username;
        changePasswordForm = new UsersChangePasswordRequest();
        await changePasswordModal.ShowAsync();
    }

    private async Task CloseUserFormModal()
    {
        await userFormModal.HideAsync();
    }

    private async Task CloseChangePasswordModal()
    {
        await changePasswordModal.HideAsync();
    }

    private async Task CloseAssignMenuModal()
    {
        await assignMenuModal.HideAsync();
    }

    private async Task CloseAssignRoleModal()
    {
        await assignRoleModal.HideAsync();
    }

    private async Task ShowAssignMenuModal(UsersGetAllResponse user)
    {
        assignMenuUserId = user.Id;
        assignMenuUsername = user.Username;
        selectedMenuIds = new List<int>();
        await Task.WhenAll(LoadMenus(), LoadUserMenus(user.Id));
        await assignMenuModal.ShowAsync();
    }

    private async Task ShowAssignRoleModal(UsersGetAllResponse user)
    {
        assignRoleUserId = user.Id;
        assignRoleUsername = user.Username;
        selectedRoleIds = new List<int>();
        await Task.WhenAll(LoadRoles(), LoadUserRoles(user.Id));
        await assignRoleModal.ShowAsync();
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

    private async Task HandleMenuCheckboxChanged(int menuId, string? value)
    {
        OnMenuCheckboxChanged(menuId, value);
    }

    private void OnRoleCheckboxChanged(int roleId, string? value)
    {
        if (bool.TryParse(value, out var isChecked))
        {
            if (isChecked && !selectedRoleIds.Contains(roleId))
            {
                selectedRoleIds.Add(roleId);
            }
            else if (!isChecked && selectedRoleIds.Contains(roleId))
            {
                selectedRoleIds.Remove(roleId);
            }
        }
    }

    private void HandleRoleCheckboxChanged(int roleId, string? value)
    {
        OnRoleCheckboxChanged(roleId, value);
    }

    private bool IsMenuAssigned(int menuId)
    {
        return selectedMenuIds.Contains(menuId);
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

    private async Task HandleToggleMenu(int menuId)
    {
        ToggleMenu(menuId);
    }

    private bool IsMenuExpanded(int menuId)
    {
        return expandedMenuIds.Contains(menuId);
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        try
        {
            if (isEditMode)
            {
                var updateRequest = new UsersUpdateRequest
                {
                    Email = userForm.Email,
                    FullName = userForm.FullName,
                    BravoId = userForm.BravoId,
                    PhoneNumber = userForm.PhoneNumber,
                    OrganizationUnitId = userForm.OrganizationUnitId,
                    IsActive = userForm.IsActive
                };
                var success = await ApiClient.PutAsync($"api/users/{editingUserId}", updateRequest);
                if (success)
                {
                    ToastService.ShowSuccess("User updated successfully");
                    await LoadUsers();
                    await CloseUserFormModal();
                }
                else
                {
                    ToastService.ShowError("Failed to update user");
                }
            }
            else
            {
                var success = await ApiClient.PostAsync("api/users", userForm);
                if (success)
                {
                    ToastService.ShowSuccess("User created successfully");
                    await LoadUsers();
                    await CloseUserFormModal();
                }
                else
                {
                    ToastService.ShowError("Failed to create user");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error saving user: {ex.Message}");
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task HandleChangePassword()
    {
        isChangingPassword = true;
        try
        {
            var success = await ApiClient.PutAsync($"api/users/{changePasswordUserId}/changepassword", changePasswordForm);
            if (success)
            {
                ToastService.ShowSuccess("Password changed successfully");
                await CloseChangePasswordModal();
            }
            else
            {
                ToastService.ShowError("Failed to change password");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error changing password: {ex.Message}");
        }
        finally
        {
            isChangingPassword = false;
        }
    }

    private async Task HandleAssignMenus()
    {
        isAssigningMenus = true;
        try
        {
            var toAdd = selectedMenuIds.Except(assignedMenuIds).ToList();
            var toRemove = assignedMenuIds.Except(selectedMenuIds).ToList();

            if (toAdd.Any())
            {
                var addRequest = new UserMenusAssignMultipleRequest
                {
                    UserId = assignMenuUserId,
                    MenuIds = toAdd
                };
                var addSuccess = await ApiClient.PostAsync("api/usermenus/assign-multiple", addRequest);
                if (!addSuccess)
                {
                    ToastService.ShowError("Failed to assign menus");
                    return;
                }
            }

            if (toRemove.Any())
            {
                var removeRequest = new UserMenusAssignMultipleRequest
                {
                    UserId = assignMenuUserId,
                    MenuIds = toRemove
                };
                var removeSuccess = await ApiClient.PostAsync("api/usermenus/remove-multiple", removeRequest);
                if (!removeSuccess)
                {
                    ToastService.ShowError("Failed to remove menus");
                    return;
                }
            }

            ToastService.ShowSuccess("Menus assigned successfully");
            await CloseAssignMenuModal();
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

    private async Task HandleAssignRoles()
    {
        isAssigningRoles = true;
        try
        {
            var request = new UserRolesAssignMultipleRequest
            {
                UserId = assignRoleUserId,
                RoleIds = selectedRoleIds
            };
            var success = await ApiClient.PostAsync("api/userroles/assign-multiple", request);
            if (success)
            {
                ToastService.ShowSuccess("Roles assigned successfully");
                await CloseAssignRoleModal();
            }
            else
            {
                ToastService.ShowError("Failed to assign roles");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error assigning roles: {ex.Message}");
        }
        finally
        {
            isAssigningRoles = false;
        }
    }

    private async Task DeleteUser(int id)
    {
        var confirmation = await DialogService.ShowDeleteConfirmAsync("this user");

        if (confirmation)
        {
            try
            {
                var success = await ApiClient.DeleteAsync($"api/users/{id}");
                if (success)
                {
                    ToastService.ShowSuccess("User deleted successfully");
                    await LoadUsers();
                }
                else
                {
                    ToastService.ShowError("Failed to delete user");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Error deleting user: {ex.Message}");
            }
        }
    }
}
