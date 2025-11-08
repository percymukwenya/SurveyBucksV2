import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminSurveyService } from '../../../../core/services/admin-survey.service';
import { catchError, finalize, of } from 'rxjs';
import { ConfirmationDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { QuestionEditorDialogComponent } from '../question-editor-dialog/question-editor-dialog.component';

@Component({
  selector: 'app-survey-question-list',
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatDialogModule,
    MatSnackBarModule,
    MatMenuModule,
    DragDropModule
  ],
  templateUrl: './survey-question-list.component.html',
  styleUrl: './survey-question-list.component.scss'
})
export class SurveyQuestionListComponent {
  @Input() sectionId: number = 0;
  
  questions: any[] = [];
  questionTypes: any[] = [];
  loading = false;
  saving = false;
  
  constructor(
    private surveyService: AdminSurveyService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) { }
  
  ngOnInit(): void {
    if (this.sectionId) {
      this.loadQuestionTypes();
      this.loadQuestions();
    }
  }

  loadQuestionTypes(): void {
    this.surveyService.getQuestionTypes().subscribe(types => {
      this.questionTypes = types;
    });
  }
  
  loadQuestions(): void {
    this.loading = true;
    
    this.surveyService.getSectionQuestions(this.sectionId)
      .pipe(
        catchError(error => {
          console.error('Error loading questions', error);
          this.snackBar.open('Error loading questions. Please try again.', 'Close', {
            duration: 5000
          });
          return of([]);
        }),
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe(questions => {
        this.questions = questions;
      });
  }

  openQuestionDialog(question?: any): void {
    const dialogRef = this.dialog.open(QuestionEditorDialogComponent, {
      width: '800px',
      data: {
        sectionId: this.sectionId,
        questionTypes: this.questionTypes,
        question: question || null
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadQuestions(); // Refresh the questions list
      }
    });
  }

  deleteQuestion(question: any): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Question',
        message: `Are you sure you want to delete this question? This action cannot be undone.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        isDestructive: true
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.surveyService.deleteQuestion(question.id)
          .pipe(
            catchError(error => {
              console.error('Error deleting question', error);
              this.snackBar.open('Error deleting question. Please try again.', 'Close', {
                duration: 5000
              });
              return of(null);
            })
          )
          .subscribe(response => {
            if (response !== null) {
              this.snackBar.open('Question deleted successfully!', 'Close', {
                duration: 3000
              });
              this.loadQuestions(); // Refresh the list
            }
          });
      }
    });
  }

  onQuestionDrop(event: CdkDragDrop<any[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }
    
    // Update the local array
    moveItemInArray(this.questions, event.previousIndex, event.currentIndex);
    
    // Prepare the order data
    const questionOrders = this.questions.map((question, index) => ({
      id: question.id,
      order: index
    }));
    
    // Update order in the backend
    this.saving = true;
    
    this.surveyService.reorderQuestions(this.sectionId, questionOrders)
      .pipe(
        catchError(error => {
          console.error('Error reordering questions', error);
          this.snackBar.open('Error saving question order. Please try again.', 'Close', {
            duration: 5000
          });
          
          // Revert to original order by reloading
          this.loadQuestions();
          return of(null);
        }),
        finalize(() => {
          this.saving = false;
        })
      )
      .subscribe(response => {
        if (response !== null) {
          this.snackBar.open('Question order updated!', 'Close', {
            duration: 2000
          });
        }
      });
  }

  getQuestionTypeLabel(typeId: number): string {
    const questionType = this.questionTypes.find(type => type.id === typeId);
    return questionType ? questionType.name : 'Unknown';
  }
  
  getQuestionTypeIcon(typeId: number): string {
    // Map question type IDs to appropriate Material icons
    switch (typeId) {
      case 1: return 'short_text'; // Short text
      case 2: return 'subject'; // Long text
      case 3: return 'radio_button_checked'; // Single choice
      case 4: return 'check_box'; // Multiple choice
      case 5: return 'linear_scale'; // Rating scale
      case 6: return 'date_range'; // Date
      case 7: return 'grid_on'; // Matrix
      case 8: return 'image'; // Image selection
      case 9: return 'file_upload'; // File upload
      case 10: return 'location_on'; // Location
      default: return 'help_outline';
    }
  }
}
