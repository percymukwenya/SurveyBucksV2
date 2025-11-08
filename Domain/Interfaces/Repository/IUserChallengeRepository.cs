using Domain.Models;
using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface IUserChallengeRepository
    {
        Task<bool> UpdateChallengeProgressAsync(string userId, int challengeId, int progress, bool isCompleted, int? pointsAwarded = null);
        Task<IEnumerable<ChallengeDto>> GetActiveChallengesAsync(string userId);
        Task<ChallengeDto> GetChallengeByIdAsync(int challengeId);
        Task<UserChallengeDto> GetUserChallengeAsync(string userId, int challengeId);
    }
}
