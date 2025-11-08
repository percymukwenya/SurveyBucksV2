using System.Collections.Generic;

namespace Application.Models
{
    public class ProfileCompletionConfiguration
    {
        public int RequiredCompletionPercentage { get; set; } = 100;
        public int RequiredInterestsCount { get; set; } = 3;
        public Dictionary<string, int> SectionWeights { get; set; } = new()
        {
            ["Demographics"] = 25,
            ["Banking"] = 25,
            ["Documents"] = 25,
            ["Interests"] = 25
        };
        public Dictionary<string, int> SectionPriorities { get; set; } = new()
        {
            ["Documents"] = 1,
            ["Demographics"] = 2,
            ["Banking"] = 3,
            ["Interests"] = 4
        };
    }
}
