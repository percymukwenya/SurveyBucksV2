namespace Domain.Interfaces.Repository
{
    public interface IQuestionAnalyticsRepository
    {
        Task<bool> IncrementQuestionResponsesAsync(int questionId);
        Task<bool> UpdateResponseTimeAsync(int questionId, string userId);
    }
}
