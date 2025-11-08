import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  FormArray,
  Validators,
  ReactiveFormsModule,
  FormControlOptions,
  ValidatorFn,
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatSliderModule } from '@angular/material/slider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import {
  ConditionalPath,
  MatrixRow,
  QuestionDetail,
  SurveyNavigation,
  SurveyProgress,
  SurveyResponse,
  SurveySectionDetail,
  SurveyService,
} from '../../../core/services/survey.service';
import { ConfirmationDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTabsModule } from '@angular/material/tabs';
import { MatBadgeModule } from '@angular/material/badge';
import {
  debounceTime,
  distinctUntilChanged,
  Subscription,
  takeUntil,
  Subject,
  firstValueFrom,
} from 'rxjs';
import { SurveyValidationService } from '../../../core/services/survey-validation';
import { SurveyBranchingService } from '../../../core/services/survey-branching.service';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';

interface ValidationSummary {
  totalErrors: number;
  errorsByQuestion: Map<number, string[]>;
}

@Component({
  selector: 'app-survey-take',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatRadioModule,
    MatCheckboxModule,
    MatSelectModule,
    MatSliderModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatDialogModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTabsModule,
    MatBadgeModule,
    MatProgressSpinnerModule,
    MatChipsModule,
  ],
  templateUrl: './survey-taking.component.html',
  styleUrls: ['./survey-taking.component.scss'],
})
export class SurveyTakeComponent implements OnInit, OnDestroy {
  participationId: number = 0;
  surveyId: number = 0;

  private hasShownTimeWarning = false;
  private hasShownFinalWarning = false;

  // State management
  surveyProgress: SurveyProgress | null = null;
  navigation: SurveyNavigation | null = null;
  currentSection: SurveySectionDetail | null = null;
  currentSectionIndex: number = 0;

  // Form management
  sectionForm!: FormGroup;
  questionStartTimes = new Map<number, Date>();

  // Loading states
  loading: boolean = true;
  sectionLoading: boolean = false;
  saving: boolean = false;
  completing: boolean = false;

  // Additional properties for conditional logic
  //conditionalLogicService!: ISurveyConditionalLogicService;
  //progressService!: ISurveyProgressService;
  availableSections: number[] = [];
  conditionalPath: ConditionalPath[] = [];
  hiddenQuestions: Set<number> = new Set();

