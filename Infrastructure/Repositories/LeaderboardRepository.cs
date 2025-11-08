using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models.Response;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class LeaderboardRepository : ILeaderboardRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public LeaderboardRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<LeaderboardSummaryDto>> GetAvailableLeaderboardsAsync(string userId)
        {
            const string sql = @"
            SELECT 
                l.Id, l.Name, l.Description, l.LeaderboardType, l.TimePeriod,
                l.StartDate, l.EndDate, l.IsActive, l.RewardPoints,
                (SELECT COUNT(*) FROM SurveyBucks.LeaderboardEntries WHERE LeaderboardId = l.Id) AS EntryCount,
                (SELECT MAX(le.Rank) 
                 FROM SurveyBucks.LeaderboardEntries le 
                 WHERE le.LeaderboardId = l.Id AND le.UserId = @UserId) AS UserRank
            FROM SurveyBucks.Leaderboards l
            WHERE l.IsActive = 1
              AND l.IsDeleted = 0
              AND (l.StartDate IS NULL OR l.StartDate <= SYSDATETIMEOFFSET())
              AND (l.EndDate IS NULL OR l.EndDate >= SYSDATETIMEOFFSET())
            ORDER BY l.TimePeriod, l.Name";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<LeaderboardSummaryDto>(sql, new { UserId = userId });
        }

        public async Task<LeaderboardDto> GetLeaderboardAsync(int leaderboardId, string userId, int top = 10)
        {
            using var connection = _connectionFactory.CreateConnection();

            // Get leaderboard info
            const string leaderboardSql = @"
            SELECT Id, Name, Description, LeaderboardType, TimePeriod
            FROM SurveyBucks.Leaderboards
            WHERE Id = @LeaderboardId AND IsActive = 1 AND IsDeleted = 0";

            var leaderboard = await connection.QuerySingleOrDefaultAsync<LeaderboardDto>(
                leaderboardSql, new { LeaderboardId = leaderboardId });

            if (leaderboard == null) return null;

            // Get top entries
            const string entriesSql = @"
            SELECT le.Id, le.LeaderboardId, le.UserId, u.UserName as Username,
                   le.Score, le.Rank, le.PreviousRank
            FROM SurveyBucks.LeaderboardEntries le
            JOIN AspNetUsers u ON le.UserId = u.Id
            WHERE le.LeaderboardId = @LeaderboardId AND le.IsDeleted = 0
            ORDER BY le.Rank
            OFFSET 0 ROWS FETCH NEXT @Top ROWS ONLY";

            leaderboard.Entries = (await connection.QueryAsync<LeaderboardEntryDto>(
                entriesSql, new { LeaderboardId = leaderboardId, Top = top })).ToList();

            // Get current user's rank
            const string userRankSql = @"
            SELECT le.Rank, le.Score
            FROM SurveyBucks.LeaderboardEntries le
            WHERE le.LeaderboardId = @LeaderboardId AND le.UserId = @UserId AND le.IsDeleted = 0";

            var userRank = await connection.QuerySingleOrDefaultAsync<(int Rank, int Score)>(
                userRankSql, new { LeaderboardId = leaderboardId, UserId = userId });

            leaderboard.UserRank = userRank.Rank;
            leaderboard.UserScore = userRank.Score;

            return leaderboard;
        }

        public async Task<bool> UpdateLeaderboardEntryAsync(int leaderboardId, string userId, int score, int rank, int? previousRank = null)
        {
            const string sql = @"
            MERGE SurveyBucks.LeaderboardEntries AS target
            USING (SELECT @LeaderboardId AS LeaderboardId, @UserId AS UserId) AS source
            ON target.LeaderboardId = source.LeaderboardId AND target.UserId = source.UserId AND target.IsDeleted = 0
            WHEN MATCHED THEN
                UPDATE SET 
                    Score = @Score,
                    Rank = @Rank,
                    PreviousRank = CASE WHEN @PreviousRank IS NOT NULL THEN @PreviousRank ELSE PreviousRank END,
                    SnapshotDate = SYSDATETIMEOFFSET(),
                    ModifiedDate = SYSDATETIMEOFFSET()
            WHEN NOT MATCHED THEN
                INSERT (LeaderboardId, UserId, Score, Rank, PreviousRank, IsRewarded, SnapshotDate, CreatedDate)
                VALUES (@LeaderboardId, @UserId, @Score, @Rank, @PreviousRank, 0, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new
            {
                LeaderboardId = leaderboardId,
                UserId = userId,
                Score = score,
                Rank = rank,
                PreviousRank = previousRank
            });

            return result > 0;
        }

        public async Task<bool> ClearLeaderboardEntriesAsync(int leaderboardId)
        {
            const string sql = @"
            DELETE FROM SurveyBucks.LeaderboardEntries    
            WHERE LeaderboardId = @LeaderboardId";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { LeaderboardId = leaderboardId });
            return result >= 0; // Can be 0 if no entries exist
        }
    }
}
