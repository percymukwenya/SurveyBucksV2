import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

export interface TimelineStep {
  id: string;
  title: string;
  subtitle?: string;
  icon: string;
  status: 'completed' | 'active' | 'pending';
  timestamp?: Date;
  estimatedTime?: string;
}

@Component({
  selector: 'app-verification-timeline',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: './verification-timeline.component.html',
  styleUrl: './verification-timeline.component.scss'
})
export class VerificationTimelineComponent {
  @Input() documentName: string = '';
  @Input() currentStatus: 'uploaded' | 'reviewing' | 'approved' | 'rejected' = 'uploaded';
  @Input() uploadDate?: Date;
  @Input() expectedCompletionDate?: Date;
  @Input() rejectionReason?: string;

  get timelineSteps(): TimelineStep[] {
    const baseSteps: TimelineStep[] = [
      {
        id: 'upload',
        title: 'Document Uploaded',
        subtitle: this.uploadDate ? this.formatDate(this.uploadDate) : undefined,
        icon: 'cloud_upload',
        status: 'completed',
        timestamp: this.uploadDate
      },
      {
        id: 'review',
        title: 'Under Review',
        subtitle: this.getReviewSubtitle(),
        icon: 'search',
        status: this.getReviewStatus(),
        estimatedTime: this.expectedCompletionDate ? this.getTimeEstimate() : '1-2 business days'
      },
      {
        id: 'complete',
        title: this.currentStatus === 'rejected' ? 'Verification Failed' : 'Verification Complete',
        subtitle: this.currentStatus === 'rejected' ? 'Action Required' : 'Document Approved',
        icon: this.currentStatus === 'rejected' ? 'error' : 'check_circle',
        status: this.getFinalStatus()
      }
    ];

    return baseSteps;
  }

  private getReviewStatus(): 'completed' | 'active' | 'pending' {
    switch (this.currentStatus) {
      case 'uploaded':
        return 'pending';
      case 'reviewing':
        return 'active';
      case 'approved':
      case 'rejected':
        return 'completed';
      default:
        return 'pending';
    }
  }

  private getFinalStatus(): 'completed' | 'active' | 'pending' {
    switch (this.currentStatus) {
      case 'approved':
      case 'rejected':
        return 'completed';
      default:
        return 'pending';
    }
  }

  private getReviewSubtitle(): string {
    switch (this.currentStatus) {
      case 'reviewing':
        return 'Review in progress...';
      case 'approved':
      case 'rejected':
        return 'Review completed';
      default:
        return 'Queued for review';
    }
  }

  private getTimeEstimate(): string {
    if (!this.expectedCompletionDate) return '';
    
    const now = new Date();
    const diff = this.expectedCompletionDate.getTime() - now.getTime();
    const hours = Math.ceil(diff / (1000 * 60 * 60));
    
    if (hours <= 0) return 'Expected soon';
    if (hours <= 24) return `Expected within ${hours} hours`;
    
    const days = Math.ceil(hours / 24);
    return `Expected in ${days} day${days > 1 ? 's' : ''}`;
  }

  private formatDate(date: Date): string {
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getStepClass(status: string): string {
    return `timeline-step timeline-step-${status}`;
  }

  getIconClass(status: string): string {
    return `timeline-icon timeline-icon-${status}`;
  }

  isLastStep(index: number): boolean {
    return index === this.timelineSteps.length - 1;
  }

  getCurrentStatusIcon(): string {
    switch (this.currentStatus) {
      case 'uploaded':
        return 'cloud_upload';
      case 'reviewing':
        return 'search';
      case 'approved':
        return 'check_circle';
      case 'rejected':
        return 'error';
      default:
        return 'help';
    }
  }

  getCurrentStatusText(): string {
    switch (this.currentStatus) {
      case 'uploaded':
        return 'Uploaded';
      case 'reviewing':
        return 'Under Review';
      case 'approved':
        return 'Approved';
      case 'rejected':
        return 'Rejected';
      default:
        return 'Unknown';
    }
  }

  trackByStepId(index: number, step: TimelineStep): string {
    return step.id;
  }
}