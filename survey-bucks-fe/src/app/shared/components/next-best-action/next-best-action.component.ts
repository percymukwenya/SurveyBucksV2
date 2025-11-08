// src/app/shared/components/next-best-action/next-best-action.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

export interface NextBestAction {
  title: string;
  description: string;
  icon: string;
  priority: 'critical' | 'high' | 'medium' | 'low';
  action: string;
  actionLabel: string;
  estimatedMinutes?: number;
  pointsValue?: number;
  progressPercentage?: number;
}

@Component({
  selector: 'app-next-best-action',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule
  ],
  template: `
    <mat-card class="next-best-action-card" [class]="getPriorityClass()">
      <mat-card-header>
        <div class="action-header">
          <div class="header-icon" [class]="getPriorityClass()">
            <mat-icon>{{ action.icon }}</mat-icon>
          </div>
          <div class="header-content">
            <mat-card-title>
              <span class="priority-badge" [class]="getPriorityClass()">
                {{ getPriorityLabel() }}
              </span>
              {{ action.title }}
            </mat-card-title>
            <mat-card-subtitle>{{ action.description }}</mat-card-subtitle>
          </div>
        </div>
      </mat-card-header>

      <mat-card-content>
        <!-- Progress indicator if applicable -->
        <div class="progress-section" *ngIf="action.progressPercentage !== undefined">
          <div class="progress-label">
            <span>{{ action.progressPercentage }}% Complete</span>
          </div>
          <mat-progress-bar
            mode="determinate"
            [value]="action.progressPercentage"
            [class]="getProgressClass()">
          </mat-progress-bar>
        </div>

        <!-- Action metadata -->
        <div class="action-meta">
          <div class="meta-item" *ngIf="action.estimatedMinutes">
            <mat-icon>schedule</mat-icon>
            <span>{{ action.estimatedMinutes }} min{{ action.estimatedMinutes > 1 ? 's' : '' }}</span>
          </div>
          <div class="meta-item" *ngIf="action.pointsValue">
            <mat-icon>star</mat-icon>
            <span>+{{ action.pointsValue }} points</span>
          </div>
        </div>
      </mat-card-content>

      <mat-card-actions>
        <button
          mat-raised-button
          [color]="getButtonColor()"
          (click)="onActionClick()"
          class="action-button">
          <mat-icon>{{ getActionIcon() }}</mat-icon>
          {{ action.actionLabel || 'Start Now' }}
        </button>
        <button
          mat-button
          *ngIf="showDismiss"
          (click)="onDismiss()">
          Later
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: [`
    .next-best-action-card {
      margin: 1rem 0;
      border-left: 4px solid var(--primary-color, #1976d2);
      transition: all 0.3s ease;
      background: linear-gradient(135deg, rgba(25, 118, 210, 0.05) 0%, rgba(255, 255, 255, 0) 100%);
    }

    .next-best-action-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    }

    .next-best-action-card.priority-critical {
      border-left-color: #f44336;
      background: linear-gradient(135deg, rgba(244, 67, 54, 0.08) 0%, rgba(255, 255, 255, 0) 100%);
    }

    .next-best-action-card.priority-high {
      border-left-color: #ff9800;
      background: linear-gradient(135deg, rgba(255, 152, 0, 0.06) 0%, rgba(255, 255, 255, 0) 100%);
    }

    .next-best-action-card.priority-medium {
      border-left-color: #2196f3;
      background: linear-gradient(135deg, rgba(33, 150, 243, 0.05) 0%, rgba(255, 255, 255, 0) 100%);
    }

    .next-best-action-card.priority-low {
      border-left-color: #4caf50;
      background: linear-gradient(135deg, rgba(76, 175, 80, 0.05) 0%, rgba(255, 255, 255, 0) 100%);
    }

    .action-header {
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      width: 100%;
    }

    .header-icon {
      width: 56px;
      height: 56px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: rgba(25, 118, 210, 0.1);
      flex-shrink: 0;
    }

    .header-icon.priority-critical {
      background-color: rgba(244, 67, 54, 0.1);
    }

    .header-icon.priority-high {
      background-color: rgba(255, 152, 0, 0.1);
    }

    .header-icon.priority-medium {
      background-color: rgba(33, 150, 243, 0.1);
    }

    .header-icon.priority-low {
      background-color: rgba(76, 175, 80, 0.1);
    }

    .header-icon mat-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      color: #1976d2;
    }

    .header-icon.priority-critical mat-icon {
      color: #f44336;
    }

    .header-icon.priority-high mat-icon {
      color: #ff9800;
    }

    .header-icon.priority-medium mat-icon {
      color: #2196f3;
    }

    .header-icon.priority-low mat-icon {
      color: #4caf50;
    }

    .header-content {
      flex: 1;
    }

    mat-card-title {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-wrap: wrap;
      font-size: 20px;
      font-weight: 600;
      margin-bottom: 0.5rem;
    }

    .priority-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .priority-badge.priority-critical {
      background-color: #f44336;
      color: white;
    }

    .priority-badge.priority-high {
      background-color: #ff9800;
      color: white;
    }

    .priority-badge.priority-medium {
      background-color: #2196f3;
      color: white;
    }

    .priority-badge.priority-low {
      background-color: #4caf50;
      color: white;
    }

    mat-card-subtitle {
      font-size: 14px;
      color: var(--text-color-secondary, #666);
      margin: 0;
    }

    mat-card-content {
      padding-top: 1rem;
    }

    .progress-section {
      margin-bottom: 1rem;
    }

    .progress-label {
      display: flex;
      justify-content: space-between;
      margin-bottom: 0.5rem;
      font-size: 14px;
      font-weight: 500;
      color: var(--text-color, #333);
    }

    .action-meta {
      display: flex;
      gap: 1.5rem;
      margin-top: 1rem;
    }

    .meta-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 14px;
      color: var(--text-color-secondary, #666);
    }

    .meta-item mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }

    mat-card-actions {
      padding: 1rem;
      display: flex;
      gap: 0.5rem;
    }

    .action-button {
      flex: 1;
      font-size: 16px;
      font-weight: 600;
      height: 48px;
    }

    .action-button mat-icon {
      margin-right: 0.5rem;
    }

    // Dark mode support
    .dark-mode .next-best-action-card {
      background: linear-gradient(135deg, rgba(25, 118, 210, 0.08) 0%, rgba(0, 0, 0, 0) 100%);
    }

    .dark-mode .next-best-action-card.priority-critical {
      background: linear-gradient(135deg, rgba(244, 67, 54, 0.12) 0%, rgba(0, 0, 0, 0) 100%);
    }

    .dark-mode .next-best-action-card.priority-high {
      background: linear-gradient(135deg, rgba(255, 152, 0, 0.1) 0%, rgba(0, 0, 0, 0) 100%);
    }

    .dark-mode mat-card-subtitle,
    .dark-mode .meta-item {
      color: #999;
    }

    // Mobile responsiveness
    @media (max-width: 768px) {
      .action-header {
        flex-direction: column;
        align-items: center;
        text-align: center;
      }

      .header-icon {
        width: 48px;
        height: 48px;
      }

      .header-icon mat-icon {
        font-size: 28px;
        width: 28px;
        height: 28px;
      }

      mat-card-title {
        font-size: 18px;
        justify-content: center;
      }

      .action-meta {
        justify-content: center;
      }

      mat-card-actions {
        flex-direction: column;
      }

      .action-button {
        width: 100%;
      }
    }
  `]
})
export class NextBestActionComponent {
  @Input() action!: NextBestAction;
  @Input() showDismiss: boolean = false;

  @Output() actionClick = new EventEmitter<string>();
  @Output() dismiss = new EventEmitter<void>();

  getPriorityClass(): string {
    return `priority-${this.action.priority}`;
  }

  getPriorityLabel(): string {
    switch (this.action.priority) {
      case 'critical': return 'Urgent';
      case 'high': return 'High Priority';
      case 'medium': return 'Recommended';
      case 'low': return 'Optional';
      default: return '';
    }
  }

  getButtonColor(): 'primary' | 'accent' | 'warn' {
    switch (this.action.priority) {
      case 'critical': return 'warn';
      case 'high': return 'accent';
      default: return 'primary';
    }
  }

  getActionIcon(): string {
    switch (this.action.priority) {
      case 'critical': return 'priority_high';
      case 'high': return 'trending_up';
      default: return 'arrow_forward';
    }
  }

  getProgressClass(): string {
    if (!this.action.progressPercentage) return '';
    if (this.action.progressPercentage >= 75) return 'progress-high';
    if (this.action.progressPercentage >= 50) return 'progress-medium';
    return 'progress-low';
  }

  onActionClick(): void {
    this.actionClick.emit(this.action.action);
  }

  onDismiss(): void {
    this.dismiss.emit();
  }
}
