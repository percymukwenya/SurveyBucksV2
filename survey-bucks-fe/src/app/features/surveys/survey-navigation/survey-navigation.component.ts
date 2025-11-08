import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { MatBadgeModule } from '@angular/material/badge';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SurveyNavigation } from '../../../core/services/survey.service';

@Component({
  selector: 'app-survey-navigation',
  standalone: true,
  imports: [
    CommonModule,
    MatTabsModule,
    MatBadgeModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule
  ],
  template: `
    <div class="survey-navigation-container" *ngIf="navigation">
      <!-- Section Navigation Tabs -->
      <mat-tab-group
        [(selectedIndex)]="currentSectionIndex"
        (selectedIndexChange)="onSectionChange($event)"
        animationDuration="200ms"
        class="enhanced-tab-group">
        
        <mat-tab
          *ngFor="let section of navigation.sections; let i = index"
          [disabled]="sectionLoading">
          
          <ng-template mat-tab-label>
            <div class="section-tab-label">
              <span class="section-name">{{ section.name }}</span>
              
              <!-- Progress indicator -->
              <div class="progress-indicator" 
                   [class]="getSectionStatusClass(section.completionPercentage)">
                <svg class="progress-ring" width="20" height="20" viewBox="0 0 20 20">
                  <circle
                    class="progress-ring-background"
                    cx="10" cy="10" r="8"
                    stroke="#e0e0e0"
                    stroke-width="2"
                    fill="transparent">
                  </circle>
                  <circle
                    class="progress-ring-progress"
                    cx="10" cy="10" r="8"
                    [attr.stroke]="getSectionStatusColor(section.completionPercentage)"
                    stroke-width="2"
                    fill="transparent"
                    [style.stroke-dasharray]="50.27"
                    [style.stroke-dashoffset]="50.27 - (50.27 * section.completionPercentage) / 100">
                  </circle>
                  
                  <!-- Status icon in center -->
                  <text x="10" y="10" 
                        text-anchor="middle" 
                        dominant-baseline="central" 
                        class="progress-text"
                        [attr.fill]="getSectionStatusColor(section.completionPercentage)"
                        font-size="8">
                    {{ getSectionStatusIcon(section.completionPercentage) }}
                  </text>
                </svg>
              </div>
              
              <!-- Question count badge -->
              <div class="question-badge" 
                   [matTooltip]="section.answeredCount + ' of ' + section.questionCount + ' answered'">
                <span class="answered">{{ section.answeredCount }}</span>
                <span class="separator">/</span>
                <span class="total">{{ section.questionCount }}</span>
              </div>
            </div>
          </ng-template>
          
          <!-- Tab content (empty as content is managed externally) -->
          <div class="tab-content-placeholder">
            <!-- Content will be rendered by parent component -->
          </div>
        </mat-tab>
      </mat-tab-group>

      <!-- Section Navigation Controls -->
      <div class="section-controls">
        <button mat-raised-button
                color="primary"
                (click)="onPrevious()"
                [disabled]="!canGoToPrevious() || sectionLoading"
                class="nav-button">
          <mat-icon>arrow_back</mat-icon>
          Previous Section
        </button>

        <div class="section-info">
          <span class="section-counter">
            Section {{ currentSectionIndex + 1 }} of {{ navigation.sections.length }}
          </span>
          
          <div class="section-status" *ngIf="currentSection">
            <mat-icon [class]="getSectionRequirementClass()">
              {{ getSectionRequirementIcon() }}
            </mat-icon>
            <span>{{ getSectionRequirementText() }}</span>
          </div>
        </div>

        <button mat-raised-button
                color="primary"
                (click)="onNext()"
                [disabled]="!canGoToNext() || sectionLoading"
                class="nav-button">
          Next Section
          <mat-icon>arrow_forward</mat-icon>
        </button>
      </div>
    </div>
  `,
  styleUrls: ['./survey-navigation.component.scss']
})
export class SurveyNavigationComponent {
  @Input() navigation: SurveyNavigation | null = null;
  @Input() currentSectionIndex: number = 0;
  @Input() sectionLoading: boolean = false;
  @Input() currentSectionComplete: boolean = false;

  @Output() sectionChange = new EventEmitter<number>();
  @Output() nextSection = new EventEmitter<void>();
  @Output() previousSection = new EventEmitter<void>();

  get currentSection() {
    return this.navigation?.sections[this.currentSectionIndex];
  }

  onSectionChange(sectionIndex: number): void {
    this.sectionChange.emit(sectionIndex);
  }

  onNext(): void {
    this.nextSection.emit();
  }

  onPrevious(): void {
    this.previousSection.emit();
  }

  canGoToNext(): boolean {
    if (!this.navigation || this.currentSectionIndex >= this.navigation.sections.length - 1) {
      return false;
    }

    const currentSection = this.navigation.sections[this.currentSectionIndex];
    return !currentSection.isRequired || this.currentSectionComplete;
  }

  canGoToPrevious(): boolean {
    return this.currentSectionIndex > 0;
  }

  getSectionStatusClass(completionPercentage: number): string {
    if (completionPercentage >= 100) return 'complete';
    if (completionPercentage > 0) return 'partial';
    return 'empty';
  }

  getSectionStatusColor(completionPercentage: number): string {
    if (completionPercentage >= 100) return '#4caf50';
    if (completionPercentage > 0) return '#ff9800';
    return '#e0e0e0';
  }

  getSectionStatusIcon(completionPercentage: number): string {
    if (completionPercentage >= 100) return '✓';
    if (completionPercentage > 0) return '◐';
    return '';
  }

  getSectionRequirementClass(): string {
    if (!this.currentSection) return '';
    
    if (this.currentSection.isRequired && !this.currentSectionComplete) {
      return 'required-warning';
    }
    
    return 'optional-info';
  }

  getSectionRequirementIcon(): string {
    if (!this.currentSection) return 'info';
    
    if (this.currentSection.isRequired) {
      return this.currentSectionComplete ? 'check_circle' : 'warning';
    }
    
    return 'info';
  }

  getSectionRequirementText(): string {
    if (!this.currentSection) return '';
    
    if (this.currentSection.isRequired) {
      return this.currentSectionComplete 
        ? 'Required section completed' 
        : 'Please complete all required questions';
    }
    
    return 'Optional section - you can skip if needed';
  }
}