namespace Domain.Models.Response
{
    public class LeaderboardDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string LeaderboardType { get; set; }
        public string TimePeriod { get; set; }
        public List<LeaderboardEntryDto> Entries { get; set; }
        // Current user's rank
        public int UserRank { get; set; }
        public int UserScore { get; set; }
    }
}
