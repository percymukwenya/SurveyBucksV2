using Application.Services.Auth;
using Application.Services.Email;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApi.Models.Auth;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService, IEmailService emailService,
            ILogger<AuthController> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model, bool isAdmin = false)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                RegistrationDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (isAdmin)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "Client");
                }

                // Generate email confirmation token (simulate sending email)
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                    new { userId = user.Id, token = emailToken }, Request.Scheme);

                try
                {
                    await _emailService.SendEmailConfirmationAsync(user.Email, user.FirstName, confirmationLink);
                    _logger.LogInformation("Email confirmation sent to {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                    // Don't fail registration if email fails
                }

                return Ok(new
                {
                    message = "User registered successfully. Please check your email to confirm your account.",
                    userId = user.Id,
                    requiresEmailConfirmation = true
                });
            }

            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return Unauthorized(new { message = "Invalid email or password" });

                // Check if email is confirmed (if required)
                if (_userManager.Options.SignIn.RequireConfirmedEmail && !user.EmailConfirmed)
                {
                    return BadRequest(new
                    {
                        message = "Please confirm your email before logging in.",
                        requiresEmailConfirmation = true,
                        email = user.Email
                    });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true); // Enable lockout

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully", user.Email);

                    var roles = await _userManager.GetRolesAsync(user);
                    var token = _tokenService.GenerateJwtToken(user.Id, user.UserName, roles);

                    return Ok(new
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        role = roles.FirstOrDefault(),
                        token
                    });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account {Email} is locked out", user.Email);

                    // Send account locked email
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    var lockoutMinutes = lockoutEnd?.Subtract(DateTimeOffset.UtcNow).TotalMinutes ?? 0;

                    try
                    {
                        await _emailService.SendAccountLockedEmailAsync(user.Email, user.FirstName, (int)Math.Ceiling(lockoutMinutes));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send account locked email to {Email}", user.Email);
                    }

                    return StatusCode(423, new
                    {
                        message = "Account is temporarily locked due to multiple failed attempts. Check your email for details.",
                        lockoutMinutes = (int)Math.Ceiling(lockoutMinutes)
                    });
                }

                if (result.IsNotAllowed)
                {
                    return BadRequest(new
                    {
                        message = "Sign in not allowed. Please confirm your email address.",
                        requiresEmailConfirmation = true
                    });
                }

                return Unauthorized(new { message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for {Email}", model.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                roles = roles
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password changed successfully for user {Email}", user.Email);

                // Send password change notification
                try
                {
                    await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.FirstName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password change notification to {Email}", user.Email);
                }

                return Ok(new { message = "Password changed successfully" });
            }

            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        /// <summary>
        /// Forgot Password endpoint:
        /// Accepts user email, generates a password reset token,
        /// and (in a production system) should trigger sending an email.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // For security reasons, always return success message
            var successMessage = "If an account with that email exists, password reset instructions have been sent.";

            if (user == null)
            {
                return Ok(new { message = successMessage });
            }

            // Check if user's email is confirmed (if required)
            if (_userManager.Options.SignIn.RequireConfirmedEmail && !user.EmailConfirmed)
            {
                return Ok(new { message = successMessage });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Create reset password link
            var resetLink = Url.Action("ResetPassword", "Auth",
                new { email = user.Email, token = token }, Request.Scheme);

            // If no frontend URL configured, use the API URL
            if (string.IsNullOrEmpty(resetLink))
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                resetLink = $"{baseUrl}/api/auth/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
            }

            try
            {
                await _emailService.SendPasswordResetAsync(user.Email, user.FirstName, resetLink);
                _logger.LogInformation("Password reset email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                // Still return success for security
            }

            return Ok(new { message = successMessage });
        }

        /// <summary>
        /// Reset Password endpoint:
        /// Accepts the email, token, and new password to update the user’s password.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Password reset failed. Invalid request." });
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successfully for user {Email}", user.Email);

                // Send confirmation email
                try
                {
                    await _emailService.SendPasswordResetConfirmationAsync(user.Email, user.FirstName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset confirmation to {Email}", user.Email);
                }

                return Ok(new { message = "Password reset successfully. You can now sign in with your new password." });
            }

            _logger.LogWarning("Password reset failed for user {Email}: {Errors}",
                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        /// <summary>
        /// Confirm Email endpoint:
        /// Accepts userId and token as query parameters to verify the user's email.
        /// </summary>
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest(new { message = "UserID and token are required" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Check if email is already confirmed
            if (user.EmailConfirmed)
            {
                return Ok(new { message = "Email address has already been confirmed." });
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed successfully for user {Email}", user.Email);

                // Send welcome email after confirmation
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                }

                return Ok(new
                {
                    message = "Email confirmed successfully! Welcome to our platform.",
                    emailConfirmed = true
                });
            }

            _logger.LogWarning("Email confirmation failed for user {Email}: {Errors}",
                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendConfirmationRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // For security, always return success message
            var successMessage = "If an account with that email exists and is not confirmed, a confirmation email has been sent.";

            if (user == null)
            {
                return Ok(new { message = successMessage });
            }

            if (user.EmailConfirmed)
            {
                return Ok(new { message = "Email address is already confirmed." });
            }

            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                new { userId = user.Id, token = emailToken }, Request.Scheme);

            try
            {
                await _emailService.SendEmailConfirmationAsync(user.Email, user.FirstName, confirmationLink);
                _logger.LogInformation("Email confirmation resent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend confirmation email to {Email}", user.Email);
            }

            return Ok(new { message = successMessage });
        }

        // <summary>
        /// Initiates external login flow - returns URL for frontend to redirect to
        /// </summary>
        [HttpGet("external-login-url/{provider}")]
        public IActionResult GetExternalLoginUrl(string provider, [FromQuery] string returnUrl = null)
        {
            var supportedProviders = new[] { "Google", "Facebook", "Microsoft" };
            if (!supportedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Unsupported authentication provider" });
            }

            // The callback URL that the external provider will redirect to
            var callbackUrl = Url.Action("ExternalLoginCallback", "Auth",
                new { returnUrl = returnUrl }, Request.Scheme);

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);

            // Return the URL for the frontend to redirect to
            var challengeUrl = $"/api/auth/challenge/{provider}?returnUrl={Uri.EscapeDataString(returnUrl ?? "")}";

            return Ok(new
            {
                provider = provider,
                url = challengeUrl,
                callbackUrl = callbackUrl
            });
        }

        /// <summary>
        /// Challenge endpoint that redirects to external provider
        /// </summary>
        [HttpGet("challenge/{provider}")]
        public IActionResult Challenge(string provider, [FromQuery] string returnUrl = null)
        {
            var callbackUrl = Url.Action("ExternalLoginCallback", "Auth",
                new { returnUrl = returnUrl }, Request.Scheme);

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);
            return Challenge(properties, provider);
        }

        /// <summary>
        /// External login callback - processes the external auth and returns JWT
        /// </summary>
        [HttpGet("external-login-callback")]
        public async Task<IActionResult> ExternalLoginCallback([FromQuery] string returnUrl = null, [FromQuery] string remoteError = null)
        {
            // Default frontend URL
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
            var redirectUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : frontendUrl;

            if (!string.IsNullOrEmpty(remoteError))
            {
                _logger.LogWarning("External login error: {Error}", remoteError);
                return Redirect($"{redirectUrl}?error={Uri.EscapeDataString(remoteError)}");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogWarning("Failed to load external login information");
                return Redirect($"{redirectUrl}?error={Uri.EscapeDataString("Failed to load external login information")}");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "";

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email claim not found for provider {Provider}", info.LoginProvider);
                return Redirect($"{redirectUrl}?error={Uri.EscapeDataString("Email address is required")}");
            }

            try
            {
                // Try to sign in with existing external login
                var result = await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

                if (result.Succeeded)
                {
                    var existingUser = await _userManager.FindByEmailAsync(email);
                    var roles = await _userManager.GetRolesAsync(existingUser);
                    var token = _tokenService.GenerateJwtToken(existingUser.Id, existingUser.UserName, roles);

                    _logger.LogInformation("External login successful for {Email} via {Provider}", email, info.LoginProvider);
                    return Redirect($"{redirectUrl}?token={token}&success=true");
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("External login account {Email} is locked", email);
                    return Redirect($"{redirectUrl}?error={Uri.EscapeDataString("Account is temporarily locked")}");
                }

                // Check if user exists with this email
                var existingUserByEmail = await _userManager.FindByEmailAsync(email);
                if (existingUserByEmail != null)
                {
                    // Link external login to existing account
                    var addLoginResult = await _userManager.AddLoginAsync(existingUserByEmail, info);
                    if (addLoginResult.Succeeded)
                    {
                        var roles = await _userManager.GetRolesAsync(existingUserByEmail);
                        var token = _tokenService.GenerateJwtToken(existingUserByEmail.Id, existingUserByEmail.UserName, roles);

                        _logger.LogInformation("External login {Provider} linked to existing account {Email}",
                            info.LoginProvider, email);

                        return Redirect($"{redirectUrl}?token={token}&success=true&linked=true");
                    }
                }

                // Create new user account
                if (string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(fullName))
                {
                    var nameParts = fullName.Split(' ', 2);
                    firstName = nameParts[0];
                    lastName = nameParts.Length > 1 ? nameParts[1] : "";
                }

                var newUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true, // External providers pre-verify emails
                    RegistrationDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(newUser);
                if (createResult.Succeeded)
                {
                    // Add default role
                    await _userManager.AddToRoleAsync(newUser, "Client");

                    // Link external login
                    var linkResult = await _userManager.AddLoginAsync(newUser, info);
                    if (linkResult.Succeeded)
                    {
                        // Send welcome email
                        try
                        {
                            await _emailService.SendWelcomeEmailAsync(newUser.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send welcome email to {Email}", newUser.Email);
                        }

                        var roles = await _userManager.GetRolesAsync(newUser);
                        var token = _tokenService.GenerateJwtToken(newUser.Id, newUser.UserName, roles);

                        _logger.LogInformation("New user created via {Provider}: {Email}", info.LoginProvider, email);
                        return Redirect($"{redirectUrl}?token={token}&success=true&newUser=true");
                    }
                    else
                    {
                        await _userManager.DeleteAsync(newUser);
                        _logger.LogError("Failed to link external login for new user {Email}", email);
                    }
                }

                return Redirect($"{redirectUrl}?error={Uri.EscapeDataString("Failed to create account")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External login callback error for {Email}", email);
                return Redirect($"{redirectUrl}?error={Uri.EscapeDataString("An error occurred during authentication")}");
            }
        }

        /// <summary>
        /// Verify external token (for mobile apps or SPA that handle OAuth directly)
        /// </summary>
        [HttpPost("external-login-token")]
        public async Task<IActionResult> ExternalLoginToken([FromBody] ExternalLoginTokenRequest request)
        {
            try
            {
                // Verify the external token with the provider
                var userInfo = await VerifyExternalToken(request.Provider, request.AccessToken);
                if (userInfo == null)
                {
                    return BadRequest(new { message = "Invalid external token" });
                }

                // Find or create user
                var user = await _userManager.FindByEmailAsync(userInfo.Email);
                if (user == null)
                {
                    // Create new user
                    user = new ApplicationUser
                    {
                        UserName = userInfo.Email,
                        Email = userInfo.Email,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                        EmailConfirmed = true,
                        RegistrationDate = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });
                    }

                    await _userManager.AddToRoleAsync(user, "Client");

                    // Send welcome email
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                    }
                }

                // Generate JWT token
                var roles = await _userManager.GetRolesAsync(user);
                var token = _tokenService.GenerateJwtToken(user.Id, user.UserName, roles);

                return Ok(new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = roles.FirstOrDefault(),
                    token,
                    provider = request.Provider
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External token verification failed for provider {Provider}", request.Provider);
                return BadRequest(new { message = "External authentication failed" });
            }
        }

        private async Task<ExternalUserInfo> VerifyExternalToken(string provider, string accessToken)
        {
            // This is a simplified example - implement actual token verification
            // For production, use the respective provider's API to verify tokens
            switch (provider.ToLower())
            {
                case "google":
                    return await VerifyGoogleToken(accessToken);
                case "facebook":
                    return await VerifyFacebookToken(accessToken);
                case "microsoft":
                    return await VerifyMicrosoftToken(accessToken);
                default:
                    return null;
            }
        }

        private async Task<ExternalUserInfo> VerifyGoogleToken(string accessToken)
        {
            // Implement Google token verification
            // Use Google.Apis.Auth library or HTTP calls to Google's tokeninfo endpoint
            return null; // Placeholder
        }

        private async Task<ExternalUserInfo> VerifyFacebookToken(string accessToken)
        {
            // Implement Facebook token verification
            return null; // Placeholder
        }

        private async Task<ExternalUserInfo> VerifyMicrosoftToken(string accessToken)
        {
            // Implement Microsoft token verification
            return null; // Placeholder
        }
    }
}
