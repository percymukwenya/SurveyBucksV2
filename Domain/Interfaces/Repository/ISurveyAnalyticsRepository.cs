namespace Domain.Interfaces.Repository
{
    public interface ISurveyAnalyticsRepository
    {
        Task<bool> IncrementSurveyViewsAsync(int surveyId);
        Task<bool> IncrementSurveyCompletionsAsync(int surveyId);
        Task<bool> UpdateCompletionRateAsync(int surveyId);
    }
}
