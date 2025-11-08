using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Response;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SurveyParticipationService : ISurveyParticipationService
    {
        private readonly ISurveyParticipationRepository _participationRepository;
        private readonly ISurveyMatchingService _surveyMatchingService; // NEW
        private readonly IUserProfileCompletionService _profileCompletionService; // NEW
        private readonly IGamificationService _gamificationService;
        private readonly INotificationService _notificationService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IBackgroundTaskService _backgroundTaskService;
        private readonly ILogger<SurveyParticipationService> _logger;

        public SurveyParticipationService(
            ISurveyParticipationRepository participationRepository,
            ISurveyMatchingService surveyMatchingService,
            IUserProfileCompletionService profileCompletionService,
            IGamificationService gamificationService,
            INotificationService notificationService,
            IAnalyticsService analyticsService,
            IBackgroundTaskService backgroundTaskService,
            ILogger<SurveyParticipationService> logger)
        {
            _participationRepository = participationRepository;
            _surveyMatchingService = surveyMatchingService;
            _profileCompletionService = profileCompletionService;
            _gamificationService = gamificationService;
            _notificationService = notificationService;
            _analyticsService = analyticsService;
            _backgroundTaskService = backgroundTaskService;
            _logger = logger;
        }

        public async Task<IEnumerable<SurveyListItemDto>> GetMatchingSurveysForUserAsync(string userId)
        {
            // 1. Check eligibility first
            var isEligible = await _profileCompletionService.IsEligibleForSurveysAsync(userId);
            if (!isEligible)
            {
                _logger.LogInformation("User {UserId} not eligible for surveys", userId);
                return Enumerable.Empty<SurveyListItemDto>();
            }

            // 2. Get matches using new service
            var matches = await _surveyMatchingService.GetMatchingSurveysAsync(userId);

            // 3. Convert to DTOs
            var surveys = matches.Select(match => new SurveyListItemDto
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
            }).ToList();

            // 4. Enhance with rewards (existing logic)
            foreach (var survey in surveys)
            {
                survey.Reward = await GetSurveyRewardSummaryAsync(survey.Id);
            }

            _logger.LogInformation("Found {Count} matching surveys for user {UserId}", surveys.Count, userId);
            return surveys;
        }

        public async Task<SurveyAccessResponseDto> GetAvailableSurveysWithAccessCheckAsync(string userId)
        {
            try
            {
                // Use the new profile completion service
                var profileCompletion = await _profileCompletionService.GetProfileCompletionAsync(userId);

                if (!profileCompletion.IsEligibleForSurveys)
                {
                    return new SurveyAccessResponseDto
                    {
                        HasAccess = false,
                        CompletionPercentage = profileCompletion.OverallCompletionPercentage,
                        Message = GetIneligibilityMessage(profileCompletion),
                        BlockingFactors = GetBlockingFactors(profileCompletion),
                        Surveys = new List<SurveyListItemDto>()
                    };
                }

                // Profile complete - get surveys using new matching service
                var surveys = await GetMatchingSurveysForUserAsync(userId);

                return new SurveyAccessResponseDto
                {
                    HasAccess = true,
                    CompletionPercentage = 100,
                    Message = $"Welcome! You have {surveys.Count()} surveys available.",
                    Surveys = surveys.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available surveys for user {UserId}", userId);

                return new SurveyAccessResponseDto
                {
                    HasAccess = false,
                    CompletionPercentage = 0,
                    Message = "Unable to load surveys at this time. Please try again.",
                    Surveys = new List<SurveyListItemDto>()
                };
            }
        }

        public async Task<SurveyDetailDto> GetSurveyDetailsAsync(int surveyId, string userId)
        {
            var survey = await _participationRepository.GetSurveyDetailsAsync(surveyId);

            if (survey == null)
            {
                throw new NotFoundException($"Survey with ID {surveyId} not found");
            }

            return survey;
        }

        public async Task<SurveyParticipationDto> EnrollInSurveyAsync(string userId, int surveyId)
        {
            _logger.LogInformation("User {UserId} enrolling in survey {SurveyId}", userId, surveyId);

            try
            {
                // 1. BUSINESS VALIDATION - Check profile eligibility
                var isEligible = await _profileCompletionService.IsEligibleForSurveysAsync(userId);
                if (!isEligible)
                {
                    throw new InvalidOperationException("Profile must be 100% complete to enroll in surveys");
                }

                // 2. BUSINESS VALIDATION - Check if survey is available for user
                var availableSurveys = await GetMatchingSurveysForUserAsync(userId);
                var targetSurvey = availableSurveys.FirstOrDefault(s => s.Id == surveyId);

                if (targetSurvey == null)
                {
                    throw new InvalidOperationException("This survey is not available for your profile");
                }

                // 3. BUSINESS VALIDATION - Check if already enrolled
                var existingParticipationId = await _participationRepository.GetExistingParticipationIdAsync(userId, surveyId);
                if (existingParticipationId.HasValue)
                {
                    _logger.LogInformation("User {UserId} already enrolled in survey {SurveyId}, returning existing participation", userId, surveyId);
                    return await _participationRepository.GetParticipationAsync(existingParticipationId.Value, userId);
                }

                // 4. CREATE PARTICIPATION - Core data operation
                var participation = new SurveyParticipationDto
                {
                    EnrolmentDateTime = DateTime.UtcNow,
                    UserId = userId,
                    SurveyId = surveyId,
                    StatusId = 1, // Enrolled
                    ProgressPercentage = 0
                };

                var participationId = await _participationRepository.CreateParticipationAsync(participation);
                participation.Id = participationId;

                // 5. EXECUTE BACKGROUND OPERATIONS WITH IMPROVED ERROR HANDLING
                _ = Task.Run(async () =>
                {
                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                    await _backgroundTaskService.ExecuteEnrollmentTasksAsync(userId, surveyId, cancellationTokenSource.Token);
                });

                _logger.LogInformation("User {UserId} successfully enrolled in survey {SurveyId} with participation {ParticipationId}",
                    userId, surveyId, participationId);

                return participation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling user {UserId} in survey {SurveyId}", userId, surveyId);
                throw;
            }
        }

        public async Task<SurveyParticipationDto> GetParticipationAsync(int participationId, string userId)
        {
            var participation = await _participationRepository.GetParticipationAsync(participationId, userId);
            if (participation == null)
            {
                throw new NotFoundException($"Participation {participationId} not found");
            }
            return participation;
        }

        public async Task<bool> UpdateParticipationProgressAsync(SurveyProgressUpdateDto progressDto, string userId)
        {
            // Validate the progress update
            if (progressDto.ProgressPercentage < 0 || progressDto.ProgressPercentage > 100)
            {
                throw new ArgumentException("Progress percentage must be between 0 and 100");
            }

            // Get current participation to verify user
            var participation = await _participationRepository.GetParticipationAsync(progressDto.ParticipationId, userId);

            if (participation == null || participation.UserId != userId)
            {
                throw new UnauthorizedAccessException("Invalid participation access");
            }

            // Check if already completed
            if (participation.StatusName == "Completed" || participation.StatusName == "Rewarded")
            {
                return false; // Already completed
            }

            var result = await _participationRepository.UpdateParticipationProgressAsync(
                progressDto.ParticipationId,
                userId,
                progressDto.SectionId,
                progressDto.QuestionId,
                progressDto.ProgressPercentage);

            // 3. EXECUTE BACKGROUND OPERATIONS (only if progress increased)
            if (result && progressDto.ProgressPercentage > participation.ProgressPercentage)
            {
                _ = Task.Run(async () =>
                {
                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                    await _backgroundTaskService.ExecuteProgressTasksAsync(userId, progressDto.ProgressPercentage, cancellationTokenSource.Token);
                });
            }

            return result;
        }

        public async Task<bool> CompleteSurveyAsync(int participationId, string userId)
        {
            _logger.LogInformation("User {UserId} completing survey participation {ParticipationId}", userId, participationId);

            // 1. BUSINESS VALIDATION
            var participation = await _participationRepository.GetParticipationAsync(participationId, userId);
            if (participation == null)
            {
                throw new NotFoundException($"Participation {participationId} not found for user {userId}");
            }

            if (participation.StatusName == "Completed" || participation.StatusName == "Rewarded")
            {
                _logger.LogInformation("Survey participation {ParticipationId} already completed", participationId);
                return false; // Already completed
            }

            // 2. COMPLETE SURVEY - Core data operation
            var result = await _participationRepository.CompleteSurveyAsync(participationId, userId);

            if (result)
            {
                // 3. EXECUTE BACKGROUND COMPLETION OPERATIONS
                _ = Task.Run(async () =>
                {
                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                    await _backgroundTaskService.ExecuteCompletionTasksAsync(userId, participation.SurveyId, cancellationTokenSource.Token);
                });

                _logger.LogInformation("User {UserId} successfully completed survey participation {ParticipationId}", userId, participationId);
            }

            return result;
        }

        private string GetIneligibilityMessage(UserProfileCompletionDto profile)
        {
            var missingPercentage = 100 - profile.OverallCompletionPercentage;
            var nextStep = profile.NextSteps.FirstOrDefault();

            return nextStep != null
                ? $"Complete your {nextStep.Section.ToLower()} ({nextStep.EstimatedTimeMinutes} min) to unlock surveys"
                : $"Complete {missingPercentage}% more of your profile to access surveys";
        }

        private List<string> GetBlockingFactors(UserProfileCompletionDto profile)
        {
            return profile.NextSteps.Take(3).Select(step =>
                $"{step.Title} ({step.EstimatedTimeMinutes} min)").ToList();
        }

        private async Task<RewardSummaryDto> GetSurveyRewardSummaryAsync(int surveyId)
        {
            // Implementation to get reward summary for a survey
            // This could be cached or retrieved from a rewards service
            return new RewardSummaryDto
            {
                RewardType = "Points",
                PointsAmount = 100, // Default or from configuration
                Description = "Complete this survey to earn points"
            };
        }

        public async Task<IEnumerable<SurveyParticipationSummaryDto>> GetUserParticipationsAsync(string userId, string status = null)
        {
            return await _participationRepository.GetUserParticipationsAsync(userId, status);
        }

        public async Task<bool> SaveSurveyResponseAsync(SurveyResponseDto response, string userId)
        {
            _logger.LogDebug("Saving response for User {UserId}, Question {QuestionId}", userId, response.QuestionId);

            // 1. BUSINESS VALIDATION
            var participation = await _participationRepository.GetParticipationAsync(response.SurveyParticipationId, userId);
            if (participation == null || participation.UserId != userId)
            {
                throw new UnauthorizedAccessException("Invalid participation access");
            }

            // 2. SAVE RESPONSE - Core data operation
            var result = await _participationRepository.SaveSurveyResponseAsync(response);

            if (result)
            {
                // 3. EXECUTE BACKGROUND OPERATIONS FOR QUESTION RESPONSE
                _ = Task.Run(async () =>
                {
                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                    await _backgroundTaskService.ExecuteQuestionResponseTasksAsync(userId, response.QuestionId, cancellationTokenSource.Token);
                });
            }

            return result;
        }

        public async Task<IEnumerable<SurveyResponseDto>> GetSavedResponsesAsync(int participationId, string userId)
        {
            // Verify ownership
            await GetParticipationAsync(participationId, userId);
            return await _participationRepository.GetSavedResponsesAsync(participationId, userId);
        }

        public async Task<bool> SubmitSurveyFeedbackAsync(SurveyFeedbackDto feedback, string userId)
        {
            // Verify the participation belongs to the user
            var participation = await _participationRepository.GetParticipationAsync(feedback.SurveyParticipationId, userId);

            if (participation == null)
            {
                throw new NotFoundException($"Participation with ID {feedback.SurveyParticipationId} not found");
            }

            if (participation.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only submit feedback for your own participations");
            }

            // Validate feedback
            if (feedback.Rating < 1 || feedback.Rating > 5)
            {
                throw new ArgumentException("Rating must be between 1 and 5");
            }

            // Since we don't have a dedicated repository method for feedback, we would need to add one
            // For now, let's just return true
            return true;
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}
