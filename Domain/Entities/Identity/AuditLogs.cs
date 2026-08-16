namespace Domain.Entities.Identity
{
    public class AuditLogs
    {
        public long Id { get; set; }

        public int? UserId { get; set; }

        public int? TargetUserId { get; set; }

        public string? Username { get; set; }

        public string EventCategory { get; set; } = "DataChange";

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public bool Success { get; set; } = true;

        public string Severity { get; set; } = "Info";

        public string? IpAddress { get; set; }

        public string? CorrelationId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public Users? User { get; set; }
        public Users? TargetUser { get; set; }
    }
}