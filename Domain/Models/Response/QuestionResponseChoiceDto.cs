namespace Domain.Models.Response
{
    public class QuestionResponseChoiceDto
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public int Order { get; set; }
        public bool IsExclusiveOption { get; set; }
    }
}
