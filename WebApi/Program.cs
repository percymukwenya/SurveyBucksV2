using Application.Services;
using Application.Services.Auth;
using Domain.Interfaces.Repository;
using Domain.Interfaces.Repository.Admin;
using Domain.Interfaces.Service;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Admin;
using Infrastructure.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Application.Middleware;
using Application.Models;
using Application.Services.Email;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmarterConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Email confirmation settings
    options.SignIn.RequireConfirmedEmail = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Authentication with JWT as primary + External providers
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"])),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };

    // Handle JWT in query string for real-time connections (SignalR, etc.)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            // If the request is for SignalR hub and token is in query string
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
})
// Add external authentication providers for token exchange
.AddGoogle("Google", options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/api/auth/signin-google"; // Custom callback path

    // Request additional scopes if needed
    options.Scope.Add("email");
    options.Scope.Add("profile");

    // Save tokens for potential API calls
    options.SaveTokens = true;
})
.AddFacebook("Facebook", options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    options.CallbackPath = "/api/auth/signin-facebook";

    options.Scope.Add("email");
    options.SaveTokens = true;
})
.AddMicrosoftAccount("Microsoft", options =>
{
    options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    options.CallbackPath = "/api/auth/signin-microsoft";

    options.Scope.Add("https://graph.microsoft.com/user.read");
    options.SaveTokens = true;
});

builder.Services.AddMemoryCache();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SurveyBucks API", Version = "v1" });

    // Add Bearer token authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "outh2",
                Name="Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", corsBuilder =>
    {
        //corsBuilder.WithOrigins(builder.Configuration["AllowedOrigins"].Split(','))
        corsBuilder.SetIsOriginAllowed(origin => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

builder.Services.AddLogging();

builder.Services.AddSingleton<IDatabaseConnectionFactory, SqlConnectionFactory>();

builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IBankingService, BankingService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IRewardsService, RewardsService>();
builder.Services.AddScoped<IBackgroundTaskService, BackgroundTaskService>();
builder.Services.AddScoped<ISurveyBranchingService, SurveyBranchingService>();
builder.Services.AddScoped<ISurveyManagementService, SurveyManagementService>();
builder.Services.AddScoped<ISurveyAccessService, SurveyAccessService>();
builder.Services.AddScoped<IUserService, UserService>();


// Register base service first
builder.Services.AddScoped<SurveyParticipationService>();
// Then register the transactional decorator
builder.Services.AddScoped<ISurveyParticipationService>(provider =>
{
    var baseService = provider.GetRequiredService<SurveyParticipationService>();
    var connectionFactory = provider.GetRequiredService<IDatabaseConnectionFactory>();
    var logger = provider.GetRequiredService<ILogger<TransactionalSurveyParticipationService>>();
    return new TransactionalSurveyParticipationService(baseService, connectionFactory, logger);
});
builder.Services.AddScoped<ISurveySectionService, SurveySectionService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IUserProfileCompletionService, UserProfileCompletionService>();

builder.Services.AddScoped<IProfileCompletionCalculator, ProfileCompletionCalculator>();
builder.Services.AddScoped<ISurveyMatchingService, SurveyMatchingService>();

builder.Services.AddScoped<ISurveyResponseService, SurveyResponseService>();

builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IBankingDetailRepository, BankingDetailRepository>();
builder.Services.AddScoped<IDemographicsRepository, DemographicsRepository>();
builder.Services.AddScoped<IDemographicTargetingRepository, DemographicTargetingRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IGamificationRepository, GamificationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IQuestionLogicRepository, QuestionLogicRepository>();
builder.Services.AddScoped<IRewardsRepository, RewardsRepository>();
builder.Services.AddScoped<IRewardsManagementRepository, RewardsManagementRepository>();
builder.Services.AddScoped<ISurveyManagementRepository, SurveyManagementRepository>();
builder.Services.AddScoped<ISurveyParticipationRepository, SurveyParticipationRepository>();
builder.Services.AddScoped<ISurveySectionRepository, SurveySectionRepository>();
builder.Services.AddScoped<IUserManagementRepository, UserManagementRepository>();
builder.Services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
builder.Services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
builder.Services.AddScoped<IUserChallengeRepository, UserChallengeRepository>();
builder.Services.AddScoped<IUserEngagementRepository, UserEngagementRepository>();
builder.Services.AddScoped<IUserPointsRepository, UserPointsRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
builder.Services.AddScoped<IUserTargetingRepository, UserTargetingRepository>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IEmailProvider, MailKitEmailProvider>();

builder.Services.Configure<SurveyMatchingConfiguration>(builder.Configuration.GetSection("SurveyMatching"));
builder.Services.Configure<ProfileCompletionConfiguration>(builder.Configuration.GetSection("ProfileCompletion"));


// Add Token service
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddFileStorageService(builder.Configuration);

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 10_485_760; // 10MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

var app = builder.Build();

if (app.Configuration["FileStorage:Type"] == "Local")
{
    var env = app.Environment;

    var uploadPath = Path.Combine(env.ContentRootPath, app.Configuration["FileStorage:LocalPath"]);
    if (!Directory.Exists(uploadPath))
    {
        Directory.CreateDirectory(uploadPath);
    }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadPath),
        RequestPath = "/uploads",
        OnPrepareResponse = ctx =>
        {
            // Add security headers
            ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            ctx.Context.Response.Headers.Append("X-Frame-Options", "DENY");
        }
    });
}

// Configure the HTTP request pipeline.
app.UseMiddleware<FileUploadSecurityMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowWebApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
