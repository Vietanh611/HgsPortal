namespace Domain.Entities.Identity
{
    public class AuditLogs
    {
        public long Id { get; set; }

        public int? UserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public string? IpAddress { get; set; }

        public string? CorrelationId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public Users? User { get; set; }
    }
}
