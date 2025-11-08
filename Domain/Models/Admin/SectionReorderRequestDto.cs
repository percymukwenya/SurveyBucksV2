namespace Domain.Models.Admin
{
    public class SectionReorderRequestDto
    {
        public int SurveyId { get; set; }
        public IEnumerable<SectionOrderDto> SectionOrders { get; set; }
    }
}
