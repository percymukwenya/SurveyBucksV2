using System;
using System.Collections.Generic;

namespace Domain.Models.Admin
{
    public enum BranchingActionType
    {
        None,
        ShowQuestion,
        HideQuestion,
        ShowQuestions,
        JumpToSection,
        SkipToQuestion,
        EndSurvey,
        Disqualify
    }

    public class BranchingAction
    {
        public BranchingActionType ActionType { get; set; }
        public int? TargetQuestionId { get; set; }
        public List<int> TargetQuestionIds { get; set; } = new();
        public int? TargetSectionId { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public static BranchingAction NoAction() => new() { ActionType = BranchingActionType.None };
    }

    public class BranchingEvaluationResult
    {
        public bool HasActions { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<BranchingAction> Actions { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        public static BranchingEvaluationResult Success(List<BranchingAction> actions) =>
            new() { HasActions = actions?.Count > 0, Actions = actions ?? new() };

        public static BranchingEvaluationResult Error(string message) =>
            new() { IsError = true, ErrorMessage = message };

        public static BranchingEvaluationResult NoActions() =>
            new() { HasActions = false, Actions = new() };
    }

    public class SurveyFlowState
    {
        public int ParticipationId { get; set; }
        public int SurveyId { get; set; }
        public int? CurrentSectionId { get; set; }
        public int? CurrentQuestionId { get; set; }
        public List<int> CompletedQuestions { get; set; } = new();
        public List<int> AvailableQuestions { get; set; } = new();
        public List<ConditionalPathStep> ConditionalPath { get; set; } = new();
        public bool IsComplete { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class ConditionalPathStep
    {
        public int QuestionId { get; set; }
        public string Response { get; set; } = string.Empty;
        public BranchingActionType ActionTaken { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}