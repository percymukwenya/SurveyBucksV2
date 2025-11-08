import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DocumentVerificationService } from '../../../../core/services/document-verification.service';

interface DocumentStats {
  totalDocuments: number;
  pendingDocuments: number;
  approvedDocuments: number;
  rejectedDocuments: number;
  documentsThisWeek: number;
  documentsThisMonth: number;
  verificationCompletionRate: number;
  averageVerificationTime: string;
  totalUsersWithDocuments: number;
  fullyVerifiedUsers: number;
  documentTypeBreakdown: DocumentTypeStats[];
  recentTrends: VerificationTrend[];
}

interface DocumentTypeStats {
  documentTypeId: number;
  documentTypeName: string;
  category: string;
  totalCount: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  approvalRate: number;
}

interface VerificationTrend {
  date: string;
  documentsSubmitted: number;
  documentsVerified: number;
  documentsApproved: number;
  documentsRejected: number;
}

@Component({
  selector: 'app-verification-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './verification-dashboard.component.html',
  styleUrl: './verification-dashboard.component.scss'
})
export class VerificationDashboardComponent implements OnInit {
  stats: DocumentStats | null = null;
  loading = false;
  error: string | null = null;

  constructor(private documentVerificationService: DocumentVerificationService) { }

  ngOnInit(): void {
    this.loadStats();
  }

  async loadStats(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      this.stats = await this.documentVerificationService.getDocumentStats();
    } catch (error: any) {
      this.error = error.message || 'Failed to load statistics';
      console.error('Error loading document stats:', error);
    } finally {
      this.loading = false;
    }
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'pending':
        return '#ffc107';
      case 'approved':
        return '#28a745';
      case 'rejected':
        return '#dc3545';
      default:
        return '#6c757d';
    }
  }

  formatPercentage(value: number): string {
    return `${value.toFixed(1)}%`;
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric' 
    });
  }
}