import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminSurveyService } from '../../../../core/services/admin-survey.service';
import { catchError, finalize, of } from 'rxjs';
import { ConfirmationDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { CdkDragDrop, moveItemInArray, DragDropModule } from '@angular/cdk/drag-drop';
import { SectionEditorDialogComponent } from '../section-editor-dialog/section-editor-dialog.component';
import { SurveyQuestionListComponent } from '../survey-question-list/survey-question-list.component';

@Component({
  selector: 'app-survey-section-list',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatExpansionModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressBarModule,
    MatTooltipModule,
    DragDropModule,
    SurveyQuestionListComponent
  ],
  templateUrl: './survey-section-list.component.html',
  styleUrl: './survey-section-list.component.scss'
})
export class SurveySectionListComponent {
  @Input() surveyId: number = 0;

  sections: any[] = [];
  loading = false;
  saving = false;

  constructor(
    private surveyService: AdminSurveyService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) { }
  
  ngOnInit(): void {
    if (this.surveyId) {
      this.loadSections();
    }
  }

  loadSections(): void {
    this.loading = true;
    
    this.surveyService.getSurveySections(this.surveyId)
      .pipe(
        catchError(error => {
          console.error('Error loading sections', error);
          this.snackBar.open('Error loading survey sections. Please try again.', 'Close', {
            duration: 5000
          });
          return of([]);
        }),
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe(sections => {
        this.sections = sections;
      });
  }
  
  openSectionDialog(section?: any): void {
    const dialogRef = this.dialog.open(SectionEditorDialogComponent, {
      width: '600px',
      data: {
        surveyId: this.surveyId,
        section: section || null
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadSections(); // Refresh the section list
      }
    });
  }

  deleteSection(section: any): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Section',
        message: `Are you sure you want to delete "${section.name}"? This will also delete all questions in this section.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        isDestructive: true
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.surveyService.deleteSection(section.id)
          .pipe(
            catchError(error => {
              console.error('Error deleting section', error);
              this.snackBar.open('Error deleting section. Please try again.', 'Close', {
                duration: 5000
              });
              return of(null);
            })
          )
          .subscribe(response => {
            if (response !== null) {
              this.snackBar.open('Section deleted successfully!', 'Close', {
                duration: 3000
              });
              this.loadSections(); // Refresh the list
            }
          });
      }
    });
  }

  onSectionDrop(event: CdkDragDrop<any[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }
    
    // Update the local array
    moveItemInArray(this.sections, event.previousIndex, event.currentIndex);
    
    // Prepare the order data
    const sectionOrders = this.sections.map((section, index) => ({
      id: section.id,
      order: index
    }));
    
    // Update order in the backend
    this.saving = true;
    
    this.surveyService.reorderSections(this.surveyId, sectionOrders)
      .pipe(
        catchError(error => {
          console.error('Error reordering sections', error);
          this.snackBar.open('Error saving section order. Please try again.', 'Close', {
            duration: 5000
          });
          
          // Revert to original order by reloading
          this.loadSections();
          return of(null);
        }),
        finalize(() => {
          this.saving = false;
        })
      )
      .subscribe(response => {
        if (response !== null) {
          this.snackBar.open('Section order updated!', 'Close', {
            duration: 2000
          });
        }
      });
  }
}

