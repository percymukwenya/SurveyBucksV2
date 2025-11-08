// src/app/features/admin/surveys/question-editor-dialog/question-editor-dialog.component.ts
import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  FormsModule, 
  ReactiveFormsModule, 
  FormBuilder, 
  FormGroup, 
  FormArray, 
  FormControl,
  AbstractControl,
  Validators 
} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatStepperModule } from '@angular/material/stepper';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { AdminSurveyService } from '../../../../core/services/admin-survey.service';
import { catchError, finalize, forkJoin, of } from 'rxjs';

@Component({
  selector: 'app-question-editor-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    MatStepperModule,
    MatTabsModule,
    MatProgressBarModule,
    MatDividerModule,
    MatChipsModule,
    MatTooltipModule,
    DragDropModule
  ],
  templateUrl: './question-editor-dialog.component.html',
  styleUrls: ['./question-editor-dialog.component.scss']
})
export class QuestionEditorDialogComponent implements OnInit {
  questionForm: FormGroup;
  isEditMode = false;
  saving = false;
  loadingChoices = false;
  loadingMatrixItems = false;
  
  // Selected question type properties
  selectedType: any = null;

  // For logic rules
  loadingLogicRules = false;
  availableQuestions: any[] = [];
  
  constructor(
    private fb: FormBuilder,
    private surveyService: AdminSurveyService,
    public dialogRef: MatDialogRef<QuestionEditorDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.questionForm = this.createQuestionForm();
    this.isEditMode = !!data.question;
  }
  
  ngOnInit(): void {
    // Set initially selected question type if editing
    if (this.isEditMode) {
      this.selectedType = this.data.questionTypes.find(
        (type: any) => type.id === this.data.question.questionTypeId
      );
      this.loadQuestionData();
    }
    
    // Listen for question type changes
    this.questionForm.get('questionTypeId')?.valueChanges.subscribe(typeId => {
      this.selectedType = this.data.questionTypes.find((type: any) => type.id === typeId);
      
      // Reset related form controls based on question type
      this.resetTypeSpecificControls();
      
      // Load choices if editing and question has choices
      if (this.isEditMode && this.data.question && this.selectedType?.hasChoices) {
        this.loadQuestionChoices();
      }
      
      // Load matrix items if editing and question has matrix
      if (this.isEditMode && this.data.question && this.selectedType?.hasMatrix) {
        this.loadMatrixItems();
      }
    });

    // Load available questions for logic rules
    this.loadAvailableQuestions();
    
    // If editing a question, load existing logic rules
    if (this.isEditMode && this.data.question?.id) {
      this.loadLogicRules();
    }
  }
  
  createQuestionForm(): FormGroup {
    return this.fb.group({
      surveySectionId: [this.data.sectionId, Validators.required],
      text: ['', [Validators.required, Validators.maxLength(500)]],
      questionTypeId: ['', Validators.required],
      isMandatory: [true],
      order: [0],
      minValue: [null],
      maxValue: [null],
      validationMessage: [''],
      helpText: [''],
      isScreeningQuestion: [false],
      screeningLogic: [''],
      timeoutInSeconds: [null],
      randomizeChoices: [false],
      choices: this.fb.array([]),
      matrixRows: this.fb.array([]),
      matrixColumns: this.fb.array([]),
      logicRules: this.fb.array([])
    });
  }
  
  loadQuestionData(): void {
    const question = this.data.question;
    
    // Set basic question properties
    this.questionForm.patchValue({
      surveySectionId: this.data.sectionId,
      text: question.text,
      questionTypeId: question.questionTypeId,
      isMandatory: question.isMandatory,
      order: question.order,
      minValue: question.minValue,
      maxValue: question.maxValue,
      validationMessage: question.validationMessage,
      helpText: question.helpText,
      isScreeningQuestion: question.isScreeningQuestion,
      screeningLogic: question.screeningLogic,
      timeoutInSeconds: question.timeoutInSeconds,
      randomizeChoices: question.randomizeChoices
    });
  }

