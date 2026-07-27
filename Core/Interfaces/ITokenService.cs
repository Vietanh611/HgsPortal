namespace Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(int userId, string username, IEnumerable<string>? roles = null);
    string GenerateRefreshToken();
}
