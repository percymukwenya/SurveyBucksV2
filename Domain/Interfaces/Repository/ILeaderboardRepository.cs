using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface ILeaderboardRepository
    {
        Task<IEnumerable<LeaderboardSummaryDto>> GetAvailableLeaderboardsAsync(string userId);
        Task<LeaderboardDto> GetLeaderboardAsync(int leaderboardId, string userId, int top = 10);
        Task<bool> UpdateLeaderboardEntryAsync(int leaderboardId, string userId, int score, int rank, int? previousRank = null);
        Task<bool> ClearLeaderboardEntriesAsync(int leaderboardId);
    }
}
