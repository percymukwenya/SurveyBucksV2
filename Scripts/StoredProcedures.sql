-- =============================================
-- SurveyBucks Stored Procedures
-- Created: 2025-01-08
-- Description: Core stored procedures for survey operations
-- =============================================

-- =============================================
-- 1. Get Matching Surveys Optimized
-- Returns surveys that match user's demographic profile
-- =============================================
CREATE OR ALTER PROCEDURE [SurveyBucks].[up_GetMatchingSurveysOptimized]
    @UserId NVARCHAR(255),
    @MatchThreshold INT = 70
AS
BEGIN
    SET NOCOUNT ON;

    -- Get user profile data
    DECLARE @UserAge INT, @UserGender NVARCHAR(50), @UserLocation NVARCHAR(100),
            @UserCountry NVARCHAR(100), @UserIncome DECIMAL(18,2), @UserEducation NVARCHAR(100);

    SELECT
        @UserAge = Age,
        @UserGender = Gender,
        @UserLocation = Location,
        @UserCountry = Country,
        @UserIncome = Income,
        @UserEducation = Education
    FROM SurveyBucks.Demographics
    WHERE UserId = @UserId AND IsDeleted = 0;

    -- If no profile exists, return empty
    IF @UserAge IS NULL
        RETURN;

    -- Get active published surveys with match scoring
    SELECT DISTINCT
        s.Id,
        s.Name,
        s.Description,
        s.CompanyName,
        s.Industry,
        s.DurationInSeconds,
        s.OpeningDateTime,
        s.ClosingDateTime,
        s.MaxTimeInMins,
        -- Calculate match score (simplified - full logic in C#)
        CAST(100 AS INT) AS MatchScore,
        -- Check if already participated
        CAST(CASE WHEN sp.Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasParticipated,
        -- Participation status
        sp.StatusId AS ParticipationStatus,
        sp.ProgressPercentage
    FROM SurveyBucks.Survey s
    LEFT JOIN SurveyBucks.SurveyParticipation sp ON s.Id = sp.SurveyId
        AND sp.UserId = @UserId
        AND sp.IsDeleted = 0
    WHERE s.IsPublished = 1
        AND s.IsActive = 1
        AND s.IsDeleted = 0
        AND s.StatusId = 3 -- Active status
        AND s.ClosingDateTime > GETUTCDATE()
        AND sp.Id IS NULL -- Only surveys user hasn't started
    ORDER BY s.CreatedDate DESC;
END
GO

-- =============================================
-- 2. Complete Survey Participation
-- Marks a survey as completed and updates statistics
-- =============================================
CREATE OR ALTER PROCEDURE [SurveyBucks].[up_CompleteSurveyParticipation]
    @ParticipationId INT,
    @UserId NVARCHAR(255),
    @CompletedBy NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @SurveyId INT;
        DECLARE @CompletionTime DATETIME2;
        SET @CompletionTime = GETUTCDATE();

        -- Verify participation exists and belongs to user
        SELECT @SurveyId = SurveyId
        FROM SurveyBucks.SurveyParticipation
        WHERE Id = @ParticipationId
            AND UserId = @UserId
            AND IsDeleted = 0;

        IF @SurveyId IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            RETURN 1; -- Participation not found
        END

        -- Update participation status to completed (StatusId = 3)
        UPDATE SurveyBucks.SurveyParticipation
        SET StatusId = 3, -- Completed
            CompletedDate = @CompletionTime,
            ProgressPercentage = 100,
            ModifiedDate = @CompletionTime,
            ModifiedBy = @CompletedBy
        WHERE Id = @ParticipationId;

        -- Calculate total time spent
        DECLARE @TotalTimeSpent INT;
        SELECT @TotalTimeSpent = ISNULL(SUM(TimeSpentSeconds), 0)
        FROM SurveyBucks.SurveyResponse
        WHERE ParticipationId = @ParticipationId AND IsDeleted = 0;

        -- Update time spent on participation
        UPDATE SurveyBucks.SurveyParticipation
        SET TimeSpentSeconds = @TotalTimeSpent
        WHERE Id = @ParticipationId;

        COMMIT TRANSACTION;
        RETURN 0; -- Success
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1; -- Error
    END CATCH
END
GO

-- =============================================
-- 3. Get User Verification Status
-- Returns comprehensive verification status for a user
-- Returns 3 result sets: Status, Documents, Banking
-- =============================================
CREATE OR ALTER PROCEDURE [SurveyBucks].[up_GetUserVerificationStatus]
    @UserId NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Overall Verification Status
    SELECT
        u.Email,
        CAST(CASE WHEN EXISTS(
            SELECT 1 FROM SurveyBucks.Documents
            WHERE UserId = @UserId
                AND DocumentTypeId IN (1, 2) -- ID documents
                AND VerificationStatus = 'Verified'
                AND IsDeleted = 0
        ) THEN 1 ELSE 0 END AS INT) AS HasVerifiedIdentity,

        CAST(CASE WHEN EXISTS(
            SELECT 1 FROM SurveyBucks.BankingDetails
            WHERE UserId = @UserId
                AND VerificationStatus = 'Verified'
                AND IsDeleted = 0
        ) THEN 1 ELSE 0 END AS INT) AS HasVerifiedBanking,

        CASE
            WHEN NOT EXISTS(SELECT 1 FROM SurveyBucks.Documents WHERE UserId = @UserId AND DocumentTypeId = 1 AND IsDeleted = 0)
                THEN 'ID Document, '
            ELSE ''
        END +
        CASE
            WHEN NOT EXISTS(SELECT 1 FROM SurveyBucks.Documents WHERE UserId = @UserId AND DocumentTypeId = 3 AND IsDeleted = 0)
                THEN 'Proof of Address, '
            ELSE ''
        END AS MissingRequiredDocuments,

        CAST(CASE WHEN
            EXISTS(SELECT 1 FROM SurveyBucks.Documents WHERE UserId = @UserId AND DocumentTypeId IN (1,2) AND VerificationStatus = 'Verified' AND IsDeleted = 0)
            AND EXISTS(SELECT 1 FROM SurveyBucks.BankingDetails WHERE UserId = @UserId AND VerificationStatus = 'Verified' AND IsDeleted = 0)
        THEN 1 ELSE 0 END AS INT) AS IsFullyVerified
    FROM AspNetUsers u
    WHERE u.Id = @UserId;

    -- Result Set 2: User Documents
    SELECT
        d.Id,
        d.UserId,
        d.DocumentTypeId,
        dt.Name AS DocumentTypeName,
        d.DocumentName,
        d.DocumentPath,
        d.UploadDate,
        d.VerificationStatus,
        d.VerificationDate,
        d.VerificationNotes,
        d.VerifiedBy
    FROM SurveyBucks.Documents d
    INNER JOIN SurveyBucks.DocumentType dt ON d.DocumentTypeId = dt.Id
    WHERE d.UserId = @UserId AND d.IsDeleted = 0
    ORDER BY d.UploadDate DESC;

    -- Result Set 3: Banking Details
    SELECT
        bd.Id,
        bd.UserId,
        bd.AccountHolderName,
        bd.BankName,
        bd.AccountNumber,
        bd.BranchCode,
        bd.AccountType,
        bd.VerificationStatus,
        bd.VerificationDate,
        bd.VerificationNotes,
        bd.VerifiedBy,
        bd.CreatedDate
    FROM SurveyBucks.BankingDetails bd
    WHERE bd.UserId = @UserId AND bd.IsDeleted = 0
    ORDER BY bd.CreatedDate DESC;
END
GO

-- =============================================
-- 4. Update User Login Streak
-- Updates or creates login streak record for user
-- =============================================
CREATE OR ALTER PROCEDURE [SurveyBucks].[up_UpdateUserLoginStreak]
    @UserId NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);
        DECLARE @LastLoginDate DATE;
        DECLARE @CurrentStreak INT;
        DECLARE @LongestStreak INT;

        -- Get existing streak data
        SELECT
            @LastLoginDate = CAST(LastLoginDate AS DATE),
            @CurrentStreak = CurrentStreak,
            @LongestStreak = LongestStreak
        FROM SurveyBucks.UserEngagement
        WHERE UserId = @UserId;

        -- If no record exists, create one
        IF @LastLoginDate IS NULL
        BEGIN
            INSERT INTO SurveyBucks.UserEngagement (UserId, CurrentStreak, LongestStreak, LastLoginDate, TotalLogins, CreatedDate, CreatedBy)
            VALUES (@UserId, 1, 1, GETUTCDATE(), 1, GETUTCDATE(), @UserId);
            RETURN 1;
        END

        -- If already logged in today, no update needed
        IF @LastLoginDate = @Today
        BEGIN
            RETURN 0;
        END

        -- If logged in yesterday, increment streak
        IF DATEDIFF(DAY, @LastLoginDate, @Today) = 1
        BEGIN
            SET @CurrentStreak = @CurrentStreak + 1;
            SET @LongestStreak = CASE WHEN @CurrentStreak > @LongestStreak THEN @CurrentStreak ELSE @LongestStreak END;
        END
        ELSE -- Streak broken, reset to 1
        BEGIN
            SET @CurrentStreak = 1;
        END

        -- Update engagement record
        UPDATE SurveyBucks.UserEngagement
        SET CurrentStreak = @CurrentStreak,
            LongestStreak = @LongestStreak,
            LastLoginDate = GETUTCDATE(),
            TotalLogins = TotalLogins + 1,
            ModifiedDate = GETUTCDATE(),
            ModifiedBy = @UserId
        WHERE UserId = @UserId;

        RETURN @CurrentStreak;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END
GO

-- =============================================
-- 5. Award Points for Action
-- Awards points to user for various actions (survey, achievement, etc.)
-- =============================================
CREATE OR ALTER PROCEDURE [SurveyBucks].[sp_AwardPointsForAction]
    @UserId NVARCHAR(255),
    @ActionType NVARCHAR(50),
    @ActionCount INT = 1,
    @ReferenceId NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @PointsToAward INT = 0;
        DECLARE @Description NVARCHAR(255);

        -- Determine points based on action type
        IF @ActionType = 'SurveyCompletion'
        BEGIN
            -- Points based on survey (could be enhanced to look up actual survey reward)
            SET @PointsToAward = 100;
            SET @Description = 'Completed Survey';
        END
        ELSE IF @ActionType = 'AchievementUnlock'
        BEGIN
            -- Points from achievement record
            SELECT @PointsToAward = PointsAwarded
            FROM SurveyBucks.Achievements
            WHERE Id = CAST(@ReferenceId AS INT);
            SET @Description = 'Achievement Unlocked';
        END
        ELSE IF @ActionType = 'DailyLogin'
        BEGIN
            SET @PointsToAward = 10;
            SET @Description = 'Daily Login Bonus';
        END
        ELSE IF @ActionType = 'ProfileComplete'
        BEGIN
            SET @PointsToAward = 50;
            SET @Description = 'Profile Completion Bonus';
        END
        ELSE IF @ActionType = 'ChallengeComplete'
        BEGIN
            -- Points from challenge record
            SELECT @PointsToAward = PointsReward
            FROM SurveyBucks.Challenges
            WHERE Id = CAST(@ReferenceId AS INT);
            SET @Description = 'Challenge Completed';
        END

        -- Award points if any
        IF @PointsToAward > 0
        BEGIN
            -- Create points transaction
            INSERT INTO SurveyBucks.UserPoints (
                UserId,
                Points,
                TransactionType,
                Description,
                ReferenceId,
                TransactionDate,
                CreatedDate,
                CreatedBy
            )
            VALUES (
                @UserId,
                @PointsToAward,
                @ActionType,
                @Description,
                @ReferenceId,
                GETUTCDATE(),
                GETUTCDATE(),
                @UserId
            );

            -- Update user's total points
            UPDATE SurveyBucks.UserEngagement
            SET TotalPointsEarned = ISNULL(TotalPointsEarned, 0) + @PointsToAward,
                ModifiedDate = GETUTCDATE(),
                ModifiedBy = @UserId
            WHERE UserId = @UserId;

            -- If UserEngagement doesn't exist, create it
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO SurveyBucks.UserEngagement (
                    UserId,
                    TotalPointsEarned,
                    CurrentStreak,
                    LongestStreak,
                    TotalLogins,
                    CreatedDate,
                    CreatedBy
                )
                VALUES (@UserId, @PointsToAward, 0, 0, 0, GETUTCDATE(), @UserId);
            END
        END

        COMMIT TRANSACTION;
        RETURN @PointsToAward;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END
GO

PRINT 'Stored procedures created successfully!';
