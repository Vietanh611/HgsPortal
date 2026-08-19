namespace Domain.Entities.Bravo.VehicleOperatorProfile
{
    /// <summary>
    /// Lý lịch nhân viên điều khiển phương tiện, vận hành thiết bị tại sân bay.
    /// Nguồn dữ liệu: database Bravo (đọc, không ghi từ HGS Portal).
    /// </summary>
    public class VehicleOperatorProfile
    {
        public int Id { get; set; }

        // 1) Họ và tên
        public string FullName { get; set; } = string.Empty;

        // 2) Sinh ngày
        public DateTime? DateOfBirth { get; set; }

        // 3) Giới tính (nam, nữ)
        public string? Gender { get; set; }

        // 4) Số căn cước công dân / Ngày cấp / Nơi cấp
        public string? IdentityNumber { get; set; }
        public DateTime? IdentityIssueDate { get; set; }
        public string? IdentityIssuePlace { get; set; }

        // Ảnh 3x4
        public string? PhotoUrl { get; set; }

        // 5) Ngày tuyển dụng — không áp dụng cho NV cơ quan QLNN hoạt động thường xuyên tại CHK
        public DateTime? RecruitmentDate { get; set; }

        // 6) Doanh nghiệp/cơ quan quản lý nhân viên
        public string? ManagingOrganization { get; set; }

        // 7) Chức danh nhân viên hàng không
        public string? AviationJobTitle { get; set; }

        // 8) Phòng/ ban/ tổ/ đội
        public string? Department { get; set; }

        // 9) Chức vụ
        public string? Position { get; set; }

        // 10) Các nghiệp vụ chuyên môn
        public string? ProfessionalSkills { get; set; }

        // 12) Cảng hàng không làm việc thường xuyên
        public string? RegularAirport { get; set; }

        // Navigation — các bảng con (mục 11, 13, 14, 15)
        public ICollection<VehicleOperatorOnSiteTraining> OnSiteTrainings { get; set; } = new List<VehicleOperatorOnSiteTraining>();
        public ICollection<VehicleOperatorReinforcementAirport> ReinforcementAirports { get; set; } = new List<VehicleOperatorReinforcementAirport>();
        public ICollection<VehicleOperatorCertification> Certifications { get; set; } = new List<VehicleOperatorCertification>();
        public ICollection<VehicleOperatorWorkHistory> WorkHistories { get; set; } = new List<VehicleOperatorWorkHistory>();

    }
}
