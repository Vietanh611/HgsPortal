using System.IdentityModel.Tokens.Jwt;

namespace WebApp.Client.Services;

public class JwtTokenService
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public int? ExtractUserId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decoding JWT token: {ex.Message}");
        }

        return null;
    }
}
