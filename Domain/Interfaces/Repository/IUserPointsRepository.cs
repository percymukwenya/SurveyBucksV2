namespace Domain.Interfaces.Repository
{
    public interface IUserPointsRepository
    {
        Task<bool> AddPointsAsync(string userId, int points, string actionType, string referenceId);
        Task<int> GetTotalPointsAsync(string userId);
        Task<int> GetActionCountAsync(string userId, string actionType);
        Task<int> GetUserLevelAsync(string userId);
        Task<bool> UpdateUserLevelAsync(string userId, int level);
        Task<int> GetTotalPointsSpentAsync(string userId);
    }
}
