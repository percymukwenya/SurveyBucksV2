// src/app/features/surveys/survey-list/survey-list.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SurveyService } from '../../../core/services/survey.service';
import { SurveyListItemDto } from '../../../core/models/survey.model';
import { UserProfileService } from '../../../core/services/user-profile.service';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-survey-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    FormsModule,
    ReactiveFormsModule,
    EmptyStateComponent
  ],
  templateUrl: './survey-list.component.html',
  styleUrls: ['./survey-list.component.scss'],
})
export class SurveyListComponent implements OnInit {
  availableSurveys: SurveyListItemDto[] = [];
  inProgressSurveys: any[] = [];
  completedSurveys: any[] = [];
  loading = true;

  // Filter options
  industries: string[] = [];
  selectedIndustry: string = '';
  searchTerm: string = '';

  // Survey access state
  hasSurveyAccess = false;
  profileCompletion = 0;
  blockingMessage = '';
  nextSteps: any[] = [];
  potentialPoints = 0;

  constructor(
    private surveyService: SurveyService, 
    private userProfileService: UserProfileService,
    private router: Router
  ) {}

  ngOnInit(): void {
    console.log('Component initialized');
    console.log('Initial inProgressSurveys:', this.inProgressSurveys);
    console.log('Initial inProgressSurveys.length:', this.inProgressSurveys?.length);
    this.loadSurveys();
  }

  getType(value: any): string {
  return typeof value;
}

  loadSurveys(): void {
    this.loading = true;

    // Get available surveys and profile completion in parallel
    forkJoin({
      surveys: this.surveyService.getAvailableSurveys(),
      profileCompletion: this.userProfileService.getProfileCompletion()
    }).subscribe({
      next: ({ surveys, profileCompletion }) => {
        // Handle surveys data
        if (Array.isArray(surveys)) {
          // User has access to surveys
          this.availableSurveys = surveys;
          this.hasSurveyAccess = true;
        } else {
          // User doesn't have access
          this.availableSurveys = (surveys as any).surveys || [];
          this.hasSurveyAccess = (surveys as any).hasAccess || false;
          this.blockingMessage = (surveys as any).message || '';
        }

        this.industries = [...new Set(this.availableSurveys.map((s: any) => s.industry))];
        this.profileCompletion = profileCompletion?.overallCompletionPercentage || 0;
        
        // Calculate potential points
        this.potentialPoints = this.availableSurveys.reduce((total: number, survey: any) => 
          total + (survey.reward?.amount || 0), 0);

        // Generate next steps (simplified for this component)
        this.nextSteps = this.generateNextSteps(profileCompletion);
        
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading surveys data', error);
        this.loading = false;
      },
    });

    // Get enrolled and in-progress surveys (combine them)
    forkJoin({
      enrolled: this.surveyService.getUserParticipations('Enrolled'),
      inProgress: this.surveyService.getUserParticipations('InProgress'),
    }).subscribe({
      next: (result) => {
        // Combine enrolled and in-progress surveys
        this.inProgressSurveys = [...result.enrolled, ...result.inProgress]
        .sort((a, b) => new Date(b.EnrolmentDateTime).getTime() - new Date(a.EnrolmentDateTime).getTime());
      
      console.log('Combined inProgressSurveys:', this.inProgressSurveys);
      },
      error: (error) => {
        console.error('Error loading in-progress surveys', error);
      },
    });

    // Get completed surveys
    this.surveyService.getUserParticipations('Completed').subscribe({
      next: (participations) => {
        this.completedSurveys = participations;
      },
      error: (error) => {
        console.error('Error loading completed surveys', error);
      },
    });
  }

  applyFilters(): void {
    this.loadSurveys();
  }

  clearFilters(): void {
    this.selectedIndustry = '';
    this.searchTerm = '';
    this.loadSurveys();
  }

  getMatchScoreColor(score: number): string {
    if (score >= 80) return 'high-match';
    if (score >= 60) return 'medium-match';
    return 'low-match';
  }

  formatDuration(seconds: number): string {
    const minutes = Math.floor(seconds / 60);
    return `${minutes} min`;
  }

  // Helper methods for survey preview
  generateNextSteps(profileCompletion: any): any[] {
    if (!profileCompletion) return [];

    const steps: any[] = [];
    
    if (profileCompletion.documents?.completionPercentage < 100) {
      steps.push({
        section: 'Documents',
        title: 'Upload Documents',
        description: 'Verify your identity with required documents',
        estimatedMinutes: 3,
        priority: 'critical'
      });
    }

    if (profileCompletion.demographics?.completionPercentage < 100) {
      steps.push({
        section: 'Demographics',
        title: 'Complete Demographics',
        description: 'Add demographic information for better matching',
        estimatedMinutes: 5,
        priority: 'high'
      });
    }

    return steps.slice(0, 2); // Only show top 2 steps
  }

  navigateToSection(section: string): void {
    const routes: { [key: string]: string } = {
      'Documents': '/client/profile?tab=documents',
      'Demographics': '/client/profile?tab=demographics',
      'Banking': '/client/profile?tab=banking',
      'Interests': '/client/profile?tab=interests',
      'Profile': '/client/profile'
    };

    const route = routes[section] || '/client/profile';
    this.router.navigateByUrl(route);
  }

  navigateToProfile(): void {
    this.router.navigate(['/client/profile']);
  }

  formatTimeEstimate(minutes: number): string {
    if (minutes < 60) {
      return `${minutes} min`;
    }
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
  }
}
