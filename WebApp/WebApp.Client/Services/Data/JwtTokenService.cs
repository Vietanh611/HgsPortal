using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApp.Client.Services.Data;

public class JwtTokenService
{
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
