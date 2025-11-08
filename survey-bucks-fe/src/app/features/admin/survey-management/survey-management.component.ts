// src/app/features/admin/surveys/survey-management/survey-management.component.ts
import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ConfirmationDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { AdminSurveyService } from '../../../core/services/admin-survey.service';
import { MatProgressBar } from '@angular/material/progress-bar';

@Component({
  selector: 'app-survey-management',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatMenuModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressBar
  ],
  templateUrl: './survey-management.component.html',
  styleUrls: ['./survey-management.component.scss']
})
export class SurveyManagementComponent implements OnInit {
  surveys: any[] = [];
  filteredSurveys: any[] = [];
  loading: boolean = true;
  
  displayedColumns: string[] = [
    'name', 
    'status', 
    'company', 
    'industry', 
    'created', 
    'completions', 
    'actions'
  ];
  
  searchControl = new FormControl('');
  statusFilter = new FormControl('');
  industryFilter = new FormControl('');
  
  statusOptions = [
    { value: 'draft', label: 'Draft' },
    { value: 'active', label: 'Active' },
    { value: 'paused', label: 'Paused' },
    { value: 'completed', label: 'Completed' },
    { value: 'archived', label: 'Archived' }
  ];
  
  industryOptions: string[] = [];
  
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatTable) table!: MatTable<any>;
  
  constructor(
    private adminSurveyService: AdminSurveyService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) { }
  
  ngOnInit(): void {
    this.loadSurveys();
    
    this.searchControl.valueChanges.subscribe(() => {
      this.applyFilters();
    });
    
    this.statusFilter.valueChanges.subscribe(() => {
      this.applyFilters();
    });
    
    this.industryFilter.valueChanges.subscribe(() => {
      this.applyFilters();
    });
  }
  
  loadSurveys(): void {
    this.loading = true;
    
    this.adminSurveyService.getAllSurveys().subscribe({
      next: (surveys: any[]) => {
        this.surveys = surveys;
        this.filteredSurveys = [...this.surveys];
        
        // Extract unique industries for filter
        const industries = new Set<string>();
        this.surveys.forEach(survey => {
          if (survey.industry) {
            industries.add(survey.industry);
          }
        });
        this.industryOptions = Array.from(industries).sort();
        
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error loading surveys', error);
        this.loading = false;
      }
    });
  }
  
  applyFilters(): void {
    const searchTerm = this.searchControl.value?.toLowerCase() || '';
    const statusFilter = this.statusFilter.value || '';
    const industryFilter = this.industryFilter.value || '';
    
    this.filteredSurveys = this.surveys.filter(survey => {
      // Search term filter
      const matchesSearch = !searchTerm || 
        survey.name.toLowerCase().includes(searchTerm) ||
        survey.description?.toLowerCase().includes(searchTerm) ||
        survey.companyName?.toLowerCase().includes(searchTerm);
      
      // Status filter
      const matchesStatus = !statusFilter || survey.status.toLowerCase() === statusFilter;
      
      // Industry filter
      const matchesIndustry = !industryFilter || survey.industry === industryFilter;
      
      return matchesSearch && matchesStatus && matchesIndustry;
    });
    
    // Reset pagination to first page
    if (this.paginator) {
      this.paginator.firstPage();
    }
    
    // Refresh table
    if (this.table) {
      this.table.renderRows();
    }
  }
  
  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusFilter.setValue('');
    this.industryFilter.setValue('');
  }
  
  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'draft': return 'status-draft';
      case 'active': return 'status-active';
      case 'paused': return 'status-paused';
      case 'completed': return 'status-completed';
      case 'archived': return 'status-archived';
      default: return '';
    }
  }
  
  publishSurvey(surveyId: number): void {
    const survey = this.surveys.find(s => s.id === surveyId);
    
    if (!survey) return;
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Publish Survey',
        message: `Are you sure you want to publish "${survey.name}"? Once published, it will be visible to matching participants.`,
        confirmText: 'Publish',
        cancelText: 'Cancel'
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.adminSurveyService.publishSurvey(surveyId).subscribe({
          next: () => {
            // Update local data
            survey.status = 'Active';
            this.snackBar.open('Survey published successfully!', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error publishing survey', error);
            this.snackBar.open('Error publishing survey. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
  
  pauseSurvey(surveyId: number): void {
    const survey = this.surveys.find(s => s.id === surveyId);
    
    if (!survey) return;
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Pause Survey',
        message: `Are you sure you want to pause "${survey.name}"? It will no longer be visible to new participants while paused.`,
        confirmText: 'Pause',
        cancelText: 'Cancel'
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.adminSurveyService.updateSurveyStatus(surveyId, 'paused').subscribe({
          next: () => {
            // Update local data
            survey.status = 'Paused';
            this.snackBar.open('Survey paused successfully!', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error pausing survey', error);
            this.snackBar.open('Error pausing survey. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
  
  resumeSurvey(surveyId: number): void {
    const survey = this.surveys.find(s => s.id === surveyId);
    
    if (!survey) return;
    
    this.adminSurveyService.updateSurveyStatus(surveyId, 'active').subscribe({
      next: () => {
        // Update local data
        survey.status = 'Active';
        this.snackBar.open('Survey resumed successfully!', 'Close', {
          duration: 3000
        });
      },
      error: (error: any) => {
        console.error('Error resuming survey', error);
        this.snackBar.open('Error resuming survey. Please try again.', 'Close', {
          duration: 5000
        });
      }
    });
  }
  
  completeSurvey(surveyId: number): void {
    const survey = this.surveys.find(s => s.id === surveyId);
    
    if (!survey) return;
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Complete Survey',
        message: `Are you sure you want to mark "${survey.name}" as completed? This will close the survey to new participants and finalize all results.`,
        confirmText: 'Complete',
        cancelText: 'Cancel'
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.adminSurveyService.updateSurveyStatus(surveyId, 'completed').subscribe({
          next: () => {
            // Update local data
            survey.status = 'Completed';
            this.snackBar.open('Survey marked as completed!', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error completing survey', error);
            this.snackBar.open('Error completing survey. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
  
  archiveSurvey(surveyId: number): void {
    const survey = this.surveys.find(s => s.id === surveyId);
    
    if (!survey) return;
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Archive Survey',
        message: `Are you sure you want to archive "${survey.name}"? Archived surveys can still be viewed but cannot be modified or published again.`,
        confirmText: 'Archive',
        cancelText: 'Cancel'
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.adminSurveyService.updateSurveyStatus(surveyId, 'archived').subscribe({
          next: () => {
            // Update local data
            survey.status = 'Archived';
            this.snackBar.open('Survey archived successfully!', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error archiving survey', error);
            this.snackBar.open('Error archiving survey. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
  
  deleteSurvey(surveyId: number): void {
    const survey = this.surveys.find(s => s.id === surveyId);
    
    if (!survey) return;
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Survey',
        message: `Are you sure you want to delete "${survey.name}"? This action cannot be undone and all associated data will be permanently removed.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        isDestructive: true
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.adminSurveyService.deleteSurvey(surveyId).subscribe({
          next: () => {
            // Remove from local data
            this.surveys = this.surveys.filter(s => s.id !== surveyId);
            this.filteredSurveys = this.filteredSurveys.filter(s => s.id !== surveyId);
            this.snackBar.open('Survey deleted successfully!', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error deleting survey', error);
            this.snackBar.open('Error deleting survey. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
}