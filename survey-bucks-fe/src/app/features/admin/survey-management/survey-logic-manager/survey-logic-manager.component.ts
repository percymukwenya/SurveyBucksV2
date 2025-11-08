import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';
import { AdminSurveyService } from '../../../../core/services/admin-survey.service';
import { SurveyBranchingService } from '../../../../core/services/survey-branching.service';

export interface QuestionLogicRule {
  id?: number;
  questionId: number;
  logicType: 'show_hide' | 'jump' | 'termination';
  conditionType: 'equals' | 'not_equals' | 'greater_than' | 'less_than' | 'between' | 'contains' | 'in_list' | 'regex_match';
  conditionValue: string;
  conditionValue2?: string;
  actionType: 'show_question' | 'hide_question' | 'show_questions' | 'hide_questions' | 'jump_to_section' | 'jump_to_question' | 'end_survey' | 'disqualify';
  targetQuestionId?: number;
  targetQuestionIds?: number[];
  targetSectionId?: number;
  message?: string;
  isActive: boolean;
  order: number;
}

export interface SurveyQuestion {
  id: number;
  questionText: string;
  questionType: string;
  sectionId: number;
  sectionName: string;
  order: number;
  hasLogic: boolean;
}

export interface LogicTemplate {
  id: string;
  name: string;
  description: string;
  iconName: string;
  logicType: 'show_hide' | 'jump' | 'termination';
  template: Partial<QuestionLogicRule>;
}

@Component({
  selector: 'app-survey-logic-manager',
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTableModule,
    MatTabsModule,
    MatChipsModule,
    MatMenuModule,
    MatTooltipModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule
  ],
  templateUrl: './survey-logic-manager.component.html',
  styleUrl: './survey-logic-manager.component.scss'
})
export class SurveyLogicManagerComponent implements OnInit {
  surveyId!: number;
  survey: any;
  questions: SurveyQuestion[] = [];
  logicRules: QuestionLogicRule[] = [];
  selectedQuestion: SurveyQuestion | null = null;
  selectedQuestionRules: QuestionLogicRule[] = [];
  
  loading = false;
  saving = false;
  validating = false;
  
  // Form for creating/editing logic rules
  logicForm: FormGroup;
  editingRule: QuestionLogicRule | null = null;
  
  // Logic templates for quick rule creation
  logicTemplates: LogicTemplate[] = [
    {
      id: 'skip_if_yes',
      name: 'Skip if Yes',
      description: 'Hide follow-up questions when user answers "Yes"',
      iconName: 'skip_next',
      logicType: 'show_hide',
      template: {
        conditionType: 'equals',
        conditionValue: 'Yes',
        actionType: 'hide_questions',
        logicType: 'show_hide'
      }
    },
    {
      id: 'show_if_no',
      name: 'Show if No',
      description: 'Show additional questions when user answers "No"',
      iconName: 'visibility',
      logicType: 'show_hide',
      template: {
        conditionType: 'equals',
        conditionValue: 'No',
        actionType: 'show_questions',
        logicType: 'show_hide'
      }
    },
    {
      id: 'age_screening',
      name: 'Age Screening',
      description: 'Disqualify users under 18',
      iconName: 'block',
      logicType: 'termination',
      template: {
        conditionType: 'less_than',
        conditionValue: '18',
        actionType: 'disqualify',
        logicType: 'termination',
        message: 'Sorry, you must be 18 or older to participate in this survey.'
      }
    },
    {
      id: 'section_jump',
      name: 'Section Jump',
      description: 'Jump to different section based on answer',
      iconName: 'call_made',
      logicType: 'jump',
      template: {
        conditionType: 'equals',
        actionType: 'jump_to_section',
        logicType: 'jump'
      }
    }
  ];
  
