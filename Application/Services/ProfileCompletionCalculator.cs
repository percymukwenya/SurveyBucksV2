using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Constants;
using Domain.Models.Enums;
using Domain.Models.Response;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ProfileCompletionCalculator : IProfileCompletionCalculator
    {
        private readonly IDemographicsRepository _demographicsRepository;
        private readonly IBankingDetailRepository _bankingRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ProfileCompletionCalculator> _logger;

        public ProfileCompletionCalculator(
            IDemographicsRepository demographicsRepository,
            IBankingDetailRepository bankingRepository,
            IDocumentRepository documentRepository,
            ILogger<ProfileCompletionCalculator> logger)
        {
            _demographicsRepository = demographicsRepository;
            _bankingRepository = bankingRepository;
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public async Task<UserProfileCompletionDto> CalculateCompletionAsync(string userId)
        {
            // Get raw data (simple SQL queries)
            var demographics = await _demographicsRepository.GetUserDemographicsAsync(userId);
            var bankingDetails = await _bankingRepository.GetUserBankingDetailsAsync(userId);
            var documents = await _documentRepository.GetUserDocumentsAsync(userId);
            var interests = await _demographicsRepository.GetUserInterestsAsync(userId);

            // Calculate completion in C# (testable, configurable)
            var demographicsSection = CalculateDemographicsCompletion(demographics);
            var bankingSection = CalculateBankingCompletion(bankingDetails);
            var documentsSection = CalculateDocumentsCompletion(documents);
            var interestsSection = CalculateInterestsCompletion(interests);

            var overallCompletion = demographicsSection.CompletionPercentage +
                                   bankingSection.CompletionPercentage +
                                   documentsSection.CompletionPercentage +
                                   interestsSection.CompletionPercentage;

            return new UserProfileCompletionDto
            {
                UserId = userId,
                OverallCompletionPercentage = overallCompletion,
                Demographics = demographicsSection,
                Banking = bankingSection,
                Documents = documentsSection,
                Interests = interestsSection,
                IsEligibleForSurveys = overallCompletion >= GetRequiredCompletionPercentage(),
                NextSteps = GenerateNextSteps(demographicsSection, bankingSection, documentsSection, interestsSection),
                LastUpdated = DateTimeOffset.UtcNow
            };
        }

        private ProfileSectionCompletionDto CalculateDemographicsCompletion(DemographicsDto demographics)
        {
            var section = new ProfileSectionCompletionDto
            {
                SectionName = "Demographics",
                Weight = 25,
                CompletedFields = new List<string>(),
                MissingFields = new List<string>(),
                Suggestions = new List<string>()
            };

            if (demographics == null)
            {
                section.MissingFields.AddRange(GetRequiredDemographicFields());
                section.Suggestions.Add("Start by adding your basic personal information");
                return section;
            }

            // Check required fields using configurable rules
            var requiredFields = GetDemographicFieldRules();

            foreach (var field in requiredFields)
            {
                var isComplete = field.ValidationRule(demographics);

                if (isComplete)
                {
                    section.CompletedFields.Add(field.DisplayName);
                }
                else
                {
                    section.MissingFields.Add(field.DisplayName);
                    if (!string.IsNullOrEmpty(field.Suggestion))
                    {
                        section.Suggestions.Add(field.Suggestion);
                    }
                }
            }

            // Calculate completion percentage
            var totalFields = requiredFields.Count;
            var completedCount = section.CompletedFields.Count;

            section.CompletionPercentage = totalFields > 0 ?
                (completedCount == totalFields ? 25 : 0) : 25;

            return section;
        }

        private ProfileSectionCompletionDto CalculateBankingCompletion(IEnumerable<BankingDetailDto> bankingDetails)
        {
            var section = new ProfileSectionCompletionDto
            {
                SectionName = "Banking",
                Weight = 25,
                CompletedFields = new List<string>(),
                MissingFields = new List<string>(),
                Suggestions = new List<string>()
            };

            var verifiedBanking = bankingDetails?.FirstOrDefault(b => b.IsVerified && b.IsActive);

            if (verifiedBanking != null)
            {
                section.CompletedFields.Add("Verified banking details");
                section.CompletionPercentage = 25;
            }
            else
            {
                var hasBanking = bankingDetails?.Any(b => b.IsActive) == true;

                if (hasBanking)
                {
                    section.CompletedFields.Add("Banking details added");
                    section.MissingFields.Add("Banking verification pending");
                    section.Suggestions.Add("Your banking details are being verified");
                }
                else
                {
                    section.MissingFields.Add("Banking details");
                    section.Suggestions.Add("Add your banking details to receive payments");
                }

                section.CompletionPercentage = 0;
            }

            return section;
        }

        private ProfileSectionCompletionDto CalculateDocumentsCompletion(IEnumerable<UserDocumentDto> documents)
        {
            var section = new ProfileSectionCompletionDto
            {
                SectionName = "Documents",
                Weight = 25,
                CompletedFields = new List<string>(),
                MissingFields = new List<string>(),
                Suggestions = new List<string>()
            };

            // Log all documents for debugging
            var docList = documents?.ToList() ?? new List<UserDocumentDto>();
            _logger.LogInformation("Calculating document completion for {DocumentCount} documents: {Documents}", 
                docList.Count, string.Join(", ", docList.Select(d => $"[{d.Category}:{d.VerificationStatus}]")));

            var approvedIdentityDoc = documents?.FirstOrDefault(d =>
                d.Category == "Identity" &&
                d.VerificationStatus == VerificationStatus.Approved &&
                (d.ExpiryDate == null || d.ExpiryDate > DateTime.UtcNow));

            if (approvedIdentityDoc != null)
            {
                _logger.LogInformation("Found approved identity document: {DocumentId}", approvedIdentityDoc.Id);
                section.CompletedFields.Add("Identity document verified");
                section.CompletionPercentage = 25;
            }
            else
            {
                var hasIdentityDoc = documents?.Any(d => d.Category == "Identity") == true;
                _logger.LogInformation("No approved identity document found. HasIdentityDoc: {HasIdentity}", hasIdentityDoc);

                if (hasIdentityDoc)
                {
                    var identityDoc = documents?.FirstOrDefault(d => d.Category == "Identity");
                    _logger.LogInformation("Identity document found but not approved: Status={Status}, Category={Category}, ExpiryDate={ExpiryDate}", 
                        identityDoc?.VerificationStatus, identityDoc?.Category, identityDoc?.ExpiryDate);
                    
                    section.CompletedFields.Add("Identity document uploaded");
                    section.MissingFields.Add("Document verification pending");
                    section.Suggestions.Add("Your identity document is being reviewed");
                }
                else
                {
                    section.MissingFields.Add("Identity document");
                    section.Suggestions.Add("Upload a government-issued ID");
                }

                section.CompletionPercentage = 0;
            }

            return section;
        }

        private ProfileSectionCompletionDto CalculateInterestsCompletion(IEnumerable<UserInterestDto> interests)
        {
            var section = new ProfileSectionCompletionDto
            {
                SectionName = "Interests",
                Weight = 25,
                CompletedFields = new List<string>(),
                MissingFields = new List<string>(),
                Suggestions = new List<string>()
            };

            var interestCount = interests?.Count() ?? 0;
            var requiredInterests = GetRequiredInterestsCount();

            if (interestCount >= requiredInterests)
            {
                section.CompletedFields.Add($"{interestCount} interests added");
                section.CompletionPercentage = 25;
            }
            else
            {
                if (interestCount > 0)
                {
                    section.CompletedFields.Add($"{interestCount} interests added");
                }

                var needed = requiredInterests - interestCount;
                section.MissingFields.Add($"Need {needed} more interests");
                section.Suggestions.Add("Add interests to get better survey matches");
                section.CompletionPercentage = 0;
            }

            return section;
        }

        // Configurable rules (easily testable and modifiable)
        private List<DemographicFieldRule> GetDemographicFieldRules()
        {
            return new List<DemographicFieldRule>
        {
            new("Gender", d => !string.IsNullOrEmpty(d.Gender), "Select your gender for better survey matching"),
            new("Age", d => d.Age > 0 && d.Age <= 120, "Add your age to unlock age-targeted surveys"),
            new("Country", d => !string.IsNullOrEmpty(d.Country), "Select your country for location-based surveys"),
            new("Location", d => !string.IsNullOrEmpty(d.Location), "Add your location for regional surveys"),
            new("Income", d => d.Income > 0, "Income helps us find surveys that match your demographic")
        };
        }

        private int GetRequiredCompletionPercentage()
        {
            // Configurable - can be changed based on business needs
            // 75% allows access with 3 of 4 sections complete
            return 75;
        }

        private int GetRequiredInterestsCount()
        {
            // Configurable - can be changed based on business needs  
            return 3;
        }

        private List<string> GetRequiredDemographicFields()
        {
            return GetDemographicFieldRules().Select(r => r.DisplayName).ToList();
        }

        private List<ProfileNextStepDto> GenerateNextSteps(params ProfileSectionCompletionDto[] sections)
        {
            var steps = new List<ProfileNextStepDto>();

            var incompleteSections = sections
                .Where(s => s.CompletionPercentage < s.Weight)
                .OrderBy(s => GetSectionPriority(s.SectionName));

            foreach (var section in incompleteSections)
            {
                steps.Add(new ProfileNextStepDto
                {
                    Section = section.SectionName,
                    Priority = GetSectionPriority(section.SectionName),
                    Title = GetStepTitle(section.SectionName),
                    Description = GetStepDescription(section.SectionName, section),
                    MissingItems = section.MissingFields,
                    EstimatedTimeMinutes = GetEstimatedTime(section.SectionName)
                });
            }

            return steps;
        }

        private ProfileStepPriority GetSectionPriority(string sectionName) => sectionName switch
        {
            "Documents" => ProfileStepPriority.Critical,
            "Demographics" => ProfileStepPriority.High,
            "Banking" => ProfileStepPriority.High,
            "Interests" => ProfileStepPriority.Medium,
            _ => ProfileStepPriority.Low
        };

        private string GetStepTitle(string sectionName) => sectionName switch
        {
            "Demographics" => "Complete Personal Information",
            "Documents" => "Verify Your Identity",
            "Banking" => "Add Payment Method",
            "Interests" => "Share Your Interests",
            _ => $"Complete {sectionName}"
        };

        private string GetStepDescription(string sectionName, ProfileSectionCompletionDto section)
        {
            var missingCount = section.MissingFields.Count;
            return sectionName switch
            {
                "Demographics" => $"Add {missingCount} required demographic details",
                "Documents" => "Upload government-issued ID for verification",
                "Banking" => "Set up payment method for survey rewards",
                "Interests" => $"Add {missingCount} more interests",
                _ => $"Complete {missingCount} remaining items"
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
    }
}
