using Domain.Models.Admin;

namespace Domain.Interfaces.Repository.Admin
{
    public interface IDemographicTargetingRepository
    {
        Task<IEnumerable<AgeRangeTargetDto>> GetAgeRangeTargetsAsync(int surveyId);
        Task<IEnumerable<GenderTargetDto>> GetGenderTargetsAsync(int surveyId);
        Task<IEnumerable<LocationTargetDto>> GetLocationTargetsAsync(int surveyId);
        Task<IEnumerable<EducationTargetDto>> GetEducationTargetsAsync(int surveyId);
        Task<IEnumerable<OccupationTargetDto>> GetOccupationTargetsAsync(int surveyId);
        Task<IEnumerable<IncomeRangeTargetDto>> GetIncomeRangeTargetsAsync(int surveyId);
        Task<IEnumerable<InterestTargetDto>> GetInterestTargetsAsync(int surveyId);

        Task<int> AddTargetAsync<T>(T target, string createdBy) where T : class;
        Task<bool> UpdateTargetAsync<T>(T target, string modifiedBy) where T : class;
        Task<bool> DeleteTargetAsync(string targetType, int targetId, string deletedBy);
        Task<bool> ClearAllTargetsAsync(int surveyId, string deletedBy);
    }
}
