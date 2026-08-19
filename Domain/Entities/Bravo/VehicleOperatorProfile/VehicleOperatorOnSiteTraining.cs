namespace Domain.Entities.Bravo.VehicleOperatorProfile
{
    public class VehicleOperatorOnSiteTraining
    {
        public int Id { get; set; }
        public int VehicleOperatorProfileId { get; set; }

        // Nghiệp vụ được đào tạo tại chỗ
        public string? TrainedSkill { get; set; }

        // Cảng hàng không được huấn luyện nghiệp vụ tại chỗ
        public string? TrainingAirport { get; set; }

        // Thời gian huấn luyện nghiệp vụ tại chỗ
        public string? TrainingPeriod { get; set; }

        // Quyết định công nhận hoàn thành huấn luyện nghiệp vụ tại chỗ
        public string? RecognitionDecisionNo { get; set; }

        public VehicleOperatorProfile? VehicleOperatorProfile { get; set; }
    }
}
