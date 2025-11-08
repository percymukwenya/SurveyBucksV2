import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, FormArray, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatSliderModule } from '@angular/material/slider';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { QuestionDetail, MatrixRow, QuestionResponseChoice } from '../../../core/services/survey.service';

@Component({
  selector: 'app-question-renderer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatRadioModule,
    MatCheckboxModule,
    MatSelectModule,
    MatSliderModule,
    MatDatepickerModule,
    MatIconModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  templateUrl: './question-renderer.component.html',
  styleUrl: './question-renderer.component.scss'
})
export class QuestionRendererComponent implements OnInit {
  @Input() question!: QuestionDetail;
  @Input() formGroup!: FormGroup;
  @Input() questionErrors: string[] = [];
  @Input() showValidationErrors: boolean = true;
  
  @Output() multipleChoiceChange = new EventEmitter<{question: QuestionDetail, choiceIndex: number, event: any}>();
  @Output() responseChange = new EventEmitter<{questionId: number, value: any}>();

  controlName: string = '';

  constructor(private snackBar: MatSnackBar) {}

  ngOnInit(): void {
    this.controlName = `question_${this.question.id}`;
  }

  get questionControl(): FormControl {
    return this.formGroup.get(this.controlName) as FormControl;
  }

  onMultipleChoiceChange(choiceIndex: number, event: any): void {
    if (!this.question.responseChoices) return;
    
    const formArray = this.formGroup.get(this.controlName) as FormArray;
    const selectedChoice = this.question.responseChoices[choiceIndex];

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
      this.question.responseChoices.forEach((choice, index) => {
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

    // Emit change event
    this.multipleChoiceChange.emit({
      question: this.question,
      choiceIndex,
      event
    });

    this.responseChange.emit({
      questionId: this.question.id,
      value: formArray.value
    });
  }

  onValueChange(value: any): void {
    this.responseChange.emit({
      questionId: this.question.id,
      value
    });
  }

  isMatrixRowAnswered(row: MatrixRow): boolean {
    const control = this.questionControl;
    if (!control) return false;

    const rowValue = control.get(row.id.toString())?.value;
    return rowValue !== null && rowValue !== undefined && rowValue !== '';
  }

  getUnansweredMatrixRows(): MatrixRow[] {
    if (!this.question.matrixRows) return [];

    return this.question.matrixRows.filter((row) => {
      const rowValue = this.questionControl?.get(row.id.toString())?.value;
      return !rowValue || rowValue === null || rowValue === undefined;
    });
  }

  isQuestionAnswered(): boolean {
    const control = this.questionControl;
    if (!control) return false;

    switch (this.question.questionTypeName) {
      case 'MultipleChoice':
        return (control.value as boolean[])?.some((v) => v === true) || false;

      case 'Matrix':
        const matrixValue = control.value;
        return (
          matrixValue &&
          Object.keys(matrixValue).some((key) => matrixValue[key] !== null)
        );

      default:
        return (
          control.value !== null &&
          control.value !== undefined &&
          control.value !== ''
        );
    }
  }

  getQuestionIcon(): string {
    switch (this.question.questionTypeName) {
      case 'ShortText':
      case 'LongText':
        return 'edit';
      case 'Email':
        return 'email';
      case 'Phone':
        return 'phone';
      case 'NumberInput':
        return 'numbers';
      case 'SingleChoice':
        return 'radio_button_checked';
      case 'MultipleChoice':
        return 'check_box';
      case 'Dropdown':
        return 'arrow_drop_down';
      case 'Rating':
        return 'star_rate';
      case 'Slider':
        return 'tune';
      case 'Date':
        return 'calendar_today';
      case 'Matrix':
        return 'grid_on';
      case 'YesNo':
        return 'help';
      default:
        return 'help_outline';
    }
  }
}
