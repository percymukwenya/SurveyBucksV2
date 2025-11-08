using Domain.Models.Response;

namespace Domain.Models
{
    public class DemographicFieldRule
    {
        public string FieldName { get; }
        public string DisplayName { get; }
        public Func<DemographicsDto, bool> ValidationRule { get; }
        public string Suggestion { get; }

        public DemographicFieldRule(string displayName, Func<DemographicsDto, bool> validationRule, string suggestion = null)
        {
            DisplayName = displayName;
            ValidationRule = validationRule;
            Suggestion = suggestion;
            FieldName = displayName.Replace(" ", "");
        }
    }
}