  // Available condition types with descriptions
  conditionTypes = [
    { value: 'equals', label: 'Equals', description: 'Answer exactly matches value' },
    { value: 'not_equals', label: 'Not Equals', description: 'Answer does not match value' },
    { value: 'greater_than', label: 'Greater Than', description: 'Numeric answer is greater than value' },
    { value: 'less_than', label: 'Less Than', description: 'Numeric answer is less than value' },
    { value: 'between', label: 'Between', description: 'Numeric answer is between two values' },
    { value: 'contains', label: 'Contains', description: 'Answer contains specified text' },
    { value: 'in_list', label: 'In List', description: 'Answer matches any value in comma-separated list' },
    { value: 'regex_match', label: 'Regex Match', description: 'Answer matches regular expression pattern' }
  ];
  
  // Available action types
  actionTypes = [
    { value: 'show_question', label: 'Show Question', requiresTarget: 'question' },
    { value: 'hide_question', label: 'Hide Question', requiresTarget: 'question' },
    { value: 'show_questions', label: 'Show Multiple Questions', requiresTarget: 'questions' },
    { value: 'hide_questions', label: 'Hide Multiple Questions', requiresTarget: 'questions' },
    { value: 'jump_to_section', label: 'Jump to Section', requiresTarget: 'section' },
    { value: 'jump_to_question', label: 'Jump to Question', requiresTarget: 'question' },
    { value: 'end_survey', label: 'End Survey', requiresTarget: 'none' },
    { value: 'disqualify', label: 'Disqualify Participant', requiresTarget: 'none' }
  ];
  
