namespace Domain.Models.Response
{
    public class LeaderboardEntryDto
    {
        public int Id { get; set; }
        public int LeaderboardId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
        public int? PreviousRank { get; set; }
    }
}