  // RxJS cleanup - Changed from Subscription to Subject
  private destroy$ = new Subject<void>();
  private autoSaveTimer: any;
  private timeTrackingTimer: any;
  private autoSaveRetryCount = 0;
  private maxAutoSaveRetries = 3;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    public surveyService: SurveyService,
    private validationService: SurveyValidationService,
    private branchingService: SurveyBranchingService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      this.participationId = +params['id'];
      this.initializeSurvey();
    });

    // Set up auto-save
    this.setupAutoSave();

    // Set up progress tracking
    this.setupProgressTracking();

    // Set up branching logic
    this.setupBranchingLogic();
  }

  ngOnDestroy(): void {
    // Complete the destroy subject to trigger takeUntil cleanup
    this.destroy$.next();
    this.destroy$.complete();

    this.clearTimers();
    this.surveyService.clearSurveySession();
    this.branchingService.clearBranchingState();
  }

  @HostListener('window:beforeunload', ['$event'])
  unloadHandler(event: any): void {
    if (this.surveyService.hasPendingResponses()) {
      event.preventDefault();
      event.returnValue =
        'You have unsaved changes. Are you sure you want to leave?';
    }
  }

  private async initializeSurvey(): Promise<void> {
    this.loading = true;

    try {
      // Get participation details
      const participation = await firstValueFrom(
        this.surveyService.getParticipation(this.participationId)
      );
      this.surveyId = participation.surveyId;

      // Load progress and navigation in parallel
      const [progress, navigation] = await Promise.all([
        firstValueFrom(this.surveyService.getSurveyProgress(this.surveyId)),
        firstValueFrom(this.surveyService.getSurveyNavigation(this.surveyId)),
      ]);

      this.surveyProgress = progress!;
      this.navigation = navigation!;

      // Determine starting section
      if (progress!.currentSectionId) {
        this.currentSectionIndex = navigation!.sections.findIndex(
          (s) => s.id === progress!.currentSectionId
        );
      }

      // Load the current section
      await this.loadSection(this.currentSectionIndex);

      this.loading = false;
    } catch (error) {
      console.error('Error initializing survey:', error);
      this.loading = false;
      this.snackBar.open('Error loading survey. Please try again.', 'Close', {
        duration: 5000,
      });
    }
  }

  private async loadSection(sectionIndex: number): Promise<void> {
    if (
      !this.navigation ||
      sectionIndex < 0 ||
      sectionIndex >= this.navigation.sections.length
    ) {
      return;
    }

    this.sectionLoading = true;

    try {
      const sectionId = this.navigation.sections[sectionIndex].id;
      const section = await this.surveyService
        .getSectionDetails(sectionId)
        .toPromise();

      this.currentSection = section!;
      this.currentSectionIndex = sectionIndex;

      this.buildSectionForm();
      this.startTimeTracking();
    } catch (error) {
      console.error('Error loading section:', error);
      this.snackBar.open('Error loading section. Please try again.', 'Close', {
        duration: 3000,
      });
    } finally {
      this.sectionLoading = false;
    }
  }

  private buildSectionForm(): void {
    if (!this.currentSection) return;

    const formControls: { [key: string]: any } = {};

    this.currentSection.questions.forEach((question) => {
      const validators = this.getValidators(question);
      let control: any;

      switch (question.questionTypeName) {
        case 'YesNo':
          control = this.fb.control(null, validators);
          break;
        case 'NumberInput':
          control = this.fb.control(null, [
            ...validators,
            Validators.pattern(/^\d+$/) // Numbers only
          ]);
          break;
        case 'LongText':
          control = this.fb.control(null, [
            ...validators,
            Validators.minLength(10) // Ensure meaningful responses
          ]);
          break;
        case 'Rating':
          control = this.fb.control(null, validators);
          break;
        case 'Dropdown':
          control = this.fb.control(null, validators);
          break;
        default:
          control = this.fb.control(null, validators);
          break;
      }
      
      // Disable conditional questions initially
      if (question.isConditional) {
        control.disable();
      }

      formControls[`question_${question.id}`] = control;
    });

    this.sectionForm = this.fb.group(formControls);

    // Pre-fill saved responses
    this.populateSavedResponses();

    // Set up real-time validation
    this.setupFormValidation();
  }

  private getValidators(question: QuestionDetail): any[] {
    const validators: any[] = [];

    if (question.isMandatory) {
      validators.push(Validators.required);
    }

    // Add type-specific validators
    switch (question.questionTypeName) {
      case 'Email':
        validators.push(Validators.email);
        break;

      case 'NumberInput':
        if (question.minValue !== undefined) {
          validators.push(Validators.min(question.minValue));
        }
        if (question.maxValue !== undefined) {
          validators.push(Validators.max(question.maxValue));
        }
        break;
    }

    return validators;
  }

  private populateSavedResponses(): void {
    if (!this.currentSection) return;

    this.currentSection.questions.forEach((question) => {
      if (question.savedResponses && question.savedResponses.length > 0) {
        const response = question.savedResponses[0];
        const controlName = `question_${question.id}`;
        const control = this.sectionForm.get(controlName);

        if (control) {
          try {
            switch (question.questionTypeName) {
              case 'MultipleChoice':
                const selectedValues = JSON.parse(response.answer);
                const checkboxArray = control as FormArray;
                question.responseChoices.forEach((choice, index) => {
                  checkboxArray
                    .at(index)
                    .setValue(selectedValues.includes(choice.value));
                });
                break;

              case 'Matrix':
                const matrixValues = JSON.parse(response.answer);
                Object.keys(matrixValues).forEach((rowId) => {
                  const rowControl = control.get(rowId);
                  if (rowControl) {
                    rowControl.setValue(matrixValues[rowId]);
                  }
                });
                break;

              case 'Date':
                control.setValue(new Date(response.answer));
                break;

              default:
                control.setValue(response.answer);
                break;
            }
          } catch (error) {
            console.warn('Error parsing saved response:', error);
            control.setValue(response.answer);
          }
        }
      }
    });
  }

  private setupFormValidation(): void {
    // Real-time validation and saving - Fixed to use destroy$ Subject
    this.sectionForm.valueChanges
      .pipe(
        debounceTime(500),
        distinctUntilChanged(),
        takeUntil(this.destroy$) // Now using Subject instead of Subscription
      )
      .subscribe(() => {
        this.validateAndSaveCurrentResponses();
      });
  }

  private setupAutoSave(): void {
    this.autoSaveTimer = setInterval(() => {
      if (this.surveyService.hasPendingResponses() && !this.saving) {
        this.performAutoSave();
      }
    }, 30000); // Auto-save every 30 seconds
  }

  private async performAutoSave(): Promise<void> {
    try {
      const pendingResponses = this.surveyService.getPendingResponses();
      if (pendingResponses.length === 0) return;

      this.saving = true;

      if (pendingResponses.length === 1) {
        await firstValueFrom(
          this.surveyService.saveResponse(pendingResponses[0])
        );
      } else {
        const result = await firstValueFrom(
          this.surveyService.saveBatchResponses(pendingResponses)
        );
        if (result.failedResponses.length > 0) {
          console.warn(
            'Some auto-save responses failed:',
            result.failedResponses
          );
        }
      }

      this.autoSaveRetryCount = 0;
      console.log('Auto-save completed successfully');
    } catch (error) {
      this.autoSaveRetryCount++;
      console.warn(`Auto-save failed (attempt ${this.autoSaveRetryCount}/${this.maxAutoSaveRetries}):`, error);
      
      if (this.autoSaveRetryCount >= this.maxAutoSaveRetries) {
        const snackBarRef = this.snackBar.open(
          'Unable to save progress automatically. Please check your connection and try saving manually.',
          'Retry',
          {
            duration: 8000
          }
        );
        snackBarRef.onAction().subscribe(() => {
          this.autoSaveRetryCount = 0;
          this.performAutoSave();
        });
      }
    } finally {
      this.saving = false;
    }
  }

  private setupProgressTracking(): void {
    // Track progress changes - Also fixed to use destroy$ Subject
    this.surveyService.surveyProgress$
      .pipe(takeUntil(this.destroy$))
      .subscribe((progress) => {
        if (progress) {
          this.surveyProgress = progress;
        }
      });
  }

  private setupBranchingLogic(): void {
    // Initialize flow state
    this.branchingService.getFlowState(this.participationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe(flowState => {
        console.log('Flow state loaded:', flowState);
      });

    // Listen for section jumps
    this.branchingService.sectionJumpRequested$
      .pipe(takeUntil(this.destroy$))
      .subscribe(jumpRequest => {
        if (jumpRequest) {
          this.handleBranchingSectionJump(jumpRequest.targetSectionId, jumpRequest.message);
        }
      });

    // Listen for survey end requests
    this.branchingService.surveyEndRequested$
      .pipe(takeUntil(this.destroy$))
      .subscribe(endRequest => {
        if (endRequest) {
          this.handleBranchingSurveyEnd(endRequest.message);
        }
      });

    // Listen for disqualification
    this.branchingService.disqualificationRequested$
      .pipe(takeUntil(this.destroy$))
      .subscribe(disqualification => {
        if (disqualification) {
          this.handleDisqualification(disqualification.reason);
        }
      });

    // Listen for question visibility changes
    this.branchingService.hiddenQuestions$
      .pipe(takeUntil(this.destroy$))
      .subscribe(hiddenQuestions => {
        this.updateQuestionVisibility(hiddenQuestions);
      });
  }

  private startTimeTracking(): void {
    if (!this.currentSection) return;

    // Record start time for each question
    this.currentSection.questions.forEach((question) => {
      this.questionStartTimes.set(question.id, new Date());
    });

    // Track time periodically
    this.timeTrackingTimer = setInterval(() => {
      this.trackQuestionTimes();
    }, 10000); // Track every 10 seconds
  }

  private trackQuestionTimes(): void {
    if (!this.currentSection) return;

    this.currentSection.questions.forEach((question) => {
      const startTime = this.questionStartTimes.get(question.id);
      if (startTime) {
        const timeData = {
          questionId: question.id,
          viewStartTime: startTime,
          viewEndTime: new Date(),
          isAnswered: this.isQuestionAnswered(question),
          deviceInfo: navigator.userAgent,
        };

        this.surveyService
          .trackQuestionTime(this.participationId, timeData)
          .subscribe({
            error: (error) => console.warn('Time tracking failed:', error),
          });
      }
    });
  }

  private validateAndSaveCurrentResponses(): void {
    if (!this.currentSection || this.saving) return;

    const responses: SurveyResponse[] = [];
    const validationErrors = new Map<number, string[]>();

    this.currentSection.questions.forEach((question) => {
      // Skip hidden questions
      if (!this.branchingService.isQuestionVisible(question.id)) {
        return;
      }

      const controlName = `question_${question.id}`;
      const control = this.sectionForm.get(controlName);

      if (control && control.value !== null && control.value !== undefined) {
        try {
          // Client-side validation
          const clientErrors = this.validationService.validateQuestion(
            question,
            control.value
          );

          if (clientErrors.length > 0) {
            validationErrors.set(question.id, clientErrors);
            control.setErrors({ custom: clientErrors });
          } else {
            // Clear custom errors
            if (control.errors?.['custom']) {
              const errors = { ...control.errors };
              delete errors['custom'];
              control.setErrors(Object.keys(errors).length > 0 ? errors : null);
            }

            // Format and prepare response
            const responseValue = this.formatResponseValue(
              question,
              control.value
            );
            if (
              responseValue &&
              responseValue !== '""' &&
              responseValue !== 'null'
            ) {
              responses.push({
                surveyParticipationId: this.participationId,
                questionId: question.id,
                answer: responseValue,
                matrixRowId:
                  question.questionTypeName === 'Matrix'
                    ? undefined
                    : undefined,
              });

              // Evaluate branching logic for this response
              this.evaluateBranchingLogic(question.id, responseValue);
            }
          }
        } catch (error) {
          console.error('Validation error for question', question.id, error);
          const errorMessage =
            error instanceof Error ? error.message : 'Validation failed';
          validationErrors.set(question.id, [errorMessage]);
          control.setErrors({ custom: [errorMessage] });
        }
      }
    });

    // Save valid responses
    if (responses.length > 0 && validationErrors.size === 0) {
      this.saveResponses(responses);
    }
  }

  private evaluateBranchingLogic(questionId: number, responseValue: string): void {
    this.branchingService.evaluateQuestionLogic({
      questionId,
      responseValue,
      participationId: this.participationId
    }).subscribe(result => {
      if (result.hasActions) {
        console.log('Branching logic triggered:', result.actions);
        // Actions are automatically processed by the branching service
      }
    });
  }

  private handleBranchingSectionJump(targetSectionId: number, message: string): void {
    // Find the section index
    const targetIndex = this.navigation?.sections.findIndex(s => s.id === targetSectionId);
    
    if (targetIndex !== undefined && targetIndex >= 0) {
      // Show message to user
      this.snackBar.open(message, 'Continue', { duration: 4000 });
      
      // Jump to the section
      setTimeout(() => {
        this.goToSection(targetIndex);
      }, 1000);
    }
  }

  private handleBranchingSurveyEnd(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 5000 });
    
    // Complete the survey automatically
    setTimeout(() => {
      this.completeSurvey();
    }, 2000);
  }

  private updateQuestionVisibility(hiddenQuestions: Set<number>): void {
    if (!this.currentSection) return;
    
    // Update DOM visibility
    this.currentSection.questions.forEach(question => {
      const questionElement = document.querySelector(`[data-question-id="${question.id}"]`);
      const controlName = `question_${question.id}`;
      const control = this.sectionForm.get(controlName);
      
      if (hiddenQuestions.has(question.id)) {
        // Hide question
        questionElement?.classList.add('hidden');
        control?.disable();
        // Clear value of hidden questions
        control?.setValue(null);
      } else {
        // Show question
        questionElement?.classList.remove('hidden');
        control?.enable();
      }
    });
    
    // Force change detection
    this.sectionForm.updateValueAndValidity();
  }

  private validateMatrixQuestion(
    question: QuestionDetail,
    value: any
  ): string[] {
    const errors: string[] = [];

    if (!question.isMandatory) return errors;

    const matrixValue = value || {};
    const answeredRows = Object.keys(matrixValue).filter((key) => {
      const rowValue = matrixValue[key];
      return rowValue !== null && rowValue !== undefined && rowValue !== '';
    });

    const requiredRows = question.matrixRows.length;

    if (answeredRows.length < requiredRows) {
      const missingCount = requiredRows - answeredRows.length;
      errors.push(
        `Please answer ${missingCount} more row${
          missingCount !== 1 ? 's' : ''
        } in this matrix`
      );
    }

    return errors;
  }

  private formatResponseValue(question: QuestionDetail, value: any): string {
    switch (question.questionTypeName) {
      case 'MultipleChoice':
        const selectedChoices: string[] = [];
        if (Array.isArray(value)) {
          value.forEach((selected, index) => {
            if (selected && question.responseChoices[index]) {
              selectedChoices.push(question.responseChoices[index].value);
            }
          });

          // Handle exclusive options
          const exclusiveChoices = question.responseChoices
            .filter((choice) => choice.isExclusiveOption)
            .map((choice) => choice.value);

          const hasExclusive = selectedChoices.some((choice) =>
            exclusiveChoices.includes(choice)
          );
          if (hasExclusive && selectedChoices.length > 1) {
            // Keep only the exclusive choice
            const exclusiveChoice = selectedChoices.find((choice) =>
              exclusiveChoices.includes(choice)
            );
            return JSON.stringify([exclusiveChoice]);
          }
        }
        return JSON.stringify(selectedChoices);

      case 'Matrix':
        // Validate all required rows are answered
        const matrixValue = value || {};
        const answeredRows = Object.keys(matrixValue).filter(
          (key) => matrixValue[key] !== null && matrixValue[key] !== undefined
        );

        if (
          question.isMandatory &&
          answeredRows.length < question.matrixRows.length
        ) {
          throw new Error('Please answer all matrix rows');
        }

        return JSON.stringify(matrixValue);

      case 'Date':
        return value instanceof Date ? value.toISOString() : value;

      default:
        return value?.toString() || '';
    }
  }

  private saveResponses(responses: SurveyResponse[]): void {
    if (responses.length === 1) {
      this.surveyService.saveResponse(responses[0]).subscribe({
        next: (result) => {
          if (!result.isValid) {
            console.warn('Response validation failed:', result.errors);
          }

          if (
            result.isScreeningResponse &&
            !result.screeningResult?.isQualified
          ) {
            this.handleDisqualification(
              result.screeningResult?.disqualificationReason
            );
          }

          if (result.nextAction) {
            this.handleConditionalAction(result.nextAction);
          }
        },
        error: (error) => {
          console.error('Error saving response:', error);
        },
      });
    } else {
      this.surveyService.saveBatchResponses(responses).subscribe({
        next: (result) => {
          if (result.failedResponses.length > 0) {
            console.warn(
              'Some responses failed to save:',
              result.failedResponses
            );
          }
          console.log(`Saved ${result.successCount} responses`);
        },
        error: (error) => {
          console.error('Error saving batch responses:', error);
        },
      });
    }
  }

  public saveCurrentResponses(showNotification: boolean = true): void {
    this.saving = true;
    this.validateAndSaveCurrentResponses();

    if (showNotification) {
      this.snackBar.open('Progress saved', 'Close', { duration: 2000 });
    }

    setTimeout(() => (this.saving = false), 1000);
  }

  private handleDisqualification(reason?: string): void {
    const message = reason || 'You do not qualify for this survey.';

    this.dialog
      .open(ConfirmationDialogComponent, {
        width: '400px',
        disableClose: true,
        data: {
          title: 'Survey Ended',
          message: `Unfortunately, ${message} Thank you for your time.`,
          confirmText: 'Return to Surveys',
          showCancel: false,
        },
      })
      .afterClosed()
      .subscribe(() => {
        this.router.navigate(['/client/surveys']);
      });
  }

  private handleConditionalAction(action: any): void {
    switch (action.actionType) {
      case 'skip_to':
        if (action.targetSectionId) {
          const targetIndex = this.navigation?.sections.findIndex(
            (s) => s.id === action.targetSectionId
          );
          if (targetIndex !== undefined && targetIndex >= 0) {
            this.goToSection(targetIndex);
          }
        }
        break;

      case 'end_survey':
        this.completeSurvey();
        break;

      default:
        console.warn('Unknown conditional action:', action.actionType);
    }
  }

  // Navigation methods
  async goToSection(sectionIndex: number): Promise<void> {
    if (sectionIndex === this.currentSectionIndex) return;

    // Check for unsaved changes
    if (this.surveyService.hasPendingResponses()) {
      const confirmed = await this.confirmNavigation();
      if (!confirmed) return;
    }

    // Save current section before navigating
    this.saveCurrentResponses(false);

    // Update progress
    await this.updateProgress();

    // Load new section
    await this.loadSection(sectionIndex);
  }

  async confirmNavigation(): Promise<boolean> {
    if (!this.surveyService.hasPendingResponses()) {
      return true;
    }

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Unsaved Changes',
        message: `You have ${
          this.surveyService.getPendingResponses().length
        } unsaved changes. What would you like to do?`,
        confirmText: 'Save & Continue',
        cancelText: 'Discard Changes',
        showCancel: true,
      },
    });

    const result = await firstValueFrom(dialogRef.afterClosed());

    if (result === true) {
      // Save before continuing
      await this.performSaveBeforeNavigation();
      return true;
    } else if (result === false) {
      // Discard changes
      //this.surveyService.clearPendingResponses();
      return true;
    }

    return false; // User cancelled
  }

  private async performSaveBeforeNavigation(): Promise<void> {
    try {
      this.saving = true;
      const pending = this.surveyService.getPendingResponses();

      if (pending.length === 1) {
        await firstValueFrom(this.surveyService.saveResponse(pending[0]));
      } else if (pending.length > 1) {
        await firstValueFrom(this.surveyService.saveBatchResponses(pending));
      }

      this.snackBar.open('Changes saved successfully', 'Close', {
        duration: 2000,
      });
    } catch (error) {
      console.error('Error saving before navigation:', error);
      this.snackBar.open('Error saving changes. Please try again.', 'Close', {
        duration: 3000,
      });
      throw error;
    } finally {
      this.saving = false;
    }
  }

  confirmExit(event: Event): void {
    if (this.surveyService.hasPendingResponses()) {
      event.preventDefault();

      const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
        width: '400px',
        data: {
          title: 'Exit Survey',
          message:
            'You have unsaved changes. Are you sure you want to exit? Your progress will be lost.',
          confirmText: 'Exit Without Saving',
          cancelText: 'Stay & Save',
          showCancel: true,
        },
      });

      dialogRef.afterClosed().subscribe((result) => {
        if (result === true) {
          this.surveyService.clearSurveySession();
          this.router.navigate(['/client/surveys']);
        } else if (result === false) {
          this.saveCurrentResponses(true);
        }
      });
    }
  }

  async nextSection(): Promise<void> {
    if (
      !this.navigation ||
      this.currentSectionIndex >= this.navigation.sections.length - 1
    ) {
      return;
    }

    // Validate current section if required
    if (this.currentSection?.requireAllQuestions && !this.isSectionComplete()) {
      this.snackBar.open(
        'Please complete all required questions before proceeding.',
        'Close',
        {
          duration: 5000,
        }
      );
      return;
    }

    await this.goToSection(this.currentSectionIndex + 1);
  }

  async prevSection(): Promise<void> {
    if (this.currentSectionIndex <= 0) return;
    await this.goToSection(this.currentSectionIndex - 1);
  }

  private async updateProgress(): Promise<void> {
    if (!this.navigation || !this.surveyProgress) return;

    const totalSections = this.navigation.sections.length;
    const currentProgress = Math.round(
      ((this.currentSectionIndex + 1) / totalSections) * 100
    );

    const progressUpdate = {
      sectionId: this.navigation.sections[this.currentSectionIndex].id,
      questionId: this.currentSection?.questions[0]?.id,
      progressPercentage: Math.min(currentProgress, 100),
    };

    try {
      await this.surveyService
        .updateProgress(this.participationId, progressUpdate)
        .toPromise();
    } catch (error) {
      console.error('Error updating progress:', error);
    }
  }

  // Validation methods
  isQuestionAnswered(question: QuestionDetail): boolean {
    const controlName = `question_${question.id}`;
    const control = this.sectionForm.get(controlName);

    if (!control) return false;

    const wasAnswered = this.wasQuestionPreviouslyAnswered(question.id);
    let isCurrentlyAnswered = false;

    switch (question.questionTypeName) {
      case 'MultipleChoice':
        isCurrentlyAnswered = (control.value as boolean[]).some((v) => v === true);
        break;

      case 'Matrix':
        const matrixValue = control.value;
        isCurrentlyAnswered = (
          matrixValue &&
          Object.keys(matrixValue).some((key) => matrixValue[key] !== null)
        );
        break;

      default:
        isCurrentlyAnswered = (
          control.value !== null &&
          control.value !== undefined &&
          control.value !== ''
        );
        break;
    }

    // Trigger completion animation if question was just answered
    if (isCurrentlyAnswered && !wasAnswered) {
      setTimeout(() => this.triggerQuestionCompletionAnimation(question.id), 100);
    }

    return isCurrentlyAnswered;
  }

  private wasQuestionPreviouslyAnswered(questionId: number): boolean {
    // Simple check if question was already marked as answered
    const questionElement = document.querySelector(`[data-question-id="${questionId}"]`);
    return questionElement?.classList.contains('answered') || false;
  }

  private triggerQuestionCompletionAnimation(questionId: number): void {
    const questionElement = document.querySelector(`[data-question-id="${questionId}"]`);
    if (questionElement) {
      questionElement.classList.add('completion-animation');
      setTimeout(() => {
        questionElement.classList.remove('completion-animation');
      }, 1000);
    }
  }

  isSectionComplete(): boolean {
    if (!this.currentSection) return false;

    return this.currentSection.questions.every((question) => {
      if (!question.isMandatory) return true;
      return this.isQuestionAnswered(question);
    });
  }

  getQuestionErrors(question: QuestionDetail): string[] {
    const controlName = `question_${question.id}`;
    const control = this.sectionForm.get(controlName);

    if (!control || !control.errors) return [];

    const errors: string[] = [];

    if (control.errors['required']) {
      errors.push('This question is required');
    }

    if (control.errors['email']) {
      errors.push('Please enter a valid email address');
    }

    if (control.errors['min']) {
      errors.push(`Value must be at least ${question.minValue}`);
    }

    if (control.errors['max']) {
      errors.push(`Value must be at most ${question.maxValue}`);
    }

    if (control.errors['custom']) {
      errors.push(...control.errors['custom']);
    }

    return errors;
  }

  // Survey completion
  async completeSurvey(): Promise<void> {
    // Final validation
    if (!this.canCompleteSurvey()) {
      this.focusFirstIncompleteSection();
      this.snackBar.open(
        'Please complete all required sections before submitting.',
        'Show Missing',
        {
          duration: 8000,
        }
      ).onAction().subscribe(() => {
        this.showMissingSections();
      });
      return;
    }

    // Save current responses
    this.saveCurrentResponses(false);

    // Enhanced completion dialog with survey summary
    const completionData = this.generateCompletionSummary();
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '500px',
      data: {
        title: '🎉 Ready to Submit Survey',
        message: `You've completed ${completionData.questionsAnswered} questions in ${completionData.timeSpent}. You'll earn ${completionData.pointsToEarn} points!`,
        confirmText: 'Submit & Earn Points',
        cancelText: 'Review Answers',
        showCancel: true,
      },
    });

    const confirmed = await dialogRef.afterClosed().toPromise();
    if (!confirmed) return;

    this.completing = true;

    try {
      const result = await this.surveyService
        .completeSurvey(this.participationId)
        .toPromise();

      // Enhanced completion celebration
      this.showCompletionCelebration(result);

      // Navigate to completion page or survey list
      setTimeout(() => {
        this.router.navigate(['/client/surveys/completed', this.participationId]);
      }, 3000);
    } catch (error: any) {
      this.completing = false;
      console.error('Error completing survey:', error);

      const errorMessage =
        error.error?.message || 'Error completing survey. Please try again.';
      this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
    }
  }

  private generateCompletionSummary() {
    const totalTime = this.surveyProgress?.timeSpentInSeconds || 0;
    const hours = Math.floor(totalTime / 3600);
    const minutes = Math.floor((totalTime % 3600) / 60);
    
    return {
      questionsAnswered: this.surveyProgress?.answeredQuestions || 0,
      timeSpent: hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`,
      pointsToEarn: 50
    };
  }

  private showCompletionCelebration(result: any): void {
    const pointsEarned = result?.pointsEarned || 50;
    
    this.snackBar.open(
      `🎉 Survey completed! You earned ${pointsEarned} points! Thank you for your valuable feedback.`,
      'View Rewards',
      {
        duration: 8000,
        panelClass: ['celebration-snackbar']
      }
    );
  }

  private focusFirstIncompleteSection(): void {
    if (!this.navigation) return;
    
    for (let i = 0; i < this.navigation.sections.length; i++) {
      if (this.navigation.sections[i].completionPercentage < 100) {
        this.goToSection(i);
        break;
      }
    }
  }

  private showMissingSections(): void {
    if (!this.navigation) return;
    
    const incompleteSections = this.navigation.sections
      .filter(s => s.completionPercentage < 100)
      .map(s => s.name);
      
    if (incompleteSections.length > 0) {
      this.snackBar.open(
        `Missing sections: ${incompleteSections.join(', ')}`,
        'Close',
        { duration: 6000 }
      );
    }
  }

  getIncompleteSectionCount(): number {
    if (!this.navigation) return 0;
    return this.navigation.sections.filter(s => s.completionPercentage < 100).length;
  }

  showProgressSummary(): void {
    if (!this.navigation || !this.surveyProgress) return;
    
    const incomplete = this.navigation.sections.filter(s => s.completionPercentage < 100);
    const totalTime = this.surveyProgress.timeSpentInSeconds;
    const minutes = Math.floor(totalTime / 60);
    
    let message = `Progress: ${this.getProgressPercentage()}% • Time: ${minutes}m`;
    
    if (incomplete.length > 0) {
      message += `\n\nIncomplete sections: ${incomplete.map(s => s.name).join(', ')}`;
    } else {
      message += `\n\n🎉 All sections complete! Ready to submit.`;
    }
    
    const action = incomplete.length > 0 ? 'Go to Next' : 'Submit Survey';
    
    this.snackBar.open(message, action, {
      duration: 8000,
      panelClass: incomplete.length > 0 ? ['info-snackbar'] : ['celebration-snackbar']
    }).onAction().subscribe(() => {
      if (incomplete.length > 0) {
        // Navigate to first incomplete section
        const incompleteIndex = this.navigation!.sections.findIndex(s => s.completionPercentage < 100);
        if (incompleteIndex >= 0) {
          this.goToSection(incompleteIndex);
        }
      } else {
        this.completeSurvey();
      }
    });
  }

  public canCompleteSurvey(): boolean {
    if (!this.navigation) return false;

    // Check if all required sections are complete
    return this.navigation.sections.every((section, index) => {
      if (index === this.currentSectionIndex) {
        return this.isSectionComplete();
      }

      // Check completion percentage for other sections
      return section.completionPercentage >= 100;
    });
  }

  // Utility methods
  getSectionCompletionStatus(
    sectionIndex: number
  ): 'complete' | 'partial' | 'empty' {
    if (!this.navigation) return 'empty';

    const section = this.navigation.sections[sectionIndex];
    if (section.completionPercentage >= 100) return 'complete';
    if (section.completionPercentage > 0) return 'partial';
    return 'empty';
  }

  formatTimeRemaining(): string {
    if (!this.surveyProgress?.maxTimeInSeconds) return '';

    const remainingSeconds =
      this.surveyProgress.maxTimeInSeconds -
      this.surveyProgress.timeSpentInSeconds;

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

  private checkTimeWarnings(): void {
    if (!this.surveyProgress?.maxTimeInSeconds) return;

    const remainingSeconds =
      this.surveyProgress.maxTimeInSeconds -
      this.surveyProgress.timeSpentInSeconds;
    const remainingMinutes = Math.floor(remainingSeconds / 60);

    // Warning at 10 minutes
    if (remainingMinutes === 10 && !this.hasShownTimeWarning) {
      this.hasShownTimeWarning = true;
      this.snackBar.open(
        '⏰ 10 minutes remaining! Please complete your responses soon.',
        'Close',
        { duration: 5000, panelClass: ['warning-snackbar'] }
      );
    }

    // Final warning at 2 minutes
    if (remainingMinutes === 2 && !this.hasShownFinalWarning) {
      this.hasShownFinalWarning = true;
      this.snackBar
        .open('⚠️ Only 2 minutes left! Save your progress now.', 'Save Now', {
          duration: 10000,
          panelClass: ['error-snackbar'],
        })
        .onAction()
        .subscribe(() => {
          this.saveCurrentResponses(true);
        });
    }

    // Auto-submit when time expires
    if (remainingSeconds <= 0) {
      this.handleTimeExpired();
    }
  }

  private handleTimeExpired(): void {
    this.snackBar.open(
      'Time has expired. Submitting your current progress...',
      'Close',
      { duration: 5000, panelClass: ['error-snackbar'] }
    );

    // Save current responses and complete survey
    this.saveCurrentResponses(false);
    setTimeout(() => {
      this.completeSurvey();
    }, 2000);
  }

  private setupConditionalLogic(): void {
    if (!this.currentSection) return;

    this.currentSection.questions.forEach((question) => {
      if (question.questionLogic || question.isConditional) {
        const controlName = `question_${question.id}`;
        const control = this.sectionForm.get(controlName);

        if (control) {
          control.valueChanges
            .pipe(
              debounceTime(300),
              distinctUntilChanged(),
              takeUntil(this.destroy$)
            )
            .subscribe((value) => {
              this.handleConditionalLogic(question, value);
            });
        }
      }
    });
  }

  private handleConditionalLogic(question: QuestionDetail, value: any): void {
    if (!question.questionLogic) return;

    const conditionMet = this.evaluateCondition(value, question.questionLogic);

    if (conditionMet) {
      switch (question.questionLogic.actionType) {
        case 'show_question':
          this.showQuestion(question.questionLogic.targetQuestionId!);
          break;
        case 'hide_question':
          this.hideQuestion(question.questionLogic.targetQuestionId!);
          break;
        case 'show_questions':
          question.questionLogic.targetQuestionIds?.forEach((id) =>
            this.showQuestion(id)
          );
          break;
        case 'jump_to_section':
          this.jumpToSection(question.questionLogic.targetSectionId!);
          break;
      }
    } else {
      // Handle condition not met - typically hide dependent questions
      if (question.questionLogic.actionType === 'show_question') {
        this.hideQuestion(question.questionLogic.targetQuestionId!);
      }
    }
  }

  async jumpToSection(targetSectionId: number, reason?: string): Promise<void> {
    if (!this.navigation) return;
    
    const targetIndex = this.navigation.sections.findIndex(s => s.id === targetSectionId);
    if (targetIndex === -1) {
      console.warn(`Target section ${targetSectionId} not found`);
      return;
    }
    
    // Check if section is accessible
    /* if (!this.isSectionAccessible(targetSectionId)) {
      this.snackBar.open('This section is not accessible based on your previous answers.', 'Close', {
        duration: 5000
      });
      return;
    } */
    
    // Log the jump action
    this.conditionalPath.push({
      questionId: this.currentSection?.questions[0]?.id || 0,
      response: `jump_to_section_${targetSectionId}`,
      triggeredAction: `jumped_to_section`,
      timestamp: new Date()
    });
    
    // Show user feedback
    if (reason) {
      this.snackBar.open(reason, 'Continue', { duration: 4000 });
    }
    
    // Save current progress before jumping
    if (this.surveyService.hasPendingResponses()) {
      await this.performSaveBeforeNavigation();
    }
    
    // Update progress to reflect the jump
    await this.updateProgress();
    
    // Navigate to target section
    await this.loadSection(targetIndex);
    
    // Update navigation state
    this.updateNavigationState();
  }
  

  private updateNavigationState(): void {
    // Update the current navigation state
    if (this.navigation && this.surveyProgress) {
      this.navigation.currentPath = this.conditionalPath
        .map(p => `${p.questionId}:${p.response}`)
        .join('|');
      
      this.navigation.availableSections = this.availableSections;
      this.navigation.completedPath = this.conditionalPath;
    }
  }

  private evaluateCondition(value: any, logic: any): boolean {
    switch (logic.conditionType) {
      case 'equals':
        return (
          value?.toString().toLowerCase() ===
          logic.conditionValue?.toString().toLowerCase()
        );
      case 'not_equals':
        return (
          value?.toString().toLowerCase() !==
          logic.conditionValue?.toString().toLowerCase()
        );
      case 'contains':
        return value
          ?.toString()
          .toLowerCase()
          .includes(logic.conditionValue?.toString().toLowerCase());
      case 'greater_than':
        return parseFloat(value) > parseFloat(logic.conditionValue);
      case 'less_than':
        return parseFloat(value) < parseFloat(logic.conditionValue);
      default:
        return false;
    }
  }
  private showQuestion(questionId: number): void {
    const questionElement = document.querySelector(
      `[data-question-id="${questionId}"]`
    );
    if (questionElement) {
      questionElement.classList.remove('hidden');
      // Enable form control
      const controlName = `question_${questionId}`;
      const control = this.sectionForm.get(controlName);
      if (control) {
        control.enable();
      }
    }
  }

  private hideQuestion(questionId: number): void {
    const questionElement = document.querySelector(
      `[data-question-id="${questionId}"]`
    );
    if (questionElement) {
      questionElement.classList.add('hidden');
      // Clear and disable form control
      const controlName = `question_${questionId}`;
      const control = this.sectionForm.get(controlName);
      if (control) {
        control.setValue(null);
        control.disable();
      }
    }
  }

  getProgressPercentage(): number {
    return this.surveyProgress?.progressPercentage || 0;
  }

  getValidationSummary(): ValidationSummary {
    if (!this.currentSection) {
      return { totalErrors: 0, errorsByQuestion: new Map() };
    }

    const errorsByQuestion = new Map<number, string[]>();
    let totalErrors = 0;

    this.currentSection.questions.forEach((question) => {
      const errors = this.getQuestionErrors(question);
      if (errors.length > 0) {
        errorsByQuestion.set(question.id, errors);
        totalErrors += errors.length;
      }
    });

    return { totalErrors, errorsByQuestion };
  }

  focusFirstError(): void {
    const firstErrorElement = document.querySelector(
      '.question-wrapper.has-errors .question-input'
    ) as HTMLElement;
    if (firstErrorElement) {
      firstErrorElement.focus();
      firstErrorElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  @HostListener('keydown', ['$event'])
  handleKeyboardNavigation(event: KeyboardEvent): void {
    if (event.ctrlKey || event.metaKey) {
      switch (event.key) {
        case 's':
          event.preventDefault();
          this.saveCurrentResponses(true);
          break;
        case 'ArrowRight':
          if (this.canGoToNextSection()) {
            event.preventDefault();
            this.nextSection();
          }
          break;
        case 'ArrowLeft':
          if (this.currentSectionIndex > 0) {
            event.preventDefault();
            this.prevSection();
          }
          break;
      }
    }
  }

  private canGoToNextSection(): boolean {
    return (
      this.navigation !== null &&
      this.currentSectionIndex < this.navigation.sections.length - 1 &&
      (!this.currentSection?.requireAllQuestions || this.isSectionComplete())
    );
  }

  private startSessionTimer(): void {
    setInterval(() => {
      if (this.surveyProgress) {
        this.surveyProgress.timeSpentInSeconds += 1;
        this.checkTimeWarnings();
      }
    }, 1000);
  }

  private clearTimers(): void {
    if (this.autoSaveTimer) {
      clearInterval(this.autoSaveTimer);
    }

    if (this.timeTrackingTimer) {
      clearInterval(this.timeTrackingTimer);
    }
  }

  getProgressColor(): string {
    const percentage = this.getProgressPercentage();
    if (percentage < 25) return 'warn';
    if (percentage < 75) return 'accent';
    return 'primary';
  }

  getSectionProgressColor(percentage: number): string {
    if (percentage === 100) return '#4caf50';
    if (percentage > 0) return '#ff9800';
    return '#e0e0e0';
  }

  getUnansweredMatrixRows(question: QuestionDetail): MatrixRow[] {
    const controlName = `question_${question.id}`;
    const control = this.sectionForm.get(controlName);

    if (!control || !question.matrixRows) return [];

    return question.matrixRows.filter((row) => {
      const rowValue = control.get(row.id.toString())?.value;
      return !rowValue || rowValue === null || rowValue === undefined;
    });
  }

  onMultipleChoiceChange(
    question: QuestionDetail,
    choiceIndex: number,
    event: any
  ): void {
    const formArray = this.sectionForm.get(
      `question_${question.id}`
    ) as FormArray;
    const selectedChoice = question.responseChoices[choiceIndex];

    if (event.checked && selectedChoice.isExclusiveOption) {
      // If exclusive option is selected, uncheck all others
      formArray.controls.forEach((control, index) => {
        if (index !== choiceIndex) {
          control.setValue(false);
        }
      });

      // Show user feedback
      this.snackBar.open(
        `"${selectedChoice.text}" is an exclusive option. Other selections have been cleared.`,
        'Close',
        { duration: 3000 }
      );
    } else if (event.checked && !selectedChoice.isExclusiveOption) {
      // If non-exclusive option is selected, uncheck any exclusive options
      let exclusiveCleared = false;
      question.responseChoices.forEach((choice, index) => {
        if (choice.isExclusiveOption && formArray.at(index).value) {
          formArray.at(index).setValue(false);
          exclusiveCleared = true;
        }
      });

      if (exclusiveCleared) {
        this.snackBar.open('Exclusive options have been cleared.', 'Close', {
          duration: 2000,
        });
      }
    }

    // Update the specific checkbox
    formArray.at(choiceIndex).setValue(event.checked);

    // Trigger validation
    this.validateAndSaveCurrentResponses();
  }

  isMatrixRowAnswered(question: QuestionDetail, row: MatrixRow): boolean {
    const controlName = `question_${question.id}`;
    const control = this.sectionForm.get(controlName);

    if (!control) return false;

    const rowValue = control.get(row.id.toString())?.value;
    return rowValue !== null && rowValue !== undefined && rowValue !== '';
  }

  // Pause/Resume functionality
  pauseSurvey(): void {
    const pauseData = {
      reason: 'User requested pause',
      currentQuestionId: this.currentSection?.questions[0]?.id,
      sessionData: {
        currentSectionIndex: this.currentSectionIndex,
        formValues: this.sectionForm.value,
      },
    };

    this.surveyService.pauseSurvey(this.participationId, pauseData).subscribe({
      next: () => {
        this.snackBar.open('Survey paused. You can resume later.', 'Close', {
          duration: 3000,
        });
        this.router.navigate(['/client/surveys']);
      },
      error: (error) => {
        console.error('Error pausing survey:', error);
        this.snackBar.open('Error pausing survey', 'Close', { duration: 3000 });
      },
    });
  }
}
