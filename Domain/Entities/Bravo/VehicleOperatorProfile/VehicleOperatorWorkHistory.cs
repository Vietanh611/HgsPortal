namespace Domain.Entities.Bravo.VehicleOperatorProfile
{
    /// <summary>
    /// Mục 15) Tóm tắt quá trình công tác — bảng con của AirsideDriverProfile.
    /// Không áp dụng đối với nhân viên thuộc cơ quan quản lý nhà nước hoạt động
    /// thường xuyên tại cảng hàng không.
    /// </summary>
    public class VehicleOperatorWorkHistory
    {
        public int Id { get; set; }
        public int VehicleOperatorProfileId { get; set; }

        // Từ tháng, năm đến tháng, năm
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Chức danh, chức vụ, đơn vị công tác, cảng hàng không làm việc
        public string? Description { get; set; }

        public VehicleOperatorProfile? VehicleOperatorProfile { get; set; }
    }
}