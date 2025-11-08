import { Injectable } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { UserProfileService } from './user-profile.service';
import { SurveyService } from './survey.service';
import { NotificationService } from './notification.service';
import { RewardsService } from './rewards.service';
import { GamificationService } from './gamification.service';
import { DocumentService, UserDocument, UserVerificationStatus } from './document.service';

export interface DocumentVerificationStatus {
  totalDocuments: number;
  pendingDocuments: number;
  approvedDocuments: number;
  rejectedDocuments: number;
  documentsInReview: number;
  averageReviewTimeHours: number;
  nextExpectedUpdate?: Date;
  rejectedItems: UserDocument[];
  actionRequired: boolean;
  hasDocuments: boolean;
}

export interface SurveyAccessInfo {
  hasAccess: boolean;
  blockingFactors: string[];
  completionPercentage: number;
  message: string;
  nextSteps: Array<{
    section: string;
    title: string;
    description: string;
    estimatedMinutes: number;
    priority: 'critical' | 'high' | 'medium' | 'low';
  }>;
  surveysUnlocked?: number;
  potentialPoints?: number;
}

export interface DashboardData {
  userProfile: any;
  availableSurveys: any[];
  inProgressSurveys: any[];
  profileCompletion: number;
  unreadNotifications: number;
  userPoints: any;
  userLevel: any;
  recentAchievements: any[];
  activeChallenges: any[];
  userStats: any;
  documentVerificationStatus: DocumentVerificationStatus;
  surveyAccessInfo: SurveyAccessInfo;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardDataService {
  constructor(
    private userProfileService: UserProfileService,
    private surveyService: SurveyService,
    private notificationService: NotificationService,
    private rewardsService: RewardsService,
    private gamificationService: GamificationService,
    private documentService: DocumentService
  ) {}

  loadDashboardData(): Observable<DashboardData> {
    return forkJoin({
      userProfile: this.userProfileService.getUserDashboard().pipe(
        catchError(() => of(null))
      ),
      availableSurveys: this.surveyService.getAvailableSurveys().pipe(
        map(surveys => surveys.slice(0, 5)), // Only top 5
        catchError(() => of([]))
      ),
      inProgressSurveys: this.surveyService.getUserParticipations('InProgress').pipe(
        catchError(() => of([]))
      ),
      profileCompletion: this.userProfileService.getProfileCompletion().pipe(
        catchError(() => of(0))
      ),
      unreadNotifications: this.notificationService.getUnreadNotificationCount().pipe(
        catchError(() => of(0))
      ),
      userPoints: this.rewardsService.getUserPoints().pipe(
        catchError(() => of({ balance: 0, totalEarned: 0, totalRedeemed: 0 }))
      ),
      userLevel: this.gamificationService.getUserLevel().pipe(
        catchError(() => of({ currentLevel: 1, currentPoints: 0, nextLevelPoints: 100, pointsToNextLevel: 100 }))
      ),
      recentAchievements: this.gamificationService.getUserAchievements().pipe(
        map(achievements => achievements
          .filter(a => a.dateEarned)
          .sort((a, b) => new Date(b.dateEarned!).getTime() - new Date(a.dateEarned!).getTime())
          .slice(0, 3)
        ),
        catchError(() => of([]))
      ),
      activeChallenges: this.gamificationService.getActiveChallenges().pipe(
        map(challenges => challenges.slice(0, 3)), // Only top 3
        catchError(() => of([]))
      ),
      userStats: this.gamificationService.getUserStats().pipe(
        catchError(() => of({}))
      ),
      documentVerificationStatus: this.loadDocumentVerificationStatus(),
      surveyAccessInfo: this.loadSurveyAccessInfo()
    });
  }

  private loadDocumentVerificationStatus(): Observable<DocumentVerificationStatus> {
    return forkJoin({
      documents: this.documentService.getUserDocuments().pipe(
        catchError(() => of([]))
      ),
      verificationStatus: this.documentService.getVerificationStatus().pipe(
        catchError(() => of({
          overallStatus: 'Incomplete',
          completionPercentage: 0,
          requiredDocuments: 0,
          uploadedDocuments: 0,
          verifiedDocuments: 0,
          pendingDocuments: 0,
          rejectedDocuments: 0,
          documentStatuses: []
        }))
      )
    }).pipe(
      map(({ documents, verificationStatus }) => {
        const pendingDocs = documents.filter(d => d.verificationStatus === 'Pending');
        const approvedDocs = documents.filter(d => d.verificationStatus === 'Approved');
        const rejectedDocs = documents.filter(d => d.verificationStatus === 'Rejected');

        // Calculate estimated review time (simulated - in real app, get from backend)
        const averageReviewTimeHours = 48; // 2 business days
        const nextExpectedUpdate = pendingDocs.length > 0 
          ? new Date(Date.now() + (averageReviewTimeHours * 60 * 60 * 1000))
          : undefined;

        return {
          totalDocuments: documents.length,
          pendingDocuments: pendingDocs.length,
          approvedDocuments: approvedDocs.length,
          rejectedDocuments: rejectedDocs.length,
          documentsInReview: pendingDocs.length,
          averageReviewTimeHours,
          nextExpectedUpdate,
          rejectedItems: rejectedDocs,
          actionRequired: rejectedDocs.length > 0,
          hasDocuments: documents.length > 0
        };
      })
    );
  }

  private loadSurveyAccessInfo(): Observable<SurveyAccessInfo> {
    return forkJoin({
      surveyData: this.surveyService.getAvailableSurveys().pipe(
        catchError(() => of({ hasAccess: false, surveys: [], message: '', completionPercentage: 0 }))
      ),
      profileCompletion: this.userProfileService.getDetailedProfileCompletion().pipe(
        catchError(() => of(null))
      )
    }).pipe(
      map(({ surveyData, profileCompletion }) => {
        // Handle the case where surveyData might be an array or an object
        let hasAccess = false;
        let surveys: any[] = [];
        let message = '';
        let completionPercentage = 0;
        let blockingFactors: string[] = [];

        if (Array.isArray(surveyData)) {
          // Old format - array of surveys means has access
          hasAccess = true;
          surveys = surveyData;
          message = `You have access to ${surveys.length} surveys`;
        } else {
          // New format - object with access information
          hasAccess = surveyData?.hasAccess || false;
          surveys = surveyData?.surveys || [];
          message = surveyData?.message || '';
          completionPercentage = surveyData?.completionPercentage || 0;
          blockingFactors = (surveyData as any)?.blockingFactors || [];
        }

        // Generate next steps based on profile completion
        const nextSteps = this.generateNextSteps(profileCompletion, blockingFactors);
        const potentialPoints = surveys.reduce((total: number, survey: any) => 
          total + (survey.rewardPoints || survey.reward?.points || 0), 0);

        return {
          hasAccess,
          blockingFactors,
          completionPercentage,
          message: hasAccess ? message : this.generateSmartBlockingMessage(nextSteps, surveys.length, potentialPoints),
          nextSteps,
          surveysUnlocked: surveys.length,
          potentialPoints
        };
      })
    );
  }

  private generateNextSteps(profileCompletion: any, blockingFactors: string[]): Array<{
    section: string;
    title: string;
    description: string;
    estimatedMinutes: number;
    priority: 'critical' | 'high' | 'medium' | 'low';
  }> {
    const steps: any[] = [];

    if (!profileCompletion) {
      return [{
        section: 'Profile',
        title: 'Complete Your Profile',
        description: 'Fill out your personal information to get started',
        estimatedMinutes: 5,
        priority: 'critical' as const
      }];
    }

    // Documents section
    if (profileCompletion.documents?.completionPercentage < 25) {
      steps.push({
        section: 'Documents',
        title: 'Upload Identity Documents',
        description: 'Upload your ID or passport for account verification',
        estimatedMinutes: 3,
        priority: 'critical' as const
      });
    }

    // Demographics section
    if (profileCompletion.demographics?.completionPercentage < 25) {
      steps.push({
        section: 'Demographics',
        title: 'Complete Demographics',
        description: 'Add your demographic information for better survey matching',
        estimatedMinutes: 5,
        priority: 'high' as const
      });
    }

    // Banking section
    if (profileCompletion.banking?.completionPercentage < 25) {
      steps.push({
        section: 'Banking',
        title: 'Add Banking Details',
        description: 'Add payment information to redeem your rewards',
        estimatedMinutes: 3,
        priority: 'medium' as const
      });
    }

    // Interests section
    if (profileCompletion.interests?.completionPercentage < 25) {
      steps.push({
        section: 'Interests',
        title: 'Select Your Interests',
        description: 'Choose topics you\'re interested in for targeted surveys',
        estimatedMinutes: 2,
        priority: 'low' as const
      });
    }

    // Sort by priority
    const priorityOrder: { [key: string]: number } = { critical: 0, high: 1, medium: 2, low: 3 };
    return steps.sort((a, b) => priorityOrder[a.priority] - priorityOrder[b.priority]);
  }

  private generateSmartBlockingMessage(nextSteps: any[], surveyCount: number, potentialPoints: number): string {
    if (nextSteps.length === 0) return 'Complete your profile to access surveys';

    const criticalSteps = nextSteps.filter(step => step.priority === 'critical');
    const highSteps = nextSteps.filter(step => step.priority === 'high');
    
    const totalEstimatedTime = nextSteps
      .slice(0, 3) // Only show first 3 steps
      .reduce((total, step) => total + step.estimatedMinutes, 0);

    if (criticalSteps.length > 0) {
      const step = criticalSteps[0];
      return `${step.title} (${step.estimatedMinutes} min) → Unlock ${surveyCount} surveys worth ${potentialPoints}+ points`;
    } else if (highSteps.length > 0) {
      const step = highSteps[0];
      return `Complete ${step.title} (${step.estimatedMinutes} min) to improve survey matching`;
    } else {
      return `Complete ${totalEstimatedTime} minutes of profile tasks → Access ${surveyCount} surveys`;
    }
  }
}