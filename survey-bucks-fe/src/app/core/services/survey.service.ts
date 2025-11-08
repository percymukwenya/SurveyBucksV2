import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, catchError, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SurveyProgress {
  surveyId: number;
  surveyName: string;
  participationId: number;
  statusId: number;
  statusName: string;
  progressPercentage: number;
  currentSectionId?: number;
  currentQuestionId?: number;
  timeSpentInSeconds: number;
  maxTimeInSeconds?: number;
  totalSections: number;
  totalQuestions: number;
  answeredQuestions: number;
}

export interface SurveyProgressDetail extends SurveyProgress {
  sessionDuration: number;
  completedSections: number;
  averageTimePerQuestion: number;
  estimatedCompletionTime: number;
  isOnline: boolean;
  pendingResponsesCount: number;
  lastSaveTime?: Date;
  conditionalPath?: string; // Track which conditional path user is on
  skippedQuestions?: number[];
  visitedSections?: number[];
}

export interface SurveyNavigation {
  surveyId: number;
  sections: SectionNavigation[];
  currentPath?: string; // Track conditional path taken
  availableSections?: number[]; // Sections available based on responses
  completedPath?: ConditionalPath[];
}

export interface ConditionalPath {
  questionId: number;
  response: string;
  triggeredAction: string;
  timestamp: Date;
}

export interface SectionNavigation {
  id: number;
  name: string;
  order: number;
  questionCount: number;
  answeredCount: number;
  completionPercentage: number;
  isAccessible: boolean; // Based on conditional logic
  isRequired: boolean;
  estimatedTime?: number;
  sectionType?: 'standard' | 'conditional' | 'branching';
}

export interface SurveySectionDetail {
  id: number;
  surveyId: number;
  name: string;
  description: string;
  order: number;
  requireAllQuestions: boolean;
  maxTimeInMins?: number;
  questions: QuestionDetail[];

  // Enhanced section properties
  isConditional?: boolean;
  parentSectionId?: number;
  showCondition?: string;
  sectionLogic?: SectionLogic[];
  completionThreshold?: number; // Percentage required to mark as complete
  estimatedTimeMinutes?: number;
}

export interface SectionLogic {
  id: number;
  sectionId: number;
  conditionType: string;
  conditionValue: string;
  actionType: string;
  targetSectionId?: number;
  isActive: boolean;
}

export interface QuestionDetail {
  id: number;
  surveySectionId: number;
  text: string;
  isMandatory: boolean;
  order: number;
  questionTypeId: number;
  questionTypeName: string;
  hasChoices: boolean;
  hasMatrix: boolean;
  minValue?: number;
  maxValue?: number;
  validationMessage?: string;
  helpText?: string;
  isScreeningQuestion: boolean;
  timeoutInSeconds?: number;
  randomizeChoices: boolean;
  responseChoices: QuestionResponseChoice[];
  matrixRows: MatrixRow[];
  matrixColumns: MatrixColumn[];
  savedResponses: SurveyResponse[];

  // New conditional logic fields
  isConditional?: boolean;
  parentQuestionId?: number;
  showCondition?: string;
  hideCondition?: string;
  questionLogic?: QuestionLogic;
  
  // Enhanced validation
  customValidationRules?: ValidationRule[];
  dependentQuestionIds?: number[];
  
  // UI enhancements
  displayFormat?: 'standard' | 'compact' | 'enhanced';
  iconName?: string;
  cssClass?: string;
}

export interface QuestionLogic {
  id?: number;
  questionId: number;
  logicType: 'show_hide' | 'jump_to' | 'skip_to' | 'end_survey' | 'disqualify';
  conditionType: 'equals' | 'not_equals' | 'contains' | 'greater_than' | 'less_than' | 'in_list' | 'between';
  conditionValue: string;
  conditionValue2?: string; // For 'between' conditions
  actionType: 'show_question' | 'hide_question' | 'show_questions' | 'jump_to_section' | 'skip_to_question' | 'end_survey';
  targetQuestionId?: number;
  targetQuestionIds?: number[];
  targetSectionId?: number;
  message?: string;
  isActive: boolean;
  order?: number;
}

export interface ValidationRule {
  id: number;
  name: string;
  description?: string;
  validationRegex?: string;
  validationScript?: string;
  errorMessage: string;
  questionTypeId?: number;
  isActive: boolean;
}

export interface QuestionResponseChoice {
  id: number;
  questionId: number;
  text: string;
  value: string;
  order: number;
  isExclusiveOption: boolean;
}

export interface MatrixRow {
  id: number;
  questionId: number;
  text: string;
  order: number;
}

export interface MatrixColumn {
  id: number;
  questionId: number;
  text: string;
  value: string;
  order: number;
}

export interface SurveyResponse {
  id?: number;
  questionId: number;
  answer: string;
  matrixRowId?: number;
  responseDateTime?: Date;
  surveyParticipationId: number;
}

export interface ResponseValidationResult {
  isValid: boolean;
  errors: string[];
  responseId?: number;
  isScreeningResponse: boolean;
  screeningResult?: ScreeningResult;
  nextAction?: ConditionalAction;
  autoCompleted?: boolean;
  triggeredLogic?: QuestionLogic[];
  updatedNavigation?: SurveyNavigation;
}

export interface ScreeningResult {
  isQualified: boolean;
  disqualificationReason?: string;
}

