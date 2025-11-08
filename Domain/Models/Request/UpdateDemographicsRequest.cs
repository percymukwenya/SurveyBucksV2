using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Request
{
    public class UpdateDemographicsRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public int Age { get; set; }

        [Required]
        public string HighestEducation { get; set; }

        public decimal? Income { get; set; } // Keep for backward compatibility
        
        [Required]
        public string IncomeRange { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string Occupation { get; set; }
        public string MaritalStatus { get; set; }
        public int? HouseholdSize { get; set; }
        public bool? HasChildren { get; set; }
        public int? NumberOfChildren { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public string UrbanRural { get; set; }
        public string Industry { get; set; }
        public string JobTitle { get; set; }
        public int? YearsOfExperience { get; set; }
        public string EmploymentStatus { get; set; }
        public string CompanySize { get; set; }
        public string FieldOfStudy { get; set; }
        public int? YearOfGraduation { get; set; }
        public string DeviceTypes { get; set; }
        public int? InternetUsageHoursPerWeek { get; set; }
        public string IncomeCurrency { get; set; }
    }
}
