using Domain.Models.Admin;

namespace Domain.Interfaces.Repository.Admin
{
    public interface IAnalyticsRepository
    {
        Task<SurveyAnalyticsDto> GetSurveyAnalyticsAsync(int surveyId);
        Task<IEnumerable<QuestionAnalyticsDto>> GetQuestionAnalyticsAsync(int surveyId);
        Task<IEnumerable<SectionAnalyticsDto>> GetSectionAnalyticsAsync(int surveyId);
        Task<IEnumerable<DemographicBreakdownDto>> GetResponseDemographicsAsync(int surveyId);
        Task<IEnumerable<ResponseSummaryDto>> GetQuestionResponseSummaryAsync(int questionId);
        Task<IEnumerable<ResponseTrendDto>> GetResponseTrendsAsync(int surveyId, string timeFrame);
        Task<PlatformMetricsDto> GetPlatformMetricsAsync();
        Task<IEnumerable<UserAdminDto>> GetTopUsersAsync(string metric, int take = 10);
        Task<IEnumerable<SurveyAnalyticsSummaryDto>> GetTopSurveysAsync(string metric, int take = 10);
        Task<IEnumerable<DemographicBreakdownDto>> GetUserDemographicsBreakdownAsync(string demographicType);
        Task<IEnumerable<ActivityTimelineDto>> GetActivityTimelineAsync(int days = 30);
        Task<IEnumerable<ConversionFunnelDto>> GetConversionFunnelAsync(int surveyId);
        Task<byte[]> ExportSurveyResponsesAsync(int surveyId, string format = "csv");
    }
}