  loadAvailableQuestions(): void {
    // Get all questions from the current section
    this.surveyService.getSectionQuestions(this.data.sectionId)
      .subscribe({
        next: (questions) => {
          if (this.isEditMode) {
            // If editing a question, only show questions that come before this one
            const currentQuestionIndex = questions.findIndex(q => q.id === this.data.question.id);
            this.availableQuestions = questions.filter((q, index) => 
              index < currentQuestionIndex && q.id !== this.data.question.id
            );
          } else {
            // If creating a new question, show all existing questions
            this.availableQuestions = questions.filter(q => 
              !this.data.question || q.id !== this.data.question.id
            );
          }
        },
        error: (error) => {
          console.error('Error loading available questions', error);
        }
      });
  }
  
  resetTypeSpecificControls(): void {
    // Clear choices array
    while (this.choicesArray.length) {
      this.choicesArray.removeAt(0);
    }
    
    // Clear matrix arrays
    while (this.matrixRowsArray.length) {
      this.matrixRowsArray.removeAt(0);
    }
    
    while (this.matrixColumnsArray.length) {
      this.matrixColumnsArray.removeAt(0);
    }
    
    // Reset min/max values if applicable
    if (this.selectedType?.hasMinMaxValues) {
      this.questionForm.patchValue({
        minValue: 1,
        maxValue: 10
      });
    } else {
      this.questionForm.patchValue({
        minValue: null,
        maxValue: null
      });
    }
  }

  loadLogicRules(): void {
    if (!this.isEditMode || !this.data.question?.id) {
      return;
    }
    
    this.loadingLogicRules = true;
    
    this.surveyService.getQuestionLogicRules(this.data.question.id)
      .pipe(
        catchError(error => {
          console.error('Error loading logic rules', error);
          return of([]);
        }),
        finalize(() => {
          this.loadingLogicRules = false;
        })
      )
      .subscribe(rules => {
        // Clear existing rules
        while (this.logicRulesArray.length) {
          this.logicRulesArray.removeAt(0);
        }
        
        // Add each rule to form
        rules.forEach(rule => {
          this.logicRulesArray.push(this.createLogicRuleForm(rule));
        });
      });
  }
  
  loadQuestionChoices(): void {
    this.loadingChoices = true;
    
    this.surveyService.getQuestionChoices(this.data.question.id)
      .pipe(
        catchError(error => {
          console.error('Error loading question choices', error);
          return of([]);
        }),
        finalize(() => {
          this.loadingChoices = false;
        })
      )
      .subscribe(choices => {
        // Clear existing choices
        while (this.choicesArray.length) {
          this.choicesArray.removeAt(0);
        }
        
        // Add each choice to the form array
        choices.forEach(choice => {
          this.choicesArray.push(this.createChoiceFormGroup(choice));
        });
      });
  }
  
  loadMatrixItems(): void {
    this.loadingMatrixItems = true;
    
    // Load both rows and columns
    forkJoin({
      rows: this.surveyService.getMatrixRows(this.data.question.id),
      columns: this.surveyService.getMatrixColumns(this.data.question.id)
    })
      .pipe(
        catchError(error => {
          console.error('Error loading matrix items', error);
          return of({ rows: [], columns: [] });
        }),
        finalize(() => {
          this.loadingMatrixItems = false;
        })
      )
      .subscribe(({ rows, columns }) => {
        // Add matrix data to form
        this.questionForm.setControl('matrixRows', this.fb.array(
          rows.map(row => this.createMatrixRowFormGroup(row))
        ));
        
        this.questionForm.setControl('matrixColumns', this.fb.array(
          columns.map(column => this.createMatrixColumnFormGroup(column))
        ));
      });
  }
  
  createChoiceFormGroup(choice?: any): FormGroup {
    const orderValue = choice?.order ?? this.choicesArray.length;
    console.log('Creating choice form group with order:', orderValue);
    return this.fb.group({
      id: [choice?.id || 0],
      questionId: [this.data.question?.id || 0],
      text: [choice?.text || '', [Validators.required, Validators.maxLength(200)]],
      value: [choice?.value || ''],
      order: [orderValue],
      isExclusiveOption: [choice?.isExclusiveOption || false]
    });
  }
  
