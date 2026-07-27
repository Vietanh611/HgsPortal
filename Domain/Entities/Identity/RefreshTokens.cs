namespace Domain.Entities.Identity
{
    public class RefreshTokens
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Token { get; set; } = string.Empty;

        public string JwtId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? CreatedByIp { get; set; }

        public string? ReplacedByToken { get; set; }

        // Navigation
        public Users User { get; set; } = null!;
    }
}
