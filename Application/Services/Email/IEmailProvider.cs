using Domain.Models.Email;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services.Email
{
    public interface IEmailProvider
    {
        Task<EmailResult> SendAsync(string toEmail, EmailContent content);
        Task<EmailResult> SendAsync(string toEmail, string toName, EmailContent content);
        Task<List<EmailResult>> SendBulkAsync(List<(string Email, string Name)> recipients, EmailContent content);
    }
}
