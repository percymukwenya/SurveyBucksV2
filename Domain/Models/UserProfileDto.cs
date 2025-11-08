namespace Domain.Models
{
    public class UserProfileDto
    {
        public string UserId { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public decimal? Income { get; set; } // Keep for backward compatibility
        public string IncomeRange { get; set; }
        public string Education { get; set; }
        public string Occupation { get; set; }
    }
}
