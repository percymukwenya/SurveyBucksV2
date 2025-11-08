import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SurveyBranchingService, BranchingEvaluationRequest, ResponseBranchingRequest, BranchingAction } from './survey-branching.service';
import { environment } from '../../../environments/environment';

describe('SurveyBranchingService', () => {
  let service: SurveyBranchingService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/api/survey-branching`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SurveyBranchingService]
    });
    service = TestBed.inject(SurveyBranchingService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('evaluateQuestionLogic', () => {
    it('should evaluate question logic and return actions', () => {
      const request: BranchingEvaluationRequest = {
        questionId: 1,
        responseValue: 'Yes',
        participationId: 100
      };

      const mockResponse = {
        hasActions: true,
        actions: [
          {
            actionType: 'ShowQuestion',
            targetQuestionId: 2,
            message: 'Show question 2'
          }
        ]
      };

      service.evaluateQuestionLogic(request).subscribe(result => {
        expect(result.hasActions).toBe(true);
        expect(result.actions.length).toBe(1);
        expect(result.actions[0].actionType).toBe('ShowQuestion');
        expect(result.actions[0].targetQuestionId).toBe(2);
      });

      const req = httpMock.expectOne(`${apiUrl}/evaluate`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);
    });

    it('should handle evaluation errors gracefully', () => {
      const request: BranchingEvaluationRequest = {
        questionId: 1,
        responseValue: 'Yes',
        participationId: 100
      };

      service.evaluateQuestionLogic(request).subscribe(result => {
        expect(result.hasActions).toBe(false);
        expect(result.actions).toEqual([]);
      });

      const req = httpMock.expectOne(`${apiUrl}/evaluate`);
      req.flush('Error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should process client-side actions when hasActions is true', () => {
      const request: BranchingEvaluationRequest = {
        questionId: 1,
        responseValue: 'Yes',
        participationId: 100
      };

      const mockResponse = {
        hasActions: true,
        actions: [
          {
            actionType: 'ShowQuestion',
            targetQuestionId: 2
          }
        ]
      };

      spyOn(service as any, 'processClientSideActions');

      service.evaluateQuestionLogic(request).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/evaluate`);
      req.flush(mockResponse);

      expect((service as any).processClientSideActions).toHaveBeenCalledWith(mockResponse.actions);
    });
  });

  describe('getFlowState', () => {
    it('should get flow state and update internal state', () => {
      const participationId = 100;
      const mockFlowState = {
        participationId: 100,
        surveyId: 1,
        currentSectionId: 1,
        currentQuestionId: 2,
        completedQuestions: [1, 3],
        availableQuestions: [1, 2, 3, 4],
        conditionalPath: [],
        isComplete: false
      };

      service.getFlowState(participationId).subscribe(flowState => {
        expect(flowState.participationId).toBe(100);
        expect(flowState.surveyId).toBe(1);
        expect(flowState.availableQuestions.length).toBe(4);
      });

      const req = httpMock.expectOne(`${apiUrl}/flow-state/${participationId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockFlowState);
    });
  });

  describe('getAvailableQuestions', () => {
    it('should get available questions for a section', () => {
      const participationId = 100;
      const sectionId = 1;
      const mockResponse = { availableQuestions: [1, 2, 4] };

      service.getAvailableQuestions(participationId, sectionId).subscribe(questions => {
        expect(questions).toEqual([1, 2, 4]);
      });

      const req = httpMock.expectOne(`${apiUrl}/available-questions/${participationId}/${sectionId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should return empty array on error', () => {
      const participationId = 100;
      const sectionId = 1;

      service.getAvailableQuestions(participationId, sectionId).subscribe(questions => {
        expect(questions).toEqual([]);
      });

      const req = httpMock.expectOne(`${apiUrl}/available-questions/${participationId}/${sectionId}`);
      req.flush('Error', { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('processResponseBranching', () => {
    it('should process response branching and return action', () => {
      const request: ResponseBranchingRequest = {
        questionId: 1,
        answer: 'Yes',
        participationId: 100
      };

      const mockAction: BranchingAction = {
        actionType: 'JumpToSection',
        targetSectionId: 2,
        message: 'Jumping to section 2'
      };

      service.processResponseBranching(request).subscribe(action => {
        expect(action.actionType).toBe('JumpToSection');
        expect(action.targetSectionId).toBe(2);
        expect(action.message).toBe('Jumping to section 2');
      });

      const req = httpMock.expectOne(`${apiUrl}/process-response`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockAction);
    });
  });

  describe('question visibility management', () => {
    beforeEach(() => {
      // Initialize some hidden questions
      (service as any).hiddenQuestions.next(new Set([2, 3]));
      (service as any).visibleQuestions.next(new Set([1, 4, 5]));
    });

    it('should check if question is visible correctly', () => {
      expect(service.isQuestionVisible(1)).toBe(true);
      expect(service.isQuestionVisible(2)).toBe(false);
      expect(service.isQuestionVisible(3)).toBe(false);
      expect(service.isQuestionVisible(4)).toBe(true);
    });

    it('should process show question action', () => {
      const actions: BranchingAction[] = [
        {
          actionType: 'ShowQuestion',
          targetQuestionId: 2
        }
      ];

      (service as any).processClientSideActions(actions);

      service.hiddenQuestions$.subscribe(hiddenQuestions => {
        expect(hiddenQuestions.has(2)).toBe(false);
      });

      service.visibleQuestions$.subscribe(visibleQuestions => {
        expect(visibleQuestions.has(2)).toBe(true);
      });
    });

    it('should process hide question action', () => {
      const actions: BranchingAction[] = [
        {
          actionType: 'HideQuestion',
          targetQuestionId: 1
        }
      ];

      (service as any).processClientSideActions(actions);

      service.hiddenQuestions$.subscribe(hiddenQuestions => {
        expect(hiddenQuestions.has(1)).toBe(true);
      });

      service.visibleQuestions$.subscribe(visibleQuestions => {
        expect(visibleQuestions.has(1)).toBe(false);
      });
    });

    it('should process show multiple questions action', () => {
      const actions: BranchingAction[] = [
        {
          actionType: 'ShowQuestions',
          targetQuestionIds: [2, 3]
        }
      ];

      (service as any).processClientSideActions(actions);

      service.hiddenQuestions$.subscribe(hiddenQuestions => {
        expect(hiddenQuestions.has(2)).toBe(false);
        expect(hiddenQuestions.has(3)).toBe(false);
      });

      service.visibleQuestions$.subscribe(visibleQuestions => {
        expect(visibleQuestions.has(2)).toBe(true);
        expect(visibleQuestions.has(3)).toBe(true);
      });
    });
  });

  describe('branching actions', () => {
    it('should handle section jump action', () => {
      const actions: BranchingAction[] = [
        {
          actionType: 'JumpToSection',
          targetSectionId: 3,
          message: 'Redirecting to section 3'
        }
      ];

      spyOn(service as any, 'handleSectionJump');

      (service as any).processClientSideActions(actions);

      expect((service as any).handleSectionJump).toHaveBeenCalledWith(3, 'Redirecting to section 3');
    });

    it('should handle survey end action', () => {
      const actions: BranchingAction[] = [
        {
          actionType: 'EndSurvey',
          message: 'Survey completed'
        }
      ];

      spyOn(service as any, 'handleSurveyEnd');

      (service as any).processClientSideActions(actions);

      expect((service as any).handleSurveyEnd).toHaveBeenCalledWith('Survey completed');
    });

    it('should handle disqualification action', () => {
      const actions: BranchingAction[] = [
        {
          actionType: 'Disqualify',
          message: 'You do not qualify'
        }
      ];

      spyOn(service as any, 'handleDisqualification');

      (service as any).processClientSideActions(actions);

      expect((service as any).handleDisqualification).toHaveBeenCalledWith('You do not qualify');
    });
  });

  describe('real-time evaluation', () => {
    it('should debounce and evaluate responses in real-time', (done) => {
      const questionId = 1;
      const responseValue = 'Test';
      const participationId = 100;

      spyOn(service, 'evaluateQuestionLogic').and.returnValue(
        new Promise(resolve => {
          setTimeout(() => {
            resolve({ hasActions: false, actions: [] });
            done();
          }, 100);
        }) as any
      );

      service.evaluateResponseInRealTime(questionId, responseValue, participationId);

      expect(service.evaluateQuestionLogic).toHaveBeenCalledWith({
        questionId,
        responseValue,
        participationId
      });
    });
  });

  describe('state management', () => {
    it('should clear all branching state', () => {
      // Set some state first
      (service as any).currentFlowState.next({
        participationId: 100,
        surveyId: 1,
        completedQuestions: [1, 2],
        availableQuestions: [3, 4],
        conditionalPath: [],
        isComplete: false
      });
      (service as any).hiddenQuestions.next(new Set([2, 3]));
      (service as any).visibleQuestions.next(new Set([1, 4]));

      service.clearBranchingState();

      service.flowState$.subscribe(state => {
        expect(state).toBeNull();
      });

      service.hiddenQuestions$.subscribe(hidden => {
        expect(hidden.size).toBe(0);
      });

      service.visibleQuestions$.subscribe(visible => {
        expect(visible.size).toBe(0);
      });
    });

    it('should calculate branched completion percentage correctly', () => {
      const responses = { 1: 'Yes', 3: 'No', 5: 'Maybe' };
      const totalQuestions = 10;

      // Mock flow state with available questions
      (service as any).currentFlowState.next({
        participationId: 100,
        surveyId: 1,
        availableQuestions: [1, 2, 3, 4, 5],
        completedQuestions: [1, 3, 5],
        conditionalPath: [],
        isComplete: false
      });

      const percentage = service.calculateBranchedCompletionPercentage(responses, totalQuestions);

      // 3 answered out of 5 available = 60%
      expect(percentage).toBe(60);
    });

    it('should return 0% when no flow state exists', () => {
      const responses = { 1: 'Yes', 2: 'No' };
      const totalQuestions = 10;

      const percentage = service.calculateBranchedCompletionPercentage(responses, totalQuestions);
      expect(percentage).toBe(0);
    });
  });

  describe('debug functionality', () => {
    it('should log current state for debugging', () => {
      spyOn(console, 'group');
      spyOn(console, 'log');
      spyOn(console, 'groupEnd');

      service.logCurrentState();

      expect(console.group).toHaveBeenCalledWith('Survey Branching State');
      expect(console.log).toHaveBeenCalledTimes(4); // Flow State, Hidden Questions, Visible Questions, Conditional Path
      expect(console.groupEnd).toHaveBeenCalled();
    });
  });
});