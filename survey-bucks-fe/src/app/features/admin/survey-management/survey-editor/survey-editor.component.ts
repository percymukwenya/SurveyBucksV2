import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AdminSurveyService } from '../../../../core/services/admin-survey.service';
import { catchError, of, finalize } from 'rxjs';
import { SurveySectionListComponent } from '../survey-section-list/survey-section-list.component';
import { SurveyTargetingComponent } from "../survey-targeting/survey-targeting.component";

@Component({
  selector: 'app-survey-editor',
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
    MatDatepickerModule,
    MatNativeDateModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatStepperModule,
    MatTabsModule,
    MatProgressBarModule,
    SurveySectionListComponent,
    SurveyTargetingComponent
],
  templateUrl: './survey-editor.component.html',
  styleUrl: './survey-editor.component.scss'
})
export class SurveyEditorComponent {
surveyForm: FormGroup;
  isEditMode = false;
  surveyId: number | null = null;
  saving = false;
  loading = false;
  industries: string[] = [
    'Technology', 'Healthcare', 'Finance', 'Education', 'Retail', 
    'Manufacturing', 'Entertainment', 'Hospitality', 'Transportation', 'Other'
  ];
  
  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private surveyService: AdminSurveyService,
    private snackBar: MatSnackBar
  ) {
    this.surveyForm = this.createSurveyForm();
  }
  
  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.isEditMode = true;
        this.surveyId = +params['id'];
        this.loadSurveyData(this.surveyId);
      }
    });
  }

  createSurveyForm(): FormGroup {
    return this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      description: ['', [Validators.required, Validators.maxLength(500)]],
      openingDateTime: [new Date(), Validators.required],
      closingDateTime: [this.getDefaultClosingDate(), Validators.required],
      durationInSeconds: [1800, [Validators.required, Validators.min(60)]],
      companyName: ['', Validators.maxLength(150)],
      companyDescription: ['', Validators.maxLength(250)],
      industry: [''],
      minQuestions: [1, [Validators.required, Validators.min(1)]],
      maxTimeInMins: [30, [Validators.required, Validators.min(1)]],
      requireAllQuestions: [true],
      isActive: [true]
    });
  }

  getDefaultClosingDate(): Date {
    const date = new Date();
    date.setDate(date.getDate() + 30); // Default to 30 days from now
    return date;
  }

  loadSurveyData(surveyId: number): void {
    this.loading = true;
    
    this.surveyService.getSurveyById(surveyId)
      .pipe(
        catchError(error => {
          console.error('Error loading survey', error);
          this.snackBar.open('Error loading survey data. Please try again.', 'Close', {
            duration: 5000
          });
          return of(null);
        }),
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe(survey => {
        if (survey) {
          // Convert string dates to Date objects
          if (survey.openingDateTime) {
            survey.openingDateTime = new Date(survey.openingDateTime);
          }
          if (survey.closingDateTime) {
            survey.closingDateTime = new Date(survey.closingDateTime);
          }
          
          this.surveyForm.patchValue(survey);
        }
      });
  }

  saveSurvey(): void {
    if (this.surveyForm.invalid) {
      this.markFormGroupTouched(this.surveyForm);
      this.snackBar.open('Please fix the validation errors before saving.', 'Close', {
        duration: 5000
      });
      return;
    }
    
    this.saving = true;
    const surveyData = { ...this.surveyForm.value };
    
    // Convert any Date objects to ISO strings before sending to API
    if (surveyData.openingDateTime instanceof Date) {
      surveyData.openingDateTime = surveyData.openingDateTime.toISOString();
    }
    if (surveyData.closingDateTime instanceof Date) {
      surveyData.closingDateTime = surveyData.closingDateTime.toISOString();
    }
    
    const saveOperation = this.isEditMode
      ? this.surveyService.updateSurvey(this.surveyId!, surveyData)
      : this.surveyService.createSurvey(surveyData);
    
    saveOperation
      .pipe(
        catchError(error => {
          console.error('Error saving survey', error);
          this.snackBar.open('Error saving survey. Please try again.', 'Close', {
            duration: 5000
          });
          return of(null);
        }),
        finalize(() => {
          this.saving = false;
        })
      )
      .subscribe(result => {
        if (result) {
          const message = this.isEditMode
            ? 'Survey updated successfully!'
            : 'Survey created successfully!';
          
          this.snackBar.open(message, 'Close', {
            duration: 3000
          });
          
          // If we're creating a new survey, redirect to edit page with sections
          if (!this.isEditMode && result.id) {
            this.router.navigate(['/admin/surveys/edit', result.id]);
          } else if (this.isEditMode) {
            // Reload the data to refresh any changes
            this.loadSurveyData(this.surveyId!);
          }
        }
      });
  }

  // Helper method to mark all controls as touched to trigger validation
  markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      
      if ((control as any).controls) {
        this.markFormGroupTouched(control as FormGroup);
      }
    });
  }
  
  // Cancel and go back to survey list
  cancel(): void {
    this.router.navigate(['/admin/surveys']);
  }

  
}
