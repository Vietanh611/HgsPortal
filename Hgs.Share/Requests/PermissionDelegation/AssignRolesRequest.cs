namespace Hgs.Share.Requests.PermissionDelegation;

/// <summary>
/// Tập đầy đủ role sẽ được ủy quyền cho user đích — danh sách gửi lên là trạng thái mong muốn
/// (replace semantics): role nằm trong danh sách mà user chưa có sẽ được gán, role đang giữ mà không
/// còn trong danh sách sẽ bị thu hồi (chỉ trong phạm vi role gán được của người thao tác).
/// </summary>
public class AssignRolesRequest
{
    public int TargetUserId { get; set; }
    public List<int> RoleIds { get; set; } = new();
}