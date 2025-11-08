using Domain.Models;

namespace Domain.Interfaces.Service
{
    public interface IProfileCompletionCalculator
    {
        Task<UserProfileCompletionDto> CalculateCompletionAsync(string userId);
    }
}
