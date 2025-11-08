namespace Domain.Models.Admin
{
    public class QuestionLogicDto
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string LogicType { get; set; } // 'Skip', 'Show', 'Hide', 'EndSurvey'
        public string ConditionType { get; set; } // 'Equals', 'NotEquals', 'Contains', 'GreaterThan', 'LessThan'
        public string ConditionValue { get; set; }
        public string ConditionValue2 { get; set; }
        public int? TargetQuestionId { get; set; }
        public int? TargetSectionId { get; set; }
        public string TargetQuestionText { get; set; }
        public string TargetSectionName { get; set; }
        public bool IsActive { get; set; }
        public string Message { get; set; }
    }

    public class QuestionLogicCreateDto
    {
        public int QuestionId { get; set; }
        public string LogicType { get; set; }
        public string ConditionType { get; set; }
        public string ConditionValue { get; set; }
        public int? TargetQuestionId { get; set; }
        public int? TargetSectionId { get; set; }
    }

    public class QuestionLogicUpdateDto : QuestionLogicCreateDto
    {
        public int Id { get; set; }
    }

    public class SurveyFlowVisualizationDto
    {
        public int SurveyId { get; set; }
        public List<FlowNode> Nodes { get; set; }
        public List<FlowEdge> Edges { get; set; }
        public List<DecisionPoint> DecisionPoints { get; set; }
        public List<string> EndPoints { get; set; }
        public List<int> OrphanedQuestions { get; set; }
    }

    public class FlowNode
    {
        public string Id { get; set; }
        public string Type { get; set; } // 'Section', 'Question', 'End'
        public string Label { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class FlowEdge
    {
        public string FromNodeId { get; set; }
        public string ToNodeId { get; set; }
        public string Label { get; set; }
        public string Condition { get; set; }
    }

    public class DecisionPoint
    {
        public int QuestionId { get; set; }
        public List<string> Conditions { get; set; }
        public List<BranchingAction> Actions { get; set; }
    }
}
