using Domain.Models.Response;

namespace Domain.Models.Admin
{
    public class SurveyAdminListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime OpeningDateTime { get; set; }
        public DateTime ClosingDateTime { get; set; }
        public bool IsPublished { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public string CompanyName { get; set; }
        public string Industry { get; set; }
        public int TotalParticipations { get; set; }
        public int CompletedParticipations { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    public class SurveyAdminDetailDto : SurveyDetailDto
    {
        public bool IsPublished { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public int Version { get; set; }
        public bool IsLatestVersion { get; set; }
        public int? PreviousVersionId { get; set; }
        public string VersionNotes { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        // Targeting information
        public List<AgeRangeTargetDto> AgeRangeTargets { get; set; }
        public List<GenderTargetDto> GenderTargets { get; set; }
        public List<LocationTargetDto> LocationTargets { get; set; }
        public List<EducationTargetDto> EducationTargets { get; set; }
        public List<OccupationTargetDto> OccupationTargets { get; set; }
        public List<IncomeRangeTargetDto> IncomeRangeTargets { get; set; }
        public List<InterestTargetDto> InterestTargets { get; set; }
    }

    public class SurveyCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime OpeningDateTime { get; set; }
        public DateTime ClosingDateTime { get; set; }
        public int DurationInSeconds { get; set; }
        public bool IsPublished { get; set; }
        public bool IsActive { get; set; }
        public int StatusId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string Industry { get; set; }
        public int MinQuestions { get; set; }
        public int MaxTimeInMins { get; set; }
        public bool RequireAllQuestions { get; set; }
        public int? TemplateId { get; set; }
    }

    public class SurveyUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime OpeningDateTime { get; set; }
        public DateTime ClosingDateTime { get; set; }
        public int DurationInSeconds { get; set; }
        public int StatusId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string Industry { get; set; }
        public int MinQuestions { get; set; }
        public int MaxTimeInMins { get; set; }
        public bool RequireAllQuestions { get; set; }
        public string VersionNotes { get; set; }
    }

    // Section and Question Management
    public class SurveySectionCreateDto
    {
        public int SurveyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
    }

    public class SurveySectionUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
    }

    public class SectionOrderDto
    {
        public int SectionId { get; set; }
        public int NewOrder { get; set; }
    }

    public class QuestionTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool HasChoices { get; set; }
        public bool HasMinMaxValues { get; set; }
        public bool HasFreeText { get; set; }
        public bool HasMedia { get; set; }
        public bool HasMatrix { get; set; }
    }

    public class QuestionCreateDto
    {
        public int SurveySectionId { get; set; }
        public string Text { get; set; }
        public bool IsMandatory { get; set; }
        public int Order { get; set; }
        public int QuestionTypeId { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public string ValidationMessage { get; set; }
        public string HelpText { get; set; }
        public bool IsScreeningQuestion { get; set; }
        public string ScreeningLogic { get; set; }
        public int? TimeoutInSeconds { get; set; }
        public bool RandomizeChoices { get; set; }
    }

    public class QuestionUpdateDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool IsMandatory { get; set; }
        public int Order { get; set; }
        public int QuestionTypeId { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public string ValidationMessage { get; set; }
        public string HelpText { get; set; }
        public bool IsScreeningQuestion { get; set; }
        public string ScreeningLogic { get; set; }
        public int? TimeoutInSeconds { get; set; }
        public bool RandomizeChoices { get; set; }
    }

    public class QuestionOrderDto
    {
        public int QuestionId { get; set; }
        public int NewOrder { get; set; }
    }

    public class QuestionDetailDto : QuestionDto
    {
        public bool IsScreeningQuestion { get; set; }
        public string ScreeningLogic { get; set; }
        public int? TimeoutInSeconds { get; set; }
        public bool RandomizeChoices { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class QuestionChoiceCreateDto
    {
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public int Order { get; set; }
        public bool IsExclusiveOption { get; set; }
    }

    public class QuestionChoiceUpdateDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public int Order { get; set; }
        public bool IsExclusiveOption { get; set; }
    }

