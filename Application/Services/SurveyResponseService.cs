using Dapper;
using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models.Response;
using Infrastructure.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SurveyResponseService : ISurveyResponseService
    {
        private readonly ISurveyParticipationRepository _participationRepository;
        private readonly IGamificationRepository _gamificationRepository;
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ILogger<SurveyResponseService> _logger;

        public SurveyResponseService(
            ISurveyParticipationRepository participationRepository,
            IGamificationRepository gamificationRepository, IDatabaseConnectionFactory connectionFactory,
            ILogger<SurveyResponseService> logger)
        {
            _participationRepository = participationRepository;
            _gamificationRepository = gamificationRepository;
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<ResponseValidationResult> ValidateAndSaveResponseAsync(SurveyResponseDto response, string userId)
        {
            var result = new ResponseValidationResult();

            try
            {
                // Get question details for validation
                var questionDetails = await GetQuestionDetailsForValidationAsync(response.QuestionId);

                if (questionDetails == null)
                {
                    result.AddError("Question not found");
                    return result;
                }

                // Verify participation ownership
                var participation = await _participationRepository.GetParticipationAsync(response.SurveyParticipationId, userId);
                if (participation == null || participation.UserId != userId)
                {
                    result.AddError("Invalid participation");
                    return result;
                }

                // Validate response based on question type
                var validationResult = await ValidateResponseContentAsync(response, questionDetails);
                if (!validationResult.IsValid)
                {
                    result.Errors.AddRange(validationResult.Errors);
                    return result;
                }

                // Check if response needs special handling (screening, conditional logic)
                if (questionDetails.IsScreeningQuestion)
                {
                    var screeningResult = await ProcessScreeningQuestionAsync(response, questionDetails, userId);
                    result.IsScreeningResponse = true;
                    result.ScreeningResult = screeningResult;
                }

                // Save the response with optimistic locking
                var saveResult = await SaveResponseWithRetryAsync(response, questionDetails);
                if (!saveResult)
                {
                    result.AddError("Failed to save response");
                    return result;
                }

                // Process conditional logic if applicable
                if (!string.IsNullOrEmpty(questionDetails.ScreeningLogic))
                {
                    result.NextAction = await ProcessConditionalLogicAsync(response, questionDetails);
                }

                // Update progress and check for auto-completion
                await UpdateQuestionProgressAsync(response, userId);

                result.IsValid = true;
                result.ResponseId = response.Id;

                _logger.LogInformation("Response saved successfully for Question {QuestionId} by User {UserId}",
                    response.QuestionId, userId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving response for Question {QuestionId} by User {UserId}",
                    response.QuestionId, userId);
                result.AddError("An error occurred while saving the response");
            }

            return result;
        }

        public async Task<BatchResponseResult> SaveMultipleResponsesAsync(List<SurveyResponseDto> responses, string userId)
        {
            var result = new BatchResponseResult();

            // Validate all responses first
            foreach (var response in responses)
            {
                var validation = await ValidateResponseContentAsync(response, await GetQuestionDetailsForValidationAsync(response.QuestionId));
                if (!validation.IsValid)
                {
                    result.FailedResponses.Add(new FailedResponse
                    {
                        QuestionId = response.QuestionId,
                        Errors = validation.Errors
                    });
                }
                else
                {
                    result.ValidResponses.Add(response);
                }
            }

            // Save valid responses in batch
            if (result.ValidResponses.Any())
            {
                var batchSaveResult = await SaveResponsesBatchAsync(result.ValidResponses, userId);
                result.SuccessCount = batchSaveResult;
            }

            return result;
        }

        private async Task<QuestionValidationDto> GetQuestionDetailsForValidationAsync(int questionId)
        {
            const string sql = @"
            SELECT 
                q.Id, q.Text, q.IsMandatory, q.QuestionTypeId, qt.Name AS QuestionTypeName,
                q.MinValue, q.MaxValue, q.ValidationMessage, q.HelpText,
                q.IsScreeningQuestion, q.ScreeningLogic, q.RandomizeChoices,
                qt.HasChoices, qt.HasMinMaxValues, qt.HasFreeText, qt.HasMatrix,
                qt.ValidationRegex, qt.DefaultMinValue, qt.DefaultMaxValue
            FROM SurveyBucks.Question q
            JOIN SurveyBucks.QuestionType qt ON q.QuestionTypeId = qt.Id
            WHERE q.Id = @QuestionId AND q.IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<QuestionValidationDto>(sql, new { QuestionId = questionId });
            }
        }

        private async Task<ValidationResult> ValidateResponseContentAsync(SurveyResponseDto response, QuestionValidationDto questionDetails)
        {
            var result = new ValidationResult();

            // Basic null/empty validation
            if (questionDetails.IsMandatory && string.IsNullOrWhiteSpace(response.Answer))
            {
                result.AddError("This question is required");
                return result;
            }

            // Type-specific validation
            switch (questionDetails.QuestionTypeName)
            {
                case "ShortText":
                case "LongText":
                    result = ValidateTextResponse(response.Answer, questionDetails);
                    break;

                case "SingleChoice":
                    result = await ValidateSingleChoiceResponseAsync(response, questionDetails);
                    break;

                case "MultipleChoice":
                    result = await ValidateMultipleChoiceResponseAsync(response, questionDetails);
                    break;

                case "Rating":
                case "Slider":
                case "NumberInput":
                    result = ValidateNumericResponse(response.Answer, questionDetails);
                    break;

                case "Matrix":
                    result = await ValidateMatrixResponseAsync(response, questionDetails);
                    break;

                case "Date":
                    result = ValidateDateResponse(response.Answer);
                    break;

                case "Email":
                    result = ValidateEmailResponse(response.Answer);
                    break;

                case "Phone":
                    result = ValidatePhoneResponse(response.Answer);
                    break;

                case "YesNo":
                    result = ValidateYesNoResponse(response.Answer);
                    break;

                default:
                    result.IsValid = true;
                    break;
            }

            return result;
        }

        private ValidationResult ValidateTextResponse(string answer, QuestionValidationDto questionDetails)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(answer))
            {
                result.IsValid = true;
                return result;
            }

            // Length validation
            if (questionDetails.MinValue.HasValue && answer.Length < questionDetails.MinValue)
            {
                result.AddError($"Answer must be at least {questionDetails.MinValue} characters long");
            }

            if (questionDetails.MaxValue.HasValue && answer.Length > questionDetails.MaxValue)
            {
                result.AddError($"Answer must be no more than {questionDetails.MaxValue} characters long");
            }

            // Regex validation
            if (!string.IsNullOrEmpty(questionDetails.ValidationRegex))
            {
                if (!Regex.IsMatch(answer, questionDetails.ValidationRegex))
                {
                    result.AddError(questionDetails.ValidationMessage ?? "Answer format is invalid");
                }
            }

            return result;
        }

        private async Task<ValidationResult> ValidateSingleChoiceResponseAsync(SurveyResponseDto response, QuestionValidationDto questionDetails)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(response.Answer))
            {
                result.IsValid = true;
                return result;
            }

            // Check if the selected choice exists
            const string sql = @"
            SELECT COUNT(1) FROM SurveyBucks.QuestionResponseChoice
            WHERE QuestionId = @QuestionId AND (Id = @ChoiceId OR Value = @Answer) AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var choiceExists = await connection.ExecuteScalarAsync<bool>(sql, new
                {
                    QuestionId = questionDetails.Id,
                    ChoiceId = int.TryParse(response.Answer, out var id) ? id : -1,
                    Answer = response.Answer
                });

                if (!choiceExists)
                {
                    result.AddError("Invalid choice selected");
                }
            }

            return result;
        }

        private async Task<ValidationResult> ValidateMultipleChoiceResponseAsync(SurveyResponseDto response, QuestionValidationDto questionDetails)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(response.Answer))
            {
                result.IsValid = true;
                return result;
            }

            try
            {
                // Parse multiple choices (assume comma-separated or JSON array)
                var selectedChoices = JsonSerializer.Deserialize<List<string>>(response.Answer);

                // Validate each choice
                const string sql = @"
                SELECT Value FROM SurveyBucks.QuestionResponseChoice
                WHERE QuestionId = @QuestionId AND IsDeleted = 0";

                using (var connection = _connectionFactory.CreateConnection())
                {
                    var validChoices = (await connection.QueryAsync<string>(sql, new { QuestionId = questionDetails.Id })).ToHashSet();

                    foreach (var choice in selectedChoices)
                    {
                        if (!validChoices.Contains(choice))
                        {
                            result.AddError($"Invalid choice: {choice}");
                        }
                    }

                    // Check for exclusive options
                    const string exclusiveSql = @"
                    SELECT Value FROM SurveyBucks.QuestionResponseChoice
                    WHERE QuestionId = @QuestionId AND IsExclusiveOption = 1 AND IsDeleted = 0";

                    var exclusiveChoices = (await connection.QueryAsync<string>(exclusiveSql, new { QuestionId = questionDetails.Id })).ToHashSet();

                    var hasExclusive = selectedChoices.Any(c => exclusiveChoices.Contains(c));
                    if (hasExclusive && selectedChoices.Count > 1)
                    {
                        result.AddError("Cannot select other options when an exclusive option is selected");
                    }
                }
            }
            catch (JsonException)
            {
                result.AddError("Invalid response format");
            }

            return result;
        }

        private ValidationResult ValidateNumericResponse(string answer, QuestionValidationDto questionDetails)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(answer))
            {
                result.IsValid = true;
                return result;
            }

            if (!decimal.TryParse(answer, out var numericValue))
            {
                result.AddError("Answer must be a valid number");
                return result;
            }

            var minValue = questionDetails.MinValue ?? questionDetails.DefaultMinValue;
            var maxValue = questionDetails.MaxValue ?? questionDetails.DefaultMaxValue;

            if (minValue.HasValue && numericValue < minValue)
            {
                result.AddError($"Value must be at least {minValue}");
            }

            if (maxValue.HasValue && numericValue > maxValue)
            {
                result.AddError($"Value must be no more than {maxValue}");
            }

            return result;
        }

        private async Task<ValidationResult> ValidateMatrixResponseAsync(SurveyResponseDto response, QuestionValidationDto questionDetails)
        {
            var result = new ValidationResult();

            if (!response.MatrixRowId.HasValue)
            {
                result.AddError("Matrix row must be specified");
                return result;
            }

            // Validate that the matrix row exists
            const string rowSql = @"
            SELECT COUNT(1) FROM SurveyBucks.MatrixRows
            WHERE Id = @RowId AND QuestionId = @QuestionId AND IsDeleted = 0";

            // Validate that the selected column value exists
            const string columnSql = @"
            SELECT COUNT(1) FROM SurveyBucks.MatrixColumns
            WHERE QuestionId = @QuestionId AND Value = @Answer AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var rowExists = await connection.ExecuteScalarAsync<bool>(rowSql, new
                {
                    RowId = response.MatrixRowId,
                    QuestionId = questionDetails.Id
                });

                var columnExists = await connection.ExecuteScalarAsync<bool>(columnSql, new
                {
                    QuestionId = questionDetails.Id,
                    Answer = response.Answer
                });

                if (!rowExists)
                {
                    result.AddError("Invalid matrix row");
                }

                if (!columnExists)
                {
                    result.AddError("Invalid matrix column value");
                }
            }

            return result;
        }

        private ValidationResult ValidateDateResponse(string answer)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(answer))
            {
                result.IsValid = true;
                return result;
            }

            if (!DateTime.TryParse(answer, out _))
            {
                result.AddError("Invalid date format");
            }

            return result;
        }

        private ValidationResult ValidateEmailResponse(string answer)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(answer))
            {
                result.IsValid = true;
                return result;
            }

            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(answer, emailRegex))
            {
                result.AddError("Invalid email format");
            }

            return result;
        }

        private ValidationResult ValidatePhoneResponse(string answer)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(answer))
            {
                result.IsValid = true;
                return result;
            }

            // Basic phone validation - adjust regex based on requirements
            var phoneRegex = @"^\+?[\d\s\-\(\)]{10,}$";
            if (!Regex.IsMatch(answer, phoneRegex))
            {
                result.AddError("Invalid phone number format");
            }

            return result;
        }

        private ValidationResult ValidateYesNoResponse(string answer)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(answer))
            {
                result.IsValid = true;
                return result;
            }

            var validAnswers = new[] { "yes", "no", "true", "false", "1", "0" };
            if (!validAnswers.Contains(answer.ToLowerInvariant()))
            {
                result.AddError("Answer must be Yes or No");
            }

            return result;
        }

        private async Task<bool> SaveResponseWithRetryAsync(SurveyResponseDto response, QuestionValidationDto questionDetails, int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    return await _participationRepository.SaveSurveyResponseAsync(response);
                }
                catch (Exception ex) when (attempt < maxRetries - 1)
                {
                    _logger.LogWarning(ex, "Failed to save response (attempt {Attempt}), retrying...", attempt + 1);
                    await Task.Delay(100 * (attempt + 1)); // Exponential backoff
                }
            }

            return false;
        }

        private async Task<int> SaveResponsesBatchAsync(List<SurveyResponseDto> responses, string userId)
        {
            // Implementation for batch saving responses
            // This would use a stored procedure or bulk insert for better performance

            int successCount = 0;
            foreach (var response in responses)
            {
                var saved = await _participationRepository.SaveSurveyResponseAsync(response);
                if (saved) successCount++;
            }

            return successCount;
        }

        private async Task UpdateQuestionProgressAsync(SurveyResponseDto response, string userId)
        {
            // Update participation progress based on answered questions
            // This could trigger automatic section completion

            await _gamificationRepository.ProcessChallengeProgressAsync(userId, "AnswerQuestion", 1);
        }

        private async Task<ScreeningResult> ProcessScreeningQuestionAsync(SurveyResponseDto response, QuestionValidationDto questionDetails, string userId)
        {
            // Process screening logic
            var result = new ScreeningResult();

            // This would implement the screening logic based on the question's ScreeningLogic field
            // For now, return a basic result
            result.IsQualified = true;

            return result;
        }

        private async Task<ConditionalAction> ProcessConditionalLogicAsync(SurveyResponseDto response, QuestionValidationDto questionDetails)
        {
            // Process conditional logic based on the response
            var action = new ConditionalAction();

            try
            {
                // Parse the screening logic (could be JSON or custom format)
                var logic = JsonSerializer.Deserialize<ConditionalLogicDto>(questionDetails.ScreeningLogic);

                // Evaluate the condition
                var conditionMet = EvaluateCondition(response.Answer, logic.ConditionType, logic.ConditionValue);

                if (conditionMet)
                {
                    action.ActionType = logic.ActionType;
                    action.TargetQuestionId = logic.TargetQuestionId;
                    action.TargetSectionId = logic.TargetSectionId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process conditional logic for question {QuestionId}", questionDetails.Id);
            }

            return action;
        }

        private bool EvaluateCondition(string answer, string conditionType, string conditionValue)
        {
            return conditionType switch
            {
                "equals" => string.Equals(answer, conditionValue, StringComparison.OrdinalIgnoreCase),
                "not_equals" => !string.Equals(answer, conditionValue, StringComparison.OrdinalIgnoreCase),
                "contains" => answer?.Contains(conditionValue, StringComparison.OrdinalIgnoreCase) == true,
                "greater_than" => decimal.TryParse(answer, out var num1) && decimal.TryParse(conditionValue, out var num2) && num1 > num2,
                "less_than" => decimal.TryParse(answer, out var num3) && decimal.TryParse(conditionValue, out var num4) && num3 < num4,
                "in_list" => conditionValue.Split(',').Contains(answer, StringComparer.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
