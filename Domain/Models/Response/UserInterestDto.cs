namespace Domain.Models.Response
{
    public class UserInterestDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Interest { get; set; }
        public int? InterestLevel { get; set; }
    }
}