  createMatrixRowFormGroup(row?: any): FormGroup {
    return this.fb.group({
      id: [row?.id || 0],
      questionId: [this.data.question?.id || 0],
      text: [row?.text || '', [Validators.required, Validators.maxLength(200)]],
      order: [row?.order || 0]
    });
  }
  
  createMatrixColumnFormGroup(column?: any): FormGroup {
    return this.fb.group({
      id: [column?.id || 0],
      questionId: [this.data.question?.id || 0],
      text: [column?.text || '', [Validators.required, Validators.maxLength(200)]],
      value: [column?.value || '', Validators.required],
      order: [column?.order || 0]
    });
  }

  createLogicRuleForm(rule?: any): FormGroup {
    return this.fb.group({
      id: [rule?.id || 0],
      questionId: [this.data.question?.id || 0],
      logicType: [rule?.logicType || 'ShowIf', Validators.required],
      sourceQuestionId: [rule?.sourceQuestionId || '', Validators.required],
      conditionType: [rule?.conditionType || 'Equals', Validators.required],
      conditionValue: [rule?.conditionValue || ''],
      targetQuestionId: [rule?.targetQuestionId || null],
      targetSectionId: [rule?.targetSectionId || null]
    });
  }
  
  get choicesArray(): FormArray {
    return this.questionForm.get('choices') as FormArray;
  }
  
  get matrixRowsArray(): FormArray {
    const control = this.questionForm.get('matrixRows');
    if (control === null) {
      // Initialize the control if it doesn't exist
      this.questionForm.addControl('matrixRows', this.fb.array([]));
      return this.questionForm.get('matrixRows') as FormArray;
    }
    return control as FormArray;
  }
  
  get matrixColumnsArray(): FormArray {
    const control = this.questionForm.get('matrixColumns');
    if (control === null) {
      // Initialize the control if it doesn't exist
      this.questionForm.addControl('matrixColumns', this.fb.array([]));
      return this.questionForm.get('matrixColumns') as FormArray;
    }
    return control as FormArray;
  }

  get logicRulesArray(): FormArray {
    const control = this.questionForm.get('logicRules');
    if (control === null) {
      // Initialize the control if it doesn't exist
      this.questionForm.addControl('logicRules', this.fb.array([]));
      return this.questionForm.get('logicRules') as FormArray;
    }
    return control as FormArray;
  }
  
  addChoice(): void {
    console.log('Adding new choice. Current choices length:', this.choicesArray.length);
    const newChoice = this.createChoiceFormGroup();
    console.log('Created choice form group:', newChoice.value);
    this.choicesArray.push(newChoice);
    console.log('After adding choice. New choices length:', this.choicesArray.length);
    console.log('Choices array value:', this.choicesArray.value);
  }
  
  removeChoice(index: number): void {
    this.choicesArray.removeAt(index);
  }
  
  addMatrixRow(): void {
    this.matrixRowsArray.push(this.createMatrixRowFormGroup());
  }
  
  removeMatrixRow(index: number): void {
    this.matrixRowsArray.removeAt(index);
  }
  
  addMatrixColumn(): void {
    this.matrixColumnsArray.push(this.createMatrixColumnFormGroup());
  }
  
  removeMatrixColumn(index: number): void {
    this.matrixColumnsArray.removeAt(index);
  }

  addLogicRule(): void {
    this.logicRulesArray.push(this.createLogicRuleForm());
  }
  
  removeLogicRule(index: number): void {
    this.logicRulesArray.removeAt(index);
  }
  
  onChoiceDrop(event: CdkDragDrop<any[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }
    
    // Update the form array
    moveItemInArray(this.choicesArray.controls, event.previousIndex, event.currentIndex);
    
    // Update order values
    this.choicesArray.controls.forEach((control, index) => {
      control.get('order')?.setValue(index);
    });
  }
  
  onMatrixRowDrop(event: CdkDragDrop<any[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }
    
    // Update the form array
    moveItemInArray(this.matrixRowsArray.controls, event.previousIndex, event.currentIndex);
    
    // Update order values
    this.matrixRowsArray.controls.forEach((control, index) => {
      control.get('order')?.setValue(index);
    });
  }
  
