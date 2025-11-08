using System;

namespace Domain.Models.Admin
{
    public class AdminDocumentStatsDto
    {
        public int TotalDocuments { get; set; }
        public int PendingDocuments { get; set; }
        public int ApprovedDocuments { get; set; }
        public int RejectedDocuments { get; set; }
        public int DocumentsThisWeek { get; set; }
        public int DocumentsThisMonth { get; set; }
        public int DocumentsToday { get; set; }

        public double VerificationCompletionRate { get; set; }
        public TimeSpan AverageVerificationTime { get; set; }
        public int TotalUsersWithDocuments { get; set; }
        public int FullyVerifiedUsers { get; set; }
        public DocumentTypeStatsDto[] DocumentTypeBreakdown { get; set; } = Array.Empty<DocumentTypeStatsDto>();
        public VerificationTrendDto[] RecentTrends { get; set; } = Array.Empty<VerificationTrendDto>();
    }

    public class DocumentTypeStatsDto
    {
        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public double ApprovalRate { get; set; }
    }

    public class VerificationTrendDto
    {
        public DateTime Date { get; set; }
        public int DocumentsSubmitted { get; set; }
        public int DocumentsVerified { get; set; }
        public int DocumentsApproved { get; set; }
        public int DocumentsRejected { get; set; }
    }
}