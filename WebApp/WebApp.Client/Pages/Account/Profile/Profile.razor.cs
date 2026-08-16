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
    private bool isUploadingAvatar = false;

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
                    ToastService.ShowSuccess("Đã cập nhật hồ sơ");
                    await LoadCurrentUser();
                }
                else
                {
                    ToastService.ShowError("Không thể cập nhật hồ sơ");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Lỗi khi cập nhật hồ sơ: {ex.Message}");
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
                    ToastService.ShowSuccess("Đã đổi mật khẩu");
                    changePasswordForm = new UsersChangePasswordRequest();
                }
                else
                {
                    ToastService.ShowError("Không thể đổi mật khẩu");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Lỗi khi đổi mật khẩu: {ex.Message}");
        }
        finally
        {
            isChangingPassword = false;
        }
    }

    private async Task HandleAvatarSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file is null || file.Size == 0)
        {
            ToastService.ShowError("Vui lòng chọn tệp ảnh.");
            return;
        }

        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (string.IsNullOrWhiteSpace(extension) || !allowed.Contains(extension))
        {
            ToastService.ShowError("Định dạng ảnh không được hỗ trợ. Chỉ chấp nhận JPG, PNG, WEBP, GIF.");
            return;
        }

        if (file.Size > 2 * 1024 * 1024)
        {
            ToastService.ShowError("Kích thước ảnh vượt quá giới hạn 2MB.");
            return;
        }

        isUploadingAvatar = true;
        try
        {
            if (currentUser == null)
            {
                return;
            }

            await using var stream = file.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024);
            var response = await ApiClient.PostFileAsync<ApiResponse<UsersUpdateResponse>>(
                $"api/users/{currentUser.Id}/avatar",
                stream,
                file.Name,
                file.ContentType);

            if (response != null && response.Success && response.Data != null)
            {
                currentUser.AvatarUrl = response.Data.AvatarUrl;
                ToastService.ShowSuccess("Đã cập nhật ảnh đại diện");
                StateHasChanged();
            }
            else
            {
                ToastService.ShowError(ApiClient.LastError ?? "Không thể tải lên ảnh đại diện");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Lỗi khi tải lên ảnh đại diện: {ex.Message}");
        }
        finally
        {
            isUploadingAvatar = false;
        }
    }
}
