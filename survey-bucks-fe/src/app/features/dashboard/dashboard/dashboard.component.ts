import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDividerModule } from '@angular/material/divider';
import { Router, RouterModule } from '@angular/router';
import { UserProfileService } from '../../../core/services/user-profile.service';
import { SurveyService } from '../../../core/services/survey.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RewardsService } from '../../../core/services/rewards.service';
import { GamificationService } from '../../../core/services/gamification.service';
import { catchError, finalize, forkJoin, of, Subject, takeUntil } from 'rxjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ErrorHandlerService } from '../../../core/utils/error-handler.service';
import { UserLevel, Achievement, Challenge } from '../../../core/models/gamification.models';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { UserPoints } from '../../../core/models/rewards.models';
import { DashboardData, DashboardDataService, DocumentVerificationStatus, SurveyAccessInfo } from '../../../core/services/dashboard-data.service';

interface SurveyInfo {
  id: number;
  title: string;
  description: string;
  estimatedTimeMinutes: number;
  rewardPoints: number;
  category?: string;
}

interface SurveyParticipation {
  participationId: number;
  surveyId: number;
  surveyTitle: string;
  completionPercentage: number;
  lastActivityDate: Date;
  estimatedTimeRemaining?: number;
}

interface UserDashboardInfo {
  firstName: string;
  lastName: string;
  email: string;
  memberSince: Date;
  surveyStats: {
    completed: number;
    inProgress: number;
    totalStarted: number;
  };
  loginStreak?: {
    currentStreak: number;
    bestStreak: number;
    lastLoginDate: Date;
  };
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatBadgeModule,
    MatTabsModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    RouterModule,
    EmptyStateComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Dashboard state
  loading = true;
  hasError = false;
  
  // Dashboard data - properly typed
  userDashboard: UserDashboardInfo | null = null;
  availableSurveys: SurveyInfo[] = [];
  inProgressSurveys: SurveyParticipation[] = [];
  profileCompletion = 0;
  unreadNotifications = 0;
  userPoints: UserPoints | null = null;
  userLevel: UserLevel | null = null;
  recentAchievements: Achievement[] = [];
  activeChallenges: Challenge[] = [];
  documentVerificationStatus: DocumentVerificationStatus | null = null;
  surveyAccessInfo: SurveyAccessInfo | null = null;
  
  constructor(
    private userProfileService: UserProfileService,
    private surveyService: SurveyService,
    private notificationService: NotificationService,
    private rewardsService: RewardsService,
    private gamificationService: GamificationService,
    private dashboardDataService: DashboardDataService,
    private errorHandler: ErrorHandlerService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }
  
  ngOnInit(): void {
    this.loadDashboardData();
    
    // Auto-refresh dashboard when window gains focus (to detect profile changes from other tabs)
    window.addEventListener('focus', this.handleWindowFocus.bind(this));
    
    // Also refresh every 5 minutes to sync with backend cache expiration
    setInterval(() => {
      this.loadDashboardData();
    }, 5 * 60 * 1000); // 5 minutes
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    
    // Clean up window focus listener
    window.removeEventListener('focus', this.handleWindowFocus.bind(this));
  }
  
