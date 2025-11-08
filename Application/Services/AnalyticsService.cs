using Domain.Interfaces.Repository.Admin;
using Domain.Interfaces.Service;
using Domain.Models.Admin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _analyticsRepository;

        public AnalyticsService(IAnalyticsRepository analyticsRepository)
        {
            _analyticsRepository = analyticsRepository;
        }

        public async Task<byte[]> ExportSurveyResponsesAsync(int surveyId, string format = "csv")
        {
            return await _analyticsRepository.ExportSurveyResponsesAsync(surveyId, format);
        }

        public async Task<IEnumerable<ActivityTimelineDto>> GetActivityTimelineAsync(int days = 30)
        {
            return await _analyticsRepository.GetActivityTimelineAsync(days);
        }

        public async Task<IEnumerable<ConversionFunnelDto>> GetConversionFunnelAsync(int surveyId)
        {
            return await _analyticsRepository.GetConversionFunnelAsync(surveyId);
        }

        public async Task<PlatformMetricsDto> GetPlatformMetricsAsync()
        {
            return await _analyticsRepository.GetPlatformMetricsAsync();
        }

        public async Task<IEnumerable<SurveyAnalyticsSummaryDto>> GetTopSurveysAsync(string metric, int take = 10)
        {
            return await _analyticsRepository.GetTopSurveysAsync(metric, take);
        }

        public async Task<IEnumerable<UserAdminDto>> GetTopUsersAsync(string metric, int take = 10)
        {
            return await _analyticsRepository.GetTopUsersAsync(metric, take);
        }

        public async Task<IEnumerable<DemographicBreakdownDto>> GetUserDemographicsBreakdownAsync(string demographicType)
        {
            return await _analyticsRepository.GetUserDemographicsBreakdownAsync(demographicType);
        }

        public Task TrackQuestionResponseAsync(int questionId, string userId)
        {
            throw new System.NotImplementedException();
        }

        public Task TrackRewardClaimAsync(int userRewardId, string userId)
        {
            throw new System.NotImplementedException();
        }

        public Task TrackRewardRedemptionAsync(int rewardId, string userId, string rewardType)
        {
            throw new System.NotImplementedException();
        }

        public Task TrackSurveyCompletionAsync(int surveyId, string userId)
        {
            throw new System.NotImplementedException();
        }

        public Task TrackSurveyViewAsync(int surveyId)
        {
            throw new System.NotImplementedException();
        }
    }
}
