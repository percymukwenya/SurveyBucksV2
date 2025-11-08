import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface BranchingAction {
  actionType: string;
  targetQuestionId?: number;
  targetQuestionIds?: number[];
  targetSectionId?: number;
  message?: string;
  metadata?: Record<string, any>;
}

export interface BranchingEvaluationResult {
  hasActions: boolean;
  actions: BranchingAction[];
}

export interface SurveyFlowState {
  participationId: number;
  surveyId: number;
  currentSectionId?: number;
  currentQuestionId?: number;
  completedQuestions: number[];
  availableQuestions: number[];
  conditionalPath: ConditionalPathStep[];
  isComplete: boolean;
}

export interface ConditionalPathStep {
  questionId: number;
  response: string;
  actionTaken: string;
  timestamp: Date;
}

export interface BranchingEvaluationRequest {
  questionId: number;
  responseValue: string;
  participationId: number;
}

export interface ResponseBranchingRequest {
  questionId: number;
  answer: string;
  participationId: number;
}

@Injectable({
  providedIn: 'root'
})
export class SurveyBranchingService {
  private apiUrl = `${environment.apiUrl}/api/survey-branching`;
  
  // State management
  private currentFlowState = new BehaviorSubject<SurveyFlowState | null>(null);
  private hiddenQuestions = new BehaviorSubject<Set<number>>(new Set());
  private visibleQuestions = new BehaviorSubject<Set<number>>(new Set());
  
  public readonly flowState$ = this.currentFlowState.asObservable();
  public readonly hiddenQuestions$ = this.hiddenQuestions.asObservable();
  public readonly visibleQuestions$ = this.visibleQuestions.asObservable();

  constructor(private http: HttpClient) {}

  /**
   * Evaluates branching logic for a question response in real-time
   */
  evaluateQuestionLogic(request: BranchingEvaluationRequest): Observable<BranchingEvaluationResult> {
    return this.http.post<BranchingEvaluationResult>(`${this.apiUrl}/evaluate`, request)
      .pipe(
        tap(result => {
          if (result.hasActions) {
            this.processClientSideActions(result.actions);
          }
        }),
        catchError(error => {
          console.error('Error evaluating question logic:', error);
          return of({ hasActions: false, actions: [] });
        })
      );
  }

  /**
   * Gets the current flow state for a participation
   */
  getFlowState(participationId: number): Observable<SurveyFlowState> {
    return this.http.get<SurveyFlowState>(`${this.apiUrl}/flow-state/${participationId}`)
      .pipe(
        tap(flowState => {
          this.currentFlowState.next(flowState);
          this.updateQuestionVisibility(flowState);
        }),
        catchError(error => {
          console.error('Error getting flow state:', error);
          throw error;
        })
      );
  }

  /**
   * Gets available questions for a specific section
   */
  getAvailableQuestions(participationId: number, sectionId: number): Observable<number[]> {
    return this.http.get<{availableQuestions: number[]}>(`${this.apiUrl}/available-questions/${participationId}/${sectionId}`)
      .pipe(
        map(response => response.availableQuestions),
        catchError(error => {
          console.error('Error getting available questions:', error);
          return of([]);
        })
      );
  }

  /**
   * Processes branching logic after a response is submitted
   */
  processResponseBranching(request: ResponseBranchingRequest): Observable<BranchingAction> {
    return this.http.post<BranchingAction>(`${this.apiUrl}/process-response`, request)
      .pipe(
        tap(action => {
          if (action.actionType !== 'None') {
            this.processClientSideActions([action]);
          }
        }),
        catchError(error => {
          console.error('Error processing response branching:', error);
          return of({ actionType: 'None' });
        })
      );
  }

  /**
   * Real-time evaluation as user types/selects answers
   */
  evaluateResponseInRealTime(questionId: number, responseValue: string, participationId: number): void {
    // Debounce rapid changes
    const request: BranchingEvaluationRequest = {
      questionId,
      responseValue,
      participationId
    };

    this.evaluateQuestionLogic(request).subscribe(result => {
      if (result.hasActions) {
        console.log('Real-time branching triggered:', result.actions);
      }
    });
  }

  /**
   * Check if a question should be visible based on current state
   */
  isQuestionVisible(questionId: number): boolean {
    const hidden = this.hiddenQuestions.getValue();
    return !hidden.has(questionId);
  }

  /**
   * Check if a question is available based on flow logic
   */
  isQuestionAvailable(questionId: number): boolean {
    const flowState = this.currentFlowState.getValue();
    return flowState?.availableQuestions.includes(questionId) ?? true;
  }

  /**
   * Get current conditional path
   */
  getCurrentPath(): ConditionalPathStep[] {
    return this.currentFlowState.getValue()?.conditionalPath ?? [];
  }

  /**
   * Clear all branching state (when starting new survey)
   */
  clearBranchingState(): void {
    this.currentFlowState.next(null);
    this.hiddenQuestions.next(new Set());
    this.visibleQuestions.next(new Set());
  }

