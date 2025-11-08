using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Response;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface ISurveyAccessService
    {
        Task<SurveyAccessResultDto> GetUserSurveyAccessAsync(string userId);
    }

    public class SurveyAccessService : ISurveyAccessService
    {
        private readonly ISurveyParticipationRepository _surveyRepository;
        private readonly IUserProfileCompletionService _profileService;
        private readonly INotificationService _notificationService;
        private readonly ISurveyMatchingService _surveyMatchingService;
        private readonly ILogger<SurveyAccessService> _logger;

        public SurveyAccessService(ISurveyParticipationRepository surveyParticipationRepository, 
            IUserProfileCompletionService userProfileCompletionService,
            INotificationService notificationService, ISurveyMatchingService surveyMatchingService,
            ILogger<SurveyAccessService> logger)
        {
            _notificationService = notificationService;
            _surveyMatchingService = surveyMatchingService;
            _logger = logger;
            _surveyRepository = surveyParticipationRepository;
            _profileService = userProfileCompletionService;
        }
        public async Task<SurveyAccessResultDto> GetUserSurveyAccessAsync(string userId)
        {
            // 1. CHECK IF PROFILE MEETS MINIMUM REQUIREMENTS (60%)
            var profileCompletion = await _profileService.GetProfileCompletionAsync(userId);

            if (!profileCompletion.IsEligibleForSurveys)
            {
                return new SurveyAccessResultDto
                {
                    HasAccess = false,
                    CompletionPercentage = profileCompletion.OverallCompletionPercentage,
                    Message = "Complete your profile to unlock surveys",
                    BlockingFactors = GetBlockingFactors(profileCompletion),
                    IncompleteSections = GetIncompleteSections(profileCompletion)
                };
            }

            // 2. IF 100% COMPLETE - GET MATCHING SURVEYS
            var surveyMatches = await _surveyMatchingService.GetMatchingSurveysAsync(userId);

            _logger.LogInformation("User {UserId} eligible for surveys. Found {Count} matching surveys",
                userId, surveyMatches.Count());

            return new SurveyAccessResultDto
            {
                HasAccess = true,
                CompletionPercentage = 100,
                Message = $"Welcome! You have {surveyMatches.Count()} surveys available.",
                AvailableSurveys = surveyMatches.Select(ConvertToSurveyListItem).ToList(),
                TotalAvailableSurveys = surveyMatches.Count()
            };
        }

        private List<string> GetBlockingFactors(UserProfileCompletionDto profile)
        {
            var factors = new List<string>();

            if (profile.Demographics.CompletionPercentage < 25)
                factors.Add("Complete your personal information (age, gender, location, income)");

            if (profile.Documents.CompletionPercentage < 25)
                factors.Add("Upload and verify your identity document");

            if (profile.Banking.CompletionPercentage < 25)
                factors.Add("Add and verify your banking details");

            if (profile.Interests.CompletionPercentage < 25)
                factors.Add("Add at least 3 interests to your profile");

            return factors;
        }

        private List<IncompleteSectionDto> GetIncompleteSections(UserProfileCompletionDto profile)
        {
            var sections = new List<IncompleteSectionDto>();

            if (profile.Demographics.CompletionPercentage < 25)
            {
                sections.Add(new IncompleteSectionDto
                {
                    SectionName = "Demographics",
                    CompletionPercentage = profile.Demographics.CompletionPercentage,
                    MissingItems = profile.Demographics.MissingFields,
                    EstimatedTimeMinutes = 3,
                    Priority = 1
                });
            }

            if (profile.Documents.CompletionPercentage < 25)
            {
                sections.Add(new IncompleteSectionDto
                {
                    SectionName = "Documents",
                    CompletionPercentage = profile.Documents.CompletionPercentage,
                    MissingItems = profile.Documents.MissingFields,
                    EstimatedTimeMinutes = 5,
                    Priority = 2
                });
            }

            if (profile.Banking.CompletionPercentage < 25)
            {
                sections.Add(new IncompleteSectionDto
                {
                    SectionName = "Banking",
                    CompletionPercentage = profile.Banking.CompletionPercentage,
                    MissingItems = profile.Banking.MissingFields,
                    EstimatedTimeMinutes = 4,
                    Priority = 3
                });
            }

            if (profile.Interests.CompletionPercentage < 25)
            {
                sections.Add(new IncompleteSectionDto
                {
                    SectionName = "Interests",
                    CompletionPercentage = profile.Interests.CompletionPercentage,
                    MissingItems = profile.Interests.MissingFields,
                    EstimatedTimeMinutes = 2,
                    Priority = 4
                });
            }

            return sections.OrderBy(s => s.Priority).ToList();
        }

        private SurveyListItemDto ConvertToSurveyListItem(SurveyMatchDto match)
        {
            return new SurveyListItemDto
            {
                Id = match.Survey.Id,
                Name = match.Survey.Name,
                Description = match.Survey.Description,
                CompanyName = match.Survey.CompanyName,
                Industry = match.Survey.Industry,
                EstimatedDurationMinutes = match.Survey.MaxTimeInMins,
                MatchScore = match.MatchScore,
                MatchReasons = match.MatchReasons,
                OpeningDateTime = match.Survey.OpeningDateTime,
                ClosingDateTime = match.Survey.ClosingDateTime
            };
        }

        private int GetEstimatedTime(string sectionName) => sectionName switch
        {
            "Demographics" => 3,
            "Documents" => 5,
            "Banking" => 4,
            "Interests" => 2,
            _ => 3
        };

        private int GetPriority(string sectionName) => sectionName switch
        {
            "Documents" => 1,    // Highest priority
            "Demographics" => 2,
            "Banking" => 3,
            "Interests" => 4,    // Lowest priority
            _ => 5
        };
    }
}
