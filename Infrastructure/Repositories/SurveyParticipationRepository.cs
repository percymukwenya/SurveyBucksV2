using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models;
using Domain.Models.Admin;
using Domain.Models.Response;
using Infrastructure.Shared;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SurveyParticipationSummaryDto = Domain.Models.Response.SurveyParticipationSummaryDto;

namespace Infrastructure.Repositories
{
    public class SurveyParticipationRepository : ISurveyParticipationRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IMemoryCache _questionCache;

        public SurveyParticipationRepository(IDatabaseConnectionFactory connectionFactory, IMemoryCache questionCache)
        {
            _connectionFactory = connectionFactory;
            _questionCache = questionCache;
        }

        public async Task<IEnumerable<SurveyListItemDto>> GetMatchingSurveysForUserAsync(string userId, int matchThreshold = 70)
        {
            var cacheKey = $"matching_surveys_{userId}_{matchThreshold}";

            if (_questionCache.TryGetValue(cacheKey, out IEnumerable<SurveyListItemDto> cachedSurveys))
            {
                return cachedSurveys;
            }

            // STEP 1: CHECK PROFILE COMPLETENESS FIRST
            var isProfileComplete = await IsProfileCompleteAsync(userId);

            if (!isProfileComplete.IsComplete)
            {
                // Return empty list - no surveys until profile is 100% complete
                return new List<SurveyListItemDto>();
            }

            // STEP 2: Profile is complete - get matching surveys using existing logic
            var surveys = await GetMatchingSurveysOptimizedAsync(userId, matchThreshold);

            // Enhance with reward information (your existing logic)
            foreach (var survey in surveys)
            {
                survey.Reward = await GetSurveyRewardSummaryAsync(_connectionFactory.CreateConnection(), survey.Id);
            }

            _questionCache.Set(cacheKey, surveys, TimeSpan.FromMinutes(5));
            return surveys;
        }

        private async Task<IEnumerable<SurveyListItemDto>> GetMatchingSurveysOptimizedAsync(string userId, int matchThreshold)
        {
            var cacheKey = $"matching_surveys_{userId}_{matchThreshold}";

            if (_questionCache.TryGetValue(cacheKey, out IEnumerable<SurveyListItemDto> cachedSurveys))
            {
                return cachedSurveys;
            }

            using var connection = _connectionFactory.CreateConnection();

            // Use the stored procedure for matching with both parameters
            var surveys = await connection.QueryAsync<SurveyListItemDto>(
                "SurveyBucks.up_GetMatchingSurveysOptimized",
                new { UserId = userId, MatchThreshold = matchThreshold },
                commandType: CommandType.StoredProcedure);

            // Enhance with reward information
            foreach (var survey in surveys)
            {
                survey.Reward = await GetSurveyRewardSummaryAsync(connection, survey.Id);
            }

            _questionCache.Set(cacheKey, surveys, TimeSpan.FromMinutes(5));

            return surveys;
        }

        private async Task<ProfileCompletenessResult> IsProfileCompleteAsync(string userId)
        {
            const string sql = @"
            WITH ProfileCompletionCheck AS (
                -- 1. DEMOGRAPHICS CHECK (25 points)
                SELECT 
                    CASE 
                        WHEN d.Gender IS NOT NULL 
                             AND d.Age > 0 
                             AND d.Country IS NOT NULL 
                             AND d.Location IS NOT NULL 
                             AND d.Income > 0 
                        THEN 25 ELSE 0 
                    END AS DemographicsPoints,
                    
                    CASE 
                        WHEN d.Gender IS NOT NULL 
                             AND d.Age > 0 
                             AND d.Country IS NOT NULL 
                             AND d.Location IS NOT NULL 
                             AND d.Income > 0 
                        THEN 1 ELSE 0 
                    END AS DemographicsComplete,
                    
                    -- 2. BANKING CHECK (25 points)
                    CASE 
                        WHEN EXISTS (
                            SELECT 1 FROM SurveyBucks.BankingDetail bd 
                            WHERE bd.UserId = @UserId 
                              AND bd.IsVerified = 1 
                              AND bd.IsActive = 1
                              AND bd.IsDeleted = 0
                        ) THEN 25 ELSE 0 
                    END AS BankingPoints,
                    
                    CASE 
                        WHEN EXISTS (
                            SELECT 1 FROM SurveyBucks.BankingDetail bd 
                            WHERE bd.UserId = @UserId 
                              AND bd.IsVerified = 1 
                              AND bd.IsActive = 1
                              AND bd.IsDeleted = 0
                        ) THEN 1 ELSE 0 
                    END AS BankingComplete,
                    
                    -- 3. DOCUMENTS CHECK (25 points)
                    CASE 
                        WHEN EXISTS (
                            SELECT 1 FROM SurveyBucks.UserDocument ud
                            JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                            WHERE ud.UserId = @UserId 
                              AND dt.Category = 'Identity' 
                              AND ud.VerificationStatus = 'Approved'
                              AND ud.IsDeleted = 0
                              AND (ud.ExpiryDate IS NULL OR ud.ExpiryDate > GETDATE())
                        ) THEN 25 ELSE 0 
                    END AS DocumentsPoints,
                    
                    CASE 
                        WHEN EXISTS (
                            SELECT 1 FROM SurveyBucks.UserDocument ud
                            JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                            WHERE ud.UserId = @UserId 
                              AND dt.Category = 'Identity' 
                              AND ud.VerificationStatus = 'Approved'
                              AND ud.IsDeleted = 0
                              AND (ud.ExpiryDate IS NULL OR ud.ExpiryDate > GETDATE())
                        ) THEN 1 ELSE 0 
                    END AS DocumentsComplete,
                    
                    -- 4. INTERESTS CHECK (25 points)
                    CASE 
                        WHEN (
                            SELECT COUNT(*) FROM SurveyBucks.UserInterests ui 
                            WHERE ui.UserId = @UserId
                        ) >= 3 THEN 25 ELSE 0 
                    END AS InterestsPoints,
                    
                    CASE 
                        WHEN (
                            SELECT COUNT(*) FROM SurveyBucks.UserInterests ui 
                            WHERE ui.UserId = @UserId
                        ) >= 3 THEN 1 ELSE 0 
                    END AS InterestsComplete,
                    
                    -- Additional context for debugging/logging
                    d.UserId,
                    (SELECT COUNT(*) FROM SurveyBucks.UserInterests WHERE UserId = @UserId) AS InterestCount,
                    (SELECT COUNT(*) FROM SurveyBucks.BankingDetail WHERE UserId = @UserId AND IsVerified = 1 AND IsDeleted = 0) AS VerifiedBankingCount,
                    (SELECT COUNT(*) FROM SurveyBucks.UserDocument ud 
                     JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                     WHERE ud.UserId = @UserId AND dt.Category = 'Identity' AND ud.VerificationStatus = 'Approved' AND ud.IsDeleted = 0) AS ApprovedDocsCount
                    
                FROM SurveyBucks.Users u
                LEFT JOIN SurveyBucks.Demographics d ON u.Id = d.UserId AND d.IsDeleted = 0
                WHERE u.Id = @UserId
            )
            SELECT 
                DemographicsPoints,
                BankingPoints, 
                DocumentsPoints,
                InterestsPoints,
                (DemographicsPoints + BankingPoints + DocumentsPoints + InterestsPoints) AS TotalPoints,
                CASE 
                    WHEN (DemographicsPoints + BankingPoints + DocumentsPoints + InterestsPoints) >= 100 
                         AND DemographicsComplete = 1 
                         AND BankingComplete = 1 
                         AND DocumentsComplete = 1 
                         AND InterestsComplete = 1
                    THEN 1 ELSE 0 
                END AS IsComplete,
                DemographicsComplete,
                BankingComplete,
                DocumentsComplete, 
                InterestsComplete,
                InterestCount,
                VerifiedBankingCount,
                ApprovedDocsCount
            FROM ProfileCompletionCheck";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.QuerySingleOrDefaultAsync(sql, new { UserId = userId });

            return new ProfileCompletenessResult
            {
                IsComplete = result?.IsComplete == 1,
                TotalPoints = result?.TotalPoints ?? 0,
                DemographicsComplete = result?.DemographicsComplete == 1,
                BankingComplete = result?.BankingComplete == 1,
                DocumentsComplete = result?.DocumentsComplete == 1,
                InterestsComplete = result?.InterestsComplete == 1,
                InterestCount = result?.InterestCount ?? 0,
                VerifiedBankingCount = result?.VerifiedBankingCount ?? 0,
                ApprovedDocsCount = result?.ApprovedDocsCount ?? 0
            };
        }

