using Application.Models;
using Domain.Interfaces.Repository;
using Domain.Models.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly IUserRepository _userRepository;
        private readonly IEmailProvider _emailProvider;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, IUserRepository userRepository,
            IEmailProvider emailProvider, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _userRepository = userRepository;
            _emailProvider = emailProvider;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var content = new EmailContent
            {
                Subject = subject,
                HtmlBody = htmlMessage
            };

            var result = await _emailProvider.SendAsync(toEmail, content);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to send email to {Email}: {Error}", toEmail, result.ErrorMessage);
                throw new InvalidOperationException($"Failed to send email: {result.ErrorMessage}");
            }
        }

        public async Task SendCompletionEmailAsync(string userId, string surveyName)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            var emailContent = new EmailContent
            {
                Subject = $"🎉 Survey Complete: {surveyName}",
                HtmlBody = $@"
                <h2>Thank you {user.FirstName}!</h2>
                <p>You've successfully completed the <strong>{surveyName}</strong> survey.</p>
                <p>Your SurveyBucks have been added to your account and you can now redeem them for rewards!</p>
                <p><a href='{GetRewardsUrl()}' style='background: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>View Rewards</a></p>",
                PlainTextBody = $"Thank you {user.FirstName}! You've completed the {surveyName} survey. Check your SurveyBucks account for your earned points."
            };

            var result = await _emailProvider.SendAsync(user.Email, user.FirstName, emailContent);
            LogEmailResult(result, user.Email, "Survey completion");
        }

        public async Task SendProfileCompletionCelebrationAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            var emailContent = new EmailContent
            {
                Subject = "🎉 Your SurveyBucks Profile is Complete! Welcome to Full Access!",
                HtmlBody = $@"
                <h2>Congratulations {user.FirstName}!</h2>
                <p>Your SurveyBucks profile is now 100% complete! 🎉</p>
                
                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                    <h3>🚀 You Now Have Access To:</h3>
                    <ul>
                        <li>✅ All premium surveys</li>
                        <li>✅ Higher-paying opportunities</li> 
                        <li>✅ Instant reward redemption</li>
                        <li>✅ Priority customer support</li>
                        <li>✅ Exclusive SurveyBucks member benefits</li>
                    </ul>
                </div>
                
                <p><a href='{GetSurveyUrl()}' style='background: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>Start Earning SurveyBucks Now! 🎯</a></p>
                
                <p>Thank you for being part of the SurveyBucks community!</p>",
                PlainTextBody = $"Congratulations {user.FirstName}! Your SurveyBucks profile is 100% complete and you now have access to all surveys and features."
            };

            await _emailProvider.SendAsync(user.Email, emailContent);
        }

        public async Task SendProfileMilestoneEmailAsync(string userId, string milestone, int completionPercentage)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            var (subject, header, motivation) = milestone switch
            {
                "50% Profile Complete" => (
                    "🎯 Halfway There! Your SurveyBucks Profile is 50% Complete",
                    "You're Making Great Progress!",
                    "You're halfway to unlocking all SurveyBucks features. Keep going!"
                ),
                "75% Profile Complete" => (
                    "💪 Almost Ready! Your SurveyBucks Profile is 75% Complete",
                    "You're So Close!",
                    "Just a few more steps and you'll have access to all SurveyBucks opportunities!"
                ),
                _ => (
                    $"⭐ Milestone Reached! Your SurveyBucks Profile is {completionPercentage}% Complete",
                    "Keep Up the Great Work!",
                    "Every step gets you closer to better survey opportunities and more SurveyBucks!"
                )
            };

            var emailContent = new EmailContent
            {
                Subject = subject,
                HtmlBody = $@"
                <h2>{header} {user.FirstName}!</h2>
                <p>{motivation}</p>
                
                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                    <div style='background: #4CAF50; height: 20px; width: {completionPercentage}%; border-radius: 10px; position: relative;'>
                        <span style='position: absolute; right: 10px; top: 2px; color: white; font-weight: bold; font-size: 12px;'>{completionPercentage}%</span>
                    </div>
                </div>
                
                <p><a href='{GetProfileUrl()}' style='background: #2196F3; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Complete Your SurveyBucks Profile</a></p>"
            };

            await _emailProvider.SendAsync(user.Email, emailContent);
        }

        public async Task SendProfileReminderEmailAsync(string userId, List<string> missingSections)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            var sectionsText = string.Join(", ", missingSections.Select(s => s.ToLower()));

            var emailContent = new EmailContent
            {
                Subject = "👋 Finish Your SurveyBucks Profile - Unlock Survey Opportunities!",
                HtmlBody = $@"
                <h2>Hi {user.FirstName},</h2>
                <p>You're so close to completing your SurveyBucks profile! Just finish your {sectionsText} and you'll unlock access to premium surveys.</p>
                
                <div style='border-left: 4px solid #FF9800; padding-left: 20px; margin: 20px 0;'>
                    <h4>📊 Did You Know?</h4>
                    <p>SurveyBucks members with complete profiles earn <strong>3x more</strong> than those with incomplete profiles!</p>
                </div>
                
                <p><a href='{GetProfileUrl()}' style='background: #FF9800; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>Complete SurveyBucks Profile Now</a></p>
                
                <p>It only takes 5-10 minutes and unlocks all SurveyBucks features!</p>"
            };

            await _emailProvider.SendAsync(user.Email, emailContent);
        }

        public async Task SendSurveyEligibilityEmailAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            var emailContent = new EmailContent
            {
                Subject = "🎯 SurveyBucks Unlocked! You Can Now Start Earning",
                HtmlBody = $@"
                <h2>Great News {user.FirstName}!</h2>
                <p>Your SurveyBucks profile is now complete enough to participate in surveys! 🎉</p>
                
                <div style='background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%); color: white; padding: 20px; border-radius: 10px; margin: 20px 0; text-align: center;'>
                    <h3>🚀 Ready to Start Earning SurveyBucks?</h3>
                    <p>You now have access to surveys that match your profile!</p>
                    <a href='{GetSurveyUrl()}' style='background: white; color: #4CAF50; padding: 12px 24px; text-decoration: none; border-radius: 25px; font-weight: bold; display: inline-block; margin-top: 10px;'>Browse Available Surveys</a>
                </div>
                
                <p><strong>💡 Pro Tip:</strong> Complete your SurveyBucks profile 100% to unlock even more high-paying opportunities!</p>"
            };

            await _emailProvider.SendAsync(user.Email, emailContent);
        }

        public async Task SendRewardClaimConfirmationEmailAsync(string userId, string rewardName)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            var emailContent = new EmailContent
            {
                Subject = "✅ SurveyBucks Reward Claim Confirmed",
                HtmlBody = $@"
                <h2>Hi {user.FirstName},</h2>
                <p>We've received your SurveyBucks reward claim for <strong>{rewardName}</strong>!</p>
                <p>Your reward is being processed and you'll receive confirmation within 24-48 hours.</p>
                <p>Thank you for being a valued SurveyBucks member!</p>",
                PlainTextBody = $"Hi {user.FirstName}, your SurveyBucks reward claim for {rewardName} has been received and is being processed."
            };

            var result = await _emailProvider.SendAsync(user.Email, user.FirstName, emailContent);
            LogEmailResult(result, user.Email, "Reward claim confirmation");
        }

        public async Task SendRewardRedemptionEmailAsync(string userId, string rewardName, string redemptionCode)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            var emailContent = new EmailContent
            {
                Subject = $"🎁 Your SurveyBucks Reward: {rewardName} is Ready!",
                HtmlBody = $@"
                <h2>Congratulations {user.FirstName}!</h2>
                <p>Your SurveyBucks reward <strong>{rewardName}</strong> has been processed successfully!</p>
                
                <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 20px 0; text-align: center;'>
                    <h3>Your Redemption Code:</h3>
                    <p style='font-size: 24px; font-weight: bold; color: #4CAF50; letter-spacing: 2px;'>{redemptionCode}</p>
                    <p><small>Please save this code for your records</small></p>
                </div>
                
                <p>Instructions for using your reward will be sent separately if applicable.</p>
                <p>Keep earning those SurveyBucks for more amazing rewards!</p>",
                PlainTextBody = $"Congratulations {user.FirstName}! Your SurveyBucks reward {rewardName} is ready. Redemption code: {redemptionCode}"
            };

            var result = await _emailProvider.SendAsync(user.Email, user.FirstName, emailContent);
            LogEmailResult(result, user.Email, "Reward redemption");
        }

        public async Task SendLevelUpCelebrationEmailAsync(string userId, int newLevel, string levelName)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            var emailContent = new EmailContent
            {
                Subject = $"🚀 SurveyBucks Level Up! Welcome to {levelName}",
                HtmlBody = $@"
                <h2>Congratulations {user.FirstName}!</h2>
                <p>You've reached <strong>SurveyBucks Level {newLevel}: {levelName}</strong>! 🎉</p>
                
                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px; margin: 20px 0; text-align: center;'>
                    <h3>🌟 New SurveyBucks Level Benefits:</h3>
                    <ul style='list-style: none; padding: 0;'>
                        <li>✨ Higher SurveyBucks multipliers</li>
                        <li>🎯 Access to exclusive premium surveys</li>
                        <li>⚡ Priority customer support</li>
                        <li>🎁 Special level rewards</li>
                    </ul>
                </div>
                
                <p><a href='{GetSurveyUrl()}' style='background: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>Start Earning More SurveyBucks!</a></p>",
                PlainTextBody = $"Congratulations {user.FirstName}! You've reached SurveyBucks Level {newLevel}: {levelName}. Enjoy your new benefits!"
            };

            var result = await _emailProvider.SendAsync(user.Email, user.FirstName, emailContent);
            LogEmailResult(result, user.Email, "Level up celebration");
        }

        // Authentication Email Methods
        public async Task SendEmailConfirmationAsync(string email, string firstName, string confirmationLink)
        {
            var emailContent = new EmailContent
            {
                Subject = "✅ Confirm Your SurveyBucks Email Address",
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='margin: 0; font-size: 28px;'>Welcome to SurveyBucks!</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Just one more step to start earning</p>
                    </div>
                    
                    <div style='padding: 30px; background: #f8f9fa; border-radius: 0 0 10px 10px;'>
                        <h2 style='color: #333; margin-top: 0;'>Hi {firstName}!</h2>
                        <p style='color: #555; line-height: 1.6;'>Thank you for joining SurveyBucks! To complete your registration and start earning with surveys, please confirm your email address.</p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{confirmationLink}' style='background: #4CAF50; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; font-size: 16px;'>
                                ✅ Confirm Email Address
                            </a>
                        </div>
                        
                        <div style='background: #e3f2fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0; color: #1976d2; font-size: 14px;'>
                                <strong>⏰ This link expires in 24 hours</strong><br>
                                If you didn't create a SurveyBucks account, you can safely ignore this email.
                            </p>
                        </div>
                        
                        <p style='color: #777; font-size: 12px; margin-top: 30px; text-align: center;'>
                            Having trouble? Copy and paste this link into your browser:<br>
                            <span style='word-break: break-all;'>{confirmationLink}</span>
                        </p>
                    </div>
                </div>",
                PlainTextBody = $@"Welcome to SurveyBucks!

                Hi {firstName},

                Thank you for joining SurveyBucks! Please confirm your email address by clicking the link below:

                {confirmationLink}

                This link expires in 24 hours. If you didn't create a SurveyBucks account, you can safely ignore this email.

                Thanks!
                The SurveyBucks Team"
            };

            var result = await _emailProvider.SendAsync(email, firstName, emailContent);
            LogEmailResult(result, email, "Email confirmation");
        }

        public async Task SendPasswordResetAsync(string email, string firstName, string resetLink)
        {
            var emailContent = new EmailContent
            {
                Subject = "🔐 Reset Your SurveyBucks Password",
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: #ff6b6b; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='margin: 0; font-size: 28px;'>🔐 SurveyBucks Password Reset</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Secure your account</p>
                    </div>
                    
                    <div style='padding: 30px; background: #f8f9fa; border-radius: 0 0 10px 10px;'>
                        <h2 style='color: #333; margin-top: 0;'>Hi {firstName},</h2>
                        <p style='color: #555; line-height: 1.6;'>We received a request to reset your SurveyBucks password. If you made this request, click the button below to create a new password.</p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' style='background: #ff6b6b; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; font-size: 16px;'>
                                🔐 Reset SurveyBucks Password
                            </a>
                        </div>
                        
                        <div style='background: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                            <p style='margin: 0; color: #856404; font-size: 14px;'>
                                <strong>⚠️ Security Notice:</strong><br>
                                • This link expires in 1 hour for your security<br>
                                • If you didn't request this reset, please ignore this email<br>
                                • Your SurveyBucks password remains unchanged until you create a new one
                            </p>
                        </div>
                        
                        <p style='color: #777; font-size: 12px; margin-top: 30px; text-align: center;'>
                            Having trouble? Copy and paste this link into your browser:<br>
                            <span style='word-break: break-all;'>{resetLink}</span>
                        </p>
                    </div>
                </div>",
                PlainTextBody = $@"SurveyBucks Password Reset Request

                Hi {firstName},

                We received a request to reset your SurveyBucks password. If you made this request, click the link below:

                {resetLink}

                This link expires in 1 hour for your security.

                If you didn't request this reset, please ignore this email. Your SurveyBucks password remains unchanged.

                Thanks!
                The SurveyBucks Team"
            };

            var result = await _emailProvider.SendAsync(email, firstName, emailContent);
            LogEmailResult(result, email, "Password reset");
        }

        public async Task SendPasswordResetConfirmationAsync(string email, string firstName)
        {
            var emailContent = new EmailContent
            {
                Subject = "✅ SurveyBucks Password Successfully Reset",
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: #4CAF50; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='margin: 0; font-size: 28px;'>✅ SurveyBucks Password Updated</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Your account is secure</p>
                    </div>
                    
                    <div style='padding: 30px; background: #f8f9fa; border-radius: 0 0 10px 10px;'>
                        <h2 style='color: #333; margin-top: 0;'>Hi {firstName},</h2>
                        <p style='color: #555; line-height: 1.6;'>Your SurveyBucks password has been successfully reset. You can now sign in to your account with your new password.</p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{GetLoginUrl()}' style='background: #4CAF50; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; font-size: 16px;'>
                                🚀 Sign In to SurveyBucks
                            </a>
                        </div>
                        
                        <div style='background: #e8f5e8; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0; color: #2e7d32; font-size: 14px;'>
                                <strong>🔒 Security Tips:</strong><br>
                                • Use a unique password you haven't used elsewhere<br>
                                • Consider using a password manager<br>
                                • Never share your SurveyBucks password with anyone
                            </p>
                        </div>
                        
                        <p style='color: #777; font-size: 14px; margin-top: 20px;'>
                            If you didn't make this change, please contact our support team immediately.
                        </p>
                    </div>
                </div>",
                PlainTextBody = $@"SurveyBucks Password Successfully Reset

                Hi {firstName},

                Your SurveyBucks password has been successfully reset. You can now sign in to your account with your new password.

                If you didn't make this change, please contact our support team immediately.

                Thanks!
                The SurveyBucks Team"
            };

            var result = await _emailProvider.SendAsync(email, firstName, emailContent);
            LogEmailResult(result, email, "Password reset confirmation");
        }

        public async Task SendWelcomeEmailAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            var emailContent = new EmailContent
            {
                Subject = "🎉 Welcome to SurveyBucks - Let's Start Earning!",
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='margin: 0; font-size: 32px;'>🎉 Welcome to SurveyBucks!</h1>
                        <p style='margin: 10px 0 0 0; font-size: 18px; opacity: 0.9;'>You're all set to start earning</p>
                    </div>
                    
                    <div style='padding: 30px; background: #f8f9fa; border-radius: 0 0 10px 10px;'>
                        <h2 style='color: #333; margin-top: 0;'>Hi {user.FirstName}!</h2>
                        <p style='color: #555; line-height: 1.6; font-size: 16px;'>Welcome to the SurveyBucks community! Your email has been confirmed and you're ready to start earning rewards through surveys.</p>
                        
                        <div style='background: white; padding: 20px; border-radius: 10px; margin: 25px 0; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                            <h3 style='color: #4CAF50; margin-top: 0;'>🚀 Here's how to get started with SurveyBucks:</h3>
                            <div style='margin: 15px 0;'>
                                <div style='display: flex; align-items: center; margin: 10px 0; padding: 10px; background: #f8f9fa; border-radius: 5px;'>
                                    <span style='background: #4CAF50; color: white; border-radius: 50%; width: 25px; height: 25px; display: inline-flex; align-items: center; justify-content: center; margin-right: 15px; font-weight: bold; font-size: 12px;'>1</span>
                                    <span style='color: #333;'>Complete your SurveyBucks profile for better survey matches</span>
                                </div>
                                <div style='display: flex; align-items: center; margin: 10px 0; padding: 10px; background: #f8f9fa; border-radius: 5px;'>
                                    <span style='background: #2196F3; color: white; border-radius: 50%; width: 25px; height: 25px; display: inline-flex; align-items: center; justify-content: center; margin-right: 15px; font-weight: bold; font-size: 12px;'>2</span>
                                    <span style='color: #333;'>Browse and participate in available surveys</span>
                                </div>
                                <div style='display: flex; align-items: center; margin: 10px 0; padding: 10px; background: #f8f9fa; border-radius: 5px;'>
                                    <span style='background: #FF9800; color: white; border-radius: 50%; width: 25px; height: 25px; display: inline-flex; align-items: center; justify-content: center; margin-right: 15px; font-weight: bold; font-size: 12px;'>3</span>
                                    <span style='color: #333;'>Earn SurveyBucks and redeem amazing rewards</span>
                                </div>
                            </div>
                        </div>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{GetProfileUrl()}' style='background: #4CAF50; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; font-size: 16px; margin: 5px;'>
                                👤 Complete Profile
                            </a>
                            <a href='{GetSurveyUrl()}' style='background: #2196F3; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; font-size: 16px; margin: 5px;'>
                                📊 Browse Surveys
                            </a>
                        </div>
                        
                        <div style='background: #e3f2fd; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0; color: #1976d2; font-size: 14px; text-align: center;'>
                                <strong>💡 Pro Tip:</strong> SurveyBucks members with complete profiles earn 3x more points!
                            </p>
                        </div>
                    </div>
                </div>",
                PlainTextBody = $@"Welcome to SurveyBucks!

                Hi {user.FirstName},

                Welcome to the SurveyBucks community! Your email has been confirmed and you're ready to start earning rewards.

                Here's how to get started with SurveyBucks:
                1. Complete your SurveyBucks profile for better survey matches
                2. Browse and participate in available surveys  
                3. Earn SurveyBucks and redeem amazing rewards

                Pro Tip: SurveyBucks members with complete profiles earn 3x more points!

                Complete your profile: {GetProfileUrl()}
                Browse surveys: {GetSurveyUrl()}

                Thanks!
                The SurveyBucks Team"
            };

            var result = await _emailProvider.SendAsync(user.Email, user.FirstName, emailContent);
            LogEmailResult(result, user.Email, "Welcome email");
        }

        public async Task SendAccountLockedEmailAsync(string email, string firstName, int lockoutMinutes)
        {
            var emailContent = new EmailContent
            {
                Subject = "🔒 SurveyBucks Account Temporarily Locked",
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: #ff9800; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='margin: 0; font-size: 28px;'>🔒 SurveyBucks Account Locked</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Security protection activated</p>
                    </div>
                    
                    <div style='padding: 30px; background: #f8f9fa; border-radius: 0 0 10px 10px;'>
                        <h2 style='color: #333; margin-top: 0;'>Hi {firstName},</h2>
                        <p style='color: #555; line-height: 1.6;'>Your SurveyBucks account has been temporarily locked due to multiple failed login attempts. This is a security measure to protect your account.</p>
                        
                        <div style='background: #fff3cd; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                            <h3 style='color: #856404; margin-top: 0;'>⏰ Lockout Details:</h3>
                            <p style='margin: 5px 0; color: #856404;'>
                                <strong>Duration:</strong> {lockoutMinutes} minutes<br>
                                <strong>Reason:</strong> Multiple failed login attempts<br>
                                <strong>Action Required:</strong> Wait for the lockout period to end
                            </p>
                        </div>
                        
                        <div style='background: #e8f5e8; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0; color: #2e7d32; font-size: 14px;'>
                                <strong>🛡️ Security Tips:</strong><br>
                                • If this wasn't you, consider changing your SurveyBucks password<br>
                                • Use a strong, unique password<br>
                                • Enable two-factor authentication if available
                            </p>
                        </div>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{GetPasswordResetUrl()}' style='background: #ff9800; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; font-size: 16px;'>
                                🔐 Reset SurveyBucks Password
                            </a>
                        </div>
                    </div>
                </div>",
                PlainTextBody = $@"SurveyBucks Account Temporarily Locked

                Hi {firstName},

                Your SurveyBucks account has been temporarily locked due to multiple failed login attempts.

                Lockout Duration: {lockoutMinutes} minutes
                Reason: Multiple failed login attempts

                If this wasn't you, consider changing your SurveyBucks password once the lockout period ends.

                Reset Password: {GetPasswordResetUrl()}

                Thanks!
                The SurveyBucks Team"
            };

            var result = await _emailProvider.SendAsync(email, firstName, emailContent);
            LogEmailResult(result, email, "Account locked notification");
        }

        public async Task SendPasswordChangedNotificationAsync(string email, string firstName)
        {
            var emailContent = new EmailContent
            {
                Subject = "🔐 SurveyBucks Password Changed Successfully",
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: #4CAF50; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='margin: 0; font-size: 28px;'>🔐 SurveyBucks Password Updated</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Security notification</p>
                    </div>
                    
                    <div style='padding: 30px; background: #f8f9fa; border-radius: 0 0 10px 10px;'>
                        <h2 style='color: #333; margin-top: 0;'>Hi {firstName},</h2>
                        <p style='color: #555; line-height: 1.6;'>This is a security notification to confirm that your SurveyBucks password was successfully changed on {DateTime.UtcNow:dddd, MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC.</p>
                        
                        <div style='background: #e8f5e8; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                            <h3 style='color: #2e7d32; margin-top: 0;'>✅ What This Means:</h3>
                            <p style='margin: 5px 0; color: #2e7d32;'>
                                • Your SurveyBucks account password has been updated<br>
                                • You'll need to use your new password for future logins<br>
                                • Your account remains secure
                            </p>
                        </div>
                        
                        <div style='background: #ffebee; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #f44336;'>
                            <p style='margin: 0; color: #c62828; font-size: 14px;'>
                                <strong>⚠️ Didn't make this change?</strong><br>
                                If you didn't change your SurveyBucks password, your account may be compromised. Please contact our support team immediately.
                            </p>
                        </div>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{GetSupportUrl()}' style='background: #f44336; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; font-size: 16px;'>
                                🆘 Contact Support
                            </a>
                        </div>
                    </div>
                </div>",
                PlainTextBody = $@"SurveyBucks Password Changed Successfully

                Hi {firstName},

                This is a security notification to confirm that your SurveyBucks password was successfully changed on {DateTime.UtcNow:dddd, MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC.

                If you didn't make this change, please contact our support team immediately as your account may be compromised.

                Contact Support: {GetSupportUrl()}

                Thanks!
                The SurveyBucks Team"
            };

            var result = await _emailProvider.SendAsync(email, firstName, emailContent);
            LogEmailResult(result, email, "Password change notification");
        }

        private void LogEmailResult(EmailResult result, string email, string emailType)
        {
            if (result.IsSuccess)
            {
                _logger.LogInformation("{EmailType} email sent successfully to {Email}. MessageId: {MessageId}",
                    emailType, email, result.MessageId);
            }
            else
            {
                _logger.LogError("{EmailType} email failed to send to {Email}: {Error}",
                    emailType, email, result.ErrorMessage);
            }
        }

        private string GetProfileUrl() => "https://surveybucks.co.za/profile";
        private string GetSurveyUrl() => "https://surveybucks.co.za/surveys";
        private string GetRewardsUrl() => "https://surveybucks.co.za/rewards";
        private string GetLoginUrl() => "https://surveybucks.co.za/login";
        private string GetPasswordResetUrl() => "https://surveybucks.co.za/forgot-password";
        private string GetSupportUrl() => "https://surveybucks.co.za/support";
    }
}