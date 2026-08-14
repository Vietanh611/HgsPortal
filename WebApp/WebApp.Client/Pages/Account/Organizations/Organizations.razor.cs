using Hgs.Share.Requests.OrganizationUnits;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.OrganizationUnits;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Network;
using BlazorBootstrap;
using CustomToastService = WebApp.Client.Services.Components.ToastService;

namespace WebApp.Client.Pages.Account.Organizations;

public partial class Organizations : ComponentBase
{
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    private OrganizationFormModal organizationFormModal = default!;
    private IEnumerable<OrganizationUnitsGetAllResponse>? organizations;
    private OrganizationUnitsCreateRequest organizationForm = new();
    private bool isLoading = true;
    private string? errorMessage;
    private bool isEditMode = false;
    private bool isSubmitting = false;
    private int editingOrganizationId = 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrganizations();
    }

    private async Task LoadOrganizations()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<OrganizationUnitsGetAllResponse>>>("api/organizationunits");
            if (response != null && response.Success && response.Data != null)
            {
                organizations = response.Data;
            }
            else
            {
                organizations = null;
                errorMessage = !string.IsNullOrWhiteSpace(ApiClient.LastError)
                    ? ApiClient.LastError
                    : "Unable to load organizations.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading organizations: {ex.Message}");
            organizations = null;
            errorMessage = "Unable to load organizations. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task ShowCreateModal()
    {
        isEditMode = false;
        organizationForm = new OrganizationUnitsCreateRequest();
        await organizationFormModal.ShowAsync();
    }

    private async Task ShowEditModal(OrganizationUnitsGetAllResponse organization)
    {
        isEditMode = true;
        editingOrganizationId = organization.Id;
        organizationForm = new OrganizationUnitsCreateRequest
        {
            Code = organization.Code,
            Name = organization.Name,
            ParentId = organization.ParentId,
            SortOrder = organization.SortOrder,
            IsActive = organization.IsActive
        };
        await organizationFormModal.ShowAsync();
    }

    private async Task CloseOrganizationFormModal()
    {
        await organizationFormModal.HideAsync();
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        try
        {
            if (isEditMode)
            {
                var updateRequest = new OrganizationUnitsUpdateRequest
                {
                    Code = organizationForm.Code,
                    Name = organizationForm.Name,
                    ParentId = organizationForm.ParentId,
                    SortOrder = organizationForm.SortOrder,
                    IsActive = organizationForm.IsActive
                };
                var success = await ApiClient.PutAsync($"api/organizationunits/{editingOrganizationId}", updateRequest);
                if (success)
                {
                    ToastService.ShowSuccess("Organization updated successfully");
                    await LoadOrganizations();
                    await organizationFormModal.HideAsync();
                }
                else
                {
                    ToastService.ShowError("Failed to update organization");
                }
            }
            else
            {
                var success = await ApiClient.PostAsync("api/organizationunits", organizationForm);
                if (success)
                {
                    ToastService.ShowSuccess("Organization created successfully");
                    await LoadOrganizations();
                    await organizationFormModal.HideAsync();
                }
                else
                {
                    ToastService.ShowError("Failed to create organization");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error: {ex.Message}");
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task DeleteOrganization(int id)
    {
        var success = await ApiClient.DeleteAsync($"api/organizationunits/{id}");
        if (success)
        {
            ToastService.ShowSuccess("Organization deleted successfully");
            await LoadOrganizations();
        }
        else
        {
            ToastService.ShowError("Failed to delete organization");
        }
    }
}
