using Application.Services.Email;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public TestController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail([FromBody] string testEmail)
        {
            try
            {
                await _emailService.SendEmailAsync(testEmail, "Test Email",
                    "<h1>Hello!</h1><p>This is a test email from your application.</p>");
                return Ok("Email sent successfully!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to send email: {ex.Message}");
            }
        }
    }
}