  displayedColumns: string[] = ['order', 'condition', 'action', 'status', 'actions'];
  
  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private adminSurveyService: AdminSurveyService,
    private branchingService: SurveyBranchingService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {
    this.logicForm = this.createLogicForm();
  }
  
  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.surveyId = +params['id'];
        this.loadSurveyData();
      }
    });
  }
  
  createLogicForm(): FormGroup {
    return this.fb.group({
      logicType: ['show_hide', Validators.required],
      conditionType: ['equals', Validators.required],
      conditionValue: ['', Validators.required],
      conditionValue2: [''],
      actionType: ['show_question', Validators.required],
      targetQuestionId: [''],
      targetQuestionIds: [[]],
      targetSectionId: [''],
      message: [''],
      isActive: [true],
      order: [1, [Validators.required, Validators.min(1)]]
    });
  }
  
  loadSurveyData(): void {
    this.loading = true;
    
    this.adminSurveyService.getSurveyById(this.surveyId)
      .pipe(
        switchMap(survey => {
          this.survey = survey;
          return this.adminSurveyService.getSurveyQuestions(this.surveyId);
        }),
        switchMap(questions => {
          this.questions = questions.map(q => ({
            ...q,
            hasLogic: false // Will be updated when we load logic rules
          }));
          return this.loadLogicRules();
        }),
        catchError(error => {
          console.error('Error loading survey data', error);
          this.snackBar.open('Error loading survey data. Please try again.', 'Close', {
            duration: 5000
          });
          return of(null);
        }),
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe();
  }
  
  loadLogicRules() {
    // TODO: Create API endpoint for getting all survey logic rules
    // For now, simulate with empty array
    return of([]);
  }
  
  selectQuestion(question: SurveyQuestion): void {
    this.selectedQuestion = question;
    this.selectedQuestionRules = this.logicRules.filter(rule => rule.questionId === question.id);
  }
  
  applyTemplate(template: LogicTemplate): void {
    if (!this.selectedQuestion) {
      this.snackBar.open('Please select a question first.', 'Close', { duration: 3000 });
      return;
    }
    
    // Reset form and apply template values
    this.logicForm.reset();
    this.logicForm.patchValue({
      ...template.template,
      order: this.getNextOrderForQuestion(this.selectedQuestion.id)
    });
    
    this.editingRule = null;
  }
  
  getNextOrderForQuestion(questionId: number): number {
    const questionRules = this.logicRules.filter(rule => rule.questionId === questionId);
    return questionRules.length > 0 ? Math.max(...questionRules.map(rule => rule.order)) + 1 : 1;
  }
  
  addRule(): void {
    if (!this.selectedQuestion) {
      this.snackBar.open('Please select a question first.', 'Close', { duration: 3000 });
      return;
    }
    
    if (this.logicForm.invalid) {
      this.markFormGroupTouched(this.logicForm);
      return;
    }
    
    const newRule: QuestionLogicRule = {
      questionId: this.selectedQuestion.id,
      ...this.logicForm.value
    };
    
    this.saveRule(newRule);
  }
  
  editRule(rule: QuestionLogicRule): void {
    this.editingRule = rule;
    this.logicForm.patchValue(rule);
  }
  
  updateRule(): void {
    if (!this.editingRule) return;
    
    if (this.logicForm.invalid) {
      this.markFormGroupTouched(this.logicForm);
      return;
    }
    
    const updatedRule: QuestionLogicRule = {
      ...this.editingRule,
      ...this.logicForm.value
    };
    
    this.saveRule(updatedRule);
  }
  
  saveRule(rule: QuestionLogicRule): void {
    this.saving = true;
    
    // TODO: Implement API call to save logic rule
    // Simulate successful save for now
    setTimeout(() => {
      if (rule.id) {
        // Update existing rule
        const index = this.logicRules.findIndex(r => r.id === rule.id);
        if (index !== -1) {
          this.logicRules[index] = rule;
        }
      } else {
        // Add new rule
        rule.id = Date.now(); // Temporary ID
        this.logicRules.push(rule);
      }
      
      // Update selected question rules
      if (this.selectedQuestion) {
        this.selectedQuestionRules = this.logicRules.filter(r => r.questionId === this.selectedQuestion!.id);
        
        // Update question hasLogic flag
        this.selectedQuestion.hasLogic = this.selectedQuestionRules.length > 0;
      }
      
      // Reset form
      this.logicForm.reset();
      this.logicForm.patchValue({ logicType: 'show_hide', conditionType: 'equals', actionType: 'show_question', isActive: true, order: 1 });
      this.editingRule = null;
      
      this.saving = false;
      this.snackBar.open('Logic rule saved successfully!', 'Close', { duration: 3000 });
    }, 1000);
  }
  
  deleteRule(rule: QuestionLogicRule): void {
    if (confirm('Are you sure you want to delete this logic rule?')) {
      const index = this.logicRules.findIndex(r => r.id === rule.id);
      if (index !== -1) {
        this.logicRules.splice(index, 1);
        
        // Update selected question rules
        if (this.selectedQuestion) {
          this.selectedQuestionRules = this.logicRules.filter(r => r.questionId === this.selectedQuestion!.id);
          this.selectedQuestion.hasLogic = this.selectedQuestionRules.length > 0;
        }
        
        this.snackBar.open('Logic rule deleted successfully!', 'Close', { duration: 3000 });
      }
    }
  }
  
  validateSurveyFlow(): void {
    this.validating = true;
    
    this.branchingService.validateSurveyFlow(this.surveyId).subscribe({
      next: (isValid) => {
        this.validating = false;
        if (isValid) {
          this.snackBar.open('Survey flow validation passed! No circular references detected.', 'Close', {
            duration: 5000,
            panelClass: ['success-snackbar']
          });
        } else {
          this.snackBar.open('Survey flow validation failed! Circular references or invalid logic detected.', 'Close', {
            duration: 5000,
            panelClass: ['error-snackbar']
          });
        }
      },
      error: (error) => {
        this.validating = false;
        console.error('Flow validation error:', error);
        this.snackBar.open('Error validating survey flow. Please try again.', 'Close', {
          duration: 5000
        });
      }
    });
  }
  
  getConditionDescription(rule: QuestionLogicRule): string {
    const conditionType = this.conditionTypes.find(ct => ct.value === rule.conditionType);
    let description = conditionType ? conditionType.label : rule.conditionType;
    
    if (rule.conditionType === 'between') {
      return `${description} ${rule.conditionValue} and ${rule.conditionValue2}`;
    }
    
    return `${description} "${rule.conditionValue}"`;
  }
  
  getActionDescription(rule: QuestionLogicRule): string {
    const actionType = this.actionTypes.find(at => at.value === rule.actionType);
    let description = actionType ? actionType.label : rule.actionType;
    
    if (rule.targetQuestionId) {
      const targetQuestion = this.questions.find(q => q.id === rule.targetQuestionId);
      if (targetQuestion) {
        description += `: ${targetQuestion.questionText.substring(0, 30)}...`;
      }
    } else if (rule.targetQuestionIds && rule.targetQuestionIds.length > 0) {
      description += `: ${rule.targetQuestionIds.length} questions`;
    } else if (rule.targetSectionId) {
      description += `: Section ${rule.targetSectionId}`;
    }
    
    return description;
  }
  
  onConditionTypeChange(): void {
    const conditionType = this.logicForm.get('conditionType')?.value;
    
    // Reset condition values when type changes
    this.logicForm.patchValue({
      conditionValue: '',
      conditionValue2: ''
    });
    
    // Update validators based on condition type
    const conditionValue2Control = this.logicForm.get('conditionValue2');
    if (conditionType === 'between') {
      conditionValue2Control?.setValidators([Validators.required]);
    } else {
      conditionValue2Control?.clearValidators();
    }
    conditionValue2Control?.updateValueAndValidity();
  }
  
  onActionTypeChange(): void {
    const actionType = this.logicForm.get('actionType')?.value;
    const actionConfig = this.actionTypes.find(at => at.value === actionType);
    
    // Reset target fields
    this.logicForm.patchValue({
      targetQuestionId: '',
      targetQuestionIds: [],
      targetSectionId: ''
    });
    
    // Update validators based on action type requirements
    const targetQuestionIdControl = this.logicForm.get('targetQuestionId');
    const targetQuestionIdsControl = this.logicForm.get('targetQuestionIds');
    const targetSectionIdControl = this.logicForm.get('targetSectionId');
    
    // Clear all validators first
    targetQuestionIdControl?.clearValidators();
    targetQuestionIdsControl?.clearValidators();
    targetSectionIdControl?.clearValidators();
    
    // Set validators based on requirements
    if (actionConfig?.requiresTarget === 'question') {
      targetQuestionIdControl?.setValidators([Validators.required]);
    } else if (actionConfig?.requiresTarget === 'questions') {
      targetQuestionIdsControl?.setValidators([Validators.required]);
    } else if (actionConfig?.requiresTarget === 'section') {
      targetSectionIdControl?.setValidators([Validators.required]);
    }
    
    // Update validity
    targetQuestionIdControl?.updateValueAndValidity();
    targetQuestionIdsControl?.updateValueAndValidity();
    targetSectionIdControl?.updateValueAndValidity();
  }
  
  cancelEdit(): void {
    this.logicForm.reset();
    this.logicForm.patchValue({ logicType: 'show_hide', conditionType: 'equals', actionType: 'show_question', isActive: true, order: 1 });
    this.editingRule = null;
  }
  
  markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if ((control as any).controls) {
        this.markFormGroupTouched(control as FormGroup);
      }
    });
  }
  
  trackByQuestionId(index: number, question: SurveyQuestion): number {
    return question.id;
  }
  
  onQuestionSelectionChange(question: SurveyQuestion, selected: boolean): void {
    const currentIds = this.logicForm.get('targetQuestionIds')?.value || [];
    
    if (selected) {
      if (!currentIds.includes(question.id)) {
        this.logicForm.patchValue({
          targetQuestionIds: [...currentIds, question.id]
        });
      }
    } else {
      this.logicForm.patchValue({
        targetQuestionIds: currentIds.filter((id: number) => id !== question.id)
      });
    }
  }
  
  goBack(): void {
    this.router.navigate(['/admin/surveys/edit', this.surveyId]);
  }
}