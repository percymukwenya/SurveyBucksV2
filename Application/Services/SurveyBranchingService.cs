using Domain.Interfaces.Repository;
using Domain.Interfaces.Repository.Admin;
using Domain.Interfaces.Service;
using Domain.Models.Admin;
using Domain.Models.Response;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface ISurveyBranchingService
    {
        Task<BranchingEvaluationResult> EvaluateQuestionLogicAsync(int questionId, string responseValue, int participationId);
        Task<SurveyFlowState> GetCurrentFlowStateAsync(int participationId);
        Task<List<int>> GetAvailableQuestionsAsync(int participationId, int sectionId);
        Task<BranchingAction> ProcessResponseBranchingAsync(SurveyResponseDto response, int participationId);
        Task<bool> ValidateSurveyFlowIntegrityAsync(int surveyId);
        Task<SurveyFlowVisualizationDto> GenerateFlowMapAsync(int surveyId);
    }

    public class SurveyBranchingService : ISurveyBranchingService
    {
        private readonly IQuestionLogicRepository _questionLogicRepository;
        private readonly ISurveyParticipationRepository _participationRepository;
        private readonly ILogger<SurveyBranchingService> _logger;

        public SurveyBranchingService(
            IQuestionLogicRepository questionLogicRepository,
            ISurveyParticipationRepository participationRepository,
            ILogger<SurveyBranchingService> logger)
        {
            _questionLogicRepository = questionLogicRepository;
            _participationRepository = participationRepository;
            _logger = logger;
        }

        public async Task<BranchingEvaluationResult> EvaluateQuestionLogicAsync(int questionId, string responseValue, int participationId)
        {
            try
            {
                _logger.LogDebug("Evaluating branching logic for Question {QuestionId}, Response: {ResponseValue}, Participation: {ParticipationId}",
                    questionId, responseValue, participationId);

                // 1. Get all logic rules for this question
                var logicRules = await _questionLogicRepository.GetQuestionLogicAsync(questionId);
                var activeRules = logicRules.Where(r => r.IsActive).ToList();

                if (!activeRules.Any())
                {
                    return BranchingEvaluationResult.NoActions();
                }

                // 2. Get current participation context
                var participationContext = await GetParticipationContextAsync(participationId);

                var result = new BranchingEvaluationResult();
                var executedActions = new List<BranchingAction>();

                // 3. Process each rule in order
                foreach (var rule in activeRules)
                {
                    var conditionMet = EvaluateCondition(responseValue, rule, participationContext);

                    if (conditionMet)
                    {
                        var action = await ProcessLogicRuleAsync(rule, participationId, participationContext);
                        executedActions.Add(action);

                        _logger.LogDebug("Logic rule {RuleId} triggered action: {ActionType}",
                            rule.Id, action.ActionType);

                        // Handle rule precedence - some actions should stop further processing
                        if (action.ActionType == BranchingActionType.EndSurvey ||
                            action.ActionType == BranchingActionType.JumpToSection)
                        {
                            break;
                        }
                    }
                }

                result.Actions = executedActions;
                result.HasActions = executedActions.Any();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating branching logic for Question {QuestionId}", questionId);
                return BranchingEvaluationResult.Error(ex.Message);
            }
        }

        public async Task<SurveyFlowState> GetCurrentFlowStateAsync(int participationId)
        {
            // Get current state of where user is in the survey flow
            var participation = await _participationRepository.GetParticipationAsync(participationId, null);
            if (participation == null)
            {
                throw new InvalidOperationException($"Participation {participationId} not found");
            }

            var responses = await _participationRepository.GetSavedResponsesAsync(participationId, null);
            var questionLogic = await _questionLogicRepository.GetSurveyLogicAsync(participation.SurveyId);

            return new SurveyFlowState
            {
                ParticipationId = participationId,
                SurveyId = participation.SurveyId,
                CurrentSectionId = participation.CurrentSectionId,
                CurrentQuestionId = participation.CurrentQuestionId,
                CompletedQuestions = responses.Select(r => r.QuestionId).ToList(),
                AvailableQuestions = (await CalculateAvailableQuestionsAsync(participationId, responses, questionLogic)).ToList(),
                ConditionalPath = await BuildConditionalPathAsync(participationId, responses, questionLogic),
                IsComplete = participation.StatusId == 3 // Completed status
            };
        }

        public async Task<List<int>> GetAvailableQuestionsAsync(int participationId, int sectionId)
        {
            var flowState = await GetCurrentFlowStateAsync(participationId);
            return flowState.AvailableQuestions
                .Where(q => GetQuestionSectionId(q) == sectionId) // You'd need to implement this
                .ToList();
        }

        public async Task<BranchingAction> ProcessResponseBranchingAsync(SurveyResponseDto response, int participationId)
        {
            var evaluation = await EvaluateQuestionLogicAsync(response.QuestionId, response.Answer, participationId);

            if (!evaluation.HasActions)
            {
                return BranchingAction.NoAction();
            }

            // Execute the primary action (first one, or highest priority)
            var primaryAction = evaluation.Actions.FirstOrDefault();
            if (primaryAction != null)
            {
                await ExecuteBranchingActionAsync(primaryAction, participationId);

                // Update participation state if needed
                await UpdateParticipationFlowStateAsync(participationId, primaryAction);
            }

            return primaryAction ?? BranchingAction.NoAction();
        }

        public async Task<bool> ValidateSurveyFlowIntegrityAsync(int surveyId)
        {
            try
            {
                var allLogic = await _questionLogicRepository.GetSurveyLogicAsync(surveyId);
                var issues = new List<string>();

                // 1. Check for circular references
                var circularRefs = DetectCircularReferences(allLogic);
                issues.AddRange(circularRefs);

                // 2. Check for unreachable questions
                var unreachableQuestions = FindUnreachableQuestions(surveyId, allLogic);
                issues.AddRange(unreachableQuestions.Select(q => $"Question {q} is unreachable"));

                // 3. Check for invalid target references
                var invalidTargets = FindInvalidTargets(allLogic);
                issues.AddRange(invalidTargets);

                if (issues.Any())
                {
                    _logger.LogWarning("Survey {SurveyId} has flow integrity issues: {Issues}",
                        surveyId, string.Join("; ", issues));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating survey flow integrity for Survey {SurveyId}", surveyId);
                return false;
            }
        }

        public async Task<SurveyFlowVisualizationDto> GenerateFlowMapAsync(int surveyId)
        {
            var allLogic = await _questionLogicRepository.GetSurveyLogicAsync(surveyId);

            // Build a comprehensive flow map for visualization
            var flowMap = new SurveyFlowVisualizationDto
            {
                SurveyId = surveyId,
                Nodes = await BuildFlowNodesAsync(surveyId, allLogic),
                Edges = BuildFlowEdges(allLogic),
                DecisionPoints = IdentifyDecisionPoints(allLogic),
                EndPoints = IdentifyEndPoints(allLogic),
                OrphanedQuestions = FindUnreachableQuestions(surveyId, allLogic)
            };

            return flowMap;
        }

        #region Private Helper Methods

        private bool EvaluateCondition(string responseValue, QuestionLogicDto rule, ParticipationContext context)
        {
            try
            {
                return rule.ConditionType.ToLower() switch
                {
                    "equals" => string.Equals(responseValue?.Trim(), rule.ConditionValue?.Trim(), StringComparison.OrdinalIgnoreCase),
                    "not_equals" => !string.Equals(responseValue?.Trim(), rule.ConditionValue?.Trim(), StringComparison.OrdinalIgnoreCase),
                    "contains" => responseValue?.Contains(rule.ConditionValue, StringComparison.OrdinalIgnoreCase) == true,
                    "greater_than" => CompareNumeric(responseValue, rule.ConditionValue, (a, b) => a > b),
                    "less_than" => CompareNumeric(responseValue, rule.ConditionValue, (a, b) => a < b),
                    "between" => EvaluateBetweenCondition(responseValue, rule.ConditionValue, rule.ConditionValue2),
                    "in_list" => EvaluateInListCondition(responseValue, rule.ConditionValue),
                    "regex_match" => EvaluateRegexCondition(responseValue, rule.ConditionValue),
                    "cross_question" => EvaluateCrossQuestionCondition(rule, context),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error evaluating condition for rule {RuleId}", rule.Id);
                return false;
            }
        }

        private bool CompareNumeric(string value1, string value2, Func<decimal, decimal, bool> comparison)
        {
            if (decimal.TryParse(value1, out var num1) && decimal.TryParse(value2, out var num2))
            {
                return comparison(num1, num2);
            }
            return false;
        }

        private bool EvaluateBetweenCondition(string value, string min, string max)
        {
            if (decimal.TryParse(value, out var numValue) &&
                decimal.TryParse(min, out var minValue) &&
                decimal.TryParse(max, out var maxValue))
            {
                return numValue >= minValue && numValue <= maxValue;
            }
            return false;
        }

        private bool EvaluateInListCondition(string value, string listValues)
        {
            var list = listValues?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            return list.Any(item => string.Equals(value?.Trim(), item.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private bool EvaluateRegexCondition(string value, string pattern)
        {
            try
            {
                return System.Text.RegularExpressions.Regex.IsMatch(value ?? "", pattern);
            }
            catch
            {
                return false;
            }
        }

        private bool EvaluateCrossQuestionCondition(QuestionLogicDto rule, ParticipationContext context)
        {
            // Implement cross-question logic evaluation
            // This would check responses to other questions
            return false; // Placeholder
        }

        private async Task<BranchingAction> ProcessLogicRuleAsync(QuestionLogicDto rule, int participationId, ParticipationContext context)
        {
            return rule.LogicType.ToLower() switch
            {
                "show_question" => new BranchingAction
                {
                    ActionType = BranchingActionType.ShowQuestion,
                    TargetQuestionId = rule.TargetQuestionId,
                    Message = rule.Message
                },
                "hide_question" => new BranchingAction
                {
                    ActionType = BranchingActionType.HideQuestion,
                    TargetQuestionId = rule.TargetQuestionId,
                    Message = rule.Message
                },
                "jump_to_section" => new BranchingAction
                {
                    ActionType = BranchingActionType.JumpToSection,
                    TargetSectionId = rule.TargetSectionId,
                    Message = rule.Message ?? "Redirecting based on your response..."
                },
                "end_survey" => new BranchingAction
                {
                    ActionType = BranchingActionType.EndSurvey,
                    Message = rule.Message ?? "Survey completed based on your responses."
                },
                "disqualify" => new BranchingAction
                {
                    ActionType = BranchingActionType.Disqualify,
                    Message = rule.Message ?? "You do not qualify to continue this survey."
                },
                _ => BranchingAction.NoAction()
            };
        }

        private async Task<ParticipationContext> GetParticipationContextAsync(int participationId)
        {
            var responses = await _participationRepository.GetSavedResponsesAsync(participationId, null);
            var participation = await _participationRepository.GetParticipationAsync(participationId, null);

            return new ParticipationContext
            {
                ParticipationId = participationId,
                SurveyId = participation?.SurveyId ?? 0,
                Responses = responses.ToDictionary(r => r.QuestionId, r => r.Answer),
                CurrentSectionId = participation?.CurrentSectionId,
                CurrentQuestionId = participation?.CurrentQuestionId
            };
        }

        private async Task<HashSet<int>> CalculateAvailableQuestionsAsync(int participationId, IEnumerable<SurveyResponseDto> responses, IEnumerable<QuestionLogicDto> logic)
        {
            var available = new HashSet<int>();
            var responseDict = responses.ToDictionary(r => r.QuestionId, r => r.Answer);
            var processedQuestions = new HashSet<int>();

            // Start with all base questions (no parent dependencies)
            var baseQuestions = await GetBaseQuestionsAsync(participationId);
            foreach (var questionId in baseQuestions)
            {
                available.Add(questionId);
            }

            // Process logic rules in dependency order
            var logicByQuestion = logic.Where(l => l.IsActive)
                                      .GroupBy(l => l.QuestionId)
                                      .ToDictionary(g => g.Key, g => g.ToList());

            bool hasChanges = true;
            int maxIterations = 10; // Prevent infinite loops
            int iteration = 0;

            while (hasChanges && iteration < maxIterations)
            {
                hasChanges = false;
                iteration++;

                foreach (var response in responseDict)
                {
                    var questionId = response.Key;
                    var responseValue = response.Value;

                    if (processedQuestions.Contains(questionId)) continue;

                    if (logicByQuestion.ContainsKey(questionId))
                    {
                        foreach (var rule in logicByQuestion[questionId])
                        {
                            var context = new ParticipationContext
                            {
                                ParticipationId = participationId,
                                Responses = responseDict
                            };

                            if (EvaluateCondition(responseValue, rule, context))
                            {
                                switch (rule.LogicType.ToLower())
                                {
                                    case "show_question":
                                        if (rule.TargetQuestionId.HasValue && available.Add(rule.TargetQuestionId.Value))
                                        {
                                            hasChanges = true;
                                        }
                                        break;

                                    case "hide_question":
                                        if (rule.TargetQuestionId.HasValue && available.Remove(rule.TargetQuestionId.Value))
                                        {
                                            hasChanges = true;
                                        }
                                        break;
                                }
                            }
                        }
                        processedQuestions.Add(questionId);
                    }
                }
            }

            return available;
        }

        private async Task<List<ConditionalPathStep>> BuildConditionalPathAsync(int participationId, IEnumerable<SurveyResponseDto> responses, IEnumerable<QuestionLogicDto> logic)
        {
            var path = new List<ConditionalPathStep>();
            var responseDict = responses.ToDictionary(r => r.QuestionId, r => r);
            var logicByQuestion = logic.Where(l => l.IsActive)
                                      .GroupBy(l => l.QuestionId)
                                      .ToDictionary(g => g.Key, g => g.ToList());

            // Sort responses by creation time to build chronological path
            var sortedResponses = responses.OrderBy(r => r.ResponseDateTime);

            foreach (var response in sortedResponses)
            {
                if (logicByQuestion.ContainsKey(response.QuestionId))
                {
                    foreach (var rule in logicByQuestion[response.QuestionId])
                    {
                        var context = new ParticipationContext
                        {
                            ParticipationId = participationId,
                            Responses = responseDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Answer)
                        };

                        if (EvaluateCondition(response.Answer, rule, context))
                        {
                            path.Add(new ConditionalPathStep
                            {
                                QuestionId = response.QuestionId,
                                Response = response.Answer,
                                ActionTaken = GetBranchingActionType(rule.LogicType),
                                Timestamp = response.ResponseDateTime
                            });
                        }
                    }
                }
            }

            return path;
        }

        private BranchingActionType GetBranchingActionType(string logicType)
        {
            return logicType.ToLower() switch
            {
                "show_question" => BranchingActionType.ShowQuestion,
                "hide_question" => BranchingActionType.HideQuestion,
                "show_questions" => BranchingActionType.ShowQuestions,
                "jump_to_section" => BranchingActionType.JumpToSection,
                "skip_to_question" => BranchingActionType.SkipToQuestion,
                "end_survey" => BranchingActionType.EndSurvey,
                "disqualify" => BranchingActionType.Disqualify,
                _ => BranchingActionType.None
            };
        }

        private async Task<List<int>> GetBaseQuestionsAsync(int participationId)
        {
            // Get the participation to find the survey
            var participation = await _participationRepository.GetParticipationAsync(participationId, null);
            if (participation == null) return new List<int>();

            // Get all questions that have no conditional dependencies (always shown initially)
            var allLogic = await _questionLogicRepository.GetSurveyLogicAsync(participation.SurveyId);
            var conditionalQuestions = allLogic.SelectMany(l => GetAffectedQuestions(l)).ToHashSet();

            // For now, return questions that aren't targets of conditional logic
            // In a full implementation, you'd query the database for all survey questions
            // and filter out the conditional ones
            var baseQuestions = new List<int>();

            // This is a simplified implementation - in reality you'd get all section questions
            // and filter out those that are conditional targets
            return baseQuestions;
        }

        private List<int> GetAffectedQuestions(QuestionLogicDto rule)
        {
            var affected = new List<int>();

            if (rule.TargetQuestionId.HasValue)
                affected.Add(rule.TargetQuestionId.Value);

            return affected;
        }

        private int GetQuestionSectionId(int questionId)
        {
            // In a real implementation, this would query the database
            // For now, return a default section ID
            return 1; // Default section
        }

        private async Task ExecuteBranchingActionAsync(BranchingAction action, int participationId)
        {
            _logger.LogInformation("Executing branching action {ActionType} for participation {ParticipationId}",
                action.ActionType, participationId);

            switch (action.ActionType)
            {
                case BranchingActionType.JumpToSection:
                    if (action.TargetSectionId.HasValue)
                    {
                        // Update participation to point to new section
                        var participation = await _participationRepository.GetParticipationAsync(participationId, null);
                        if (participation != null)
                        {
                            // Log the jump action for analytics
                            _logger.LogInformation("User jumped from current section to section {TargetSectionId} via branching logic",
                                action.TargetSectionId.Value);
                        }
                    }
                    break;

                case BranchingActionType.EndSurvey:
                    // Mark survey as completed due to logic
                    await _participationRepository.CompleteSurveyAsync(participationId, null);
                    _logger.LogInformation("Survey {ParticipationId} completed via branching logic", participationId);
                    break;

                case BranchingActionType.Disqualify:
                    // Mark as disqualified - you'd need to add this status to your system
                    _logger.LogInformation("User disqualified from survey {ParticipationId} via branching logic", participationId);
                    break;

                case BranchingActionType.ShowQuestion:
                case BranchingActionType.HideQuestion:
                case BranchingActionType.ShowQuestions:
                    // These are handled client-side, just log for analytics
                    _logger.LogDebug("Question visibility changed via branching logic: {ActionType}", action.ActionType);
                    break;
            }
        }

        private async Task UpdateParticipationFlowStateAsync(int participationId, BranchingAction action)
        {
            try
            {
                // Update participation state based on the action taken
                if (action.ActionType == BranchingActionType.JumpToSection && action.TargetSectionId.HasValue)
                {
                    // In a full implementation, you'd update the current section in the participation record
                    _logger.LogDebug("Would update participation {ParticipationId} to section {SectionId}",
                        participationId, action.TargetSectionId.Value);
                }
                else if (action.ActionType == BranchingActionType.SkipToQuestion && action.TargetQuestionId.HasValue)
                {
                    // Update current question pointer
                    _logger.LogDebug("Would update participation {ParticipationId} to question {QuestionId}",
                        participationId, action.TargetQuestionId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating participation flow state for {ParticipationId}", participationId);
            }
        }

        private List<string> DetectCircularReferences(IEnumerable<QuestionLogicDto> logic)
        {
            var issues = new List<string>();
            var graph = BuildLogicGraph(logic);
            var visited = new HashSet<int>();
            var recursionStack = new HashSet<int>();

            foreach (var node in graph.Keys)
            {
                if (!visited.Contains(node))
                {
                    if (HasCycleRecursive(node, graph, visited, recursionStack))
                    {
                        issues.Add($"Circular reference detected involving question {node}");
                    }
                }
            }

            return issues;
        }

        private Dictionary<int, List<int>> BuildLogicGraph(IEnumerable<QuestionLogicDto> logic)
        {
            var graph = new Dictionary<int, List<int>>();

            foreach (var rule in logic.Where(l => l.IsActive))
            {
                if (!graph.ContainsKey(rule.QuestionId))
                {
                    graph[rule.QuestionId] = new List<int>();
                }

                // Add edges for show/hide logic
                if (rule.TargetQuestionId.HasValue &&
                    (rule.LogicType.ToLower().Contains("show") || rule.LogicType.ToLower().Contains("hide")))
                {
                    graph[rule.QuestionId].Add(rule.TargetQuestionId.Value);
                }
            }

            return graph;
        }

        private bool HasCycleRecursive(int node, Dictionary<int, List<int>> graph, HashSet<int> visited, HashSet<int> recursionStack)
        {
            visited.Add(node);
            recursionStack.Add(node);

            if (graph.ContainsKey(node))
            {
                foreach (var neighbor in graph[node])
                {
                    if (!visited.Contains(neighbor))
                    {
                        if (HasCycleRecursive(neighbor, graph, visited, recursionStack))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(neighbor))
                    {
                        return true; // Back edge found
                    }
                }
            }

            recursionStack.Remove(node);
            return false;
        }

        private List<int> FindUnreachableQuestions(int surveyId, IEnumerable<QuestionLogicDto> logic)
        {
            var unreachable = new List<int>();

            var allQuestionIds = logic.Select(l => l.QuestionId)
                                     .Union(logic.Where(l => l.TargetQuestionId.HasValue).Select(l => l.TargetQuestionId.Value))
                                     .Distinct()
                                     .ToList();

            var reachableQuestions = new HashSet<int>();
            var hiddenQuestions = new HashSet<int>();

            var graph = BuildLogicGraph(logic);
            var baseQuestions = allQuestionIds.Except(
                logic.Where(l => l.LogicType.ToLower().Contains("show"))
                     .SelectMany(l => GetAffectedQuestions(l))
            ).ToList();

            foreach (var baseQuestion in baseQuestions)
            {
                reachableQuestions.Add(baseQuestion);
            }

            bool hasChanges = true;
            int iterations = 0;
            const int maxIterations = 20;

            while (hasChanges && iterations < maxIterations)
            {
                hasChanges = false;
                iterations++;

                foreach (var rule in logic.Where(l => l.IsActive))
                {
                    var sourceReachable = reachableQuestions.Contains(rule.QuestionId);

                    if (sourceReachable)
                    {
                        foreach (var targetId in GetAffectedQuestions(rule))
                        {
                            if (rule.LogicType.ToLower().Contains("show") && reachableQuestions.Add(targetId))
                            {
                                hasChanges = true;
                                hiddenQuestions.Remove(targetId);
                            }
                            else if (rule.LogicType.ToLower().Contains("hide") && hiddenQuestions.Add(targetId))
                            {
                                hasChanges = true;
                                reachableQuestions.Remove(targetId);
                            }
                        }
                    }
                }
            }

            unreachable = allQuestionIds.Except(reachableQuestions).ToList();

            return unreachable;
        }

        private List<string> FindInvalidTargets(IEnumerable<QuestionLogicDto> logic)
        {
            var issues = new List<string>();

            // In a real implementation, you'd validate against actual database records
            // For now, we'll do basic validation against what we can see in the logic
            var allQuestionIds = logic.Select(l => l.QuestionId)
                                     .Union(logic.Where(l => l.TargetQuestionId.HasValue).Select(l => l.TargetQuestionId.Value))
                                     .ToHashSet();

            foreach (var rule in logic)
            {
                // Check target question ID
                if (rule.TargetQuestionId.HasValue && !allQuestionIds.Contains(rule.TargetQuestionId.Value))
                {
                    issues.Add($"Rule {rule.Id} references non-existent question {rule.TargetQuestionId.Value}");
                }

                // Validate that self-references are not allowed
                if (rule.TargetQuestionId == rule.QuestionId)
                {
                    issues.Add($"Rule {rule.Id} has invalid self-reference");
                }
            }

            return issues;
        }

        private async Task<List<FlowNode>> BuildFlowNodesAsync(int surveyId, IEnumerable<QuestionLogicDto> logic)
        {
            return new List<FlowNode>();
        }

        private List<FlowEdge> BuildFlowEdges(IEnumerable<QuestionLogicDto> logic)
        {
            return new List<FlowEdge>();
        }

        private List<DecisionPoint> IdentifyDecisionPoints(IEnumerable<QuestionLogicDto> logic)
        {
            return new List<DecisionPoint>();
        }

        private List<string> IdentifyEndPoints(IEnumerable<QuestionLogicDto> logic)
        {
            var endPointIds = new List<string>();

            // Example logic: find questions that are not sources in any logic rule (i.e., not QuestionId in any rule)
            var allTargetIds = logic.Where(l => l.IsActive)
                .SelectMany(l =>
                {
                    var targets = new List<int>();
                    if (l.TargetQuestionId.HasValue) targets.Add(l.TargetQuestionId.Value);
                    return targets;
                })
                .Distinct()
                .ToHashSet();

            var allSourceIds = logic.Where(l => l.IsActive)
                .Select(l => l.QuestionId)
                .Distinct()
                .ToHashSet();

            // End points: targets that are not sources
            var endPoints = allTargetIds.Except(allSourceIds);

            // Convert to string as required by SurveyFlowVisualizationDto
            endPointIds.AddRange(endPoints.Select(id => id.ToString()));

            return endPointIds;
        }

        #endregion
    }

    #region Supporting Classes

    public class ParticipationContext
    {
        public int ParticipationId { get; set; }
        public int SurveyId { get; set; }
        public Dictionary<int, string> Responses { get; set; } = new();
        public int? CurrentSectionId { get; set; }
        public int? CurrentQuestionId { get; set; }
    }

    #endregion
}