using Domain.Models.Admin;

namespace Domain.Models.Response
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public int SurveySectionId { get; set; }
        public string Text { get; set; }
        public bool IsMandatory { get; set; }
        public int Order { get; set; }
        public int QuestionTypeId { get; set; }
        public string QuestionTypeName { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public string ValidationMessage { get; set; }
        public string HelpText { get; set; }
        public List<QuestionResponseChoiceDto> ResponseChoices { get; set; }
        public List<QuestionMediaDto> Media { get; set; }
        public List<MatrixRowDto> MatrixRows { get; set; }
        public List<MatrixColumnDto> MatrixColumns { get; set; }
    }
}