  /**
   * Handle complex branching scenarios client-side
   */
  private processClientSideActions(actions: BranchingAction[]): void {
    const hidden = new Set(this.hiddenQuestions.getValue());
    const visible = new Set(this.visibleQuestions.getValue());
    let hasChanges = false;

    actions.forEach(action => {
      switch (action.actionType) {
        case 'ShowQuestion':
          if (action.targetQuestionId && hidden.has(action.targetQuestionId)) {
            hidden.delete(action.targetQuestionId);
            visible.add(action.targetQuestionId);
            hasChanges = true;
          }
          break;

        case 'HideQuestion':
          if (action.targetQuestionId && !hidden.has(action.targetQuestionId)) {
            hidden.add(action.targetQuestionId);
            visible.delete(action.targetQuestionId);
            hasChanges = true;
          }
          break;

        case 'ShowQuestions':
          action.targetQuestionIds?.forEach(id => {
            if (hidden.has(id)) {
              hidden.delete(id);
              visible.add(id);
              hasChanges = true;
            }
          });
          break;

        case 'JumpToSection':
          if (action.targetSectionId) {
            this.handleSectionJump(action.targetSectionId, action.message);
          }
          break;

        case 'EndSurvey':
          this.handleSurveyEnd(action.message);
          break;

        case 'Disqualify':
          this.handleDisqualification(action.message);
          break;
      }
    });

    if (hasChanges) {
      this.hiddenQuestions.next(hidden);
      this.visibleQuestions.next(visible);
    }
  }

  private updateQuestionVisibility(flowState: SurveyFlowState): void {
    // Update visibility based on flow state
    const allQuestions = new Set([
      ...flowState.availableQuestions,
      ...flowState.completedQuestions
    ]);
    
    this.visibleQuestions.next(new Set(flowState.availableQuestions));
  }

  private handleSectionJump(targetSectionId: number, message?: string): void {
    // This would be handled by the parent survey component
    // We emit an event or update a service that the component subscribes to
    console.log(`Jumping to section ${targetSectionId}:`, message);
    
    // You could emit through a subject that the survey component subscribes to
    this.sectionJumpRequested.next({ 
      targetSectionId, 
      message: message || 'Redirecting based on your response...' 
    });
  }

  private handleSurveyEnd(message?: string): void {
    console.log('Survey ended via branching logic:', message);
    this.surveyEndRequested.next({ 
      reason: 'branching_logic', 
      message: message || 'Survey completed based on your responses.' 
    });
  }

  private handleDisqualification(message?: string): void {
    console.log('User disqualified:', message);
    this.disqualificationRequested.next({ 
      reason: message || 'You do not qualify to continue this survey.' 
    });
  }

  // Subjects for communicating with parent components
  private sectionJumpRequested = new BehaviorSubject<{ targetSectionId: number; message: string } | null>(null);
  private surveyEndRequested = new BehaviorSubject<{ reason: string; message: string } | null>(null);
  private disqualificationRequested = new BehaviorSubject<{ reason: string } | null>(null);

  public readonly sectionJumpRequested$ = this.sectionJumpRequested.asObservable();
  public readonly surveyEndRequested$ = this.surveyEndRequested.asObservable();
  public readonly disqualificationRequested$ = this.disqualificationRequested.asObservable();

  /**
   * Advanced features for complex scenarios
   */
  
  /**
   * Validate that user can proceed to next section based on branching logic
   */
  canProceedToNextSection(currentSectionId: number, responses: Record<number, any>): Observable<boolean> {
    // This would evaluate all branching rules for the current section
    // and determine if the user can proceed
    return of(true); // Simplified for now
  }

  /**
   * Get all possible next questions based on current responses
   */
  getPossibleNextQuestions(responses: Record<number, any>): Observable<number[]> {
    // Complex evaluation of what questions could be shown next
    return of([]); // Simplified for now
  }

  /**
   * Calculate completion percentage accounting for branching logic
   */
  calculateBranchedCompletionPercentage(responses: Record<number, any>, totalQuestions: number): number {
    const flowState = this.currentFlowState.getValue();
    if (!flowState) return 0;

    // Only count available questions in the percentage
    const availableCount = flowState.availableQuestions.length;
    const answeredAvailable = flowState.availableQuestions.filter(qId => 
      qId in responses && responses[qId] !== null && responses[qId] !== undefined
    ).length;

    return availableCount > 0 ? Math.round((answeredAvailable / availableCount) * 100) : 0;
  }

  /**
   * Debug method to log current branching state
   */
  logCurrentState(): void {
    const flowState = this.currentFlowState.getValue();
    const hidden = this.hiddenQuestions.getValue();
    const visible = this.visibleQuestions.getValue();

    console.group('Survey Branching State');
    console.log('Flow State:', flowState);
    console.log('Hidden Questions:', Array.from(hidden));
    console.log('Visible Questions:', Array.from(visible));
    console.log('Conditional Path:', flowState?.conditionalPath);
    console.groupEnd();
  }
  
  /**
   * Validate survey flow for circular references and logic errors
   */
  validateSurveyFlow(surveyId: number): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/validate-flow/${surveyId}`).pipe(
      catchError(error => {
        console.error('Flow validation error:', error);
        return of(false);
      })
    );
  }
}