    public class QuestionMediaDto
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public int MediaTypeId { get; set; }
        public string MediaTypeName { get; set; }
        public string FileName { get; set; }
        public int FileSize { get; set; }
        public string StoragePath { get; set; }
        public int DisplayOrder { get; set; }
        public string AltText { get; set; }
    }

    public class MatrixRowDto
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public int Order { get; set; }
    }

    public class MatrixColumnDto
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public int Order { get; set; }
    }

    // Demographic Targeting
    public class AgeRangeTargetDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
    }

    public class GenderTargetDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Gender { get; set; }
    }

    public class LocationTargetDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Location { get; set; }
    }

    public class EducationTargetDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Education { get; set; }
    }

    public class OccupationTargetDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Occupation { get; set; }
    }

    public class IncomeRangeTargetDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public decimal MinIncome { get; set; }
        public decimal MaxIncome { get; set; }
    }

    public class InterestTargetDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Interest { get; set; }
        public int? MinInterestLevel { get; set; }
    }

    // Rewards Management
    public class RewardAdminDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string SurveyName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }
        public string RewardType { get; set; }
        public string RewardCategory { get; set; }
        public int? PointsCost { get; set; }
        public bool IsActive { get; set; }
        public int AvailableQuantity { get; set; }
        public int RedeemedCount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }

    public class RewardDetailAdminDto : RewardAdminDto
    {
        public decimal? MonetaryValue { get; set; }
        public string ImageUrl { get; set; }
        public string RedemptionInstructions { get; set; }
        public string TermsAndConditions { get; set; }
        public int? MinimumUserLevel { get; set; }
        public string RedemptionUrl { get; set; }
        public bool IsExternallyFulfilled { get; set; }
        public string ExternalReferenceId { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public List<UserRewardSummaryDto> Redemptions { get; set; }
    }

    public class RewardCreateDto
    {
        public int SurveyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }
        public string RewardType { get; set; }
        public string RewardCategory { get; set; }
        public int? PointsCost { get; set; }
        public decimal? MonetaryValue { get; set; }
        public string ImageUrl { get; set; }
        public string RedemptionInstructions { get; set; }
        public string TermsAndConditions { get; set; }
        public int? AvailableQuantity { get; set; }
        public int? MinimumUserLevel { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string RedemptionUrl { get; set; }
        public bool IsExternallyFulfilled { get; set; }
        public string ExternalReferenceId { get; set; }
        public bool IsActive { get; set; }
    }

    public class RewardUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }
        public string RewardType { get; set; }
        public string RewardCategory { get; set; }
        public int? PointsCost { get; set; }
        public decimal? MonetaryValue { get; set; }
        public string ImageUrl { get; set; }
        public string RedemptionInstructions { get; set; }
        public string TermsAndConditions { get; set; }
        public int? AvailableQuantity { get; set; }
        public int? MinimumUserLevel { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string RedemptionUrl { get; set; }
        public bool IsExternallyFulfilled { get; set; }
        public string ExternalReferenceId { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserRewardAdminDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int RewardId { get; set; }
        public string RewardName { get; set; }
        public string RewardType { get; set; }
        public decimal? RewardAmount { get; set; }
        public int SurveyParticipationId { get; set; }
        public string SurveyName { get; set; }
        public DateTime EarnedDate { get; set; }
        public string RedemptionStatus { get; set; }
        public DateTime? ClaimedDate { get; set; }
        public string RedemptionCode { get; set; }
        public string RedemptionMethod { get; set; }
        public string DeliveryStatus { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string FulfillmentProvider { get; set; }
        public string FulfillmentReferenceId { get; set; }
    }

    public class UserRewardSummaryDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTimeOffset EarnedDate { get; set; }
        public DateTimeOffset? ClaimedDate { get; set; }
        public string RedemptionStatus { get; set; }
        public string DeliveryStatus { get; set; }
    }

    // Analytics
    public class SurveyAnalyticsDto
    {
        public int SurveyId { get; set; }
        public string SurveyName { get; set; }
        public int TotalViews { get; set; }
        public int TotalStarts { get; set; }
        public int TotalCompletions { get; set; }
        public decimal CompletionRate { get; set; }
        public int AverageCompletionTimeSeconds { get; set; }
        public decimal DropOffRate { get; set; }
        public int TotalDisqualifications { get; set; }
        public DateTime LastUpdated { get; set; }
        // Summary stats
        public Dictionary<string, int> CompletionsByDay { get; set; }
        public Dictionary<string, decimal> AverageTimeBySection { get; set; }
        public Dictionary<string, int> ParticipationsByStatus { get; set; }
    }

    public class QuestionAnalyticsDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public int TotalResponses { get; set; }
        public int AverageTimeToAnswerSeconds { get; set; }
        public decimal SkipRate { get; set; }
        public Dictionary<string, int> ResponseDistribution { get; set; }
    }

    public class SectionAnalyticsDto
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public int TotalEntered { get; set; }
        public int TotalCompleted { get; set; }
        public decimal CompletionRate { get; set; }
        public int AverageTimeInSectionSeconds { get; set; }
        public decimal DropOffRate { get; set; }
    }

    public class DemographicBreakdownDto
    {
        public string Category { get; set; } // Age, Gender, Location, etc.
        public string Value { get; set; } // 18-24, Male, New York, etc.
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class ResponseSummaryDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string ResponseValue { get; set; }
        public int ResponseCount { get; set; }
        public decimal ResponsePercentage { get; set; }
    }

    public class ResponseTrendDto
    {
        public DateTime Date { get; set; }
        public int Responses { get; set; }
        public int Completions { get; set; }
        public decimal AverageCompletionTime { get; set; }
    }

    public class PlatformMetricsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalSurveys { get; set; }
        public int ActiveSurveys { get; set; }
        public int TotalParticipations { get; set; }
        public int TotalCompletions { get; set; }
        public decimal OverallCompletionRate { get; set; }
        public int TotalPointsAwarded { get; set; }
        public int TotalRewardsRedeemed { get; set; }
        public decimal AverageUserEngagement { get; set; } // average logins per user
        public Dictionary<string, int> UsersByLevel { get; set; }
        public Dictionary<string, int> ParticipationsByDay { get; set; }
        public Dictionary<string, int> CompletionsByDay { get; set; }
    }

    // User Management
    public class UserAdminDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int TotalSurveysCompleted { get; set; }
        public int TotalPoints { get; set; }
        public int PointsLevel { get; set; }
        public bool ProfileComplete { get; set; }
        
        // Additional properties for UI
        public string Status { get; set; }
        public string Role { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime? LastActiveDate { get; set; }
        public int ProfileCompletionPercentage { get; set; }
    }

    public class UserDetailAdminDto : UserAdminDto
    {
        public DemographicsDto Demographics { get; set; }
        public List<UserInterestDto> Interests { get; set; }
        public UserEngagementDto Engagement { get; set; }
        public List<UserAchievementDto> Achievements { get; set; }
        public List<SurveyParticipationSummaryDto> RecentParticipations { get; set; }
        public List<PointTransactionDto> RecentPointTransactions { get; set; }
        public List<UserRewardDto> RecentRewards { get; set; }
    }

    public class SurveyParticipationSummaryDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string SurveyName { get; set; }
        public DateTime EnrolmentDateTime { get; set; }
        public DateTime? StartedAtDateTime { get; set; }
        public DateTime? CompletedAtDateTime { get; set; }
        public string Status { get; set; }
        public int ProgressPercentage { get; set; }
        public int TimeSpentInSeconds { get; set; }
    }

    public class AuditLogDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
