using Application.Services;
using Domain.Interfaces.Repository;
using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Domain.Models.Response;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests
{
    public class SurveyBranchingServiceTests
    {
        private readonly Mock<IQuestionLogicRepository> _questionLogicRepositoryMock;
        private readonly Mock<ISurveyParticipationRepository> _participationRepositoryMock;
        private readonly Mock<ILogger<SurveyBranchingService>> _loggerMock;
        private readonly SurveyBranchingService _service;

        public SurveyBranchingServiceTests()
        {
            _questionLogicRepositoryMock = new Mock<IQuestionLogicRepository>();
            _participationRepositoryMock = new Mock<ISurveyParticipationRepository>();
            _loggerMock = new Mock<ILogger<SurveyBranchingService>>();
            
            _service = new SurveyBranchingService(
                _questionLogicRepositoryMock.Object,
                _participationRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task EvaluateQuestionLogic_WithEqualsCondition_ReturnsCorrectAction()
        {
            // Arrange
            var questionId = 1;
            var responseValue = "Yes";
            var participationId = 100;

            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = questionId,
                    LogicType = "show_hide",
                    ConditionType = "equals",
                    ConditionValue = "Yes",
                    ActionType = "show_question",
                    TargetQuestionId = 2,
                    IsActive = true,
                    Order = 1
                }
            };

            var participation = new SurveyParticipationDto
            {
                Id = participationId,
                SurveyId = 1,
                UserId = "user123"
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetQuestionLogicAsync(questionId))
                .ReturnsAsync(logicRules);

            _participationRepositoryMock
                .Setup(x => x.GetParticipationAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(participation);

            _participationRepositoryMock
                .Setup(x => x.GetSavedResponsesAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(new List<SurveyResponseDto>());

            // Act
            var result = await _service.EvaluateQuestionLogicAsync(questionId, responseValue, participationId);

            // Assert
            Assert.True(result.HasActions);
            Assert.Single(result.Actions);
            Assert.Equal(BranchingActionType.ShowQuestion, result.Actions[0].ActionType);
            Assert.Equal(2, result.Actions[0].TargetQuestionId);
        }

        [Fact]
        public async Task EvaluateQuestionLogic_WithNotEqualsCondition_SkipsAction()
        {
            // Arrange
            var questionId = 1;
            var responseValue = "No";
            var participationId = 100;

            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = questionId,
                    ConditionType = "equals",
                    ConditionValue = "Yes",
                    ActionType = "show_question",
                    TargetQuestionId = 2,
                    IsActive = true,
                    Order = 1
                }
            };

            var participation = new SurveyParticipationDto
            {
                Id = participationId,
                SurveyId = 1,
                UserId = "user123"
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetQuestionLogicAsync(questionId))
                .ReturnsAsync(logicRules);

            _participationRepositoryMock
                .Setup(x => x.GetParticipationAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(participation);

            _participationRepositoryMock
                .Setup(x => x.GetSavedResponsesAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(new List<SurveyResponseDto>());

            // Act
            var result = await _service.EvaluateQuestionLogicAsync(questionId, responseValue, participationId);

            // Assert
            Assert.False(result.HasActions);
            Assert.Empty(result.Actions);
        }

        [Theory]
        [InlineData("5", "greater_than", "3", true)]
        [InlineData("2", "greater_than", "3", false)]
        [InlineData("5", "less_than", "10", true)]
        [InlineData("15", "less_than", "10", false)]
        [InlineData("apple", "contains", "app", true)]
        [InlineData("apple", "contains", "orange", false)]
        public async Task EvaluateQuestionLogic_WithDifferentConditions_EvaluatesCorrectly(
            string responseValue, string conditionType, string conditionValue, bool expectedMatch)
        {
            // Arrange
            var questionId = 1;
            var participationId = 100;

            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = questionId,
                    ConditionType = conditionType,
                    ConditionValue = conditionValue,
                    ActionType = "show_question",
                    TargetQuestionId = 2,
                    IsActive = true,
                    Order = 1
                }
            };

            var participation = new SurveyParticipationDto
            {
                Id = participationId,
                SurveyId = 1,
                UserId = "user123"
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetQuestionLogicAsync(questionId))
                .ReturnsAsync(logicRules);

            _participationRepositoryMock
                .Setup(x => x.GetParticipationAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(participation);

            _participationRepositoryMock
                .Setup(x => x.GetSavedResponsesAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(new List<SurveyResponseDto>());

            // Act
            var result = await _service.EvaluateQuestionLogicAsync(questionId, responseValue, participationId);

            // Assert
            Assert.Equal(expectedMatch, result.HasActions);
        }

        [Fact]
        public async Task EvaluateQuestionLogic_WithBetweenCondition_EvaluatesCorrectly()
        {
            // Arrange
            var questionId = 1;
            var responseValue = "25";
            var participationId = 100;

            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = questionId,
                    ConditionType = "between",
                    ConditionValue = "18",
                    ConditionValue2 = "65",
                    ActionType = "show_question",
                    TargetQuestionId = 2,
                    IsActive = true,
                    Order = 1
                }
            };

            var participation = new SurveyParticipationDto
            {
                Id = participationId,
                SurveyId = 1,
                UserId = "user123"
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetQuestionLogicAsync(questionId))
                .ReturnsAsync(logicRules);

            _participationRepositoryMock
                .Setup(x => x.GetParticipationAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(participation);

            _participationRepositoryMock
                .Setup(x => x.GetSavedResponsesAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(new List<SurveyResponseDto>());

            // Act
            var result = await _service.EvaluateQuestionLogicAsync(questionId, responseValue, participationId);

            // Assert
            Assert.True(result.HasActions);
            Assert.Single(result.Actions);
            Assert.Equal(BranchingActionType.ShowQuestion, result.Actions[0].ActionType);
        }

        [Fact]
        public async Task EvaluateQuestionLogic_WithInListCondition_EvaluatesCorrectly()
        {
            // Arrange
            var questionId = 1;
            var responseValue = "Blue";
            var participationId = 100;

            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = questionId,
                    ConditionType = "in_list",
                    ConditionValue = "Red,Blue,Green",
                    ActionType = "jump_to_section",
                    TargetSectionId = 3,
                    IsActive = true,
                    Order = 1
                }
            };

            var participation = new SurveyParticipationDto
            {
                Id = participationId,
                SurveyId = 1,
                UserId = "user123"
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetQuestionLogicAsync(questionId))
                .ReturnsAsync(logicRules);

            _participationRepositoryMock
                .Setup(x => x.GetParticipationAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(participation);

            _participationRepositoryMock
                .Setup(x => x.GetSavedResponsesAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(new List<SurveyResponseDto>());

            // Act
            var result = await _service.EvaluateQuestionLogicAsync(questionId, responseValue, participationId);

            // Assert
            Assert.True(result.HasActions);
            Assert.Single(result.Actions);
            Assert.Equal(BranchingActionType.JumpToSection, result.Actions[0].ActionType);
            Assert.Equal(3, result.Actions[0].TargetSectionId);
        }

        [Fact]
        public async Task EvaluateQuestionLogic_WithMultipleRules_ProcessesInOrder()
        {
            // Arrange
            var questionId = 1;
            var responseValue = "Yes";
            var participationId = 100;

            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = questionId,
                    ConditionType = "equals",
                    ConditionValue = "Yes",
                    ActionType = "show_question",
                    TargetQuestionId = 2,
                    IsActive = true,
                    Order = 1
                },
                new QuestionLogicDto
                {
                    Id = 2,
                    QuestionId = questionId,
                    ConditionType = "equals",
                    ConditionValue = "Yes",
                    ActionType = "show_question",
                    TargetQuestionId = 3,
                    IsActive = true,
                    Order = 2
                }
            };

            var participation = new SurveyParticipationDto
            {
                Id = participationId,
                SurveyId = 1,
                UserId = "user123"
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetQuestionLogicAsync(questionId))
                .ReturnsAsync(logicRules);

            _participationRepositoryMock
                .Setup(x => x.GetParticipationAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(participation);

            _participationRepositoryMock
                .Setup(x => x.GetSavedResponsesAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(new List<SurveyResponseDto>());

            // Act
            var result = await _service.EvaluateQuestionLogicAsync(questionId, responseValue, participationId);

            // Assert
            Assert.True(result.HasActions);
            Assert.Equal(2, result.Actions.Count);
            Assert.All(result.Actions, action => Assert.Equal(BranchingActionType.ShowQuestion, action.ActionType));
        }

        [Fact]
        public async Task EvaluateQuestionLogic_WithEndSurveyAction_StopsProcessing()
        {
            // Arrange
            var questionId = 1;
            var responseValue = "No";
            var participationId = 100;

            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = questionId,
                    ConditionType = "equals",
                    ConditionValue = "No",
                    ActionType = "end_survey",
                    Message = "Thank you for your participation.",
                    IsActive = true,
                    Order = 1
                },
                new QuestionLogicDto
                {
                    Id = 2,
                    QuestionId = questionId,
                    ConditionType = "equals",
                    ConditionValue = "No",
                    ActionType = "show_question",
                    TargetQuestionId = 5,
                    IsActive = true,
                    Order = 2
                }
            };

            var participation = new SurveyParticipationDto
            {
                Id = participationId,
                SurveyId = 1,
                UserId = "user123"
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetQuestionLogicAsync(questionId))
                .ReturnsAsync(logicRules);

            _participationRepositoryMock
                .Setup(x => x.GetParticipationAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(participation);

            _participationRepositoryMock
                .Setup(x => x.GetSavedResponsesAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(new List<SurveyResponseDto>());

            // Act
            var result = await _service.EvaluateQuestionLogicAsync(questionId, responseValue, participationId);

            // Assert
            Assert.True(result.HasActions);
            Assert.Single(result.Actions); // Should stop after EndSurvey action
            Assert.Equal(BranchingActionType.EndSurvey, result.Actions[0].ActionType);
            Assert.Equal("Thank you for your participation.", result.Actions[0].Message);
        }

        [Fact]
        public async Task ValidateSurveyFlowIntegrity_WithCircularReference_ReturnsInvalid()
        {
            // Arrange
            var surveyId = 1;
            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = 1,
                    ActionType = "show_question",
                    TargetQuestionId = 2,
                    IsActive = true
                },
                new QuestionLogicDto
                {
                    Id = 2,
                    QuestionId = 2,
                    ActionType = "show_question",
                    TargetQuestionId = 1, // Creates circular reference
                    IsActive = true
                }
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetSurveyLogicAsync(surveyId))
                .ReturnsAsync(logicRules);

            // Act
            var result = await _service.ValidateSurveyFlowIntegrityAsync(surveyId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateSurveyFlowIntegrity_WithSelfReference_ReturnsInvalid()
        {
            // Arrange
            var surveyId = 1;
            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = 1,
                    ActionType = "show_question",
                    TargetQuestionId = 1, // Self-reference
                    IsActive = true
                }
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetSurveyLogicAsync(surveyId))
                .ReturnsAsync(logicRules);

            // Act
            var result = await _service.ValidateSurveyFlowIntegrityAsync(surveyId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ProcessResponseBranching_WithValidResponse_ExecutesAction()
        {
            // Arrange
            var response = new SurveyResponseDto
            {
                QuestionId = 1,
                Answer = "Yes",
                SurveyParticipationId = 100
            };
            var participationId = 100;

            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = 1,
                    ConditionType = "equals",
                    ConditionValue = "Yes",
                    ActionType = "jump_to_section",
                    TargetSectionId = 2,
                    Message = "Jumping to section 2",
                    IsActive = true,
                    Order = 1
                }
            };

            var participation = new SurveyParticipationDto
            {
                Id = participationId,
                SurveyId = 1,
                UserId = "user123"
            };

            _questionLogicRepositoryMock
                .Setup(x => x.GetQuestionLogicAsync(response.QuestionId))
                .ReturnsAsync(logicRules);

            _participationRepositoryMock
                .Setup(x => x.GetParticipationAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(participation);

            _participationRepositoryMock
                .Setup(x => x.GetSavedResponsesAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(new List<SurveyResponseDto>());

            // Act
            var result = await _service.ProcessResponseBranchingAsync(response, participationId);

            // Assert
            Assert.Equal(BranchingActionType.JumpToSection, result.ActionType);
            Assert.Equal(2, result.TargetSectionId);
            Assert.Equal("Jumping to section 2", result.Message);
        }

        [Fact]
        public async Task GetCurrentFlowState_WithValidParticipation_ReturnsFlowState()
        {
            // Arrange
            var participationId = 100;
            var participation = new SurveyParticipationDto
            {
                Id = participationId,
                SurveyId = 1,
                UserId = "user123",
                CurrentSectionId = 1,
                CurrentQuestionId = 2,
                StatusId = 2 // In progress
            };

            var responses = new List<SurveyResponseDto>
            {
                new SurveyResponseDto { QuestionId = 1, Answer = "Yes" },
                new SurveyResponseDto { QuestionId = 3, Answer = "No" }
            };

            var logicRules = new List<QuestionLogicDto>
            {
                new QuestionLogicDto
                {
                    Id = 1,
                    QuestionId = 1,
                    ActionType = "show_question",
                    TargetQuestionId = 2,
                    IsActive = true
                }
            };

            _participationRepositoryMock
                .Setup(x => x.GetParticipationAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(participation);

            _participationRepositoryMock
                .Setup(x => x.GetSavedResponsesAsync(participationId, It.IsAny<string>()))
                .ReturnsAsync(responses);

            _questionLogicRepositoryMock
                .Setup(x => x.GetSurveyLogicAsync(participation.SurveyId))
                .ReturnsAsync(logicRules);

            // Act
            var result = await _service.GetCurrentFlowStateAsync(participationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(participationId, result.ParticipationId);
            Assert.Equal(1, result.SurveyId);
            Assert.Equal(1, result.CurrentSectionId);
            Assert.Equal(2, result.CurrentQuestionId);
            Assert.Equal(new[] { 1, 3 }, result.CompletedQuestions);
            Assert.False(result.IsComplete);
        }
    }
}