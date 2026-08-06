using Domain.Entities.Identity;
using Hgs.Share.Responses.AuditLogs;

namespace Core.Interfaces
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
    }
}
