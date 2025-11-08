using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SurveyMatchingService : ISurveyMatchingService
    {
        private readonly ISurveyRepository _surveyRepository;
        private readonly IDemographicsRepository _demographicsRepository;
        private readonly IUserTargetingRepository _targetingRepository;
        private readonly ILogger<SurveyMatchingService> _logger;

        public SurveyMatchingService(
            ISurveyRepository surveyRepository,
            IDemographicsRepository demographicsRepository,
            IUserTargetingRepository targetingRepository,
            ILogger<SurveyMatchingService> logger)
        {
            _surveyRepository = surveyRepository;
            _demographicsRepository = demographicsRepository;
            _targetingRepository = targetingRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<SurveyMatchDto>> GetMatchingSurveysAsync(string userId, int matchThreshold = 70)
        {
            // 1. Get user profile (simple SQL)
            var userProfile = await _demographicsRepository.GetUserProfileForMatchingAsync(userId);
            if (userProfile == null) return Enumerable.Empty<SurveyMatchDto>();

            // 2. Get active surveys (simple SQL)
            var activeSurveys = await _surveyRepository.GetActiveSurveysAsync();

            // 3. Apply matching logic in C# (testable, debuggable)
            var matches = new List<SurveyMatchDto>();

            foreach (var survey in activeSurveys)
            {
                var matchScore = await CalculateMatchScoreAsync(userProfile, survey);

                if (matchScore >= matchThreshold)
                {
                    matches.Add(new SurveyMatchDto
                    {
                        Survey = survey,
                        MatchScore = matchScore,
                        MatchReasons = GetMatchReasons(userProfile, survey)
                    });
                }
            }

            return matches.OrderByDescending(m => m.MatchScore);
        }

        private async Task<int> CalculateMatchScoreAsync(UserProfileDto userProfile, SurveyDto survey)
        {
            var score = 0;
            var targetingCriteria = await _targetingRepository.GetSurveyTargetingAsync(survey.Id);

            // Age matching (20 points max)
            score += CalculateAgeMatch(userProfile.Age, targetingCriteria.AgeRanges);

            // Gender matching (15 points max)
            score += CalculateGenderMatch(userProfile.Gender, targetingCriteria.Genders);

            // Location matching (20 points max)
            score += CalculateLocationMatch(userProfile.Location, userProfile.Country, targetingCriteria.Locations);

            // Income matching (15 points max)
            score += CalculateIncomeMatch(userProfile.IncomeRange, targetingCriteria.IncomeRanges);

            // Interest matching (20 points max)
            score += await CalculateInterestMatchAsync(userProfile.UserId, targetingCriteria.Interests);

            // Education matching (10 points max)
            score += CalculateEducationMatch(userProfile.Education, targetingCriteria.EducationLevels);

            return Math.Min(score, 100); // Cap at 100%
        }

        private int CalculateAgeMatch(int userAge, List<AgeRangeDto> ageRanges)
        {
            if (!ageRanges?.Any() == true) return 20; // No restrictions = full points

            var matchingRange = ageRanges.FirstOrDefault(range =>
                userAge >= range.MinAge && userAge <= range.MaxAge);

            return matchingRange != null ? 20 : 0;
        }

        private int CalculateGenderMatch(string userGender, List<string> targetGenders)
        {
            if (!targetGenders?.Any() == true) return 15; // No restrictions = full points

            return targetGenders.Contains(userGender, StringComparer.OrdinalIgnoreCase) ? 15 : 0;
        }

        private int CalculateLocationMatch(string userLocation, string userCountry, List<LocationTargetDto> targetLocations)
        {
            if (!targetLocations?.Any() == true) return 20; // No restrictions = full points

            // Exact location match
            if (targetLocations.Any(l => l.Location.Equals(userLocation, StringComparison.OrdinalIgnoreCase)))
                return 20;

            // Country match
            if (targetLocations.Any(l => l.Country.Equals(userCountry, StringComparison.OrdinalIgnoreCase)))
                return 15;

            // Partial location match (contains)
            if (targetLocations.Any(l => userLocation.Contains(l.Location, StringComparison.OrdinalIgnoreCase)))
                return 10;

            return 0;
        }

        private int CalculateIncomeMatch(string userIncomeRange, List<IncomeRangeDto> incomeRanges)
        {
            if (!incomeRanges?.Any() == true) return 15; // No restrictions = full points
            if (string.IsNullOrWhiteSpace(userIncomeRange)) return 0;

            // Parse user income range to get min/max values
            var (userMinIncome, userMaxIncome) = ParseIncomeRange(userIncomeRange);
            if (userMinIncome == 0 && userMaxIncome == 0) return 0; // Invalid range

            // Check if user's income range overlaps with any target range
            var hasMatch = incomeRanges.Any(range =>
                !(userMaxIncome < range.MinIncome || userMinIncome > range.MaxIncome));

            return hasMatch ? 15 : 0;
        }

        private (decimal min, decimal max) ParseIncomeRange(string incomeRange)
        {
            if (string.IsNullOrWhiteSpace(incomeRange)) return (0, 0);

            // Handle "Prefer not to say" case
            if (incomeRange.ToLower().Contains("prefer not to say")) return (0, 0);

            try
            {
                // Remove currency symbols and spaces
                var cleanRange = incomeRange.Replace("R", "").Replace(" ", "").Replace(",", "");
                
                if (cleanRange.Contains("-"))
                {
                    // Range format: "50000-99999"
                    var parts = cleanRange.Split('-');
                    if (parts.Length == 2 &&
                        decimal.TryParse(parts[0], out var min) &&
                        decimal.TryParse(parts[1], out var max))
                    {
                        return (min, max);
                    }
                }
                else if (cleanRange.Contains("+"))
                {
                    // Format: "1000000+"
                    var amount = cleanRange.Replace("+", "");
                    if (decimal.TryParse(amount, out var min))
                    {
                        return (min, decimal.MaxValue);
                    }
                }
                else if (cleanRange.ToLower().StartsWith("under"))
                {
                    // Format: "Under50000"
                    var amount = cleanRange.ToLower().Replace("under", "");
                    if (decimal.TryParse(amount, out var max))
                    {
                        return (0, max);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to parse income range '{incomeRange}': {ex.Message}");
            }

            return (0, 0);
        }

        private async Task<int> CalculateInterestMatchAsync(string userId, List<string> targetInterests)
        {
            if (!targetInterests?.Any() == true) return 20; // No restrictions = full points

            var userInterests = await _demographicsRepository.GetUserInterestNamesAsync(userId);

            var matchingInterests = userInterests.Intersect(targetInterests, StringComparer.OrdinalIgnoreCase).Count();
            var maxPossibleMatches = Math.Min(targetInterests.Count, userInterests.Count);

            if (maxPossibleMatches == 0) return 0;

            // Calculate percentage match, scale to 20 points
            var matchPercentage = (double)matchingInterests / maxPossibleMatches;
            return (int)(matchPercentage * 20);
        }

        private int CalculateEducationMatch(string userEducation, List<string> targetEducationLevels)
        {
            if (!targetEducationLevels?.Any() == true) return 10; // No restrictions = full points

            return targetEducationLevels.Contains(userEducation, StringComparer.OrdinalIgnoreCase) ? 10 : 0;
        }

        private List<string> GetMatchReasons(UserProfileDto userProfile, SurveyDto survey)
        {
            var reasons = new List<string>();

            // Add specific reasons why this survey matched
            // This helps with debugging and user communication
            reasons.Add($"Age group: {GetAgeGroup(userProfile.Age)}");
            reasons.Add($"Location: {userProfile.Location}");
            reasons.Add($"Income bracket: {userProfile.IncomeRange ?? (userProfile.Income?.ToString() ?? "Not specified")}");

            return reasons;
        }

        private string GetAgeGroup(int age) => age switch
        {
            < 25 => "18-24",
            < 35 => "25-34",
            < 45 => "35-44",
            < 55 => "45-54",
            _ => "55+"
        };

        private string GetIncomeGroup(decimal income) => income switch
        {
            < 30000 => "Under R30k",
            < 50000 => "R30k-R50k",
            < 75000 => "R50k-R75k",
            < 100000 => "R75k-R100k",
            _ => "Over R100k"
        };
    }
}
