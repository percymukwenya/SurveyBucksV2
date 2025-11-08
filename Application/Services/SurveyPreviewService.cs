using Domain.Interfaces.Repository.Admin;
using Domain.Interfaces.Service;
using Domain.Models.Admin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SurveyPreviewService : ISurveyPreviewService
    {
        private readonly ISurveyManagementRepository _surveyRepository;
        private readonly IQuestionLogicRepository _logicRepository;
        private readonly IQuestionRepository _questionRepository;

        public SurveyPreviewService(
            ISurveyManagementRepository surveyRepository,
            IQuestionLogicRepository logicRepository,
            IQuestionRepository questionRepository)
        {
            _surveyRepository = surveyRepository;
            _logicRepository = logicRepository;
            _questionRepository = questionRepository;
        }

        public Task<SurveyPreviewDto> ApplyLogicAsync(int surveyId, PreviewStateDto currentState)
        {
            throw new System.NotImplementedException();
        }

        public Task<PreviewNavigationDto> CalculateNavigationAsync(int surveyId, PreviewStateDto currentState)
        {
            throw new System.NotImplementedException();
        }

        public async Task<SurveyPreviewDto> GetSurveyPreviewAsync(int surveyId, PreviewStateDto currentState = null)
        {
            var survey = await _surveyRepository.GetSurveyAdminDetailsAsync(surveyId);
            if (survey == null)
                throw new NotFoundException($"Survey {surveyId} not found");

            var preview = new SurveyPreviewDto
            {
                SurveyId = survey.Id,
                SurveyName = survey.Name,
                Sections = new List<SectionPreviewDto>()
            };

            // Build preview structure
            foreach (var section in survey.Sections.OrderBy(s => s.Order))
            {
                var sectionPreview = new SectionPreviewDto
                {
                    SectionId = section.Id,
                    SectionName = section.Name,
                    Description = section.Description,
                    Order = section.Order,
                    Questions = new List<QuestionPreviewDto>()
                };

                // Get questions for section
                var questions = await _questionRepository.GetSectionQuestionsAsync(section.Id);

                foreach (var question in questions.OrderBy(q => q.Order))
                {
                    var questionPreview = new QuestionPreviewDto
                    {
                        QuestionId = question.Id,
                        Text = question.Text,
                        QuestionType = question.QuestionTypeName,
                        IsMandatory = question.IsMandatory,
                        HelpText = question.HelpText,
                        IsVisible = true
                    };

                    // Get choices if applicable
                    if (question.ResponseChoices != null)
                    {
                        questionPreview.Choices = question.ResponseChoices.ToList();
                    }

                    // Get logic
                    questionPreview.Logic = (await _logicRepository.GetQuestionLogicAsync(question.Id)).ToList();

                    sectionPreview.Questions.Add(questionPreview);
                }

                preview.Sections.Add(sectionPreview);
            }

            // Apply logic if we have current state
            if (currentState != null)
            {
                preview = await ApplyLogicAsync(surveyId, currentState);
            }

            return preview;
        }

        public Task<bool> TestSurveyCompletionAsync(int surveyId, PreviewStateDto finalState)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<string>> ValidateResponseAsync(int questionId, PreviewResponseDto response)
        {
            throw new System.NotImplementedException();
        }
    }
}
