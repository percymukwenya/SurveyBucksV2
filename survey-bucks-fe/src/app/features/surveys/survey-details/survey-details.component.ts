// src/app/features/surveys/survey-details/survey-details.component.ts
import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { SurveyService } from '../../../core/services/survey.service';
import { SurveyDetailDto } from '../../../core/models/survey.model';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatBadgeModule } from '@angular/material/badge';
import { Subscription } from 'rxjs';
import { ConfirmationDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-survey-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    MatExpansionModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatDialogModule,
    MatBadgeModule
  ],
  templateUrl: './survey-details.component.html',
  styleUrls: ['./survey-details.component.scss']
})
export class SurveyDetailsComponent implements OnInit, OnDestroy {
  surveyId: number = 0;
  survey: SurveyDetailDto | null = null;
  loading: boolean = true;
  enrolling: boolean = false;

  // Enhanced state
  existingParticipation: any = null;
  canEnroll: boolean = true;
  estimatedTimeMinutes: number = 0;

  private subscriptions = new Subscription();
  
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private surveyService: SurveyService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) { }
  
  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.surveyId = +params['id'];
      this.loadSurveyDetails();
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
  
  loadSurveyDetails(): void {
    this.loading = true;
    
    this.surveyService.getSurveyDetails(this.surveyId).subscribe({
      next: (survey) => {
        this.survey = survey;
        this.estimatedTimeMinutes = Math.ceil(survey.durationInSeconds / 60);
        this.checkExistingParticipation();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading survey details', error);
        this.loading = false;
        this.handleError('Error loading survey details. Please try again.');
      }
    });
  }
  
  private checkExistingParticipation(): void {
    // Check if user already has participation for this survey
    this.surveyService.getUserParticipations().subscribe({
      next: (participations) => {
        this.existingParticipation = participations.find(p => p.surveyId === this.surveyId);
        
        if (this.existingParticipation) {
          // Determine if user can still interact with the survey
          this.canEnroll = false;
          
          if (this.existingParticipation.statusName === 'InProgress' || 
              this.existingParticipation.statusName === 'Enrolled') {
            this.canEnroll = true;
          }
        }
      },
      error: (error) => {
        console.warn('Could not check existing participations:', error);
        // Allow enrollment attempt
      }
    });
  }
  
  enrollInSurvey(): void {
    if (!this.survey) return;
    
    // If user has existing participation, handle continuation
    if (this.existingParticipation) {
      this.handleExistingParticipation();
      return;
    }
    
    // Show enrollment confirmation with survey details
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '450px',
      data: {
        title: 'Start Survey',
        message: `Are you ready to start "${this.survey.name}"? This survey will take approximately ${this.estimatedTimeMinutes} minutes to complete.`,
        confirmText: 'Start Survey',
        cancelText: 'Cancel',
        details: [
          `${this.calculateTotalQuestions()} questions`,
          `Estimated time: ${this.estimatedTimeMinutes} minutes`,
          this.survey.requireAllQuestions ? 'All questions required' : 'Some questions optional'
        ]
      }
    });
    
    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.performEnrollment();
      }
    });
  }

  private handleExistingParticipation(): void {
    const status = this.existingParticipation.statusName;
    
    switch (status) {
      case 'Enrolled':
        this.performEnrollment();
        break;
        
      case 'InProgress':
        const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
          width: '400px',
          data: {
            title: 'Continue Survey',
            message: `You have already started this survey (${this.existingParticipation.progressPercentage}% complete). Would you like to continue where you left off?`,
            confirmText: 'Continue Survey',
            cancelText: 'Cancel'
          }
        });
        
        dialogRef.afterClosed().subscribe(confirmed => {
          if (confirmed) {
            this.router.navigate(['/client/surveys/take', this.existingParticipation.id]);
          }
        });
        break;
        
      case 'Completed':
      case 'Rewarded':
        this.snackBar.open('You have already completed this survey.', 'View Results', {
          duration: 5000
        }).onAction().subscribe(() => {
          this.router.navigate(['/client/surveys/completed', this.existingParticipation.id]);
        });
        break;
        
      case 'Disqualified':
        this.snackBar.open('You were disqualified from this survey.', 'Close', {
          duration: 5000
        });
        break;
        
      default:
        this.performEnrollment();
    }
  }
  
  private performEnrollment(): void {
    this.enrolling = true;
    
    this.surveyService.enrollInSurvey(this.surveyId).subscribe({
      next: (participation) => {
        this.enrolling = false;
        this.snackBar.open('Successfully enrolled in survey!', 'Close', {
          duration: 3000
        });
        
        // Navigate to survey taking with the participation ID
        this.router.navigate(['/client/surveys/take', participation.id || participation.participationId]);
      },
      error: (error) => {
        this.enrolling = false;
        console.error('Error enrolling in survey', error);
        
        let errorMessage = 'Error enrolling in survey. Please try again.';
        
        if (error.status === 400) {
          errorMessage = error.error?.message || 'You may not be eligible for this survey.';
        } else if (error.status === 409) {
          errorMessage = 'You are already enrolled in this survey.';
        }
        
        this.handleError(errorMessage);
      }
    });
  }
  
  private handleError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }
  
  formatDuration(seconds: number): string {
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) {
      return `${minutes} min`;
    }
    
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
  }
  
  calculateTotalQuestions(): number {
    if (!this.survey) return 0;
    
    return this.survey.sections.reduce((total, section) => {
      return total + section.questions.length;
    }, 0);
  }

  getMandatoryQuestionCount(): number {
    if (!this.survey) return 0;
    
    return this.survey.sections.reduce((total, section) => {
      return total + section.questions.filter(q => q.isMandatory).length;
    }, 0);
  }
  
  getOptionalQuestionCount(): number {
    return this.calculateTotalQuestions() - this.getMandatoryQuestionCount();
  }

  getRewardDisplay(reward: any): string {
    if (reward.rewardType === 'Points') {
      return `${reward.amount} Points`;
    } else if (reward.monetaryValue) {
      return `${reward.monetaryValue} ${reward.currency || 'USD'}`;
    } else {
      return reward.name;
    }
  }

  getDifficultyLevel(): string {
    const totalQuestions = this.calculateTotalQuestions();
    const estimatedTime = this.estimatedTimeMinutes;
    
    if (totalQuestions <= 10 && estimatedTime <= 5) {
      return 'Easy';
    } else if (totalQuestions <= 25 && estimatedTime <= 15) {
      return 'Medium';
    } else {
      return 'Long';
    }
  }

  getDifficultyColor(): string {
    const difficulty = this.getDifficultyLevel();
    switch (difficulty) {
      case 'Easy': return 'primary';
      case 'Medium': return 'accent';
      case 'Long': return 'warn';
      default: return 'primary';
    }
  }
  
  isClosingSoon(): boolean {
    if (!this.survey?.closingDateTime) return false;
    
    const closingDate = new Date(this.survey.closingDateTime);
    const now = new Date();
    const hoursUntilClose = (closingDate.getTime() - now.getTime()) / (1000 * 60 * 60);
    
    return hoursUntilClose <= 24 && hoursUntilClose > 0;
  }
  
  getTimeUntilClose(): string {
    if (!this.survey?.closingDateTime) return '';
    
    const closingDate = new Date(this.survey.closingDateTime);
    const now = new Date();
    const msUntilClose = closingDate.getTime() - now.getTime();
    
    if (msUntilClose <= 0) return 'Closed';
    
    const hours = Math.floor(msUntilClose / (1000 * 60 * 60));
    const days = Math.floor(hours / 24);
    
    if (days > 0) {
      return `${days} day${days > 1 ? 's' : ''} remaining`;
    } else {
      return `${hours} hour${hours > 1 ? 's' : ''} remaining`;
    }
  }
  
  getStatusChipText(): string {
    if (!this.existingParticipation) return '';
    
    switch (this.existingParticipation.statusName) {
      case 'Enrolled': return 'Enrolled';
      case 'InProgress': return `${this.existingParticipation.progressPercentage}% Complete`;
      case 'Completed': return 'Completed';
      case 'Rewarded': return 'Completed & Rewarded';
      case 'Disqualified': return 'Disqualified';
      default: return this.existingParticipation.statusName;
    }
  }
  
  getStatusChipColor(): string {
    if (!this.existingParticipation) return 'primary';
    
    switch (this.existingParticipation.statusName) {
      case 'Completed':
      case 'Rewarded':
        return 'primary';
      case 'InProgress':
        return 'accent';
      case 'Disqualified':
        return 'warn';
      default:
        return 'primary';
    }
  }
  
  getButtonText(): string {
    if (this.enrolling) return 'Enrolling...';
    
    if (this.existingParticipation) {
      switch (this.existingParticipation.statusName) {
        case 'Enrolled': return 'Start Survey';
        case 'InProgress': return 'Continue Survey';
        case 'Completed':
        case 'Rewarded': return 'View Results';
        case 'Disqualified': return 'Disqualified';
        default: return 'View Survey';
      }
    }
    
    return 'Start Survey';
  }
  
  shouldShowButton(): boolean {
    if (!this.existingParticipation) return true;
    
    return ['Enrolled', 'InProgress', 'Completed', 'Rewarded'].includes(
      this.existingParticipation.statusName
    );
  }
}
