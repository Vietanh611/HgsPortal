namespace Core.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Tạo JWT access token ký HMAC-SHA256 bằng JwtSettings:Secret; role được nhúng thành claim
    /// ClaimTypes.Role để API thực thi RBAC. Ném InvalidOperationException khi secret chưa cấu hình
    /// — chủ động fail-fast thay vì ký bằng khóa rỗng.
    /// </summary>
    string GenerateAccessToken(int userId, string username, IEnumerable<string>? roles = null, int? expiryMinutes = null);

    /// <summary>
    /// Refresh token 64 bytes ngẫu nhiên từ crypto RNG (512 bit entropy), Base64 — cần entropy cao
    /// vì DB chỉ lưu dạng hash nên không thể phục hồi giá trị gốc.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Băm SHA-256 refresh token để DB chỉ lưu hash — không bao giờ lưu token dạng plaintext
    /// (chống lộ token khi DB bị xem hoặc đánh cắp).
    /// </summary>
    string HashRefreshToken(string token);

    Guid GenerateTokenFamily();

    /// <summary>
    /// ApiKey thiết bị: 32 bytes ngẫu nhiên, Base64 — chuẩn cho header X-Device-Key.
    /// </summary>
    string GenerateDeviceKey();

    /// <summary>
    /// Băm SHA-256 device key — DB chỉ lưu hash, không lưu khóa thiết bị dạng plaintext.
    /// </summary>
    string HashDeviceKey(string deviceKey);

    /// <summary>
    /// Mã pairing thiết bị: 8 ký tự alphanumeric viết hoa, loại bỏ 0/O/1/I để tránh nhầm lẫn
    /// khi người dùng nhập bằng tay.
    /// </summary>
    string GeneratePairingCode();

    /// <summary>
    /// Băm SHA-256 mã pairing — DB chỉ lưu hash để chống lộ mã khi DB bị xem.
    /// </summary>
    string HashPairingCode(string pairingCode);
}
