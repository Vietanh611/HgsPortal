using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApp.Client.Services.Data;

/// <summary>
/// Trích xuất thông tin danh tính từ access token JWT ngay tại client (không cần gọi API),
/// dùng khi xây dựng AuthenticationState cho Blazor.
/// </summary>
public class JwtTokenService
{
    /// <summary>
    /// Trích user id từ claim sub (hoặc NameIdentifier); trả null khi token không hợp lệ
    /// hoặc không có claim tương ứng.
    /// </summary>
    public int? ExtractUserId(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting user ID from token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Trích danh sách role từ claim role; luôn trả về một danh sách (rỗng khi token lỗi)
    /// để caller khỏi phải xử lý null.
    /// </summary>
    public IEnumerable<string> ExtractRoles(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            return jwtToken.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting roles from token: {ex.Message}");
            return Array.Empty<string>();
        }
    }
}
