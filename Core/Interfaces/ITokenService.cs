namespace Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(int userId, string username, IEnumerable<string>? roles = null, int? expiryMinutes = null);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
    Guid GenerateTokenFamily();
    string GenerateDeviceKey();
    string HashDeviceKey(string deviceKey);
    string GeneratePairingCode();
    string HashPairingCode(string pairingCode);
}