  onMatrixColumnDrop(event: CdkDragDrop<any[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }
    
    // Update the form array
    moveItemInArray(this.matrixColumnsArray.controls, event.previousIndex, event.currentIndex);
    
    // Update order values
    this.matrixColumnsArray.controls.forEach((control, index) => {
      control.get('order')?.setValue(index);
    });
  }
  
  onCancel(): void {
    this.dialogRef.close();
  }
  
  onSave(): void {
    if (this.questionForm.invalid) {
      this.markFormGroupTouched(this.questionForm);
      return;
    }
    
    this.saving = true;
    
    const questionData = this.prepareQuestionData();
    
    if (this.isEditMode) {
      this.updateQuestion(questionData);
    } else {
      this.createQuestion(questionData);
    }
  }
  
  prepareQuestionData(): any {
    const formValue = { ...this.questionForm.value };
    
    // Remove choices and matrix arrays from main payload
    delete formValue.choices;
    delete formValue.matrixRows;
    delete formValue.matrixColumns;
    delete formValue.logicRules;
    
    if (this.isEditMode) {
      formValue.id = this.data.question.id;
    }
    
    return formValue;
  }
  
  createQuestion(questionData: any): void {
    this.surveyService.createQuestion(questionData)
      .pipe(
        catchError(error => {
          console.error('Error creating question', error);
          return of(null);
        }),
        finalize(() => {
          this.saving = false;
        })
      )
      .subscribe(result => {
        if (result) {
          // Save related data like choices, matrix items, logic rules
          const saveObservables = [];
          
          // If question has choices, add them
          if (this.selectedType?.hasChoices || this.choicesArray.length > 0) {
            console.log('Question has choices, saving choices for questionId:', result.id);
            console.log('Choices array length:', this.choicesArray.length);
            console.log('Choices array value:', this.choicesArray.value);
            saveObservables.push(this.saveChoices(result.id));
          } else {
            console.log('No choices to save:', {
              hasChoices: this.selectedType?.hasChoices,
              choicesLength: this.choicesArray.length
            });
          } 
          
          // If question has matrix items, add them
          if (this.selectedType?.hasMatrix && 
              this.matrixRowsArray?.length > 0 && 
              this.matrixColumnsArray?.length > 0) {
            saveObservables.push(this.saveMatrixItems(result.id));
          }
          
          // Save logic rules
          if (this.logicRulesArray.length > 0) {
            saveObservables.push(this.saveLogicRules(result.id));
          }
          
          // Wait for all save operations to complete
          if (saveObservables.length > 0) {
            forkJoin(saveObservables)
              .pipe(
                catchError(error => {
                  console.error('Error saving related data', error);
                  return of(null);
                }),
                finalize(() => {
                  this.dialogRef.close(true);
                })
              )
              .subscribe();
          } else {
            this.dialogRef.close(true);
          }
        }
      });
  }
  
  updateQuestion(questionData: any): void {
    console.log('Updating question with data:', questionData);
    this.surveyService.updateQuestion(this.data.question.id, questionData)
      .pipe(
        catchError(error => {
          console.error('Error updating question', error);
          return of(null);
        }),
        finalize(() => {
          this.saving = false;
        })
      )
      .subscribe(result => {
        console.log('Question update result:', result);
        console.log('Selected type:', this.selectedType);
        console.log('Selected type hasChoices:', this.selectedType?.hasChoices);
        console.log('Choices array length:', this.choicesArray.length);
        if (result) {
          // Save related data like choices, matrix items, logic rules
          const saveObservables = [];
          
          // If question has choices, update them
          if (this.selectedType?.hasChoices || this.choicesArray.length > 0) {
            console.log('Question has choices, updating choices for questionId:', this.data.question.id);
            console.log('Choices array length:', this.choicesArray.length);
            console.log('Choices array value:', this.choicesArray.value);
            saveObservables.push(this.saveChoices(this.data.question.id));
          } else {
            console.log('No choices to update:', {
              hasChoices: this.selectedType?.hasChoices,
              choicesLength: this.choicesArray.length,
              selectedType: this.selectedType
            });
          } 
          
          // If question has matrix items, update them
          if (this.selectedType?.hasMatrix) {
            saveObservables.push(this.saveMatrixItems(this.data.question.id));
          }
          
          // Save logic rules
          if (this.logicRulesArray.length > 0) {
            saveObservables.push(this.saveLogicRules(this.data.question.id));
          }
          
          // Wait for all save operations to complete
          if (saveObservables.length > 0) {
            forkJoin(saveObservables)
              .pipe(
                catchError(error => {
                  console.error('Error saving related data', error);
                  return of(null);
                }),
                finalize(() => {
                  this.dialogRef.close(true);
                })
              )
              .subscribe();
          } else {
            this.dialogRef.close(true);
          }
        }
      });
  }
  
