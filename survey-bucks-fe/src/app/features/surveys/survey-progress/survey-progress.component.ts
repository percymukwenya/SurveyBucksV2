import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SurveyProgress } from '../../../core/services/survey.service';

@Component({
  selector: 'app-survey-progress',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressBarModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule
  ],
  template: `
    <div class="survey-progress-container">
      <div class="progress-header">
        <h2 class="survey-title">{{ surveyProgress?.surveyName }}</h2>
        
        <div class="progress-actions">
          <button mat-icon-button 
                  (click)="onSave()" 
                  [disabled]="saving"
                  matTooltip="Save Progress">
            <mat-icon>{{ saving ? 'hourglass_empty' : 'save' }}</mat-icon>
          </button>
          
          <button mat-icon-button 
                  (click)="onPause()" 
                  [disabled]="saving"
                  matTooltip="Pause Survey">
            <mat-icon>pause</mat-icon>
          </button>
        </div>
      </div>

      <div class="progress-details">
        <div class="progress-stats">
          <span class="progress-percentage">{{ getProgressPercentage() }}% Complete</span>
          
          <span class="answered-questions" *ngIf="surveyProgress">
            {{ surveyProgress.answeredQuestions }} / {{ surveyProgress.totalQuestions }} questions answered
          </span>
          
          <span class="time-remaining" 
                *ngIf="surveyProgress?.maxTimeInSeconds"
                [class.warning]="isTimeWarning()"
                [class.critical]="isTimeCritical()">
            <mat-icon>{{ getTimeIcon() }}</mat-icon>
            {{ formatTimeRemaining() }}
          </span>
        </div>
        
        <mat-progress-bar mode="determinate"
                          [value]="getProgressPercentage()"
                          [color]="getProgressColor()"
                          class="progress-bar">
        </mat-progress-bar>
        
        <div class="progress-indicators" *ngIf="surveyProgress">
          <div class="indicator sections">
            <mat-icon>folder</mat-icon>
            <span>{{ getCurrentSection() }} / {{ surveyProgress.totalSections }} sections</span>
          </div>
          
          <div class="indicator time-spent" *ngIf="surveyProgress.timeSpentInSeconds">
            <mat-icon>schedule</mat-icon>
            <span>{{ formatTimeSpent() }} spent</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./survey-progress.component.scss']
})
export class SurveyProgressComponent {
  @Input() surveyProgress: SurveyProgress | null = null;
  @Input() saving: boolean = false;
  @Input() currentSectionIndex: number = 0;

  @Output() saveProgress = new EventEmitter<void>();
  @Output() pauseSurvey = new EventEmitter<void>();

  onSave(): void {
    this.saveProgress.emit();
  }

  onPause(): void {
    this.pauseSurvey.emit();
  }

  getProgressPercentage(): number {
    return this.surveyProgress?.progressPercentage || 0;
  }

  getCurrentSection(): number {
    return this.currentSectionIndex + 1;
  }

  formatTimeRemaining(): string {
    if (!this.surveyProgress?.maxTimeInSeconds) return '';

    const remainingSeconds =
      this.surveyProgress.maxTimeInSeconds - this.surveyProgress.timeSpentInSeconds;

    if (remainingSeconds <= 0) {
      return 'Time expired';
    }

    const hours = Math.floor(remainingSeconds / 3600);
    const minutes = Math.floor((remainingSeconds % 3600) / 60);
    const seconds = remainingSeconds % 60;

    if (hours > 0) {
      return `${hours}h ${minutes}m remaining`;
    } else if (minutes > 0) {
      return `${minutes}m ${seconds}s remaining`;
    } else {
      return `${seconds}s remaining`;
    }
  }

  formatTimeSpent(): string {
    if (!this.surveyProgress?.timeSpentInSeconds) return '0m';

    const totalSeconds = this.surveyProgress.timeSpentInSeconds;
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);

    if (hours > 0) {
      return `${hours}h ${minutes}m`;
    } else {
      return `${minutes}m`;
    }
  }

  isTimeWarning(): boolean {
    if (!this.surveyProgress?.maxTimeInSeconds) return false;
    
    const remainingSeconds = this.surveyProgress.maxTimeInSeconds - this.surveyProgress.timeSpentInSeconds;
    const remainingMinutes = Math.floor(remainingSeconds / 60);
    
    return remainingMinutes <= 10 && remainingMinutes > 2;
  }

  isTimeCritical(): boolean {
    if (!this.surveyProgress?.maxTimeInSeconds) return false;
    
    const remainingSeconds = this.surveyProgress.maxTimeInSeconds - this.surveyProgress.timeSpentInSeconds;
    const remainingMinutes = Math.floor(remainingSeconds / 60);
    
    return remainingMinutes <= 2;
  }

  getTimeIcon(): string {
    if (this.isTimeCritical()) return 'warning';
    if (this.isTimeWarning()) return 'access_time';
    return 'schedule';
  }

  getProgressColor(): string {
    const percentage = this.getProgressPercentage();
    if (percentage < 25) return 'warn';
    if (percentage < 75) return 'accent';
    return 'primary';
  }
}