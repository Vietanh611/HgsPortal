namespace Core.Services.Settings;

public class LockoutSettings
{
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}