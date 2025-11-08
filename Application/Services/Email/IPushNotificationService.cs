using System.Threading.Tasks;

namespace Application.Services.Email
{
    public interface IPushNotificationService
    {
        Task SendAsync(string userId, string title, string message);
    }
}
