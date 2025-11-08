using System.Collections.Generic;

namespace Application.Models
{
    public class SurveyMatchingConfiguration
    {
        public int DefaultMatchThreshold { get; set; } = 70;
        public Dictionary<string, int> FieldWeights { get; set; } = new()
        {
            ["Age"] = 20,
            ["Gender"] = 15,
            ["Location"] = 20,
            ["Income"] = 15,
            ["Interests"] = 20,
            ["Education"] = 10
        };
        public bool EnableCaching { get; set; } = true;
        public int CacheExpirationMinutes { get; set; } = 10;
    }
}
