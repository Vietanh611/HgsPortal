using Domain.Entities.Identity;
using Hgs.Share.Requests.Audit;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.AuditLogs;

namespace Core.Interfaces.Operations
{
    public interface IAuditLogService
    {
        /// <param name="action">Tên hành động, VD: "INSERT", "UPDATE", "DELETE", hoặc tên nghiệp vụ như "ApproveLeaveRequest"</param>
        /// <param name="entityName">Tên bảng/entity, VD: "Roles", "RoleMenus"</param>
        /// <param name="entityId">Id của bản ghi (để NULL nếu entity có khóa ghép, VD: RoleMenus)</param>
        /// <param name="oldValue">Object trạng thái cũ (sẽ được serialize sang JSON), truyền null nếu là INSERT</param>
        /// <param name="newValue">Object trạng thái mới (sẽ được serialize sang JSON), truyền null nếu là DELETE</param>
        void Log(
            string action,
            string entityName,
            int? entityId,
            object? oldValue,
            object? newValue);

        Task<(IEnumerable<AuditLogsGetAllResponse> Items, int TotalCount)> GetAllAsync(
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ghi sự kiện bảo mật (login fail, lockout, đổi quyền, ...).
        /// Khác <see cref="Log"/>: method này TỰ GỌI SaveChangesAsync — không nhờ nơi gọi.
        /// Lý do: các sự kiện bảo mật chủ yếu rơi vào nhánh fail mà service throw trước khi
        /// có bất kỳ SaveChangesAsync nào (VD: login với username không tồn tại).
        /// </summary>
        /// <param name="action">Tên sự kiện, VD: "LOGIN_FAIL_INVALID_CREDENTIALS", "ACCOUNT_LOCKED", "ROLE_ASSIGNED"</param>
        /// <param name="eventCategory">DataChange | Auth | Security | Permission</param>
        /// <param name="success">Kết quả sự kiện (login fail → false)</param>
        /// <param name="severity">Info | Warning | Critical</param>
        /// <param name="userId">Người thực hiện (để null → lấy từ HTTP context)</param>
        /// <param name="targetUserId">User bị tác động (khác người thực hiện)</param>
        /// <param name="username">Tên đăng nhập (denormalize — sống sót khi UserId null)</param>
        /// <param name="entityName">Tên entity liên quan (VD: "Roles", "AuditLogs")</param>
        /// <param name="entityId">Id bản ghi liên quan (int? — giữ nguyên convention hiện có)</param>
        /// <param name="detail">Mô tả ngắn gọn bằng chữ thường (không JSON)</param>
        /// <param name="oldValue">Object trạng thái cũ (sẽ serialize sang JSON), null nếu không có</param>
        /// <param name="newValue">Object trạng thái mới (sẽ serialize sang JSON), null nếu không có</param>
        Task LogSecurityEventAsync(
            string action,
            string eventCategory,
            bool success,
            string severity,
            int? userId = null,
            int? targetUserId = null,
            string? username = null,
            string? entityName = null,
            int? entityId = null,
            string? detail = null,
            object? oldValue = null,
            object? newValue = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lọc audit log theo nhiều chiều (entity/user/thời gian/sự kiện).
        /// Clamp PageNumber ≥ 1, PageSize ∈ [1, 200] — chống DoS qua [FromQuery].
        /// </summary>
        Task<PagedResponse<AuditLogsGetAllResponse>> GetFilteredAsync(
            AuditLogsFilterRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Đếm số bản ghi khớp filter (dùng cho pre-check 50.000 dòng trước export).
        /// </summary>
        Task<long> CountAsync(
            AuditLogsFilterRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy toàn bộ bản ghi khớp filter (KHÔNG phân trang), có cap cứng 50.000 dòng —
        /// dùng cho export. Phòng race: dữ liệu tăng giữa lúc count và lúc query.
        /// </summary>
        Task<List<AuditLogs>> GetAllFilteredAsync(
            AuditLogsFilterRequest request,
            CancellationToken cancellationToken = default);
    }
}
