namespace Domain.Entities.Bravo.VehicleOperatorProfile
{
    /// <summary>
    /// Mục 14) Đào tạo, huấn luyện nhân viên hàng không — bảng con của AirsideDriverProfile.
    /// </summary>
    public class VehicleOperatorCertification
    {
        public int Id { get; set; }
        public int VehicleOperatorProfileId { get; set; }

        // Tên cơ sở đào tạo
        public string? TrainingProviderName { get; set; }

        // Nội dung đào tạo/Nghiệp vụ chuyên môn
        public string? TrainingContent { get; set; }

        // Từ ngày tháng, năm - đến ngày tháng, năm
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // CCCM/Chứng nhận/Thẻ nghiệp vụ
        public string? CertificateNumber { get; set; }

        // Hình thức đào tạo
        public string? TrainingFormat { get; set; }

        // Ngày cấp
        public DateTime? IssueDate { get; set; }

        // Hiệu lực
        public string? ValidityPeriod { get; set; }

        // Ghi chú
        public string? Note { get; set; }

        public VehicleOperatorProfile? VehicleOperatorProfile { get; set; }
    }
}