        public async Task<int> CreateParticipationAsync(SurveyParticipationDto participation)
        {
            const string sql = @"
            INSERT INTO SurveyBucks.SurveyParticipation (
                EnrolmentDateTime, UserId, SurveyId, StatusId,
                ProgressPercentage, CreatedDate, CreatedBy
            ) VALUES (
                @EnrolmentDateTime, @UserId, @SurveyId, @StatusId,
                @ProgressPercentage, @CreatedDate, @CreatedBy
            );
            SELECT CAST(SCOPE_IDENTITY() as int);";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<int>(sql, participation);
        }

        public async Task<int?> GetExistingParticipationIdAsync(string userId, int surveyId)
        {
            const string sql = @"
            SELECT Id FROM SurveyBucks.SurveyParticipation 
            WHERE UserId = @UserId AND SurveyId = @SurveyId AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<int?>(sql, new { UserId = userId, SurveyId = surveyId });
        }

        public async Task<int> EnrollInSurveyAsync(string userId, int surveyId)
        {
            const string sql = @"
                -- Check if already enrolled
                IF NOT EXISTS (
                    SELECT 1 FROM SurveyBucks.SurveyParticipation 
                    WHERE UserId = @UserId AND SurveyId = @SurveyId AND IsDeleted = 0
                )
                BEGIN
                    -- Insert new participation
                    INSERT INTO SurveyBucks.SurveyParticipation (
                        EnrolmentDateTime, UserId, SurveyId, StatusId,
                        ProgressPercentage, CreatedDate, CreatedBy
                    ) VALUES (
                        SYSDATETIMEOFFSET(), @UserId, @SurveyId, 1, -- Status 1 = Enrolled
                        0, SYSDATETIMEOFFSET(), @UserId
                    );
            
                    -- Return the new ID
                    SELECT CAST(SCOPE_IDENTITY() as int);
            
                    -- Add notification
                    INSERT INTO SurveyBucks.UserNotification (
                        UserId, NotificationTypeId, Title, Message, 
                        ReferenceId, ReferenceType, CreatedDate
                    )
                    SELECT 
                        @UserId, Id, 'Survey Enrollment', 
                        'You have successfully enrolled in a new survey!',
                        CONVERT(NVARCHAR(50), @SurveyId), 'Survey', SYSDATETIMEOFFSET()
                    FROM SurveyBucks.NotificationType
                    WHERE Name = 'SurveyInvitation';
            
                    -- Update analytics
                    IF EXISTS (SELECT 1 FROM SurveyBucks.SurveyAnalytics WHERE SurveyId = @SurveyId)
                    BEGIN
                        UPDATE SurveyBucks.SurveyAnalytics
                        SET TotalViews = TotalViews + 1,
                            LastUpdated = SYSDATETIMEOFFSET()
                        WHERE SurveyId = @SurveyId;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO SurveyBucks.SurveyAnalytics (
                            SurveyId, TotalViews, LastUpdated
                        ) VALUES (
                            @SurveyId, 1, SYSDATETIMEOFFSET()
                        );
                    END
                END
                ELSE
                BEGIN
                    -- Return existing participation ID
                    SELECT Id FROM SurveyBucks.SurveyParticipation 
                    WHERE UserId = @UserId AND SurveyId = @SurveyId AND IsDeleted = 0;
                END";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, SurveyId = surveyId });
            }
        }

        public async Task<DetailedProfileCompletionDto> GetDetailedProfileCompletionAsync(string userId)
        {
            var completionResult = await IsProfileCompleteAsync(userId);

            return new DetailedProfileCompletionDto
            {
                IsEligibleForSurveys = completionResult.IsComplete,
                OverallCompletionPercentage = completionResult.TotalPoints,

                Demographics = new SectionCompletionDto
                {
                    IsComplete = completionResult.DemographicsComplete,
                    CompletionPercentage = completionResult.DemographicsComplete ? 25 : 0,
                    RequiredItems = new List<string> { "Gender", "Age", "Country", "Location", "Income" }
                },

                Banking = new SectionCompletionDto
                {
                    IsComplete = completionResult.BankingComplete,
                    CompletionPercentage = completionResult.BankingComplete ? 25 : 0,
                    RequiredItems = new List<string> { "Verified banking details" },
                    CurrentCount = completionResult.VerifiedBankingCount
                },

                Documents = new SectionCompletionDto
                {
                    IsComplete = completionResult.DocumentsComplete,
                    CompletionPercentage = completionResult.DocumentsComplete ? 25 : 0,
                    RequiredItems = new List<string> { "Approved identity document" },
                    CurrentCount = completionResult.ApprovedDocsCount
                },

                Interests = new SectionCompletionDto
                {
                    IsComplete = completionResult.InterestsComplete,
                    CompletionPercentage = completionResult.InterestsComplete ? 25 : 0,
                    RequiredItems = new List<string> { "At least 3 interests" },
                    CurrentCount = completionResult.InterestCount,
                    RequiredCount = 3
                }
            };
        }

        public async Task<SurveyParticipationDto> GetParticipationAsync(int participationId, string userId)
        {
            const string sql = @"
                SELECT 
                    sp.Id, sp.SurveyId, sp.UserId, sp.EnrolmentDateTime, sp.StartedAtDateTime,
                    sp.CompletedAtDateTime, sp.StatusId, ps.Name AS StatusName,
                    sp.ProgressPercentage, sp.CurrentSectionId, sp.CurrentQuestionId,
                    sp.TimeSpentInSeconds, sp.CompletionCode
                FROM SurveyBucks.SurveyParticipation sp
                JOIN SurveyBucks.ParticipationStatus ps ON sp.StatusId = ps.Id
                WHERE sp.Id = @ParticipationId AND sp.UserId = @UserId AND sp.IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<SurveyParticipationDto>(
                sql, new { ParticipationId = participationId, UserId = userId });
        }

        public async Task<IEnumerable<SurveyResponseDto>> GetSavedResponsesAsync(int participationId, string userId)
        {
            const string sql = @"
                SELECT sr.Id, sr.SurveyParticipationId, sr.QuestionId, sr.Answer, 
                        sr.ResponseDateTime, sr.MatrixRowId
                FROM SurveyBucks.SurveyResponse sr
                JOIN SurveyBucks.SurveyParticipation sp ON sr.SurveyParticipationId = sp.Id
                WHERE sr.SurveyParticipationId = @ParticipationId 
                    AND sp.UserId = @UserId 
                    AND sr.IsDeleted = 0
                ORDER BY sr.ResponseDateTime";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<SurveyResponseDto>(
                sql, new { ParticipationId = participationId, UserId = userId });
        }

        public async Task<bool> CompleteSurveyAsync(int participationId, string userId)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteScalarAsync<int>(
                    "SurveyBucks.up_CompleteSurveyParticipation",
                    new { ParticipationId = participationId, UserId = userId, CompletedBy = userId },
                    commandType: CommandType.StoredProcedure);

                return result == 0; // 0 = success
            }
        }

        public async Task<SurveyDetailDto> GetSurveyDetailsAsync(int surveyId, string userId)
        {
            const string surveySql = @"
                SELECT 
                    s.Id, s.Name, s.Description, s.OpeningDateTime, s.ClosingDateTime,
                    s.DurationInSeconds, s.CompanyName, s.CompanyDescription, s.Industry,
                    s.MinQuestions, s.MaxTimeInMins, s.RequireAllQuestions,
                    -- Get participation info if exists
                    sp.Id AS ParticipationId, sp.StatusId, sp.ProgressPercentage, 
                    sp.CurrentSectionId, sp.CurrentQuestionId
                FROM SurveyBucks.Survey s
                LEFT JOIN SurveyBucks.SurveyParticipation sp ON s.Id = sp.SurveyId 
                    AND sp.UserId = @UserId AND sp.IsDeleted = 0
                WHERE s.Id = @SurveyId AND s.IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            var survey = await connection.QuerySingleOrDefaultAsync<SurveyDetailDto>(
                surveySql, new { SurveyId = surveyId, UserId = userId });

            if (survey != null)
            {
                // Only load sections (questions loaded separately)
                const string sectionSql = @"
                        SELECT Id, SurveyId, Name, Description, [Order],
                               (SELECT COUNT(*) FROM SurveyBucks.Question q 
                                WHERE q.SurveySectionId = ss.Id AND q.IsDeleted = 0) AS QuestionCount
                        FROM SurveyBucks.SurveySection ss
                        WHERE SurveyId = @SurveyId AND IsDeleted = 0
                        ORDER BY [Order]";

                survey.Sections = (await connection.QueryAsync<SurveySectionDto>(
                    sectionSql, new { SurveyId = surveyId })).ToList();

                // Load rewards summary
                survey.Rewards = (await GetSurveyRewardsSummaryAsync(connection, surveyId));
            }

            return survey;
        }

        public async Task<SurveyDetailDto> GetSurveyDetailsAsync(int surveyId)
        {
            const string surveySql = @"
                SELECT 
                    s.Id, s.Name, s.Description, s.OpeningDateTime, s.ClosingDateTime,
                    s.DurationInSeconds, s.CompanyName, s.CompanyDescription, s.Industry,
                    s.MinQuestions, s.MaxTimeInMins, s.RequireAllQuestions
                FROM SurveyBucks.Survey s
                WHERE s.Id = @SurveyId AND s.IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var survey = await connection.QuerySingleOrDefaultAsync<SurveyDetailDto>(surveySql, new { SurveyId = surveyId });

                if (survey != null)
                {
                    // Get sections
                    const string sectionSql = @"
                SELECT Id, SurveyId, Name, Description, [Order]
                FROM SurveyBucks.SurveySection
                WHERE SurveyId = @SurveyId AND IsDeleted = 0
                ORDER BY [Order]";

                    survey.Sections = (await connection.QueryAsync<SurveySectionDto>(sectionSql, new { SurveyId = surveyId })).ToList();

                    // Get questions for each section
                    foreach (var section in survey.Sections)
                    {
                        const string questionSql = @"
                    SELECT 
                        q.Id, q.SurveySectionId, q.Text, q.IsMandatory, q.[Order],
                        q.QuestionTypeId, qt.Name as QuestionTypeName, 
                        q.MinValue, q.MaxValue, q.ValidationMessage, q.HelpText
                    FROM SurveyBucks.Question q
                    JOIN SurveyBucks.QuestionType qt ON q.QuestionTypeId = qt.Id
                    WHERE q.SurveySectionId = @SectionId AND q.IsDeleted = 0
                    ORDER BY q.[Order]";

                        section.Questions = (await connection.QueryAsync<QuestionDto>(questionSql, new { SectionId = section.Id })).ToList();

                        // Get response choices for each question
                        foreach (var question in section.Questions)
                        {
                            if (question.QuestionTypeName == "MultipleChoice" ||
                                question.QuestionTypeName == "SingleChoice" ||
                                question.QuestionTypeName == "Dropdown" ||
                                question.QuestionTypeName == "Ranking")
                            {
                                const string choicesSql = @"
                            SELECT Id, QuestionId, Text, Value, [Order], IsExclusiveOption
                            FROM SurveyBucks.QuestionResponseChoice
                            WHERE QuestionId = @QuestionId AND IsDeleted = 0
                            ORDER BY [Order]";

                                question.ResponseChoices = (await connection.QueryAsync<QuestionResponseChoiceDto>(
                                    choicesSql, new { QuestionId = question.Id })).ToList();
                            }

                            if (question.QuestionTypeName == "Matrix")
                            {
                                // Get matrix rows
                                const string rowsSql = @"
                            SELECT Id, QuestionId, Text, [Order]
                            FROM SurveyBucks.MatrixRows
                            WHERE QuestionId = @QuestionId AND IsDeleted = 0
                            ORDER BY [Order]";

                                question.MatrixRows = (await connection.QueryAsync<MatrixRowDto>(
                                    rowsSql, new { QuestionId = question.Id })).ToList();

                                // Get matrix columns
                                const string columnsSql = @"
                            SELECT Id, QuestionId, Text, Value, [Order]
                            FROM SurveyBucks.MatrixColumns
                            WHERE QuestionId = @QuestionId AND IsDeleted = 0
                            ORDER BY [Order]";

                                question.MatrixColumns = (await connection.QueryAsync<MatrixColumnDto>(
                                    columnsSql, new { QuestionId = question.Id })).ToList();
                            }

                            // Get media attachments
                            if (question.QuestionTypeName == "Image" || question.QuestionTypeName == "FileUpload")
                            {
                                const string mediaSql = @"
                            SELECT qm.Id, qm.QuestionId, qm.MediaTypeId, mt.Name as MediaTypeName,
                                qm.FileName, qm.FileSize, qm.StoragePath, qm.DisplayOrder, qm.AltText
                            FROM SurveyBucks.QuestionMedia qm
                            JOIN SurveyBucks.MediaType mt ON qm.MediaTypeId = mt.Id
                            WHERE qm.QuestionId = @QuestionId AND qm.IsDeleted = 0
                            ORDER BY qm.DisplayOrder";

                                question.Media = (await connection.QueryAsync<QuestionMediaDto>(
                                    mediaSql, new { QuestionId = question.Id })).ToList();
                            }
                        }
                    }

                    // Get rewards
                    const string rewardsSql = @"
                        SELECT 
                            Id, SurveyId, Name, Description, Amount, RewardType, RewardCategory,
                            PointsCost, MonetaryValue, ImageUrl, RedemptionInstructions, MinimumUserLevel
                        FROM SurveyBucks.Rewards
                        WHERE SurveyId = @SurveyId AND IsActive = 1 AND IsDeleted = 0";

                    survey.Rewards = (await connection.QueryAsync<RewardDto>(rewardsSql, new { SurveyId = surveyId })).ToList();
                }

                return survey;
            }
        }

        public async Task<SurveySectionDetailDto> GetSectionWithQuestionsAsync(int sectionId, string userId)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                // Use multiple result sets to minimize round trips
                const string multiQuerySql = @"
                    -- Get section details
                    SELECT ss.Id, ss.SurveyId, ss.Name, ss.Description, ss.[Order],
                           s.RequireAllQuestions, s.MaxTimeInMins
                    FROM SurveyBucks.SurveySection ss
                    JOIN SurveyBucks.Survey s ON ss.SurveyId = s.Id
                    WHERE ss.Id = @SectionId AND ss.IsDeleted = 0;
            
                    -- Get questions
                    SELECT q.Id, q.SurveySectionId, q.Text, q.IsMandatory, q.[Order],
                           q.QuestionTypeId, qt.Name as QuestionTypeName, qt.HasChoices, qt.HasMatrix,
                           q.MinValue, q.MaxValue, q.ValidationMessage, q.HelpText,
                           q.IsScreeningQuestion, q.TimeoutInSeconds, q.RandomizeChoices
                    FROM SurveyBucks.Question q
                    JOIN SurveyBucks.QuestionType qt ON q.QuestionTypeId = qt.Id
                    WHERE q.SurveySectionId = @SectionId AND q.IsDeleted = 0
                    ORDER BY q.[Order];
            
                    -- Get all choices for questions in this section
                    SELECT qrc.Id, qrc.QuestionId, qrc.Text, qrc.Value, qrc.[Order], qrc.IsExclusiveOption
                    FROM SurveyBucks.QuestionResponseChoice qrc
                    JOIN SurveyBucks.Question q ON qrc.QuestionId = q.Id
                    WHERE q.SurveySectionId = @SectionId AND qrc.IsDeleted = 0 AND q.IsDeleted = 0
                    ORDER BY qrc.QuestionId, qrc.[Order];
            
                    -- Get matrix rows
                    SELECT mr.Id, mr.QuestionId, mr.Text, mr.[Order]
                    FROM SurveyBucks.MatrixRows mr
                    JOIN SurveyBucks.Question q ON mr.QuestionId = q.Id
                    WHERE q.SurveySectionId = @SectionId AND mr.IsDeleted = 0 AND q.IsDeleted = 0
                    ORDER BY mr.QuestionId, mr.[Order];
            
                    -- Get matrix columns
                    SELECT mc.Id, mc.QuestionId, mc.Text, mc.Value, mc.[Order]
                    FROM SurveyBucks.MatrixColumns mc
                    JOIN SurveyBucks.Question q ON mc.QuestionId = q.Id
                    WHERE q.SurveySectionId = @SectionId AND mc.IsDeleted = 0 AND q.IsDeleted = 0
                    ORDER BY mc.QuestionId, mc.[Order];
            
                    -- Get saved responses
                    SELECT sr.QuestionId, sr.Answer, sr.MatrixRowId, sr.ResponseDateTime
                    FROM SurveyBucks.SurveyResponse sr
                    JOIN SurveyBucks.Question q ON sr.QuestionId = q.Id
                    JOIN SurveyBucks.SurveyParticipation sp ON sr.SurveyParticipationId = sp.Id
                    WHERE q.SurveySectionId = @SectionId AND sp.UserId = @UserId 
                      AND sr.IsDeleted = 0 AND q.IsDeleted = 0 AND sp.IsDeleted = 0;";

                using var multi = await connection.QueryMultipleAsync(multiQuerySql, new { SectionId = sectionId, UserId = userId });

                // Read section details
                var section = await multi.ReadSingleOrDefaultAsync<SurveySectionDetailDto>();
                if (section == null) return null;

                // Read questions
                var questions = (await multi.ReadAsync<ParticipationQuestionDetailDto>()).ToList();

                // Read choices and group by question
                var choices = await multi.ReadAsync<QuestionResponseChoiceDto>();
                var choicesDict = choices.GroupBy(c => c.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

                // Read matrix rows and group by question
                var matrixRows = await multi.ReadAsync<MatrixRowDto>();
                var rowsDict = matrixRows.GroupBy(r => r.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

                // Read matrix columns and group by question
                var matrixColumns = await multi.ReadAsync<MatrixColumnDto>();
                var columnsDict = matrixColumns.GroupBy(c => c.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

                // Read responses and group by question
                var responses = await multi.ReadAsync<SurveyResponseDto>();
                var responsesDict = responses.GroupBy(r => r.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

                // Assign related data to questions
                foreach (var question in questions)
                {
                    question.ResponseChoices = choicesDict.GetValueOrDefault(question.Id, new List<QuestionResponseChoiceDto>());
                    question.MatrixRows = rowsDict.GetValueOrDefault(question.Id, new List<MatrixRowDto>());
                    question.MatrixColumns = columnsDict.GetValueOrDefault(question.Id, new List<MatrixColumnDto>());
                    question.SavedResponses = responsesDict.GetValueOrDefault(question.Id, new List<SurveyResponseDto>());

                    // Randomize choices if required
                    if (question.RandomizeChoices && question.ResponseChoices.Any())
                    {
                        question.ResponseChoices = question.ResponseChoices.OrderBy(_ => Guid.NewGuid()).ToList();
                    }
                }

                section.Questions = questions;
                return section;
            }
        }

        private async Task LoadQuestionChoicesAsync(IDbConnection connection, List<ParticipationQuestionDetailDto> questions)
        {
            var questionIds = questions.Where(q => q.HasChoices).Select(q => q.Id).ToArray();
            if (!questionIds.Any()) return;

            var choicesDict = new Dictionary<int, List<QuestionResponseChoiceDto>>();

            const string choicesSql = @"
                SELECT Id, QuestionId, Text, Value, [Order], IsExclusiveOption
                FROM SurveyBucks.QuestionResponseChoice
                WHERE QuestionId IN @QuestionIds AND IsDeleted = 0
                ORDER BY QuestionId, [Order]";

            var choices = await connection.QueryAsync<QuestionResponseChoiceDto>(
                choicesSql, new { QuestionIds = questionIds });

            foreach (var choice in choices)
            {
                if (!choicesDict.ContainsKey(choice.QuestionId))
                    choicesDict[choice.QuestionId] = new List<QuestionResponseChoiceDto>();

                choicesDict[choice.QuestionId].Add(choice);
            }

            // Assign choices to questions
            foreach (var question in questions.Where(q => q.HasChoices))
            {
                question.ResponseChoices = choicesDict.GetValueOrDefault(question.Id, new List<QuestionResponseChoiceDto>());
            }
        }

        private async Task LoadMatrixDataAsync(IDbConnection connection, List<ParticipationQuestionDetailDto> questions)
        {
            var matrixQuestionIds = questions.Where(q => q.HasMatrix).Select(q => q.Id).ToArray();
            if (!matrixQuestionIds.Any()) return;

            // Load rows and columns in parallel
            var rowsTask = connection.QueryAsync<MatrixRowDto>(@"
                SELECT Id, QuestionId, Text, [Order]
                FROM SurveyBucks.MatrixRows
                WHERE QuestionId IN @QuestionIds AND IsDeleted = 0
                ORDER BY QuestionId, [Order]", new { QuestionIds = matrixQuestionIds });

            var columnsTask = connection.QueryAsync<MatrixColumnDto>(@"
                SELECT Id, QuestionId, Text, Value, [Order]
                FROM SurveyBucks.MatrixColumns
                WHERE QuestionId IN @QuestionIds AND IsDeleted = 0
                ORDER BY QuestionId, [Order]", new { QuestionIds = matrixQuestionIds });

            await Task.WhenAll(rowsTask, columnsTask);

            var rows = await rowsTask;
            var columns = await columnsTask;

            // Group by question
            var rowsDict = rows.GroupBy(r => r.QuestionId).ToDictionary(g => g.Key, g => g.ToList());
            var columnsDict = columns.GroupBy(c => c.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

            // Assign to questions
            foreach (var question in questions.Where(q => q.HasMatrix))
            {
                question.MatrixRows = rowsDict.GetValueOrDefault(question.Id, new List<MatrixRowDto>());
                question.MatrixColumns = columnsDict.GetValueOrDefault(question.Id, new List<MatrixColumnDto>());
            }
        }

        private async Task LoadSavedResponsesAsync(IDbConnection connection, List<ParticipationQuestionDetailDto> questions, string userId, int surveyId)
        {
            // Get participation ID
            const string participationSql = @"
                SELECT Id FROM SurveyBucks.SurveyParticipation
                WHERE SurveyId = @SurveyId AND UserId = @UserId AND IsDeleted = 0";

            var participationId = await connection.QuerySingleOrDefaultAsync<int?>(
                participationSql, new { SurveyId = surveyId, UserId = userId });

            if (!participationId.HasValue) return;

            var questionIds = questions.Select(q => q.Id).ToArray();

            const string responsesSql = @"
                SELECT sr.QuestionId, sr.Answer, sr.MatrixRowId, sr.ResponseDateTime
                FROM SurveyBucks.SurveyResponse sr
                WHERE sr.SurveyParticipationId = @ParticipationId 
                  AND sr.QuestionId IN @QuestionIds
                  AND sr.IsDeleted = 0";

            var responses = await connection.QueryAsync<SurveyResponseDto>(
                responsesSql, new { ParticipationId = participationId, QuestionIds = questionIds });

            var responsesDict = responses.GroupBy(r => r.QuestionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Assign responses to questions
            foreach (var question in questions)
            {
                question.SavedResponses = responsesDict.GetValueOrDefault(question.Id, new List<SurveyResponseDto>());
            }
        }

        // Get minimal survey progress info for navigation
        public async Task<SurveyProgressDto> GetSurveyProgressAsync(int surveyId, string userId)
        {
            const string sql = @"
            SELECT 
                s.Id AS SurveyId, s.Name AS SurveyName,
                sp.Id AS ParticipationId, sp.StatusId, ps.Name AS StatusName,
                sp.ProgressPercentage, sp.CurrentSectionId, sp.CurrentQuestionId,
                sp.TimeSpentInSeconds, s.MaxTimeInMins * 60 AS MaxTimeInSeconds,
                -- Section navigation info
                (SELECT COUNT(*) FROM SurveyBucks.SurveySection WHERE SurveyId = s.Id AND IsDeleted = 0) AS TotalSections,
                (SELECT COUNT(*) FROM SurveyBucks.Question q
                 JOIN SurveyBucks.SurveySection ss ON q.SurveySectionId = ss.Id
                 WHERE ss.SurveyId = s.Id AND q.IsDeleted = 0 AND ss.IsDeleted = 0) AS TotalQuestions,
                (SELECT COUNT(DISTINCT sr.QuestionId) FROM SurveyBucks.SurveyResponse sr
                 WHERE sr.SurveyParticipationId = sp.Id AND sr.IsDeleted = 0) AS AnsweredQuestions
            FROM SurveyBucks.Survey s
            JOIN SurveyBucks.SurveyParticipation sp ON s.Id = sp.SurveyId
            JOIN SurveyBucks.ParticipationStatus ps ON sp.StatusId = ps.Id
            WHERE s.Id = @SurveyId AND sp.UserId = @UserId AND s.IsDeleted = 0 AND sp.IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<SurveyProgressDto>(sql, new { SurveyId = surveyId, UserId = userId });
            }
        }

        public async Task<NavigationDto> GetSurveyNavigationAsync(int surveyId, string userId)
        {
            const string sql = @"
                WITH SectionNavigation AS (
                    SELECT ss.Id, ss.Name, ss.[Order], ss.SurveyId,
                           COUNT(q.Id) AS QuestionCount,
                           COUNT(CASE WHEN sr.Id IS NOT NULL THEN 1 END) AS AnsweredCount
                    FROM SurveyBucks.SurveySection ss
                    LEFT JOIN SurveyBucks.Question q ON ss.Id = q.SurveySectionId AND q.IsDeleted = 0
                    LEFT JOIN SurveyBucks.SurveyResponse sr ON q.Id = sr.QuestionId 
                        AND sr.SurveyParticipationId = (
                            SELECT Id FROM SurveyBucks.SurveyParticipation 
                            WHERE SurveyId = @SurveyId AND UserId = @UserId AND IsDeleted = 0
                        )
                        AND sr.IsDeleted = 0
                    WHERE ss.SurveyId = @SurveyId AND ss.IsDeleted = 0
                    GROUP BY ss.Id, ss.Name, ss.[Order], ss.SurveyId
                )
                SELECT Id, Name, [Order], QuestionCount, AnsweredCount,
                       CASE WHEN QuestionCount > 0 THEN 
                            CAST((AnsweredCount * 100.0 / QuestionCount) AS INT) 
                            ELSE 0 END AS CompletionPercentage
                FROM SectionNavigation
                ORDER BY [Order]";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var sections = await connection.QueryAsync<SectionNavigationDto>(sql, new { SurveyId = surveyId, UserId = userId });

                return new NavigationDto
                {
                    SurveyId = surveyId,
                    Sections = sections.ToList()
                };
            }
        }

        public async Task<IEnumerable<SurveyParticipationSummaryDto>> GetUserParticipationsAsync(string userId, string status = null)
        {
            string sql = @"
                SELECT 
                    sp.Id, sp.SurveyId, s.Name AS SurveyName,
                    sp.EnrolmentDateTime, sp.StartedAtDateTime, sp.CompletedAtDateTime,
                    ps.Name AS StatusName, sp.ProgressPercentage, sp.TimeSpentInSeconds
                FROM SurveyBucks.SurveyParticipation sp
                JOIN SurveyBucks.Survey s ON sp.SurveyId = s.Id
                JOIN SurveyBucks.ParticipationStatus ps ON sp.StatusId = ps.Id
                WHERE sp.UserId = @UserId AND sp.IsDeleted = 0";

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND ps.Name = @Status";
            }

            sql += " ORDER BY CASE WHEN sp.CompletedAtDateTime IS NULL THEN 0 ELSE 1 END, sp.EnrolmentDateTime DESC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<SurveyParticipationSummaryDto>(
                    sql, new { UserId = userId, Status = status });
            }
        }

        public async Task<bool> SaveSurveyResponseAsync(SurveyResponseDto response)
        {
            const string upsertSql = @"
                DECLARE @ResponseId INT;
        
                -- Use MERGE for better performance and concurrency
                MERGE SurveyBucks.SurveyResponse AS target
                USING (
                    SELECT @SurveyParticipationId AS SurveyParticipationId,
                           @QuestionId AS QuestionId,
                           @MatrixRowId AS MatrixRowId,
                           @Answer AS Answer
                ) AS source 
                ON target.SurveyParticipationId = source.SurveyParticipationId 
                   AND target.QuestionId = source.QuestionId
                   AND (target.MatrixRowId = source.MatrixRowId OR (target.MatrixRowId IS NULL AND source.MatrixRowId IS NULL))
                   AND target.IsDeleted = 0
                WHEN MATCHED THEN
                    UPDATE SET 
                        Answer = source.Answer,
                        ResponseDateTime = SYSDATETIMEOFFSET(),
                        ModifiedDate = SYSDATETIMEOFFSET(),
                        ModifiedBy = (SELECT UserId FROM SurveyBucks.SurveyParticipation WHERE Id = @SurveyParticipationId)
                WHEN NOT MATCHED THEN
                    INSERT (Answer, ResponseDateTime, SurveyParticipationId, QuestionId, MatrixRowId, CreatedDate, CreatedBy)
                    VALUES (source.Answer, SYSDATETIMEOFFSET(), source.SurveyParticipationId, source.QuestionId, 
                           source.MatrixRowId, SYSDATETIMEOFFSET(), 
                           (SELECT UserId FROM SurveyBucks.SurveyParticipation WHERE Id = @SurveyParticipationId));
        
                SET @ResponseId = SCOPE_IDENTITY();
        
                SELECT @ResponseId AS ResponseId;";

            using var connection = _connectionFactory.CreateConnection();

            var result = await connection.QuerySingleOrDefaultAsync<int?>(upsertSql, response);

            return result > 0;
        }

        public async Task<bool> UpdateParticipationProgressAsync(int participationId, string userId, int sectionId, int questionId, int progressPercentage)
        {
            const string sql = @"
            UPDATE SurveyBucks.SurveyParticipation
            SET 
                StartedAtDateTime = CASE WHEN StartedAtDateTime IS NULL THEN SYSDATETIMEOFFSET() ELSE StartedAtDateTime END,
                StatusId = CASE WHEN StatusId = 1 THEN 2 ELSE StatusId END,
                ProgressPercentage = @ProgressPercentage,
                CurrentSectionId = @SectionId,
                CurrentQuestionId = @QuestionId,
                LastQuestionAnsweredId = @QuestionId,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @UserId
            WHERE Id = @ParticipationId AND UserId = @UserId AND IsDeleted = 0
              AND StatusId NOT IN (3, 7)"; // Not Completed or Rewarded

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new
            {
                ParticipationId = participationId,
                UserId = userId,
                SectionId = sectionId,
                QuestionId = questionId,
                ProgressPercentage = progressPercentage
            });

            return result > 0;
        }

        private async Task<RewardSummaryDto> GetSurveyRewardSummaryAsync(IDbConnection connection, int surveyId)
        {
            const string sql = @"
            SELECT TOP 1 
                RewardType,
                Amount,
                CASE WHEN RewardType = 'Points' THEN Amount ELSE NULL END AS PointsAmount,
                Description
            FROM SurveyBucks.Rewards
            WHERE SurveyId = @SurveyId AND IsActive = 1 AND IsDeleted = 0";

            return await connection.QuerySingleOrDefaultAsync<RewardSummaryDto>(sql, new { SurveyId = surveyId });
        }

        private async Task<List<RewardDto>> GetSurveyRewardsSummaryAsync(IDbConnection connection, int surveyId)
        {
            const string rewardsSql = @"
                        SELECT 
                            Id, SurveyId, Name, Description, Amount, RewardType, RewardCategory,
                            PointsCost, MonetaryValue, ImageUrl, RedemptionInstructions, MinimumUserLevel
                        FROM SurveyBucks.Rewards
                        WHERE SurveyId = @SurveyId AND IsActive = 1 AND IsDeleted = 0"
            ;

            var result = await connection.QueryAsync<RewardDto>(rewardsSql, new { SurveyId = surveyId });

            return result.ToList();
        }

        private async Task ProcessAnalyticsQueueAsync(IDbConnection connection)
        {
            const string processSql = @"
                -- Update question analytics from queue
                MERGE SurveyBucks.QuestionAnalytics AS target
                USING (
                    SELECT QuestionId, SUM(Value) AS TotalResponses
                    FROM #AnalyticsQueue
                    WHERE ActionType = 'response_saved'
                    GROUP BY QuestionId
                ) AS source ON target.QuestionId = source.QuestionId
                WHEN MATCHED THEN
                    UPDATE SET 
                        TotalResponses = TotalResponses + source.TotalResponses,
                        LastUpdated = SYSDATETIMEOFFSET()
                WHEN NOT MATCHED THEN
                    INSERT (QuestionId, TotalResponses, LastUpdated)
                    VALUES (source.QuestionId, source.TotalResponses, SYSDATETIMEOFFSET());
        
                -- Clear the queue
                DELETE FROM #AnalyticsQueue;";

            await connection.ExecuteAsync(processSql);
        }

        public async Task<bool> SaveMultipleResponsesAsync(List<SurveyResponseDto> responses, string userId)
        {
            if (!responses.Any()) return true;

            const string bulkInsertSql = @"
                DECLARE @ResponseTable TABLE (
                    QuestionId INT,
                    Answer NVARCHAR(MAX),
                    MatrixRowId INT,
                    SurveyParticipationId INT,
                    ResponseDateTime DATETIMEOFFSET,
                    UserId NVARCHAR(255)
                );
        
                -- Insert all responses into temp table
                INSERT INTO @ResponseTable (QuestionId, Answer, MatrixRowId, SurveyParticipationId, ResponseDateTime, UserId)
                SELECT QuestionId, Answer, MatrixRowId, SurveyParticipationId, SYSDATETIMEOFFSET(), @UserId
                FROM OPENJSON(@ResponsesJson) WITH (
                    QuestionId INT '$.QuestionId',
                    Answer NVARCHAR(MAX) '$.Answer',
                    MatrixRowId INT '$.MatrixRowId',
                    SurveyParticipationId INT '$.SurveyParticipationId'
                );
        
                -- Perform bulk merge operation
                MERGE SurveyBucks.SurveyResponse AS target
                USING @ResponseTable AS source
                ON target.SurveyParticipationId = source.SurveyParticipationId 
                   AND target.QuestionId = source.QuestionId
                   AND (target.MatrixRowId = source.MatrixRowId OR (target.MatrixRowId IS NULL AND source.MatrixRowId IS NULL))
                   AND target.IsDeleted = 0
                WHEN MATCHED THEN
                    UPDATE SET 
                        Answer = source.Answer,
                        ResponseDateTime = source.ResponseDateTime,
                        ModifiedDate = source.ResponseDateTime,
                        ModifiedBy = source.UserId
                WHEN NOT MATCHED THEN
                    INSERT (Answer, ResponseDateTime, SurveyParticipationId, QuestionId, MatrixRowId, CreatedDate, CreatedBy)
                    VALUES (source.Answer, source.ResponseDateTime, source.SurveyParticipationId, 
                           source.QuestionId, source.MatrixRowId, source.ResponseDateTime, source.UserId);
        
                -- Update analytics for all questions at once
                MERGE SurveyBucks.QuestionAnalytics AS target
                USING (
                    SELECT QuestionId, COUNT(*) AS ResponseCount
                    FROM @ResponseTable
                    GROUP BY QuestionId
                ) AS source ON target.QuestionId = source.QuestionId
                WHEN MATCHED THEN
                    UPDATE SET 
                        TotalResponses = TotalResponses + source.ResponseCount,
                        LastUpdated = SYSDATETIMEOFFSET()
                WHEN NOT MATCHED THEN
                    INSERT (QuestionId, TotalResponses, LastUpdated)
                    VALUES (source.QuestionId, source.ResponseCount, SYSDATETIMEOFFSET());
        
                SELECT @@ROWCOUNT AS AffectedRows;";

            using var connection = _connectionFactory.CreateConnection();
            var responsesJson = JsonSerializer.Serialize(responses);
            var affectedRows = await connection.ExecuteScalarAsync<int>(bulkInsertSql,
                new { ResponsesJson = responsesJson, UserId = userId });

            return affectedRows > 0;
        }

        public async Task<QuestionValidationDto> GetQuestionDetailsForValidationAsync(int questionId)
        {
            var cacheKey = $"question_validation_{questionId}";

            if (_questionCache.TryGetValue(cacheKey, out QuestionValidationDto cachedQuestion))
            {
                return cachedQuestion;
            }

            const string sql = @"
                SELECT 
                    q.Id, q.Text, q.IsMandatory, q.QuestionTypeId, qt.Name AS QuestionTypeName,
                    q.MinValue, q.MaxValue, q.ValidationMessage, q.HelpText,
                    q.IsScreeningQuestion, q.ScreeningLogic, q.RandomizeChoices,
                    qt.HasChoices, qt.HasMinMaxValues, qt.HasFreeText, qt.HasMatrix,
                    qt.ValidationRegex, qt.DefaultMinValue, qt.DefaultMaxValue
                FROM SurveyBucks.Question q
                JOIN SurveyBucks.QuestionType qt ON q.QuestionTypeId = qt.Id
                WHERE q.Id = @QuestionId AND q.IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var question = await connection.QuerySingleOrDefaultAsync<QuestionValidationDto>(sql, new { QuestionId = questionId });

                if (question != null)
                {
                    // Cache for 10 minutes
                    _questionCache.Set(cacheKey, question, TimeSpan.FromMinutes(10));
                }

                return question;
            }
        }
    }
}
