import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { QuestionDetail } from '../../../core/services/survey.service';

interface ValidationSummary {
  totalErrors: number;
  errorsByQuestion: Map<number, string[]>;
}

interface CompletionStatus {
  canComplete: boolean;
  requiredAnswers: number;
  totalAnswers: number;
  missingSections: string[];
}

@Component({
  selector: 'app-survey-validation',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatChipsModule
  ],
  template: `
    <div class="survey-validation-container">
      
      <!-- Validation Summary Panel -->
      <div class="validation-panel" 
           *ngIf="validationSummary && validationSummary.totalErrors > 0"
           [class.expanded]="showValidationDetails">
        
        <div class="validation-header" (click)="toggleValidationDetails()">
          <div class="validation-title">
            <mat-icon class="error-icon">error</mat-icon>
            <span>{{ validationSummary.totalErrors }} validation error(s) found</span>
          </div>
          <button mat-icon-button>
            <mat-icon>{{ showValidationDetails ? 'expand_less' : 'expand_more' }}</mat-icon>
          </button>
        </div>

        <div class="validation-details" *ngIf="showValidationDetails">
          <div *ngFor="let question of getCurrentQuestions()" class="validation-item">
            <div *ngIf="hasQuestionErrors(question.id)" class="question-errors">
              <div class="question-title">
                <mat-icon>help</mat-icon>
                <span>Question {{ question.order }}: {{ getQuestionPreview(question.text) }}</span>
              </div>
              <ul class="error-list">
                <li *ngFor="let error of getQuestionErrors(question.id)" class="error-message">
                  {{ error }}
                </li>
              </ul>
            </div>
          </div>
          
          <div class="validation-actions">
            <button mat-raised-button color="primary" (click)="focusFirstError()">
              <mat-icon>center_focus_weak</mat-icon>
              Go to First Error
            </button>
          </div>
        </div>
      </div>

      <!-- Auto-Save Status -->
      <div class="auto-save-status" *ngIf="showAutoSaveStatus">
        <div class="status-item" [class]="getAutoSaveStatusClass()">
          <mat-icon [class]="getAutoSaveStatusClass()">{{ getAutoSaveIcon() }}</mat-icon>
          <span>{{ getAutoSaveMessage() }}</span>
          <mat-spinner *ngIf="saving" diameter="16"></mat-spinner>
        </div>
        
        <div class="unsaved-changes" *ngIf="pendingResponsesCount > 0">
          <mat-chip-set>
            <mat-chip>{{ pendingResponsesCount }} unsaved change(s)</mat-chip>
          </mat-chip-set>
        </div>
      </div>

      <!-- Survey Completion Panel -->
      <div class="completion-panel" *ngIf="showCompletionPanel">
        <div class="completion-status">
          <div class="completion-header">
            <mat-icon [class]="getCompletionStatusClass()">
              {{ getCompletionIcon() }}
            </mat-icon>
            <h3>{{ getCompletionTitle() }}</h3>
          </div>
          
          <div class="completion-details">
            <div class="progress-stats">
              <div class="stat-item">
                <span class="stat-value">{{ completionStatus?.totalAnswers || 0 }}</span>
                <span class="stat-label">Questions Answered</span>
              </div>
              
              <div class="stat-item" *ngIf="completionStatus && !completionStatus.canComplete">
                <span class="stat-value">{{ completionStatus.requiredAnswers }}</span>
                <span class="stat-label">Still Required</span>
              </div>
            </div>
            
            <div class="missing-sections" *ngIf="completionStatus?.missingSections.length">
              <p>Complete these sections to finish:</p>
              <mat-chip-set>
                <mat-chip *ngFor="let section of completionStatus.missingSections">
                  {{ section }}
                </mat-chip>
              </mat-chip-set>
            </div>
          </div>
        </div>

        <div class="completion-actions">
          <button mat-raised-button 
                  (click)="onSaveProgress()"
                  [disabled]="saving"
                  class="save-button">
            <mat-icon>save</mat-icon>
            Save Progress
          </button>

          <button mat-raised-button 
                  color="primary"
                  (click)="onCompleteSurvey()"
                  [disabled]="!completionStatus?.canComplete || completing"
                  class="complete-button">
            <mat-spinner *ngIf="completing" diameter="20"></mat-spinner>
            <mat-icon *ngIf="!completing">check_circle</mat-icon>
            {{ completing ? 'Submitting...' : 'Complete Survey' }}
          </button>
        </div>
      </div>

      <!-- Quick Actions -->
      <div class="quick-actions" *ngIf="showQuickActions">
        <button mat-fab 
                color="primary"
                (click)="onSaveProgress()"
                [disabled]="saving"
                matTooltip="Save Progress"
                class="save-fab">
          <mat-icon>{{ saving ? 'hourglass_empty' : 'save' }}</mat-icon>
        </button>
      </div>
    </div>
  `,
  styleUrls: ['./survey-validation.component.scss']
})
export class SurveyValidationComponent {
  @Input() validationSummary: ValidationSummary | null = null;
  @Input() completionStatus: CompletionStatus | null = null;
  @Input() saving: boolean = false;
  @Input() completing: boolean = false;
  @Input() pendingResponsesCount: number = 0;
  @Input() currentQuestions: QuestionDetail[] = [];
  @Input() showValidationDetails: boolean = false;
  @Input() showAutoSaveStatus: boolean = true;
  @Input() showCompletionPanel: boolean = false;
  @Input() showQuickActions: boolean = true;

  @Output() saveProgress = new EventEmitter<void>();
  @Output() completeSurvey = new EventEmitter<void>();
  @Output() focusFirstError = new EventEmitter<void>();
  @Output() toggleValidationDetails = new EventEmitter<void>();

  getCurrentQuestions(): QuestionDetail[] {
    return this.currentQuestions || [];
  }

  hasQuestionErrors(questionId: number): boolean {
    return this.validationSummary?.errorsByQuestion.has(questionId) || false;
  }

  getQuestionErrors(questionId: number): string[] {
    return this.validationSummary?.errorsByQuestion.get(questionId) || [];
  }

  getQuestionPreview(text: string): string {
    return text.length > 50 ? text.substring(0, 50) + '...' : text;
  }

  onSaveProgress(): void {
    this.saveProgress.emit();
  }

  onCompleteSurvey(): void {
    this.completeSurvey.emit();
  }

  onFocusFirstError(): void {
    this.focusFirstError.emit();
  }

  toggleValidationDetailsState(): void {
    this.toggleValidationDetails.emit();
  }

  getAutoSaveStatusClass(): string {
    if (this.saving) return 'saving';
    if (this.pendingResponsesCount > 0) return 'pending';
    return 'saved';
  }

  getAutoSaveIcon(): string {
    if (this.saving) return 'cloud_upload';
    if (this.pendingResponsesCount > 0) return 'cloud_queue';
    return 'cloud_done';
  }

  getAutoSaveMessage(): string {
    if (this.saving) return 'Saving changes...';
    if (this.pendingResponsesCount > 0) return 'Changes pending';
    return 'All changes saved';
  }

  getCompletionStatusClass(): string {
    return this.completionStatus?.canComplete ? 'complete' : 'incomplete';
  }

  getCompletionIcon(): string {
    return this.completionStatus?.canComplete ? 'check_circle' : 'radio_button_unchecked';
  }

  getCompletionTitle(): string {
    return this.completionStatus?.canComplete 
      ? 'Ready to Complete!' 
      : 'Complete Required Sections';
  }
}