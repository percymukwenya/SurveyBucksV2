using Domain.Interfaces.Repository.Admin;
using Domain.Interfaces.Service;
using Domain.Models.Admin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SurveyManagementService : ISurveyManagementService
    {
        private readonly ISurveyManagementRepository _surveyManagementRepository;
        private readonly IAnalyticsRepository _analyticsRepository;

        public SurveyManagementService(ISurveyManagementRepository surveyManagementRepository, IAnalyticsRepository analyticsRepository)
        {
            _surveyManagementRepository = surveyManagementRepository;
            _analyticsRepository = analyticsRepository;
        }

        public async Task<bool> CloseSurveyAsync(int surveyId, string closedBy)
        {
            return await _surveyManagementRepository.CloseSurveyAsync(surveyId, closedBy);
        }

        public async Task<int> CreateSurveyAsync(SurveyCreateDto survey, string createdBy)
        {
            return await _surveyManagementRepository.CreateSurveyAsync(survey, createdBy);
        }

        public async Task<bool> DeleteSurveyAsync(int surveyId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteSurveyAsync(surveyId, deletedBy);
        }

        public async Task<bool> DuplicateSurveyAsync(int surveyId, string createdBy)
        {
            return await _surveyManagementRepository.DuplicateSurveyAsync(surveyId, 1, createdBy);
        }

        public async Task<IEnumerable<SurveyAdminListItemDto>> GetAllSurveysAsync(string status = null)
        {
            return await _surveyManagementRepository.GetAllSurveysAsync(status);
        }

        public async Task<SurveyAdminDetailDto> GetSurveyAdminDetailsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyAdminDetailsAsync(surveyId);
        }

        public async Task<SurveyAnalyticsDto> GetSurveyAnalyticsAsync(int surveyId)
        {
            return await _analyticsRepository.GetSurveyAnalyticsAsync(surveyId);
        }

        public async Task<bool> PublishSurveyAsync(int surveyId, string publishedBy)
        {
            return await _surveyManagementRepository.PublishSurveyAsync(surveyId, publishedBy);
        }

        public Task<bool> UnpublishSurveyAsync(int surveyId, string unpublishedBy)
        {
            return _surveyManagementRepository.UnpublishSurveyAsync(surveyId, unpublishedBy);
        }

        public Task<bool> UpdateSurveyAsync(SurveyUpdateDto survey, string modifiedBy)
        {
            return _surveyManagementRepository.UpdateSurveyAsync(survey, modifiedBy);
        }

        public async Task<IEnumerable<AgeRangeTargetDto>> GetSurveyAgeRangeTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyAgeRangeTargetsAsync(surveyId);
        }

        public async Task<int> AddAgeRangeTargetAsync(AgeRangeTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddAgeRangeTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteAgeRangeTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteAgeRangeTargetAsync(targetId, deletedBy);
        }

        // Gender Targets
        public async Task<IEnumerable<GenderTargetDto>> GetSurveyGenderTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyGenderTargetsAsync(surveyId);
        }

        public async Task<int> AddGenderTargetAsync(GenderTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddGenderTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteGenderTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteGenderTargetAsync(targetId, deletedBy);
        }

        // Education Targets
        public async Task<IEnumerable<EducationTargetDto>> GetSurveyEducationTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyEducationTargetsAsync(surveyId);
        }

        public async Task<int> AddEducationTargetAsync(EducationTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddEducationTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteEducationTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteEducationTargetAsync(targetId, deletedBy);
        }

        // Income Range Targets
        public async Task<IEnumerable<IncomeRangeTargetDto>> GetSurveyIncomeRangeTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyIncomeRangeTargetsAsync(surveyId);
        }

        public async Task<int> AddIncomeRangeTargetAsync(IncomeRangeTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddIncomeRangeTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteIncomeRangeTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteIncomeRangeTargetAsync(targetId, deletedBy);
        }

        // Location Targets
        public async Task<IEnumerable<LocationTargetDto>> GetSurveyLocationTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyLocationTargetsAsync(surveyId);
        }

        public async Task<int> AddLocationTargetAsync(LocationTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddLocationTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteLocationTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteLocationTargetAsync(targetId, deletedBy);
        }

        // Country Targets
        public async Task<IEnumerable<CountryTargetDto>> GetSurveyCountryTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyCountryTargetsAsync(surveyId);
        }

        public async Task<int> AddCountryTargetAsync(CountryTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddCountryTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteCountryTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteCountryTargetAsync(targetId, deletedBy);
        }

        // State Targets
        public async Task<IEnumerable<StateTargetDto>> GetSurveyStateTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyStateTargetsAsync(surveyId);
        }

        public async Task<int> AddStateTargetAsync(StateTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddStateTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteStateTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteStateTargetAsync(targetId, deletedBy);
        }

        // Household Size Targets
        public async Task<IEnumerable<HouseholdSizeTargetDto>> GetSurveyHouseholdSizeTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyHouseholdSizeTargetsAsync(surveyId);
        }

        public async Task<int> AddHouseholdSizeTargetAsync(HouseholdSizeTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddHouseholdSizeTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteHouseholdSizeTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteHouseholdSizeTargetAsync(targetId, deletedBy);
        }

        // Parental Status Targets
        public async Task<IEnumerable<ParentalStatusTargetDto>> GetSurveyParentalStatusTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyParentalStatusTargetsAsync(surveyId);
        }

        public async Task<int> AddParentalStatusTargetAsync(ParentalStatusTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddParentalStatusTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteParentalStatusTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteParentalStatusTargetAsync(targetId, deletedBy);
        }

        // Industry Targets
        public async Task<IEnumerable<IndustryTargetDto>> GetSurveyIndustryTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyIndustryTargetsAsync(surveyId);
        }

        public async Task<int> AddIndustryTargetAsync(IndustryTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddIndustryTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteIndustryTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteIndustryTargetAsync(targetId, deletedBy);
        }

        // Occupation Targets
        public async Task<IEnumerable<OccupationTargetDto>> GetSurveyOccupationTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyOccupationTargetsAsync(surveyId);
        }

        public async Task<int> AddOccupationTargetAsync(OccupationTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddOccupationTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteOccupationTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteOccupationTargetAsync(targetId, deletedBy);
        }

        // Marital Status Targets
        public async Task<IEnumerable<MaritalStatusTargetDto>> GetSurveyMaritalStatusTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyMaritalStatusTargetsAsync(surveyId);
        }

        public async Task<int> AddMaritalStatusTargetAsync(MaritalStatusTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddMaritalStatusTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteMaritalStatusTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteMaritalStatusTargetAsync(targetId, deletedBy);
        }

        // Interest Targets
        public async Task<IEnumerable<InterestTargetDto>> GetSurveyInterestTargetsAsync(int surveyId)
        {
            return await _surveyManagementRepository.GetSurveyInterestTargetsAsync(surveyId);
        }

        public async Task<int> AddInterestTargetAsync(InterestTargetDto target, string createdBy)
        {
            return await _surveyManagementRepository.AddInterestTargetAsync(target, createdBy);
        }

        public async Task<bool> DeleteInterestTargetAsync(int targetId, string deletedBy)
        {
            return await _surveyManagementRepository.DeleteInterestTargetAsync(targetId, deletedBy);
        }
    }
}