  loadDashboardData(): void {
    this.loading = true;
    this.hasError = false;
    
    this.dashboardDataService.loadDashboardData()
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (data: DashboardData) => {
          this.userDashboard = data.userProfile;
          this.availableSurveys = data.availableSurveys;
          this.inProgressSurveys = data.inProgressSurveys;
          this.profileCompletion = data.profileCompletion;
          this.unreadNotifications = data.unreadNotifications;
          this.userPoints = data.userPoints;
          this.userLevel = data.userLevel;
          this.recentAchievements = data.recentAchievements;
          this.activeChallenges = data.activeChallenges;
          this.documentVerificationStatus = data.documentVerificationStatus;
          this.surveyAccessInfo = data.surveyAccessInfo;
          
          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Error loading dashboard data', error);
          this.hasError = true;
          this.errorHandler.handleError(error, 'Error loading dashboard data');
          this.cdr.markForCheck();
        }
      });
  }

  // Computed properties for template
  get pointsBalance(): number {
    return this.userPoints?.balance || 0;
  }

  get currentLevel(): number {
    return this.userLevel?.currentLevel || 1;
  }

  get pointsToNextLevel(): number {
    return this.userLevel?.pointsToNextLevel || 0;
  }

  get levelProgressPercentage(): number {
    return this.userLevel?.progressPercentage || 0;
  }

  get completedSurveys(): number {
    return this.userDashboard?.surveyStats?.completed || 0;
  }

  get currentStreak(): number {
    return this.userDashboard?.loginStreak?.currentStreak || 0;
  }

  get userName(): string {
    if (!this.userDashboard) return 'Survey Participant';
    return `${this.userDashboard.firstName || ''} ${this.userDashboard.lastName || ''}`.trim();
  }

   // Navigation methods
  navigateToProfile(): void {
    this.router.navigate(['/client/profile']);
  }
  
  navigateToSurveys(): void {
    this.router.navigate(['/client/surveys']);
  }
  
  navigateToRewards(): void {
    this.router.navigate(['/client/rewards']);
  }
  
  navigateToAchievements(): void {
    this.router.navigate(['/client/achievements']);
  }
  
  navigateToNotifications(): void {
    this.router.navigate(['/client/notifications']);
  }

  navigateToChallenges(): void {
    this.router.navigate(['/client/challenges']);
  }
  
  continueSurvey(participationId: number): void {
    this.router.navigate(['/client/surveys/take', participationId]);
  }

  viewSurveyDetails(surveyId: number): void {
    this.router.navigate(['/client/surveys', surveyId]);
  }

  // Helper methods for template
  getGreetingMessage(): string {
    const hour = new Date().getHours();
    const firstName = this.userDashboard?.firstName || 'there';
    
    if (hour < 12) {
      return `Good morning, ${firstName}!`;
    } else if (hour < 18) {
      return `Good afternoon, ${firstName}!`;
    } else {
      return `Good evening, ${firstName}!`;
    }
  }

  getCompletionPercentageClass(percentage: number): string {
    if (percentage >= 80) return 'high-completion';
    if (percentage >= 50) return 'medium-completion';
    return 'low-completion';
  }

  getAchievementRarityClass(rarity: string): string {
    return `achievement-${rarity.toLowerCase()}`;
  }

  getChallengeProgressClass(challenge: Challenge): string {
    if (challenge.isCompleted) return 'challenge-completed';
    if (challenge.progressPercentage >= 75) return 'challenge-near-complete';
    if (challenge.progressPercentage >= 50) return 'challenge-halfway';
    return 'challenge-started';
  }

  formatTimeEstimate(minutes: number): string {
    if (minutes < 60) {
      return `${minutes} min`;
    }
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
  }

  formatPointsDisplay(points: number): string {
    if (!points && points !== 0) return '0';
    if (points >= 1000000) {
      return `${(points / 1000000).toFixed(1)}M`;
    }
    if (points >= 1000) {
      return `${(points / 1000).toFixed(1)}K`;
    }
    return points.toString();
  }

  getDaysUntilChallenge(endDate: Date): number {
    const now = new Date();
    const end = new Date(endDate);
    const diffTime = end.getTime() - now.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  // Refresh functionality
  refreshDashboard(): void {
    this.loadDashboardData();
  }

  private handleWindowFocus(): void {
    // Refresh dashboard data when window gains focus to pick up profile changes
    this.loadDashboardData();
  }

  // Track by functions for performance
  trackBySurveyId = (index: number, survey: SurveyInfo): number => survey.id;
  trackByAchievementId = (index: number, achievement: Achievement): number => achievement.id;
  trackByParticipationId = (index: number, participation: SurveyParticipation): number => participation.participationId;
  trackByChallengeId = (index: number, challenge: Challenge): number => challenge.id;

  // Error handling
  onImageError(event: any): void {
    event.target.src = 'assets/images/placeholder-reward.png';
  }

  retryLoadData(): void {
    this.hasError = false;
    this.loadDashboardData();
  }

  // Document Verification Status helpers
  get hasDocumentVerificationIssues(): boolean {
    return this.documentVerificationStatus?.actionRequired || false;
  }

  get documentsInReview(): number {
    return this.documentVerificationStatus?.documentsInReview || 0;
  }

  get rejectedDocumentsCount(): number {
    return this.documentVerificationStatus?.rejectedDocuments || 0;
  }

  getVerificationTimelineText(): string {
    if (!this.documentVerificationStatus) return '';
    
    const hours = this.documentVerificationStatus.averageReviewTimeHours;
    if (hours <= 24) {
      return `${hours} hours`;
    } else {
      const days = Math.round(hours / 24);
      return `${days} business day${days > 1 ? 's' : ''}`;
    }
  }

  getNextUpdateText(): string {
    if (!this.documentVerificationStatus?.nextExpectedUpdate) return '';
    
    const nextUpdate = new Date(this.documentVerificationStatus.nextExpectedUpdate);
    const now = new Date();
    const diffHours = Math.ceil((nextUpdate.getTime() - now.getTime()) / (1000 * 60 * 60));
    
    if (diffHours <= 24) {
      return `Expected within ${diffHours} hours`;
    } else {
      const days = Math.ceil(diffHours / 24);
      return `Expected in ${days} day${days > 1 ? 's' : ''}`;
    }
  }

  // Survey Access helpers
  get hasSurveyAccess(): boolean {
    return this.surveyAccessInfo?.hasAccess || false;
  }

  get surveysBlocked(): boolean {
    return Boolean(this.surveyAccessInfo && !this.surveyAccessInfo.hasAccess);
  }

  get nextSteps(): any[] {
    return this.surveyAccessInfo?.nextSteps || [];
  }

  get criticalNextSteps(): any[] {
    return this.nextSteps.filter(step => step.priority === 'critical');
  }

  get smartBlockingMessage(): string {
    return this.surveyAccessInfo?.message || 'Complete your profile to access surveys';
  }

  getPriorityClass(priority: string): string {
    const classes = {
      critical: 'priority-critical',
      high: 'priority-high',
      medium: 'priority-medium',
      low: 'priority-low'
    };
    return classes[priority as keyof typeof classes] || '';
  }

  getPriorityIcon(priority: string): string {
    const icons = {
      critical: 'priority_high',
      high: 'trending_up',
      medium: 'schedule',
      low: 'info'
    };
    return icons[priority as keyof typeof icons] || 'info';
  }

  navigateToSection(section: string): void {
    const routes = {
      'Documents': '/client/profile?tab=documents',
      'Demographics': '/client/profile?tab=demographics', 
      'Banking': '/client/profile?tab=banking',
      'Interests': '/client/profile?tab=interests',
      'Profile': '/client/profile'
    };

    const route = routes[section as keyof typeof routes] || '/client/profile';
    this.router.navigateByUrl(route);
  }
}