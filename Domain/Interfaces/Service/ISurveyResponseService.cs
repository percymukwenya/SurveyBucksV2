using Domain.Models.Response;

namespace Domain.Interfaces.Service
{
    public interface ISurveyResponseService
    {
        Task<ResponseValidationResult> ValidateAndSaveResponseAsync(SurveyResponseDto response, string userId);
        Task<BatchResponseResult> SaveMultipleResponsesAsync(List<SurveyResponseDto> responses, string userId);
    }
}
