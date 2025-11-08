using System.Collections.Generic;

namespace Application.Models
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public static ServiceResult SuccessResult(string message) =>
            new ServiceResult { Success = true, Message = message };

        public static ServiceResult FailureResult(string message, List<string> errors = null) =>
            new ServiceResult { Success = false, Message = message, Errors = errors ?? new List<string>() };
    }
}
