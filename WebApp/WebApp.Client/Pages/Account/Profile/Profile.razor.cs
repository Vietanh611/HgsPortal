using Hgs.Share.Requests.Users;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Network;
using CustomToastService = WebApp.Client.Services.Components.ToastService;

namespace WebApp.Client.Pages.Account.Profile;

public partial class Profile : ComponentBase
{
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    private UsersGetByIdResponse? currentUser;
    private UsersUpdateRequest profileForm = new();
    private UsersChangePasswordRequest changePasswordForm = new();
    private bool isLoading = true;
    private bool isSubmitting = false;
    private bool isChangingPassword = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentUser();
    }

    private async Task LoadCurrentUser()
    {
        isLoading = true;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<UsersGetByIdResponse>>("api/users/me");
            if (response != null && response.Success && response.Data != null)
            {
                currentUser = response.Data;
                profileForm = new UsersUpdateRequest
                {
                    Email = currentUser.Email,
                    FullName = currentUser.FullName,
                    BravoId = currentUser.BravoId,
                    PhoneNumber = currentUser.PhoneNumber,
                    OrganizationUnitId = currentUser.OrganizationUnitId,
                    IsActive = currentUser.IsActive
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading current user: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleUpdateProfile()
    {
        isSubmitting = true;
        try
        {
            if (currentUser != null)
            {
                var success = await ApiClient.PutAsync($"api/users/{currentUser.Id}", profileForm);
                if (success)
                {
                    ToastService.ShowSuccess("Profile updated successfully");
                    await LoadCurrentUser();
                }
                else
                {
                    ToastService.ShowError("Failed to update profile");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error updating profile: {ex.Message}");
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
            if (currentUser != null)
            {
                var success = await ApiClient.PutAsync($"api/users/{currentUser.Id}/changepassword", changePasswordForm);
                if (success)
                {
                    ToastService.ShowSuccess("Password changed successfully");
                    changePasswordForm = new UsersChangePasswordRequest();
                }
                else
                {
                    ToastService.ShowError("Failed to change password");
                }
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
}
