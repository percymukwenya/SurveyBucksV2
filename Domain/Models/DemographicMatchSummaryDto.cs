namespace Domain.Models
{
    public class DemographicMatchSummaryDto
    {
        public int HasGender { get; set; }
        public int HasAge { get; set; }
        public int HasLocation { get; set; }
        public int HasCountry { get; set; }
        public int HasIncome { get; set; }
        public int HasEducation { get; set; }
        public int HasOccupation { get; set; }
        public int HasMaritalStatus { get; set; }
        public int HasHouseholdSize { get; set; }
        public int HasParentalStatus { get; set; }
        public int HasIndustry { get; set; }
        public int InterestCount { get; set; }
        public int TotalActiveSurveys { get; set; }
        public int MatchingSurveyCount { get; set; }

        // Additional calculated properties
        public Dictionary<string, int> ImportantFactors { get; set; }
        public int TotalMatchingPoints { get; set; }
        public int MaxPossiblePoints => 100;

        // Helper properties for UI
        public decimal CompletionPercentage => (decimal)TotalMatchingPoints / MaxPossiblePoints * 100;
        public decimal SurveyMatchPercentage => TotalActiveSurveys > 0
            ? (decimal)MatchingSurveyCount / TotalActiveSurveys * 100
            : 0;

        public List<string> MissingImportantFactors
        {
            get
            {
                var missing = new List<string>();
                if (HasGender == 0) missing.Add("Gender");
                if (HasAge == 0) missing.Add("Age");
                if (HasLocation == 0) missing.Add("Location");
                if (HasCountry == 0) missing.Add("Country");
                if (HasIncome == 0) missing.Add("Income");
                if (InterestCount == 0) missing.Add("Interests");
                return missing;
            }
        }
    }
}
