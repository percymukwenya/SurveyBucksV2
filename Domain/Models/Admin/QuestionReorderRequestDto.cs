namespace Domain.Models.Admin
{
    public class QuestionReorderRequestDto
    {
        public int SectionId { get; set; }
        public IEnumerable<QuestionOrderDto> QuestionOrders { get; set; }
    }
}
