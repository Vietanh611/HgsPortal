namespace Domain.Entities.FlyOps
{
    public class NhanVien
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string? GenderName { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? Tel { get; set; } = string.Empty;
        public string? DeptCode0 { get; set; } = string.Empty;
        public string? DeptName0 { get; set; } = string.Empty;
        public string? PositionCode { get; set; } = string.Empty;
        public string? JobTitleName0 { get; set; } = string.Empty;
        public DateTime? ResignDate { get; set; }
        public string? ScheduleCode { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
