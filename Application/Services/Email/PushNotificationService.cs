using System.Threading.Tasks;

namespace Application.Services.Email
{
    public class PushNotificationService : IPushNotificationService
    {
        public Task SendAsync(string userId, string title, string message)
        {
            throw new System.NotImplementedException();
        }
    }
}
