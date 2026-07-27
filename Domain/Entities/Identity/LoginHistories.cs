namespace Domain.Entities.Identity
{
    public class LoginHistories
    {
        public long Id { get; set; }

        public int UserId { get; set; }

        public bool IsSuccess { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public string? FailReason { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public Users User { get; set; } = null!;
    }
}
