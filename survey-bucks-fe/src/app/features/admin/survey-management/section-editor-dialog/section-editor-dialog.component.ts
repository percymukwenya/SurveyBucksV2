import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AdminSurveyService } from '../../../../core/services/admin-survey.service';
import { catchError, finalize, of } from 'rxjs';

@Component({
  selector: 'app-section-editor-dialog',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatProgressBarModule
  ],
  templateUrl: './section-editor-dialog.component.html',
  styleUrl: './section-editor-dialog.component.scss'
})
export class SectionEditorDialogComponent {
  sectionForm: FormGroup;
  isEditMode = false;
  saving = false;
  
  constructor(
    private fb: FormBuilder,
    private surveyService: AdminSurveyService,
    public dialogRef: MatDialogRef<SectionEditorDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.sectionForm = this.createSectionForm();
    this.isEditMode = !!data.section;
  }

  ngOnInit(): void {
    if (this.isEditMode) {
      this.sectionForm.patchValue({
        name: this.data.section.name,
        description: this.data.section.description
      });
    }
  }

  createSectionForm(): FormGroup {
    return this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      description: ['', Validators.maxLength(250)]
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.sectionForm.invalid) {
      this.markFormGroupTouched(this.sectionForm);
      return;
    }
    
    this.saving = true;
    
    const sectionData = {
      ...this.sectionForm.value,
      surveyId: this.data.surveyId
    };
    
    if (this.isEditMode) {
      sectionData.id = this.data.section.id;
      
      this.surveyService.updateSection(this.data.section.id, sectionData)
        .pipe(
          catchError(error => {
            console.error('Error updating section', error);
            return of(null);
          }),
          finalize(() => {
            this.saving = false;
          })
        )
        .subscribe(result => {
          if (result !== null) {
            this.dialogRef.close(true);
          }
        });
    } else {
      this.surveyService.createSection(sectionData)
        .pipe(
          catchError(error => {
            console.error('Error creating section', error);
            return of(null);
          }),
          finalize(() => {
            this.saving = false;
          })
        )
        .subscribe(result => {
          if (result !== null) {
            this.dialogRef.close(true);
          }
        });
    }
  }

  markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      
      if ((control as any).controls) {
        this.markFormGroupTouched(control as FormGroup);
      }
    });
  }
}
