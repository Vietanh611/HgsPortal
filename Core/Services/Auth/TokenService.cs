using Core.Interfaces;
using Core.Services.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Core.Services.Auth;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateAccessToken(int userId, string username, IEnumerable<string>? roles = null, int? expiryMinutes = null)
    {
        if (string.IsNullOrWhiteSpace(_jwtSettings.Secret))
        {
            throw new InvalidOperationException("JWT secret is not configured. Set JwtSettings:Secret in configuration.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (roles is not null)
        {
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes ?? _jwtSettings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>ApiKey thiết bị: 32 bytes ngẫu nhiên, mã hoá Base64 (chuẩn cho X-Device-Key header).</summary>
    public string GenerateDeviceKey()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashDeviceKey(string deviceKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(deviceKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>Mã pairing thiết bị: 8 ký tự alphanumeric viết hoa, loại bỏ 0/O/1/I để tránh nhầm khi đọc.</summary>
    public string GeneratePairingCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var randomBytes = new byte[8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[randomBytes[i] % alphabet.Length];
        }

        return new string(chars);
    }

    public string HashPairingCode(string pairingCode)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(pairingCode);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public string HashRefreshToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public Guid GenerateTokenFamily()
    {
        return Guid.NewGuid();
    }
}
