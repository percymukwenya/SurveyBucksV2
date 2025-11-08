using Domain.Models.Admin;
using Domain.Models.Response;

namespace Domain.Interfaces.Service
{
    public interface IQuestionService
    {
        Task<IEnumerable<QuestionTypeDto>> GetQuestionTypesAsync();
        Task<IEnumerable<QuestionDto>> GetSectionQuestionsAsync(int sectionId);
        Task<QuestionDetailDto> GetQuestionDetailsAsync(int questionId);
        Task<int> CreateQuestionAsync(QuestionCreateDto question, string createdBy);
        Task<bool> UpdateQuestionAsync(QuestionUpdateDto question, string modifiedBy);
        Task<bool> DeleteQuestionAsync(int questionId, string deletedBy);
        Task<bool> ReorderQuestionsAsync(int sectionId, IEnumerable<QuestionOrderDto> questionOrders, string modifiedBy);
        Task<IEnumerable<QuestionResponseChoiceDto>> GetQuestionChoicesAsync(int questionId);
        Task<int> AddQuestionChoiceAsync(QuestionChoiceCreateDto choice, string createdBy);
        Task<bool> UpdateQuestionChoiceAsync(QuestionChoiceUpdateDto choice, string modifiedBy);
        Task<bool> DeleteQuestionChoiceAsync(int choiceId, string deletedBy);
        Task<IEnumerable<MatrixRowDto>> GetMatrixRowsAsync(int questionId);
        Task<IEnumerable<MatrixColumnDto>> GetMatrixColumnsAsync(int questionId);
        Task<int> AddMatrixRowAsync(MatrixRowDto row, string createdBy);
        Task<int> AddMatrixColumnAsync(MatrixColumnDto column, string createdBy);
        Task<bool> DeleteMatrixRowAsync(int rowId, string deletedBy);
        Task<bool> DeleteMatrixColumnAsync(int columnId, string deletedBy);
    }
}
