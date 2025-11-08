// src/app/features/admin/surveys/survey-results/survey-results.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { Chart } from 'chart.js';
import { AdminSurveyService } from '../../../../core/services/admin-survey.service';

@Component({
  selector: 'app-survey-results',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTabsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressBarModule,
    MatDividerModule,
    MatSelectModule
  ],
  templateUrl: './survey-results.component.html',
  styleUrls: ['./survey-results.component.scss']
})
export class SurveyResultsComponent implements OnInit {
  surveyId!: number;
  survey: any = null;
  results: any = null;
  loading: boolean = true;
  
  // Charts
  participantChart: any;
  completionRateChart: any;
  responseTimeChart: any;
  
  // Question specific charts
  questionCharts: Map<number, any> = new Map();
  
  // Filters
  selectedSection = new FormControl('all');
  
  constructor(
    private route: ActivatedRoute,
    private adminSurveyService: AdminSurveyService
  ) { }
  
  ngOnInit(): void {
    this.surveyId = +this.route.snapshot.paramMap.get('id')!;
    this.loadSurveyAndResults();
  }
  
  loadSurveyAndResults(): void {
    this.loading = true;
    
    // Load survey details
    this.adminSurveyService.getSurveyById(this.surveyId).subscribe({
      next: (survey) => {
        this.survey = survey;
        
        // Load survey results
        this.adminSurveyService.getSurveyResults(this.surveyId).subscribe({
          next: (results) => {
            this.results = results;
            this.loading = false;
            
            // Initialize charts after data is loaded
            setTimeout(() => {
              this.initializeCharts();
              this.initializeQuestionCharts();
            }, 0);
          },
          error: (error) => {
            console.error('Error loading survey results', error);
            this.loading = false;
          }
        });
      },
      error: (error) => {
        console.error('Error loading survey details', error);
        this.loading = false;
      }
    });
  }
  
  initializeCharts(): void {
    // Participant demographics chart
    const participantCtx = document.getElementById('participantChart') as HTMLCanvasElement;
    if (participantCtx) {
      this.participantChart = new Chart(participantCtx, {
        type: 'doughnut',
        data: {
          labels: this.results.demographics.ageGroups.map((item: any) => item.label),
          datasets: [{
            data: this.results.demographics.ageGroups.map((item: any) => item.count),
            backgroundColor: [
              '#673AB7', '#9C27B0', '#E91E63', '#F44336', '#FF9800', '#FFC107', '#8BC34A', '#4CAF50'
            ]
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              position: 'right'
            }
          }
        }
      });
    }
    
    // Completion rate chart
    const completionCtx = document.getElementById('completionRateChart') as HTMLCanvasElement;
    if (completionCtx) {
      this.completionRateChart = new Chart(completionCtx, {
        type: 'bar',
        data: {
          labels: this.results.completionStats.completionBySection.map((item: any) => item.sectionName),
          datasets: [{
            label: 'Completion Rate (%)',
            data: this.results.completionStats.completionBySection.map((item: any) => item.completionRate * 100),
            backgroundColor: '#673AB7'
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          scales: {
            y: {
              beginAtZero: true,
              max: 100
            }
          }
        }
      });
    }
    
    // Response time chart
    const timeCtx = document.getElementById('responseTimeChart') as HTMLCanvasElement;
    if (timeCtx) {
      this.responseTimeChart = new Chart(timeCtx, {
        type: 'line',
        data: {
          labels: this.results.completionStats.avgTimeBySection.map((item: any) => item.sectionName),
          datasets: [{
            label: 'Average Time (seconds)',
            data: this.results.completionStats.avgTimeBySection.map((item: any) => item.averageTimeInSeconds),
            backgroundColor: 'rgba(103, 58, 183, 0.2)',
            borderColor: '#673AB7',
            tension: 0.1,
            fill: true
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false
        }
      });
    }
  }
  
  initializeQuestionCharts(): void {
    if (!this.results || !this.results.questionResponses) return;
    
    this.results.questionResponses.forEach((question: any) => {
      setTimeout(() => {
        if (question.questionType === 'MultipleChoice' || question.questionType === 'Checkbox') {
          const chartId = `questionChart_${question.questionId}`;
          const ctx = document.getElementById(chartId) as HTMLCanvasElement;
          
          if (ctx) {
            const chart = new Chart(ctx, {
              type: question.questionType === 'MultipleChoice' ? 'doughnut' : 'bar',
              data: {
                labels: question.options.map((option: any) => option.text),
                datasets: [{
                  data: question.options.map((option: any) => option.count),
                  backgroundColor: [
                    '#673AB7', '#9C27B0', '#E91E63', '#F44336', '#FF9800', '#FFC107', '#8BC34A', '#4CAF50'
                  ]
                }]
              },
              options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                  legend: {
                    position: question.questionType === 'MultipleChoice' ? 'right' : 'top'
                  }
                }
              }
            });
            
            this.questionCharts.set(question.questionId, chart);
          }
        } else if (question.questionType === 'Rating' || question.questionType === 'Scale') {
          const chartId = `questionChart_${question.questionId}`;
          const ctx = document.getElementById(chartId) as HTMLCanvasElement;
          
          if (ctx) {
            const chart = new Chart(ctx, {
              type: 'bar',
              data: {
                labels: question.scaleValues.map((value: any) => value.label),
                datasets: [{
                  label: 'Responses',
                  data: question.scaleValues.map((value: any) => value.count),
                  backgroundColor: '#673AB7'
                }]
              },
              options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                  y: {
                    beginAtZero: true,
                    ticks: {
                      precision: 0
                    }
                  }
                }
              }
            });
            
            this.questionCharts.set(question.questionId, chart);
          }
        }
      }, 0);
    });
  }
  
  filterQuestionsBySectionId(sectionId: string): void {
    this.selectedSection.setValue(sectionId);
    
    // Destroy existing charts to recreate them with filtered data
    this.questionCharts.forEach((chart) => {
      chart.destroy();
    });
    this.questionCharts.clear();
    
    // Reinitialize question charts with filtered questions
    setTimeout(() => {
      this.initializeQuestionCharts();
    }, 0);
  }
  
  exportResults(format: string): void {
    this.adminSurveyService.exportSurveyResults(this.surveyId, format).subscribe({
      next: (data: any) => {
        const blob = new Blob([data], { type: this.getContentType(format) });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `survey-results-${this.surveyId}.${format}`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        console.error(`Error exporting results as ${format}`, error);
      }
    });
  }
  
  getContentType(format: string): string {
    switch (format.toLowerCase()) {
      case 'csv': return 'text/csv';
      case 'xlsx': return 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
      case 'pdf': return 'application/pdf';
      case 'json': return 'application/json';
      default: return 'text/plain';
    }
  }
  
  getQuestionTypeIcon(type: string): string {
    switch (type) {
      case 'Text': return 'text_fields';
      case 'MultipleChoice': return 'radio_button_checked';
      case 'Checkbox': return 'check_box';
      case 'Rating': return 'star_rate';
      case 'Scale': return 'linear_scale';
      case 'Date': return 'calendar_today';
      case 'Time': return 'access_time';
      case 'File': return 'attach_file';
      default: return 'help';
    }
  }
}
