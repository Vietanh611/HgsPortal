namespace Core.Services.Settings;

public class DeviceAuthSettings
{
    /// <summary>Thiết bị được xem là ONLINE nếu LastSeenAt trong khoảng này (phút).</summary>
    public int OnlineThresholdMinutes { get; set; } = 2;

    /// <summary>Thời gian hết hạn mã pairing thiết bị (phút). Mặc định 10 phút theo SPEC-Device-Auth.</summary>
    public int PairingCodeTtlMinutes { get; set; } = 10;
}