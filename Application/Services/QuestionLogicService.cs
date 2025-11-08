using Domain.Interfaces.Repository.Admin;
using Domain.Interfaces.Service;
using Domain.Models.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class QuestionLogicService : IQuestionLogicService
    {
        private readonly IQuestionLogicRepository _logicRepository;

        public QuestionLogicService(IQuestionLogicRepository logicRepository)
        {
            _logicRepository = logicRepository;
        }

        public async Task<IEnumerable<QuestionLogicDto>> GetQuestionLogicAsync(int questionId)
        {
            return await _logicRepository.GetQuestionLogicAsync(questionId);
        }

        public async Task<IEnumerable<QuestionLogicDto>> GetSurveyLogicAsync(int surveyId)
        {
            return await _logicRepository.GetSurveyLogicAsync(surveyId);
        }

        public async Task<int> CreateQuestionLogicAsync(QuestionLogicCreateDto logic, string createdBy)
        {
            // Validate logic
            ValidateLogic(logic);

            return await _logicRepository.CreateQuestionLogicAsync(logic, createdBy);
        }

        public async Task<bool> UpdateQuestionLogicAsync(QuestionLogicUpdateDto logic, string modifiedBy)
        {
            // Validate logic
            ValidateLogic(logic);

            return await _logicRepository.UpdateQuestionLogicAsync(logic, modifiedBy);
        }

        public async Task<bool> DeleteQuestionLogicAsync(int logicId, string deletedBy)
        {
            return await _logicRepository.DeleteQuestionLogicAsync(logicId, deletedBy);
        }

        public async Task<bool> ValidateSurveyLogicAsync(int surveyId)
        {
            return await _logicRepository.ValidateLogicAsync(surveyId);
        }

        public async Task<SurveyFlowVisualizationDto> GetSurveyFlowVisualizationAsync(int surveyId)
        {
            return await _logicRepository.GetSurveyFlowVisualizationAsync(surveyId);
        }

        private void ValidateLogic(QuestionLogicCreateDto logic)
        {
            // Validate logic type
            var validLogicTypes = new[] { "Skip", "Show", "Hide", "EndSurvey" };
            if (!validLogicTypes.Contains(logic.LogicType))
            {
                throw new ArgumentException($"Invalid logic type: {logic.LogicType}");
            }

            // Validate condition type
            var validConditionTypes = new[] { "Equals", "NotEquals", "Contains", "GreaterThan", "LessThan", "Between" };
            if (!validConditionTypes.Contains(logic.ConditionType))
            {
                throw new ArgumentException($"Invalid condition type: {logic.ConditionType}");
            }

            // Validate targets
            if (logic.LogicType != "EndSurvey" && logic.TargetQuestionId == null && logic.TargetSectionId == null)
            {
                throw new ArgumentException("Target question or section must be specified");
            }

            if (logic.TargetQuestionId != null && logic.TargetSectionId != null)
            {
                throw new ArgumentException("Cannot specify both target question and target section");
            }
        }
    }
}
