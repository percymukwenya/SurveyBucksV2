using Domain.Interfaces.Repository.Admin;
using Domain.Interfaces.Service;
using Domain.Models.Admin;
using Domain.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SurveySectionService : ISurveySectionService
    {
        private readonly ISurveySectionRepository _sectionRepository;

        public SurveySectionService(ISurveySectionRepository sectionRepository)
        {
            _sectionRepository = sectionRepository;
        }

        public async Task<IEnumerable<SurveySectionDto>> GetSurveySectionsAsync(int surveyId)
        {
            return await _sectionRepository.GetSurveySectionsAsync(surveyId);
        }

        public async Task<SurveySectionDto> GetSectionByIdAsync(int sectionId)
        {
            var section = await _sectionRepository.GetSectionByIdAsync(sectionId);
            if (section == null)
            {
                throw new NotFoundException($"Section with ID {sectionId} not found");
            }
            return section;
        }

        public async Task<int> CreateSectionAsync(SurveySectionCreateDto section, string createdBy)
        {
            if (string.IsNullOrWhiteSpace(section.Name))
            {
                throw new ArgumentException("Section name is required");
            }

            return await _sectionRepository.CreateSectionAsync(section, createdBy);
        }

        public async Task<bool> UpdateSectionAsync(SurveySectionUpdateDto section, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(section.Name))
            {
                throw new ArgumentException("Section name is required");
            }

            var existingSection = await _sectionRepository.GetSectionByIdAsync(section.Id);
            if (existingSection == null)
            {
                throw new NotFoundException($"Section with ID {section.Id} not found");
            }

            return await _sectionRepository.UpdateSectionAsync(section, modifiedBy);
        }

        public async Task<bool> DeleteSectionAsync(int sectionId, string deletedBy)
        {
            var existingSection = await _sectionRepository.GetSectionByIdAsync(sectionId);
            if (existingSection == null)
            {
                throw new NotFoundException($"Section with ID {sectionId} not found");
            }

            return await _sectionRepository.DeleteSectionAsync(sectionId, deletedBy);
        }

        public async Task<bool> ReorderSectionsAsync(int surveyId, IEnumerable<SectionOrderDto> sectionOrders, string modifiedBy)
        {
            if (sectionOrders == null || !sectionOrders.Any())
            {
                throw new ArgumentException("Section orders cannot be empty");
            }

            // Validate all section IDs exist and belong to the survey
            var existingSections = await _sectionRepository.GetSurveySectionsAsync(surveyId);
            var existingSectionIds = existingSections.Select(s => s.Id).ToHashSet();

            foreach (var order in sectionOrders)
            {
                if (!existingSectionIds.Contains(order.SectionId))
                {
                    throw new ArgumentException($"Section with ID {order.SectionId} does not exist or does not belong to the survey");
                }
            }

            return await _sectionRepository.ReorderSectionsAsync(surveyId, sectionOrders, modifiedBy);
        }
    }
}
