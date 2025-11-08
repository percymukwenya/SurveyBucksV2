using Domain.Models.Admin;

namespace Domain.Interfaces.Service
{
    public interface ISurveyManagementService
    {
        Task<IEnumerable<SurveyAdminListItemDto>> GetAllSurveysAsync(string status = null);
        Task<SurveyAdminDetailDto> GetSurveyAdminDetailsAsync(int surveyId);
        Task<int> CreateSurveyAsync(SurveyCreateDto survey, string createdBy);
        Task<bool> UpdateSurveyAsync(SurveyUpdateDto survey, string modifiedBy);
        Task<bool> DeleteSurveyAsync(int surveyId, string deletedBy);
        Task<bool> PublishSurveyAsync(int surveyId, string publishedBy);
        Task<bool> UnpublishSurveyAsync(int surveyId, string unpublishedBy);
        Task<bool> CloseSurveyAsync(int surveyId, string closedBy);
        Task<bool> DuplicateSurveyAsync(int surveyId, string createdBy);
        Task<SurveyAnalyticsDto> GetSurveyAnalyticsAsync(int surveyId);

        Task<IEnumerable<AgeRangeTargetDto>> GetSurveyAgeRangeTargetsAsync(int surveyId);
        Task<int> AddAgeRangeTargetAsync(AgeRangeTargetDto target, string createdBy);
        Task<bool> DeleteAgeRangeTargetAsync(int targetId, string deletedBy);

        // Gender targets
        Task<IEnumerable<GenderTargetDto>> GetSurveyGenderTargetsAsync(int surveyId);
        Task<int> AddGenderTargetAsync(GenderTargetDto target, string createdBy);
        Task<bool> DeleteGenderTargetAsync(int targetId, string deletedBy);

        // Education targets
        Task<IEnumerable<EducationTargetDto>> GetSurveyEducationTargetsAsync(int surveyId);
        Task<int> AddEducationTargetAsync(EducationTargetDto target, string createdBy);
        Task<bool> DeleteEducationTargetAsync(int targetId, string deletedBy);

        // Income Range targets
        Task<IEnumerable<IncomeRangeTargetDto>> GetSurveyIncomeRangeTargetsAsync(int surveyId);
        Task<int> AddIncomeRangeTargetAsync(IncomeRangeTargetDto target, string createdBy);
        Task<bool> DeleteIncomeRangeTargetAsync(int targetId, string deletedBy);

        // Location targets
        Task<IEnumerable<LocationTargetDto>> GetSurveyLocationTargetsAsync(int surveyId);
        Task<int> AddLocationTargetAsync(LocationTargetDto target, string createdBy);
        Task<bool> DeleteLocationTargetAsync(int targetId, string deletedBy);

        // Country targets
        Task<IEnumerable<CountryTargetDto>> GetSurveyCountryTargetsAsync(int surveyId);
        Task<int> AddCountryTargetAsync(CountryTargetDto target, string createdBy);
        Task<bool> DeleteCountryTargetAsync(int targetId, string deletedBy);

        // State targets
        Task<IEnumerable<StateTargetDto>> GetSurveyStateTargetsAsync(int surveyId);
        Task<int> AddStateTargetAsync(StateTargetDto target, string createdBy);
        Task<bool> DeleteStateTargetAsync(int targetId, string deletedBy);

        // Household Size targets
        Task<IEnumerable<HouseholdSizeTargetDto>> GetSurveyHouseholdSizeTargetsAsync(int surveyId);
        Task<int> AddHouseholdSizeTargetAsync(HouseholdSizeTargetDto target, string createdBy);
        Task<bool> DeleteHouseholdSizeTargetAsync(int targetId, string deletedBy);

        // Parental Status targets
        Task<IEnumerable<ParentalStatusTargetDto>> GetSurveyParentalStatusTargetsAsync(int surveyId);
        Task<int> AddParentalStatusTargetAsync(ParentalStatusTargetDto target, string createdBy);
        Task<bool> DeleteParentalStatusTargetAsync(int targetId, string deletedBy);

        // Industry targets
        Task<IEnumerable<IndustryTargetDto>> GetSurveyIndustryTargetsAsync(int surveyId);
        Task<int> AddIndustryTargetAsync(IndustryTargetDto target, string createdBy);
        Task<bool> DeleteIndustryTargetAsync(int targetId, string deletedBy);

        // Occupation targets
        Task<IEnumerable<OccupationTargetDto>> GetSurveyOccupationTargetsAsync(int surveyId);
        Task<int> AddOccupationTargetAsync(OccupationTargetDto target, string createdBy);
        Task<bool> DeleteOccupationTargetAsync(int targetId, string deletedBy);

        // Marital Status targets
        Task<IEnumerable<MaritalStatusTargetDto>> GetSurveyMaritalStatusTargetsAsync(int surveyId);
        Task<int> AddMaritalStatusTargetAsync(MaritalStatusTargetDto target, string createdBy);
        Task<bool> DeleteMaritalStatusTargetAsync(int targetId, string deletedBy);

        // Interest targets
        Task<IEnumerable<InterestTargetDto>> GetSurveyInterestTargetsAsync(int surveyId);
        Task<int> AddInterestTargetAsync(InterestTargetDto target, string createdBy);
        Task<bool> DeleteInterestTargetAsync(int targetId, string deletedBy);
    }
}
