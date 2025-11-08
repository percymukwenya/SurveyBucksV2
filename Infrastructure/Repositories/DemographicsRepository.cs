using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models;
using Domain.Models.Enums;
using Domain.Models.Response;
using Infrastructure.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class DemographicsRepository : IDemographicsRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public DemographicsRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<DemographicsDto> GetUserDemographicsAsync(string userId)
        {
            const string sql = @"
            SELECT Id, UserId, Gender, Age, HighestEducation, Income, IncomeRange, Location, 
                   Occupation, MaritalStatus, HouseholdSize, HasChildren, NumberOfChildren,
                   Country, State, City, ZipCode, UrbanRural, Industry, JobTitle,
                   YearsOfExperience, EmploymentStatus, CompanySize, FieldOfStudy,
                   YearOfGraduation, DeviceTypes, InternetUsageHoursPerWeek, IncomeCurrency
            FROM SurveyBucks.Demographics 
            WHERE UserId = @UserId AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<DemographicsDto>(sql, new { UserId = userId });
        }

        public async Task<UserProfileDto> GetUserProfileForMatchingAsync(string userId)
        {
            const string sql = @"
            SELECT d.UserId, d.Age, d.Gender, d.Location, d.Country, d.Income, d.IncomeRange,
                   d.HighestEducation as Education, d.Occupation
            FROM SurveyBucks.Demographics d
            WHERE d.UserId = @UserId AND d.IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<UserProfileDto>(sql, new { UserId = userId });
        }

        public async Task<List<string>> GetUserInterestNamesAsync(string userId)
        {
            const string sql = @"
            SELECT Interest 
            FROM SurveyBucks.UserInterests 
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            var interests = await connection.QueryAsync<string>(sql, new { UserId = userId });
            return interests.ToList();
        }

        public async Task<IEnumerable<UserInterestDto>> GetUserInterestsAsync(string userId)
        {
            const string sql = @"
            SELECT Id, UserId, Interest, InterestLevel
            FROM SurveyBucks.UserInterests
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<UserInterestDto>(sql, new { UserId = userId });
        }

        public async Task<int> GetPotentialMatchingCountAsync(string userId)
        {
            const string sql = @"
                WITH UserWithFullData AS (
                    SELECT
                        COALESCE(d.Gender, 'Unknown') AS Gender,
                        COALESCE(d.Age, 30) AS Age,
                        COALESCE(d.Location, 'Unknown') AS Location,
                        COALESCE(d.Country, 'Unknown') AS Country,
                        COALESCE(d.Income, 50000) AS Income,
                        COALESCE(d.HighestEducation, 'Unknown') AS HighestEducation,
                        COALESCE(d.Occupation, 'Unknown') AS Occupation,
                        COALESCE(d.MaritalStatus, 'Unknown') AS MaritalStatus,
                        COALESCE(d.HouseholdSize, 2) AS HouseholdSize,
                        COALESCE(d.HasChildren, 0) AS HasChildren,
                        COALESCE(d.Industry, 'Unknown') AS Industry
                    FROM SurveyBucks.Demographics d
                    WHERE d.UserId = @UserId
                )
                SELECT COUNT(DISTINCT s.Id) AS PotentialMatches
                FROM SurveyBucks.Survey s
                LEFT JOIN SurveyBucks.AgeRangeTargets art ON s.Id = art.SurveyId AND art.IsDeleted = 0
                LEFT JOIN SurveyBucks.GenderTargets gt ON s.Id = gt.SurveyId AND gt.IsDeleted = 0
                LEFT JOIN SurveyBucks.LocationTargets lt ON s.Id = lt.SurveyId AND lt.IsDeleted = 0
                LEFT JOIN SurveyBucks.CountryTargets ct ON s.Id = ct.SurveyId AND ct.IsDeleted = 0
                LEFT JOIN SurveyBucks.IncomeRangeTargets irt ON s.Id = irt.SurveyId AND irt.IsDeleted = 0
                LEFT JOIN SurveyBucks.EducationTargets et ON s.Id = et.SurveyId AND et.IsDeleted = 0
                LEFT JOIN SurveyBucks.OccupationTargets ot ON s.Id = ot.SurveyId AND ot.IsDeleted = 0
                LEFT JOIN SurveyBucks.MaritalStatusTargets mst ON s.Id = mst.SurveyId AND mst.IsDeleted = 0
                LEFT JOIN SurveyBucks.HouseholdSizeTargets hst ON s.Id = hst.SurveyId AND hst.IsDeleted = 0
                LEFT JOIN SurveyBucks.ParentalStatusTargets pst ON s.Id = pst.SurveyId AND pst.IsDeleted = 0
                LEFT JOIN SurveyBucks.IndustryTargets it ON s.Id = it.SurveyId AND it.IsDeleted = 0
                CROSS JOIN UserWithFullData u
                WHERE s.IsActive = 1 AND s.IsPublished = 1 AND s.IsDeleted = 0
                AND GETDATE() BETWEEN s.OpeningDateTime AND s.ClosingDateTime
                AND (art.Id IS NULL OR (u.Age BETWEEN art.MinAge AND art.MaxAge))
                AND (gt.Id IS NULL OR gt.Gender = u.Gender)
                AND (lt.Id IS NULL OR lt.Location = u.Location OR u.Location LIKE '%' + lt.Location + '%')
                AND (ct.Id IS NULL OR ct.Country = u.Country)
                AND (irt.Id IS NULL OR u.Income BETWEEN irt.MinIncome AND irt.MaxIncome)
                AND (et.Id IS NULL OR et.Education = u.HighestEducation)
                AND (ot.Id IS NULL OR ot.Occupation = u.Occupation)
                AND (mst.Id IS NULL OR mst.MaritalStatus = u.MaritalStatus)
                AND (hst.Id IS NULL OR u.HouseholdSize BETWEEN hst.MinSize AND hst.MaxSize)
                AND (pst.Id IS NULL OR pst.HasChildren = u.HasChildren)
                AND (it.Id IS NULL OR it.Industry = u.Industry)
                AND NOT EXISTS (
                    SELECT 1 FROM SurveyBucks.SurveyParticipation sp
                    WHERE sp.SurveyId = s.Id AND sp.UserId = @UserId AND sp.IsDeleted = 0
                )";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
            }
        }

        public async Task<DemographicMatchSummaryDto> GetDemographicMatchSummaryAsync(string userId)
        {
            const string sql = @"
                SELECT 
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Gender IS NOT NULL) AS HasGender,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Age > 0) AS HasAge,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Location IS NOT NULL) AS HasLocation,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Country IS NOT NULL) AS HasCountry,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND (Income > 0 OR IncomeRange IS NOT NULL)) AS HasIncome,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND HighestEducation IS NOT NULL) AS HasEducation,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Occupation IS NOT NULL) AS HasOccupation,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND MaritalStatus IS NOT NULL) AS HasMaritalStatus,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND HouseholdSize IS NOT NULL) AS HasHouseholdSize,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND HasChildren IS NOT NULL) AS HasParentalStatus,
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Industry IS NOT NULL) AS HasIndustry,
                    (SELECT COUNT(*) FROM SurveyBucks.UserInterests 
                     WHERE UserId = @UserId) AS InterestCount,
                    (SELECT COUNT(*) FROM SurveyBucks.Survey 
                     WHERE IsActive = 1 AND IsPublished = 1 AND IsDeleted = 0) AS TotalActiveSurveys,
                    (SELECT COUNT(*) FROM SurveyBucks.Survey s
                     CROSS APPLY (
                        SELECT TOP 1 MatchScore FROM OPENJSON(
                            (SELECT * FROM OPENROWSET(
                                BULK 'EXEC SurveyBucks.up_GetMatchingSurveysOptimized @UserId='''+@UserId+''', @MatchThreshold=70, @MaxResults=100'
                                ,SINGLE_CLOB
                            ) AS j)
                        ) WITH (MatchScore int '$[0].MatchScore')
                     ) AS m
                     WHERE s.IsActive = 1 AND s.IsPublished = 1 AND s.IsDeleted = 0
                    ) AS MatchingSurveyCount
                ";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.QuerySingleAsync<DemographicMatchSummaryDto>(sql, new { UserId = userId });

                // Calculate importance scores for each demographic factor
                result.ImportantFactors = new Dictionary<string, int>();

                if (result.HasGender > 0) result.ImportantFactors.Add("Gender", 10);
                if (result.HasAge > 0) result.ImportantFactors.Add("Age", 15);
                if (result.HasLocation > 0) result.ImportantFactors.Add("Location", 10);
                if (result.HasCountry > 0) result.ImportantFactors.Add("Country", 10);
                if (result.HasIncome > 0) result.ImportantFactors.Add("Income", 10);
                if (result.HasEducation > 0) result.ImportantFactors.Add("Education", 5);
                if (result.HasOccupation > 0) result.ImportantFactors.Add("Occupation", 5);
                if (result.HasMaritalStatus > 0) result.ImportantFactors.Add("Marital Status", 5);
                if (result.HasHouseholdSize > 0) result.ImportantFactors.Add("Household Size", 5);
                if (result.HasParentalStatus > 0) result.ImportantFactors.Add("Parental Status", 5);
                if (result.HasIndustry > 0) result.ImportantFactors.Add("Industry", 5);
                if (result.InterestCount > 0) result.ImportantFactors.Add("Interests", 10);

                // Calculate completed points
                result.TotalMatchingPoints = result.ImportantFactors.Values.Sum();

                return result;
            }
        }

        public async Task<List<string>> GetSuggestedFieldsToCompleteAsync(string userId)
        {
            // Get current demographic status
            var summary = await GetDemographicMatchSummaryAsync(userId);

            // List the most valuable missing fields first (based on point value)
            var suggestions = new List<(string Field, int Points)>();

            if (summary.HasAge == 0) suggestions.Add(("Age", 15));
            if (summary.HasGender == 0) suggestions.Add(("Gender", 10));
            if (summary.HasLocation == 0) suggestions.Add(("Location", 10));
            if (summary.HasCountry == 0) suggestions.Add(("Country", 10));
            if (summary.HasIncome == 0) suggestions.Add(("Income", 10));
            if (summary.InterestCount == 0) suggestions.Add(("Interests", 10));
            if (summary.HasEducation == 0) suggestions.Add(("Education", 5));
            if (summary.HasOccupation == 0) suggestions.Add(("Occupation", 5));
            if (summary.HasMaritalStatus == 0) suggestions.Add(("Marital Status", 5));
            if (summary.HasHouseholdSize == 0) suggestions.Add(("Household Size", 5));
            if (summary.HasParentalStatus == 0) suggestions.Add(("Parental Status", 5));
            if (summary.HasIndustry == 0) suggestions.Add(("Industry", 5));

            // Sort by points (highest first)
            suggestions.Sort((a, b) => b.Points.CompareTo(a.Points));

            // Return just the field names
            return suggestions.Select(s => s.Field).ToList();
        }

        public async Task<bool> HasSufficientDemographicDataAsync(string userId)
        {
            const string sql = @"
            SELECT 
                CASE WHEN (
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Gender IS NOT NULL) +
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Age > 0) +
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Location IS NOT NULL) +
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND Country IS NOT NULL) +
                    (SELECT COUNT(*) FROM SurveyBucks.Demographics 
                     WHERE UserId = @UserId AND (Income > 0 OR IncomeRange IS NOT NULL))
                ) >= 3 THEN 1 ELSE 0 END AS HasSufficientData";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.ExecuteScalarAsync<bool>(sql, new { UserId = userId });
            }
        }

        public async Task<bool> UpdateDemographicsAsync(DemographicsDto demographics, string updatedBy)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var exists = await connection.ExecuteScalarAsync<bool>(
                    "SELECT COUNT(1) FROM SurveyBucks.Demographics WHERE UserId = @UserId",
                    new { UserId = demographics.UserId }, transaction);

                if (exists)
                {
                    const string updateSql = @"
                    UPDATE SurveyBucks.Demographics SET
                        Gender = @Gender, Age = @Age, HighestEducation = @HighestEducation,
                        Income = @Income, IncomeRange = @IncomeRange, Location = @Location, Occupation = @Occupation,
                        MaritalStatus = @MaritalStatus, HouseholdSize = @HouseholdSize,
                        HasChildren = @HasChildren, NumberOfChildren = @NumberOfChildren,
                        Country = @Country, State = @State, City = @City, ZipCode = @ZipCode,
                        UrbanRural = @UrbanRural, Industry = @Industry, JobTitle = @JobTitle,
                        YearsOfExperience = @YearsOfExperience, EmploymentStatus = @EmploymentStatus,
                        CompanySize = @CompanySize, FieldOfStudy = @FieldOfStudy,
                        YearOfGraduation = @YearOfGraduation, DeviceTypes = @DeviceTypes,
                        InternetUsageHoursPerWeek = @InternetUsageHoursPerWeek,
                        IncomeCurrency = @IncomeCurrency,
                        ModifiedDate = SYSDATETIMEOFFSET(), ModifiedBy = @ModifiedBy
                    WHERE UserId = @UserId";

                    await connection.ExecuteAsync(updateSql, new
                    {
                        demographics.Gender,
                        demographics.Age,
                        demographics.HighestEducation,
                        demographics.Income,
                        demographics.IncomeRange,
                        demographics.Location,
                        demographics.Occupation,
                        demographics.MaritalStatus,
                        demographics.HouseholdSize,
                        demographics.HasChildren,
                        demographics.NumberOfChildren,
                        demographics.Country,
                        demographics.State,
                        demographics.City,
                        demographics.ZipCode,
                        demographics.UrbanRural,
                        demographics.Industry,
                        demographics.JobTitle,
                        demographics.YearsOfExperience,
                        demographics.EmploymentStatus,
                        demographics.CompanySize,
                        demographics.FieldOfStudy,
                        demographics.YearOfGraduation,
                        demographics.DeviceTypes,
                        demographics.InternetUsageHoursPerWeek,
                        demographics.IncomeCurrency,
                        demographics.UserId,
                        ModifiedBy = updatedBy
                    }, transaction);
                }
                else
                {
                    const string insertSql = @"
                    INSERT INTO SurveyBucks.Demographics (
                        UserId, Gender, Age, HighestEducation, Income, IncomeRange, Location, Occupation,
                        MaritalStatus, HouseholdSize, HasChildren, NumberOfChildren,
                        Country, State, City, ZipCode, UrbanRural, Industry, JobTitle,
                        YearsOfExperience, EmploymentStatus, CompanySize, FieldOfStudy,
                        YearOfGraduation, DeviceTypes, InternetUsageHoursPerWeek,
                        IncomeCurrency, CreatedDate, CreatedBy
                    ) VALUES (
                        @UserId, @Gender, @Age, @HighestEducation, @Income, @IncomeRange, @Location, @Occupation,
                        @MaritalStatus, @HouseholdSize, @HasChildren, @NumberOfChildren,
                        @Country, @State, @City, @ZipCode, @UrbanRural, @Industry, @JobTitle,
                        @YearsOfExperience, @EmploymentStatus, @CompanySize, @FieldOfStudy,
                        @YearOfGraduation, @DeviceTypes, @InternetUsageHoursPerWeek,
                        @IncomeCurrency, SYSDATETIMEOFFSET(), @CreatedBy
                    )";

                    await connection.ExecuteAsync(insertSql, new
                    {
                        demographics.UserId,
                        demographics.Gender,
                        demographics.Age,
                        demographics.HighestEducation,
                        demographics.Income,
                        demographics.IncomeRange,
                        demographics.Location,
                        demographics.Occupation,
                        demographics.MaritalStatus,
                        demographics.HouseholdSize,
                        demographics.HasChildren,
                        demographics.NumberOfChildren,
                        demographics.Country,
                        demographics.State,
                        demographics.City,
                        demographics.ZipCode,
                        demographics.UrbanRural,
                        demographics.Industry,
                        demographics.JobTitle,
                        demographics.YearsOfExperience,
                        demographics.EmploymentStatus,
                        demographics.CompanySize,
                        demographics.FieldOfStudy,
                        demographics.YearOfGraduation,
                        demographics.DeviceTypes,
                        demographics.InternetUsageHoursPerWeek,
                        demographics.IncomeCurrency,
                        CreatedBy = updatedBy
                    }, transaction);
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> AddUserInterestAsync(string userId, string interest, int? interestLevel)
        {
            const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM SurveyBucks.UserInterests WHERE UserId = @UserId AND Interest = @Interest)
            BEGIN
                INSERT INTO SurveyBucks.UserInterests (UserId, Interest, InterestLevel)
                VALUES (@UserId, @Interest, @InterestLevel)
            END
            ELSE
            BEGIN
                UPDATE SurveyBucks.UserInterests
                SET InterestLevel = @InterestLevel
                WHERE UserId = @UserId AND Interest = @Interest
            END";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { UserId = userId, Interest = interest, InterestLevel = interestLevel });
            return result > 0;
        }

        public async Task<decimal> GetProfileCompletionPercentageAsync(string userId)
        {
            const string sql = @"
            SELECT CompletionPercentage 
            FROM SurveyBucks.DemographicProfileStatus
            WHERE UserId = @UserId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.ExecuteScalarAsync<decimal>(sql, new { UserId = userId });
            }
        }

        public async Task<UserProfileCompletionDto> GetSimplifiedProfileCompletionAsync(string userId)
        {
            const string sql = @"
        SELECT 
            -- Demographics check (basic required fields)
            CASE WHEN d.Gender IS NOT NULL 
                 AND d.Age > 0 
                 AND d.Country IS NOT NULL 
                 AND d.Location IS NOT NULL 
                 AND (d.Income > 0 OR d.IncomeRange IS NOT NULL) 
                 THEN 1 ELSE 0 END AS DemographicsComplete,
            
            -- Banking check (verified banking exists)
            CASE WHEN EXISTS (
                SELECT 1 FROM SurveyBucks.BankingDetail bd 
                WHERE bd.UserId = @UserId 
                  AND bd.IsVerified = 1 
                  AND bd.IsDeleted = 0
            ) THEN 1 ELSE 0 END AS BankingComplete,
            
            -- Documents check (approved ID document exists)
            CASE WHEN EXISTS (
                SELECT 1 FROM SurveyBucks.UserDocument ud
                JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                WHERE ud.UserId = @UserId 
                  AND dt.Category = 'Identity'
                  AND ud.VerificationStatus = 'Approved'
                  AND ud.IsDeleted = 0
            ) THEN 1 ELSE 0 END AS DocumentsComplete,
            
            -- Interests check (at least 3 interests)
            CASE WHEN (
                SELECT COUNT(*) FROM SurveyBucks.UserInterests 
                WHERE UserId = @UserId
            ) >= 3 THEN 1 ELSE 0 END AS InterestsComplete,

            -- Detailed demographics info for section breakdown
            d.Gender, d.Age, d.Country, d.Location, d.Income,
            d.HighestEducation, d.Occupation, d.MaritalStatus,
            d.State, d.City, d.Industry, d.JobTitle,

            -- Banking details count
            (SELECT COUNT(*) FROM SurveyBucks.BankingDetail 
             WHERE UserId = @UserId AND IsDeleted = 0) AS BankingDetailsCount,
             
            (SELECT COUNT(*) FROM SurveyBucks.BankingDetail 
             WHERE UserId = @UserId AND IsVerified = 1 AND IsDeleted = 0) AS VerifiedBankingCount,

            -- Document details
            (SELECT COUNT(*) FROM SurveyBucks.UserDocument ud
             JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
             WHERE ud.UserId = @UserId AND dt.Category = 'Identity' AND ud.IsDeleted = 0) AS IdentityDocsCount,
             
            (SELECT COUNT(*) FROM SurveyBucks.UserDocument ud
             JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
             WHERE ud.UserId = @UserId AND dt.Category = 'Identity' 
               AND ud.VerificationStatus = 'Approved' AND ud.IsDeleted = 0) AS ApprovedIdentityDocsCount,

            -- Interests count
            (SELECT COUNT(*) FROM SurveyBucks.UserInterests 
             WHERE UserId = @UserId) AS InterestsCount
            
        FROM SurveyBucks.Users u
        LEFT JOIN SurveyBucks.Demographics d ON u.Id = d.UserId AND d.IsDeleted = 0
        WHERE u.Id = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.QuerySingleOrDefaultAsync(sql, new { UserId = userId });

            if (result == null)
            {
                return CreateEmptyProfileCompletion(userId);
            }

            // Build section completions
            var demographics = BuildDemographicsSection(result);
            var banking = BuildBankingSection(result);
            var documents = BuildDocumentsSection(result);
            var interests = BuildInterestsSection(result);

            var overallCompletion = demographics.CompletionPercentage + banking.CompletionPercentage +
                                   documents.CompletionPercentage + interests.CompletionPercentage;

            var profileCompletion = new UserProfileCompletionDto
            {
                UserId = userId,
                OverallCompletionPercentage = overallCompletion,
                Demographics = demographics,
                Banking = banking,
                Documents = documents,
                Interests = interests,
                IsEligibleForSurveys = overallCompletion >= 100,
                LastUpdated = DateTimeOffset.UtcNow
            };

            profileCompletion.NextSteps = BuildNextSteps(profileCompletion);

            return profileCompletion;
        }

        private ProfileSectionCompletionDto BuildDemographicsSection(dynamic result)
        {
            var completed = new List<string>();
            var missing = new List<string>();
            var suggestions = new List<string>();

            // Check required fields
            if (!string.IsNullOrEmpty(result.Gender)) completed.Add("Gender");
            else missing.Add("Gender");

            if (result.Age > 0) completed.Add("Age");
            else missing.Add("Age");

            if (!string.IsNullOrEmpty(result.Country)) completed.Add("Country");
            else missing.Add("Country");

            if (!string.IsNullOrEmpty(result.Location)) completed.Add("Location");
            else missing.Add("Location");

            if (!string.IsNullOrEmpty(result.IncomeRange) || result.Income > 0) completed.Add("Income");
            else missing.Add("Income");

            // Check optional fields that add value
            if (!string.IsNullOrEmpty(result.HighestEducation)) completed.Add("Education");
            if (!string.IsNullOrEmpty(result.Occupation)) completed.Add("Occupation");
            if (!string.IsNullOrEmpty(result.MaritalStatus)) completed.Add("Marital Status");

            // Build suggestions
            if (missing.Any())
            {
                suggestions.Add("Complete all required fields to unlock this section");
                if (missing.Contains("Income")) suggestions.Add("Income helps us match you with relevant surveys");
                if (missing.Contains("Location")) suggestions.Add("Location is important for regional surveys");
            }

            var isComplete = missing.Count == 0; // All 5 required fields must be complete

            return new ProfileSectionCompletionDto
            {
                SectionName = "Demographics",
                Weight = 25,
                CompletionPercentage = isComplete ? 25 : 0,
                CompletedFields = completed,
                MissingFields = missing,
                Suggestions = suggestions
            };
        }

        private ProfileSectionCompletionDto BuildBankingSection(dynamic result)
        {
            var completed = new List<string>();
            var missing = new List<string>();
            var suggestions = new List<string>();

            var bankingCount = (int)result.BankingDetailsCount;
            var verifiedCount = (int)result.VerifiedBankingCount;

            if (bankingCount > 0) completed.Add("Banking details added");
            if (verifiedCount > 0) completed.Add("Banking details verified");

            if (bankingCount == 0)
            {
                missing.Add("Banking details");
                suggestions.Add("Add your banking details to receive payments");
            }
            else if (verifiedCount == 0)
            {
                missing.Add("Banking verification");
                suggestions.Add("Your banking details are pending verification");
            }

            var isComplete = verifiedCount > 0;

            return new ProfileSectionCompletionDto
            {
                SectionName = "Banking",
                Weight = 25,
                CompletionPercentage = isComplete ? 25 : 0,
                CompletedFields = completed,
                MissingFields = missing,
                Suggestions = suggestions
            };
        }

        private ProfileSectionCompletionDto BuildDocumentsSection(dynamic result)
        {
            var completed = new List<string>();
            var missing = new List<string>();
            var suggestions = new List<string>();

            var identityDocsCount = (int)result.IdentityDocsCount;
            var approvedDocsCount = (int)result.ApprovedIdentityDocsCount;

            if (identityDocsCount > 0) completed.Add("Identity document uploaded");
            if (approvedDocsCount > 0) completed.Add("Identity document approved");

            if (identityDocsCount == 0)
            {
                missing.Add("Identity document");
                suggestions.Add("Upload a government-issued ID (passport, driver's license, etc.)");
            }
            else if (approvedDocsCount == 0)
            {
                missing.Add("Document verification");
                suggestions.Add("Your identity document is being reviewed");
            }

            var isComplete = approvedDocsCount > 0;

            return new ProfileSectionCompletionDto
            {
                SectionName = "Documents",
                Weight = 25,
                CompletionPercentage = isComplete ? 25 : 0,
                CompletedFields = completed,
                MissingFields = missing,
                Suggestions = suggestions
            };
        }

        private ProfileSectionCompletionDto BuildInterestsSection(dynamic result)
        {
            var completed = new List<string>();
            var missing = new List<string>();
            var suggestions = new List<string>();

            var interestsCount = (int)result.InterestsCount;

            if (interestsCount >= 3)
            {
                completed.Add($"{interestsCount} interests added");
            }
            else
            {
                if (interestsCount > 0) completed.Add($"{interestsCount} interests added");
                missing.Add($"Need {3 - interestsCount} more interests");
                suggestions.Add("Add interests to get better survey matches");
                suggestions.Add("Choose topics you're passionate about");
            }

            var isComplete = interestsCount >= 3;

            return new ProfileSectionCompletionDto
            {
                SectionName = "Interests",
                Weight = 25,
                CompletionPercentage = isComplete ? 25 : 0,
                CompletedFields = completed,
                MissingFields = missing,
                Suggestions = suggestions
            };
        }

        private List<ProfileNextStepDto> BuildNextSteps(UserProfileCompletionDto profile)
        {
            var steps = new List<ProfileNextStepDto>();

            // Get incomplete sections ordered by priority
            var incompleteSections = new[]
            {
                (profile.Demographics, "Demographics", ProfileStepPriority.High, 3),
                (profile.Documents, "Documents", ProfileStepPriority.Critical, 5),
                (profile.Banking, "Banking", ProfileStepPriority.High, 4),
                (profile.Interests, "Interests", ProfileStepPriority.Medium, 2)
            }
            .Where(s => s.Item1.CompletionPercentage < 25)
            .OrderBy(s => s.Item3); // Order by priority

            foreach (var (section, sectionName, priority, timeMinutes) in incompleteSections)
            {
                var step = new ProfileNextStepDto
                {
                    Section = sectionName,
                    Priority = priority,
                    Title = GetStepTitle(sectionName, section),
                    Description = GetStepDescription(sectionName, section),
                    MissingItems = section.MissingFields,
                    ImpactDescription = GetImpactDescription(sectionName),
                    EstimatedTimeMinutes = timeMinutes
                };

                steps.Add(step);
            }

            return steps;
        }

        private string GetStepTitle(string sectionName, ProfileSectionCompletionDto section)
        {
            return sectionName switch
            {
                "Demographics" => "Complete Personal Information",
                "Documents" => "Verify Your Identity",
                "Banking" => "Add Payment Method",
                "Interests" => "Share Your Interests",
                _ => $"Complete {sectionName}"
            };
        }

        private string GetStepDescription(string sectionName, ProfileSectionCompletionDto section)
        {
            return sectionName switch
            {
                "Demographics" => $"Add your {string.Join(", ", section.MissingFields.Take(3))} to unlock survey matching",
                "Documents" => "Upload a government-issued ID to verify your identity and unlock rewards",
                "Banking" => "Add your banking details to receive payments for completed surveys",
                "Interests" => $"Add {section.MissingFields.FirstOrDefault()?.Replace("Need ", "").Replace(" more interests", "")} interests to improve survey recommendations",
                _ => $"Complete the {sectionName} section"
            };
        }

        private string GetImpactDescription(string sectionName)
        {
            return sectionName switch
            {
                "Demographics" => "Enables survey matching and unlocks 25% of your profile",
                "Documents" => "Required for payments and unlocks 25% of your profile",
                "Banking" => "Enables reward payments and unlocks 25% of your profile",
                "Interests" => "Improves survey matching and unlocks 25% of your profile",
                _ => "Contributes to your profile completion"
            };
        }

        private UserProfileCompletionDto CreateEmptyProfileCompletion(string userId)
        {
            return new UserProfileCompletionDto
            {
                UserId = userId,
                OverallCompletionPercentage = 0,
                Demographics = new ProfileSectionCompletionDto
                {
                    SectionName = "Demographics",
                    Weight = 25,
                    CompletionPercentage = 0,
                    MissingFields = new List<string> { "Gender", "Age", "Country", "Location", "Income" },
                    Suggestions = new List<string> { "Start by completing your basic personal information" }
                },
                Banking = new ProfileSectionCompletionDto
                {
                    SectionName = "Banking",
                    Weight = 25,
                    CompletionPercentage = 0,
                    MissingFields = new List<string> { "Banking details" },
                    Suggestions = new List<string> { "Add your banking details to receive payments" }
                },
                Documents = new ProfileSectionCompletionDto
                {
                    SectionName = "Documents",
                    Weight = 25,
                    CompletionPercentage = 0,
                    MissingFields = new List<string> { "Identity document" },
                    Suggestions = new List<string> { "Upload a government-issued ID" }
                },
                Interests = new ProfileSectionCompletionDto
                {
                    SectionName = "Interests",
                    Weight = 25,
                    CompletionPercentage = 0,
                    MissingFields = new List<string> { "Need 3 more interests" },
                    Suggestions = new List<string> { "Add interests to get better survey matches" }
                },
                IsEligibleForSurveys = false,
                LastUpdated = DateTimeOffset.UtcNow,
                NextSteps = new List<ProfileNextStepDto>()
            };
        }

        private List<string> GetNextSteps(dynamic completionData)
        {
            var steps = new List<string>();

            if (completionData.DemographicsPercentage == 0)
                steps.Add("Complete your personal information (age, gender, location, income)");

            if (completionData.BankingPercentage == 0)
                steps.Add("Add and verify your banking details");

            if (completionData.DocumentsPercentage == 0)
                steps.Add("Upload and verify your identity document");

            if (completionData.InterestsPercentage == 0)
                steps.Add("Add at least 3 interests to your profile");

            return steps;
        }

        public async Task<bool> RemoveUserInterestAsync(string userId, string interest)
        {
            const string sql = @"
            DELETE FROM SurveyBucks.UserInterests
            WHERE UserId = @UserId AND Interest = @Interest";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { UserId = userId, Interest = interest });
            return result > 0;
        }

        private async Task AddDemographicChangesAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        DemographicsDto oldDemo,
        DemographicsDto newDemo,
        string changedBy)
        {
            var changes = new List<(string FieldName, string OldValue, string NewValue)>();

            // Compare properties and log changes
            if (oldDemo.Gender != newDemo.Gender)
                changes.Add(("Gender", oldDemo.Gender, newDemo.Gender));

            if (oldDemo.Age != newDemo.Age)
                changes.Add(("Age", oldDemo.Age.ToString(), newDemo.Age.ToString()));

            // Add other property comparisons...

            // Insert change records
            if (changes.Any())
            {
                const string insertChangesSql = @"
                INSERT INTO SurveyBucks.DemographicHistory (
                    UserId, FieldName, OldValue, NewValue, ChangeDate, ChangedBy
                ) VALUES (
                    @UserId, @FieldName, @OldValue, @NewValue, SYSDATETIMEOFFSET(), @ChangedBy
                )";

                foreach (var (fieldName, oldValue, newValue) in changes)
                {
                    await connection.ExecuteAsync(insertChangesSql,
                        new
                        {
                            UserId = newDemo.UserId,
                            FieldName = fieldName,
                            OldValue = oldValue,
                            NewValue = newValue,
                            ChangedBy = changedBy
                        },
                        transaction);
                }
            }
        }


        private async Task UpdateProfileCompletionStatusAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string userId)
        {
            // Count filled fields to calculate completion percentage
            const string getFilledFieldsCountSql = @"
            SELECT 
                CAST(
                    (SUM(CASE WHEN Gender IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN Age != 0 THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN HighestEducation IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN (Income != 0 OR IncomeRange IS NOT NULL) THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN Location IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN Occupation IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN MaritalStatus IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN HouseholdSize IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN HasChildren IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN NumberOfChildren IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN Country IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN State IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN City IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN ZipCode IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN UrbanRural IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN Industry IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN JobTitle IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN YearsOfExperience IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN EmploymentStatus IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN CompanySize IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN FieldOfStudy IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN YearOfGraduation IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN DeviceTypes IS NOT NULL THEN 1 ELSE 0 END) +
                    SUM(CASE WHEN InternetUsageHoursPerWeek IS NOT NULL THEN 1 ELSE 0 END)) * 100.0 / 24 AS INT
                ) AS CompletionPercentage
            FROM SurveyBucks.Demographics
            WHERE UserId = @UserId";

            var completionPercentage = await connection.ExecuteScalarAsync<int>(
                getFilledFieldsCountSql,
                new { UserId = userId },
                transaction);

            // Check if interests are added
            var hasInterests = await connection.ExecuteScalarAsync<bool>(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM SurveyBucks.UserInterests WHERE UserId = @UserId) THEN 1 ELSE 0 END",
                new { UserId = userId },
                transaction);

            // Check if required fields are completed
            var requiredFieldsCompleted = await connection.ExecuteScalarAsync<bool>(
                @"SELECT CASE WHEN 
                Gender IS NOT NULL AND 
                Age > 0 AND 
                (Income > 0 OR IncomeRange IS NOT NULL) AND 
                Location IS NOT NULL AND
                Country IS NOT NULL
              THEN 1 ELSE 0 END
              FROM SurveyBucks.Demographics
              WHERE UserId = @UserId",
                new { UserId = userId },
                transaction);

            // Update profile status
            var exists = await connection.ExecuteScalarAsync<bool>(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM SurveyBucks.DemographicProfileStatus WHERE UserId = @UserId) THEN 1 ELSE 0 END",
                new { UserId = userId },
                transaction);

            if (exists)
            {
                await connection.ExecuteAsync(
                    @"UPDATE SurveyBucks.DemographicProfileStatus
                  SET CompletionPercentage = @CompletionPercentage,
                      RequiredFieldsCompleted = @RequiredFieldsCompleted,
                      InterestsAdded = @InterestsAdded,
                      LastUpdated = SYSDATETIMEOFFSET()
                  WHERE UserId = @UserId",
                    new
                    {
                        UserId = userId,
                        CompletionPercentage = completionPercentage,
                        RequiredFieldsCompleted = requiredFieldsCompleted,
                        InterestsAdded = hasInterests
                    },
                    transaction);
            }
            else
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO SurveyBucks.DemographicProfileStatus (
                    UserId, CompletionPercentage, RequiredFieldsCompleted, 
                    InterestsAdded, LastUpdated
                  ) VALUES (
                    @UserId, @CompletionPercentage, @RequiredFieldsCompleted,
                    @InterestsAdded, SYSDATETIMEOFFSET()
                  )",
                    new
                    {
                        UserId = userId,
                        CompletionPercentage = completionPercentage,
                        RequiredFieldsCompleted = requiredFieldsCompleted,
                        InterestsAdded = hasInterests
                    },
                    transaction);
            }
        }

        private int CalculateWeightedProfileCompletionPercentage(DemographicsDto demographic, bool hasInterests)
        {
            int totalPoints = 0;
            int maxPossiblePoints = 100;

            // Core demographic fields used in matching (higher weights)
            if (demographic.Age > 0) totalPoints += 15;  // Age (15 points)
            if (!string.IsNullOrEmpty(demographic.Gender)) totalPoints += 10;  // Gender (10 points)
            if (!string.IsNullOrEmpty(demographic.Location)) totalPoints += 10;  // Location (10 points)
            if (!string.IsNullOrEmpty(demographic.Country)) totalPoints += 10;  // Country (10 points)
            if (!string.IsNullOrEmpty(demographic.IncomeRange) || demographic.Income > 0) totalPoints += 10;  // Income (10 points)
            if (hasInterests) totalPoints += 10;  // Interests (10 points)

            // Secondary demographic fields (medium weights)
            if (!string.IsNullOrEmpty(demographic.HighestEducation)) totalPoints += 5;  // Education (5 points)
            if (!string.IsNullOrEmpty(demographic.Occupation)) totalPoints += 5;  // Occupation (5 points)
            if (!string.IsNullOrEmpty(demographic.MaritalStatus)) totalPoints += 5;  // MaritalStatus (5 points)
            if (demographic.HouseholdSize > 0) totalPoints += 5;  // HouseholdSize (5 points)
            if (demographic.HasChildren.HasValue) totalPoints += 5;  // ParentalStatus (5 points)
            if (!string.IsNullOrEmpty(demographic.Industry)) totalPoints += 5;  // Industry (5 points)

            // Additional fields with smaller weights
            if (!string.IsNullOrEmpty(demographic.State)) totalPoints += 1;
            if (!string.IsNullOrEmpty(demographic.City)) totalPoints += 1;
            if (!string.IsNullOrEmpty(demographic.ZipCode)) totalPoints += 1;
            if (!string.IsNullOrEmpty(demographic.UrbanRural)) totalPoints += 1;
            if (!string.IsNullOrEmpty(demographic.JobTitle)) totalPoints += (int)0.5;
            if (demographic.YearsOfExperience > 0) totalPoints += (int)0.5;
            if (!string.IsNullOrEmpty(demographic.EmploymentStatus)) totalPoints += (int)0.5;
            if (!string.IsNullOrEmpty(demographic.CompanySize)) totalPoints += (int)0.5;
            if (!string.IsNullOrEmpty(demographic.FieldOfStudy)) totalPoints += (int)0.5;
            if (demographic.YearOfGraduation > 0) totalPoints += (int)0.5;
            if (!string.IsNullOrEmpty(demographic.DeviceTypes)) totalPoints += (int)0.5;
            if (demographic.InternetUsageHoursPerWeek > 0) totalPoints += (int)0.5;

            // Calculate percentage
            return (int)Math.Ceiling((double)totalPoints / maxPossiblePoints * 100);
        }
    }
}
