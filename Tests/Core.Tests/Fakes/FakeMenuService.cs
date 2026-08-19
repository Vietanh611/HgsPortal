using Core.Interfaces.Identity;
using Domain.Entities.System;
using Hgs.Share.Requests.Menus;
using Hgs.Share.Responses.Menus;

namespace Core.Tests.Fakes;

/// <summary>
/// Stub của IMenuService chỉ phục vụ GetUserIdsWithMenuCodeAsync (đường dùng của
/// NotificationService); các method khác ném NotImplementedException vì không liên quan
/// tới phạm vi test.
/// </summary>
public sealed class FakeMenuService : IMenuService
{
    public List<int> UserIdsWithMenuCode { get; } = new();

    /// <summary>Menu code cuối cùng được yêu cầu — dùng để xác minh resolve category→menu.</summary>
    public string? LastMenuCode { get; private set; }

    public Task<List<int>> GetUserIdsWithMenuCodeAsync(string menuCode, CancellationToken cancellationToken = default)
    {
        LastMenuCode = menuCode;
        return Task.FromResult(UserIdsWithMenuCode);
    }

    public Task<IEnumerable<Menus>> GetAllAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<IEnumerable<Menus>> GetAllFlatAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<Menus?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<Menus> CreateAsync(MenusCreateRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<Menus?> UpdateAsync(int id, MenusUpdateRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<List<MenusGetByUserIdResponse>> GetMenusByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<HashSet<string>> GetEffectiveMenuCodesAsync(int userId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<bool> IsSuperAdminAsync(int userId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}