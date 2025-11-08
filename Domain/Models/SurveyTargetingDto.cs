using Domain.Models.Admin;

namespace Domain.Models
{
    public class SurveyTargetingDto
    {
        public List<AgeRangeDto> AgeRanges { get; set; } = new();
        public List<string> Genders { get; set; } = new();
        public List<LocationTargetDto> Locations { get; set; } = new();
        public List<IncomeRangeDto> IncomeRanges { get; set; } = new();
        public List<string> Interests { get; set; } = new();
        public List<string> EducationLevels { get; set; } = new();
    }

    public class SurveyMatchDto
    {
        public SurveyDto Survey { get; set; }
        public int MatchScore { get; set; }
        public List<string> MatchReasons { get; set; } = new();
    }

    public class AgeRangeDto
    {
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
    }

    public class LocationTargetDto
    {
        public string Country { get; set; }
        public string Location { get; set; }
    }

    public class IncomeRangeDto
    {
        public decimal MinIncome { get; set; }
        public decimal MaxIncome { get; set; }
    }

    public class SurveyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime OpeningDateTime { get; set; }
        public DateTime ClosingDateTime { get; set; }
        public int DurationInSeconds { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string Industry { get; set; }
        public int MinQuestions { get; set; }
        public int MaxTimeInMins { get; set; }
        public bool RequireAllQuestions { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDeleted { get; set; }
    }
}
