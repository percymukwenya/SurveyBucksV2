using Domain.Models.Admin;

namespace Domain.Interfaces.Repository.Admin
{
    public interface IQuestionLogicRepository
    {
        Task<IEnumerable<QuestionLogicDto>> GetQuestionLogicAsync(int questionId);
        Task<IEnumerable<QuestionLogicDto>> GetSurveyLogicAsync(int surveyId);
        Task<int> CreateQuestionLogicAsync(QuestionLogicCreateDto logic, string createdBy);
        Task<bool> UpdateQuestionLogicAsync(QuestionLogicUpdateDto logic, string modifiedBy);
        Task<bool> DeleteQuestionLogicAsync(int logicId, string deletedBy);
        Task<bool> ValidateLogicAsync(int surveyId);
        Task<SurveyFlowVisualizationDto> GetSurveyFlowVisualizationAsync(int surveyId);
    }
}