  saveChoices(questionId: number): any {
    // Prepare choices with the question ID
    const choices = this.choicesArray.value.map((choice: any, index: number) => ({
      ...choice,
      questionId: questionId,
      order: index
    }));
    
    // Create observables for create, update, delete operations
    const createObservables = choices
      .filter((choice: any) => !choice.id || choice.id === 0)
      .map((choice: any) => {
        console.log('Creating choice:', choice);
        return this.surveyService.addQuestionChoice(choice);
      });
    
    const updateObservables = choices
      .filter((choice: any) => choice.id && choice.id > 0)
      .map((choice: any) => {
        console.log('Updating choice:', choice);
        return this.surveyService.updateQuestionChoice(choice.id, choice);
      });
    
    // If no operations needed, return empty observable
    if (createObservables.length === 0 && updateObservables.length === 0) {
      console.log('No choice operations to perform');
      return of(true);
    }
    
    // Return forkJoin of all operations
    return forkJoin([...createObservables, ...updateObservables]);
  }
  
  saveMatrixItems(questionId: number): any {
    // Prepare matrix rows and columns with the question ID
    const rows = this.matrixRowsArray?.value.map((row: any, index: number) => ({
      ...row,
      questionId: questionId,
      order: index
    }));
    
    const columns = this.matrixColumnsArray?.value.map((column: any, index: number) => ({
      ...column,
      questionId: questionId,
      order: index
    }));
    
    // Create observables for row operations
    const rowObservables = rows
      .map((row: any) => {
        if (row.id && row.id > 0) {
          // Update existing row
          return this.surveyService.updateMatrixRow(row.id, row);
        } else {
          // Create new row
          return this.surveyService.addMatrixRow(row);
        }
      });
    
    // Create observables for column operations
    const columnObservables = columns
      .map((column: any) => {
        if (column.id && column.id > 0) {
          // Update existing column
          return this.surveyService.updateMatrixColumn(column.id, column);
        } else {
          // Create new column
          return this.surveyService.addMatrixColumn(column);
        }
      });
    
    // Return forkJoin of all operations
    return forkJoin([...rowObservables, ...columnObservables]);
  }

  saveLogicRules(questionId: number): any {
    const logicRules = this.logicRulesArray.value.map((rule: any) => ({
      ...rule,
      questionId: questionId
    }));
    
    // Create observables for operations
    const createObservables = logicRules
      .filter((rule: any) => !rule.id || rule.id === 0)
      .map((rule: any) => this.surveyService.addQuestionLogicRule(rule));
    
    const updateObservables = logicRules
      .filter((rule: any) => rule.id && rule.id > 0)
      .map((rule: any) => this.surveyService.updateQuestionLogicRule(rule.id, rule));
    
    // Return forkJoin of all operations
    return forkJoin([...createObservables, ...updateObservables]);
  }
  
  // Helper method to mark all controls as touched to trigger validation
  markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      } else if (control instanceof FormArray) {
        control.controls.forEach(arrayControl => {
          if (arrayControl instanceof FormGroup) {
            this.markFormGroupTouched(arrayControl);
          } else {
            arrayControl.markAsTouched();
          }
        });
      }
    });
  }

  // Helper method to determine if the condition value field should be shown
  showConditionValueField(conditionType: string): boolean {
    return !['Selected', 'NotSelected'].includes(conditionType);
  }
}