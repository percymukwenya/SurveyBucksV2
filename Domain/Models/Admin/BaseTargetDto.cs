namespace Domain.Models.Admin
{
    public class BaseTargetDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    /// <summary>
    /// DTO for Country targeting criteria
    /// </summary>
    public class CountryTargetDto : BaseTargetDto
    {
        /// <summary>
        /// The targeted country name
        /// </summary>
        public string Country { get; set; }
    }

    /// <summary>
    /// DTO for State/Province targeting criteria
    /// </summary>
    public class StateTargetDto : BaseTargetDto
    {
        /// <summary>
        /// The targeted state or province name
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Optional reference to the country ID this state belongs to
        /// </summary>
        public int? CountryId { get; set; }
    }

    /// <summary>
    /// DTO for household size targeting criteria
    /// </summary>
    public class HouseholdSizeTargetDto : BaseTargetDto
    {
        /// <summary>
        /// Minimum household size (number of people)
        /// </summary>
        public int MinSize { get; set; }

        /// <summary>
        /// Maximum household size (number of people)
        /// </summary>
        public int MaxSize { get; set; }
    }

    /// <summary>
    /// DTO for parental status targeting criteria
    /// </summary>
    public class ParentalStatusTargetDto : BaseTargetDto
    {
        /// <summary>
        /// Indicates whether the target is for parents (true) or non-parents (false)
        /// </summary>
        public bool HasChildren { get; set; }
    }

    /// <summary>
    /// DTO for industry targeting criteria
    /// </summary>
    public class IndustryTargetDto : BaseTargetDto
    {
        /// <summary>
        /// The targeted industry name
        /// </summary>
        public string Industry { get; set; }
    }

    /// <summary>
    /// DTO for marital status targeting criteria
    /// </summary>
    public class MaritalStatusTargetDto : BaseTargetDto
    {
        /// <summary>
        /// The targeted marital status
        /// </summary>
        public string MaritalStatus { get; set; }
    }
}