export interface ConditionalAction {
  actionType: string;
  targetQuestionId?: number;
  targetSectionId?: number;
}

export interface BatchResponseResult {
  validResponses: SurveyResponse[];
  failedResponses: FailedResponse[];
  successCount: number;
}

export interface FailedResponse {
  questionId: number;
  errors: string[];
}

@Injectable({
  providedIn: 'root'
})
export class SurveyService {
  private apiUrl = `${environment.apiUrl}/api/survey`;

  // State management for current survey session
  private currentSurveyProgress = new BehaviorSubject<SurveyProgress | null>(null);
  private currentNavigation = new BehaviorSubject<SurveyNavigation | null>(null);
  private loadedSections = new Map<number, SurveySectionDetail>();
  private pendingResponses = new Map<number, SurveyResponse>();
  
  public readonly surveyProgress$ = this.currentSurveyProgress.asObservable();
  public readonly navigation$ = this.currentNavigation.asObservable();
  
  constructor(private http: HttpClient) { }
  
  getAvailableSurveys(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/available`);
  }
  
  getSurveyDetails(surveyId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${surveyId}/details`);
  }
  
  enrollInSurvey(surveyId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${surveyId}/enroll`, {});
  }
  
  getParticipation(participationId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/participation/${participationId}`);
  }
  
  completeSurvey(participationId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/participation/${participationId}/complete`, {});
  }
  
  getUserParticipations(status?: string): Observable<any[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<any[]>(`${this.apiUrl}/participation`, { params });
  }
  
  saveSurveyResponse(response: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/response`, response);
  }
  
  getSavedResponses(participationId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/participation/${participationId}/responses`);
  }
  
  submitSurveyFeedback(feedback: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/feedback`, feedback);
  }

  getSurveyProgress(surveyId: number): Observable<SurveyProgress> {
    return this.http.get<SurveyProgress>(`${this.apiUrl}/${surveyId}/progress`)
      .pipe(
        tap(progress => this.currentSurveyProgress.next(progress))
      );
  }
  
  getSurveyNavigation(surveyId: number): Observable<SurveyNavigation> {
    return this.http.get<SurveyNavigation>(`${this.apiUrl}/${surveyId}/navigation`)
      .pipe(
        tap(navigation => this.currentNavigation.next(navigation))
      );
  }

  getSectionDetails(sectionId: number): Observable<SurveySectionDetail> {
    // Check cache first
    if (this.loadedSections.has(sectionId)) {
      return new Observable(observer => {
        observer.next(this.loadedSections.get(sectionId)!);
        observer.complete();
      });
    }
    
    return this.http.get<SurveySectionDetail>(`${this.apiUrl}/section/${sectionId}`)
      .pipe(
        tap(section => this.loadedSections.set(sectionId, section))
      );
  }

  saveResponse(response: SurveyResponse): Observable<ResponseValidationResult> {
    // Add to pending responses for offline support
    this.pendingResponses.set(response.questionId, response);
    
    return this.http.post<ResponseValidationResult>(`${this.apiUrl}/response`, response)
      .pipe(
        tap(result => {
          if (result.isValid) {
            this.pendingResponses.delete(response.questionId);
          }
        }),
        catchError(error => {
          // Keep in pending for retry
          console.error('Failed to save response:', error);
          throw error;
        })
      );
  }

  saveBatchResponses(responses: SurveyResponse[]): Observable<BatchResponseResult> {
    return this.http.post<BatchResponseResult>(`${this.apiUrl}/responses/batch`, responses);
  }

  updateProgress(participationId: number, progressDto: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/participation/${participationId}/progress`, progressDto)
      .pipe(
        tap(() => {
          // Update local progress state
          const currentProgress = this.currentSurveyProgress.value;
          if (currentProgress) {
            this.currentSurveyProgress.next({
              ...currentProgress,
              progressPercentage: progressDto.progressPercentage,
              currentSectionId: progressDto.sectionId,
              currentQuestionId: progressDto.questionId
            });
          }
        })
      );
  }
  
  // Session management
  pauseSurvey(participationId: number, pauseData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/participation/${participationId}/pause`, pauseData);
  }
  
  resumeSurvey(participationId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/participation/${participationId}/resume`, {});
  }

  trackQuestionTime(participationId: number, timeData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/participation/${participationId}/time-tracking`, timeData);
  }

  validateCurrentState(participationId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/participation/${participationId}/validation`);
  }

  // Utility methods for state management
  clearSurveySession(): void {
    this.currentSurveyProgress.next(null);
    this.currentNavigation.next(null);
    this.loadedSections.clear();
    this.pendingResponses.clear();
  }
  
  getPendingResponses(): SurveyResponse[] {
    return Array.from(this.pendingResponses.values());
  }
  
  hasPendingResponses(): boolean {
    return this.pendingResponses.size > 0;
  }

  // Auto-save functionality
  enableAutoSave(participationId: number, intervalMs: number = 30000): void {
    setInterval(() => {
      const pending = this.getPendingResponses();
      if (pending.length > 0) {
        this.saveBatchResponses(pending).subscribe({
          next: (result) => {
            console.log('Auto-saved responses:', result.successCount);
            // Clear successfully saved responses
            result.validResponses.forEach(response => {
              this.pendingResponses.delete(response.questionId);
            });
          },
          error: (error) => {
            console.warn('Auto-save failed:', error);
          }
        });
      }
    }, intervalMs);
  }
}