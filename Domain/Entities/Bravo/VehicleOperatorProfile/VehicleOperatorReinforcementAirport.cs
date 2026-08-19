namespace Domain.Entities.Bravo.VehicleOperatorProfile
{
    /// <summary>
    /// Mục 13) Cảng hàng không được tăng cường (nếu có) — bảng con của AirsideDriverProfile.
    /// Không áp dụng đối với nhân viên thuộc cơ quan quản lý nhà nước hoạt động
    /// thường xuyên tại cảng hàng không.
    /// </summary>
    public class VehicleOperatorReinforcementAirport
    {
        public int Id { get; set; }
        public int VehicleOperatorProfileId { get; set; }
        // Cảng hàng không được tăng cường
        public string? Airport { get; set; }

        // Nghiệp vụ được tăng cường
        public string? AssignedSkill { get; set; }

        // Từ ngày / Đến ngày
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public VehicleOperatorProfile? VehicleOperatorProfile { get; set; }
    }
}