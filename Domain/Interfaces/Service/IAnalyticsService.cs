using Domain.Models.Admin;

namespace Domain.Interfaces.Service
{
    public interface IAnalyticsService
    {
        Task<PlatformMetricsDto> GetPlatformMetricsAsync();
        Task<IEnumerable<UserAdminDto>> GetTopUsersAsync(string metric, int take = 10);
        Task<IEnumerable<SurveyAnalyticsSummaryDto>> GetTopSurveysAsync(string metric, int take = 10);
        Task<IEnumerable<DemographicBreakdownDto>> GetUserDemographicsBreakdownAsync(string demographicType);
        Task<IEnumerable<ActivityTimelineDto>> GetActivityTimelineAsync(int days = 30);
        Task<IEnumerable<ConversionFunnelDto>> GetConversionFunnelAsync(int surveyId);
        Task<byte[]> ExportSurveyResponsesAsync(int surveyId, string format = "csv");

        Task TrackSurveyViewAsync(int surveyId);
        Task TrackSurveyCompletionAsync(int surveyId, string userId);
        Task TrackQuestionResponseAsync(int questionId, string userId);
        Task TrackRewardRedemptionAsync(int rewardId, string userId, string rewardType);
        Task TrackRewardClaimAsync(int userRewardId, string userId);
    }
}
