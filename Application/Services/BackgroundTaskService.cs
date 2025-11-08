using Domain.Interfaces.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IBackgroundTaskService
    {
        Task ExecuteEnrollmentTasksAsync(string userId, int surveyId, CancellationToken cancellationToken = default);
        Task ExecuteCompletionTasksAsync(string userId, int surveyId, CancellationToken cancellationToken = default);
        Task ExecuteProgressTasksAsync(string userId, int progressPercentage, CancellationToken cancellationToken = default);
        Task ExecuteQuestionResponseTasksAsync(string userId, int questionId, CancellationToken cancellationToken = default);
    }

    public class BackgroundTaskService : IBackgroundTaskService
    {
        private readonly IGamificationService _gamificationService;
        private readonly INotificationService _notificationService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<BackgroundTaskService> _logger;

        // Task execution metrics
        private readonly Dictionary<string, TaskMetrics> _taskMetrics = new();
        private readonly object _metricsLock = new object();

        public BackgroundTaskService(
            IGamificationService gamificationService,
            INotificationService notificationService,
            IAnalyticsService analyticsService,
            ILogger<BackgroundTaskService> logger)
        {
            _gamificationService = gamificationService;
            _notificationService = notificationService;
            _analyticsService = analyticsService;
            _logger = logger;
        }

        public async Task ExecuteEnrollmentTasksAsync(string userId, int surveyId, CancellationToken cancellationToken = default)
        {
            var tasks = new List<(string Name, Func<Task> Task)>
            {
                ("Gamification", () => _gamificationService.ProcessEnrollmentAsync(userId, surveyId)),
                ("Notification", () => _notificationService.SendEnrollmentNotificationAsync(userId, surveyId)),
                ("Analytics", () => _analyticsService.TrackSurveyViewAsync(surveyId))
            };

            await ExecuteTasksWithRetryAsync("Enrollment", $"User:{userId},Survey:{surveyId}", tasks, cancellationToken);
        }

        public async Task ExecuteCompletionTasksAsync(string userId, int surveyId, CancellationToken cancellationToken = default)
        {
            var tasks = new List<(string Name, Func<Task> Task)>
            {
                ("Gamification", () => _gamificationService.ProcessSurveyCompletionAsync(userId, surveyId)),
                ("Notification", () => _notificationService.SendCompletionNotificationAsync(userId, surveyId)),
                ("Analytics", () => _analyticsService.TrackSurveyCompletionAsync(surveyId, userId))
            };

            await ExecuteTasksWithRetryAsync("Completion", $"User:{userId},Survey:{surveyId}", tasks, cancellationToken);
        }

        public async Task ExecuteProgressTasksAsync(string userId, int progressPercentage, CancellationToken cancellationToken = default)
        {
            var tasks = new List<(string Name, Func<Task> Task)>
            {
                ("Gamification", () => _gamificationService.ProcessProgressUpdateAsync(userId, progressPercentage))
            };

            await ExecuteTasksWithRetryAsync("Progress", $"User:{userId},Progress:{progressPercentage}%", tasks, cancellationToken);
        }

        public async Task ExecuteQuestionResponseTasksAsync(string userId, int questionId, CancellationToken cancellationToken = default)
        {
            var tasks = new List<(string Name, Func<Task> Task)>
            {
                ("Gamification", () => _gamificationService.ProcessQuestionAnsweredAsync(userId, questionId)),
                ("Analytics", () => _analyticsService.TrackQuestionResponseAsync(questionId, userId))
            };

            await ExecuteTasksWithRetryAsync("QuestionResponse", $"User:{userId},Question:{questionId}", tasks, cancellationToken);
        }

        private async Task ExecuteTasksWithRetryAsync(
            string operationType, 
            string context, 
            List<(string Name, Func<Task> Task)> tasks,
            CancellationToken cancellationToken)
        {
            var operationId = $"{operationType}_{context}_{DateTime.UtcNow.Ticks}";
            
            _logger.LogInformation("Starting background tasks for {OperationType}: {Context} (ID: {OperationId})", 
                operationType, context, operationId);

            var startTime = DateTime.UtcNow;
            var successCount = 0;
            var failureCount = 0;
            var exceptions = new List<Exception>();

            // Execute tasks in parallel with individual error handling
            var taskExecutions = tasks.Select(async task =>
            {
                var taskName = $"{operationType}.{task.Name}";
                var attempt = 1;
                const int maxAttempts = 3;

                while (attempt <= maxAttempts && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var taskStartTime = DateTime.UtcNow;
                        
                        await task.Task();
                        
                        var taskDuration = DateTime.UtcNow - taskStartTime;
                        
                        RecordTaskSuccess(taskName, taskDuration);
                        
                        _logger.LogDebug("Background task {TaskName} completed successfully in {Duration}ms (Attempt {Attempt}/{MaxAttempts})", 
                            taskName, taskDuration.TotalMilliseconds, attempt, maxAttempts);
                        
                        Interlocked.Increment(ref successCount);
                        return;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Background task {TaskName} was cancelled", taskName);
                        return;
                    }
                    catch (Exception ex)
                    {
                        var taskDuration = DateTime.UtcNow - DateTime.UtcNow; // Reset for failure timing
                        
                        RecordTaskFailure(taskName, ex);
                        
                        if (attempt == maxAttempts)
                        {
                            _logger.LogError(ex, "Background task {TaskName} failed after {MaxAttempts} attempts: {Context}", 
                                taskName, maxAttempts, context);
                            
                            lock (exceptions)
                            {
                                exceptions.Add(new Exception($"Task {taskName} failed: {ex.Message}", ex));
                            }
                            
                            Interlocked.Increment(ref failureCount);
                            return;
                        }
                        else
                        {
                            var delayMs = CalculateRetryDelay(attempt);
                            _logger.LogWarning(ex, "Background task {TaskName} failed (attempt {Attempt}/{MaxAttempts}), retrying in {DelayMs}ms: {Context}", 
                                taskName, attempt, maxAttempts, delayMs, context);
                            
                            await Task.Delay(delayMs, cancellationToken);
                        }
                    }
                    
                    attempt++;
                }
            });

            await Task.WhenAll(taskExecutions);

            var totalDuration = DateTime.UtcNow - startTime;

            // Log overall operation results
            if (failureCount == 0)
            {
                _logger.LogInformation("All background tasks completed successfully for {OperationType}: {Context} (Duration: {Duration}ms)", 
                    operationType, context, totalDuration.TotalMilliseconds);
            }
            else if (successCount > 0)
            {
                _logger.LogWarning("Background tasks partially completed for {OperationType}: {Context} - Success: {SuccessCount}, Failed: {FailureCount} (Duration: {Duration}ms)", 
                    operationType, context, successCount, failureCount, totalDuration.TotalMilliseconds);
            }
            else
            {
                _logger.LogError("All background tasks failed for {OperationType}: {Context} (Duration: {Duration}ms). Errors: {Errors}", 
                    operationType, context, totalDuration.TotalMilliseconds, string.Join("; ", exceptions.Select(e => e.Message)));
            }

            // Record operation metrics
            RecordOperationMetrics(operationType, successCount, failureCount, totalDuration);
        }

        private int CalculateRetryDelay(int attempt)
        {
            // Exponential backoff with jitter
            var baseDelay = Math.Pow(2, attempt - 1) * 1000; // 1s, 2s, 4s
            var jitter = new Random().Next(0, 500); // Add up to 500ms jitter
            return (int)(baseDelay + jitter);
        }

        private void RecordTaskSuccess(string taskName, TimeSpan duration)
        {
            lock (_metricsLock)
            {
                if (!_taskMetrics.ContainsKey(taskName))
                {
                    _taskMetrics[taskName] = new TaskMetrics();
                }

                var metrics = _taskMetrics[taskName];
                metrics.SuccessCount++;
                metrics.TotalDuration += duration;
                metrics.LastSuccess = DateTime.UtcNow;
                
                if (metrics.MinDuration == TimeSpan.Zero || duration < metrics.MinDuration)
                    metrics.MinDuration = duration;
                
                if (duration > metrics.MaxDuration)
                    metrics.MaxDuration = duration;
            }
        }

        private void RecordTaskFailure(string taskName, Exception ex)
        {
            lock (_metricsLock)
            {
                if (!_taskMetrics.ContainsKey(taskName))
                {
                    _taskMetrics[taskName] = new TaskMetrics();
                }

                var metrics = _taskMetrics[taskName];
                metrics.FailureCount++;
                metrics.LastFailure = DateTime.UtcNow;
                metrics.LastError = ex.Message;
                
                // Track error types
                var errorType = ex.GetType().Name;
                if (!metrics.ErrorCounts.ContainsKey(errorType))
                {
                    metrics.ErrorCounts[errorType] = 0;
                }
                metrics.ErrorCounts[errorType]++;
            }
        }

        private void RecordOperationMetrics(string operationType, int successCount, int failureCount, TimeSpan duration)
        {
            // This could integrate with monitoring systems (Application Insights, Prometheus, etc.)
            _logger.LogInformation("Operation Metrics - Type: {OperationType}, Success: {SuccessCount}, Failures: {FailureCount}, Duration: {Duration}ms", 
                operationType, successCount, failureCount, duration.TotalMilliseconds);
        }

        public Dictionary<string, TaskMetrics> GetMetrics()
        {
            lock (_metricsLock)
            {
                return new Dictionary<string, TaskMetrics>(_taskMetrics);
            }
        }

        public void ResetMetrics()
        {
            lock (_metricsLock)
            {
                _taskMetrics.Clear();
            }
        }
    }

    public class TaskMetrics
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public DateTime? LastSuccess { get; set; }
        public DateTime? LastFailure { get; set; }
        public string LastError { get; set; } = string.Empty;
        public Dictionary<string, int> ErrorCounts { get; set; } = new Dictionary<string, int>();
        
        public double SuccessRate => SuccessCount + FailureCount == 0 ? 0 : (double)SuccessCount / (SuccessCount + FailureCount) * 100;
        public TimeSpan AverageDuration => SuccessCount == 0 ? TimeSpan.Zero : new TimeSpan(TotalDuration.Ticks / SuccessCount);
    }
}