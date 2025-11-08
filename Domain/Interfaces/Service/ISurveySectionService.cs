using Domain.Models.Admin;
using Domain.Models.Response;

namespace Domain.Interfaces.Service
{
    public interface ISurveySectionService
    {
        Task<IEnumerable<SurveySectionDto>> GetSurveySectionsAsync(int surveyId);
        Task<SurveySectionDto> GetSectionByIdAsync(int sectionId);
        Task<int> CreateSectionAsync(SurveySectionCreateDto section, string createdBy);
        Task<bool> UpdateSectionAsync(SurveySectionUpdateDto section, string modifiedBy);
        Task<bool> DeleteSectionAsync(int sectionId, string deletedBy);
        Task<bool> ReorderSectionsAsync(int surveyId, IEnumerable<SectionOrderDto> sectionOrders, string modifiedBy);
    }
}
