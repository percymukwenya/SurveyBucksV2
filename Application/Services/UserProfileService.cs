using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Request;
using Domain.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IDemographicsRepository _demographicsRepository;
        private readonly IGamificationRepository _gamificationRepository;
        private readonly ILeaderboardRepository _leaderboardRepository;
        private readonly IUserAchievementRepository _userAchievementRepository;
        private readonly IUserChallengeRepository _userChallengeRepository;
        private readonly IUserEngagementRepository _userEngagementRepository;
        private readonly IUserPointsRepository _userPointsRepository;
        private readonly ISurveyParticipationRepository _surveyParticipationRepository;
        private readonly IRewardsRepository _rewardsRepository;
        private readonly INotificationRepository _notificationRepository;

        public UserProfileService(
            IDemographicsRepository demographicsRepository,
            IGamificationRepository gamificationRepository,
            ILeaderboardRepository leaderboardRepository,
            IUserAchievementRepository userAchievementRepository,
            IUserChallengeRepository userChallengeRepository,
            IUserEngagementRepository userEngagementRepository,
            IUserPointsRepository userPointsRepository,
            ISurveyParticipationRepository surveyParticipationRepository,
            IRewardsRepository rewardsRepository,
            INotificationRepository notificationRepository)
        {
            _demographicsRepository = demographicsRepository;
            _gamificationRepository = gamificationRepository;
            _leaderboardRepository = leaderboardRepository;
            _userAchievementRepository = userAchievementRepository;
            _userChallengeRepository = userChallengeRepository;
            _userEngagementRepository = userEngagementRepository;
            _userPointsRepository = userPointsRepository;
            _surveyParticipationRepository = surveyParticipationRepository;
            _rewardsRepository = rewardsRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<DemographicsDto> GetUserDemographicsAsync(string userId)
        {
            var demographics = await _demographicsRepository.GetUserDemographicsAsync(userId);

            var demoDto = new DemographicsDto();

            if (demographics != null)
            {
                demoDto = new DemographicsDto
                {
                    // Map properties from entity to DTO
                    Id = demographics.Id,
                    UserId = demographics.UserId,
                    Gender = demographics.Gender,
                    Age = demographics.Age,
                    HighestEducation = demographics.HighestEducation,
                    Income = demographics.Income,
                    IncomeRange = demographics.IncomeRange,
                    Location = demographics.Location,
                    Occupation = demographics.Occupation,
                    MaritalStatus = demographics.MaritalStatus,
                    HouseholdSize = demographics.HouseholdSize,
                    HasChildren = demographics.HasChildren,
                    NumberOfChildren = demographics.NumberOfChildren,
                    Country = demographics.Country,
                    State = demographics.State,
                    City = demographics.City,
                    ZipCode = demographics.ZipCode,
                    UrbanRural = demographics.UrbanRural,
                    Industry = demographics.Industry,
                    JobTitle = demographics.JobTitle,
                    YearsOfExperience = demographics.YearsOfExperience,
                    EmploymentStatus = demographics.EmploymentStatus,
                    CompanySize = demographics.CompanySize,
                    FieldOfStudy = demographics.FieldOfStudy,
                    YearOfGraduation = demographics.YearOfGraduation,
                    DeviceTypes = demographics.DeviceTypes,
                    InternetUsageHoursPerWeek = demographics.InternetUsageHoursPerWeek,
                    IncomeCurrency = demographics.IncomeCurrency
                };
            }

            return demoDto;
        }

        public async Task<bool> UpdateDemographicsAsync(UpdateDemographicsRequest demographicsDto, string userId)
        {
            if (demographicsDto.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only update your own demographics");
            }

            // Convert from DTO to entity if needed
            var demographics = new DemographicsDto
            {
                // Map properties from DTO to entity
                Id = demographicsDto.Id,
                UserId = demographicsDto.UserId,
                Gender = demographicsDto.Gender,
                Age = demographicsDto.Age,
                HighestEducation = demographicsDto.HighestEducation,
                Income = demographicsDto.Income,
                IncomeRange = demographicsDto.IncomeRange,
                Location = demographicsDto.Location,
                Occupation = demographicsDto.Occupation,
                MaritalStatus = demographicsDto.MaritalStatus,
                HouseholdSize = demographicsDto.HouseholdSize,
                HasChildren = demographicsDto.HasChildren,
                NumberOfChildren = demographicsDto.NumberOfChildren,
                Country = demographicsDto.Country,
                State = demographicsDto.State,
                City = demographicsDto.City,
                ZipCode = demographicsDto.ZipCode,
                UrbanRural = demographicsDto.UrbanRural,
                Industry = demographicsDto.Industry,
                JobTitle = demographicsDto.JobTitle,
                YearsOfExperience = demographicsDto.YearsOfExperience,
                EmploymentStatus = demographicsDto.EmploymentStatus,
                CompanySize = demographicsDto.CompanySize,
                FieldOfStudy = demographicsDto.FieldOfStudy,
                YearOfGraduation = demographicsDto.YearOfGraduation,
                DeviceTypes = demographicsDto.DeviceTypes,
                InternetUsageHoursPerWeek = demographicsDto.InternetUsageHoursPerWeek,
                IncomeCurrency = demographicsDto.IncomeCurrency
            };

            // Validate data
            ValidateDemographics(demographics);

            var result = await _demographicsRepository.UpdateDemographicsAsync(demographics, userId);

            if (result)
            {
                // Trigger gamification checks for profile updates
                await _gamificationRepository.ProcessChallengeProgressAsync(userId, "ProfileUpdate");
                await _gamificationRepository.CheckForAchievementsAsync(userId);
            }

            return result;
        }

        private void ValidateDemographics(DemographicsDto demographics)
        {
            // Implement validation logic
            if (demographics.Age < 0 || demographics.Age > 120)
            {
                throw new ArgumentException("Age must be between 0 and 120");
            }

            if (demographics.Income < 0)
            {
                throw new ArgumentException("Income cannot be negative");
            }

            // Add more validations as needed
        }

        public async Task<decimal> GetProfileCompletionPercentageAsync(string userId)
        {
            return await _demographicsRepository.GetProfileCompletionPercentageAsync(userId);
        }

        public async Task<IEnumerable<UserInterestDto>> GetUserInterestsAsync(string userId)
        {
            var interests = await _demographicsRepository.GetUserInterestsAsync(userId);

            // Convert from entity to DTO
            var interestDtos = interests.Select(i => new UserInterestDto
            {
                Id = i.Id,
                UserId = i.UserId,
                Interest = i.Interest,
                InterestLevel = i.InterestLevel
            });

            return interestDtos;
        }

        public async Task<bool> AddUserInterestAsync(string userId, string interest, int? interestLevel = null)
        {
            // Validate interest
            if (string.IsNullOrEmpty(interest))
            {
                throw new ArgumentException("Interest cannot be empty");
            }

            if (interestLevel.HasValue && (interestLevel < 1 || interestLevel > 5))
            {
                throw new ArgumentException("Interest level must be between 1 and 5");
            }

            return await _demographicsRepository.AddUserInterestAsync(userId, interest, interestLevel);
        }

        public async Task<bool> RemoveUserInterestAsync(string userId, string interest)
        {
            return await _demographicsRepository.RemoveUserInterestAsync(userId, interest);
        }

        public async Task<UserEngagementDto> GetUserEngagementAsync(string userId)
        {
            return await _gamificationRepository.GetUserEngagementAsync(userId);
        }

        public async Task<UserDashboardDto> GetUserDashboardAsync(string userId)
        {
            // This method aggregates data from multiple repositories to create a comprehensive dashboard
            var dashboard = new UserDashboardDto();

            // Get user points and level
            dashboard.UserPoints = await _rewardsRepository.GetUserPointsAsync(userId);
            dashboard.UserLevel = await _gamificationRepository.GetUserLevelAsync(userId);

            // Get user engagement stats
            dashboard.Engagement = await _gamificationRepository.GetUserEngagementAsync(userId);

            // Get profile completion
            dashboard.ProfileCompletionPercentage = await _demographicsRepository.GetProfileCompletionPercentageAsync(userId);

            // Get available surveys (limited to top 5)
            dashboard.AvailableSurveys = (await _surveyParticipationRepository.GetMatchingSurveysForUserAsync(userId))
                .Take(5)
                .ToList();

            // Get in-progress surveys
            dashboard.InProgressSurveys = (await _surveyParticipationRepository.GetUserParticipationsAsync(userId, "InProgress"))
                .Take(5)
                .ToList();

            // Get recently completed surveys
            dashboard.CompletedSurveys = (await _surveyParticipationRepository.GetUserParticipationsAsync(userId, "Completed"))
                .Take(5)
                .ToList();

            // Get recent rewards
            dashboard.RecentRewards = (await _rewardsRepository.GetUserRewardsAsync(userId))
                .Take(5)
                .ToList();

            // Get active challenges
            dashboard.ActiveChallenges = (await _gamificationRepository.GetActiveChallengesAsync(userId))
                .Take(3)
                .ToList();

            // Get unread notifications count
            dashboard.UnreadNotificationsCount = await _notificationRepository.GetUnreadNotificationCountAsync(userId);

            // Get recent notifications
            dashboard.RecentNotifications = (await _notificationRepository.GetUserNotificationsAsync(userId, true))
                .Take(5)
                .ToList();

            return dashboard;
        }
    }
}
