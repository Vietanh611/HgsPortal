namespace Hgs.Share.Exceptions;

/// <summary>
/// Tài khoản tạm thời bị khóa do đăng nhập sai quá số lần cho phép (lockout).
/// ErrorCode "ACCOUNT_LOCKED" cho client nhận biết trạng thái này và hiển thị
/// thông báo riêng trên màn hình đăng nhập thay vì thông báo sai chung chung.
/// </summary>
public class AccountLockedException : UnauthorizedException
{
    public AccountLockedException(string message) : base(message, "ACCOUNT_LOCKED")
    {
    }
}
