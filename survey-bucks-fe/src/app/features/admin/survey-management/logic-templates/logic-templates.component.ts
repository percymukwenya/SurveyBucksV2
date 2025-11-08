import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';

export interface LogicTemplate {
  id: string;
  name: string;
  description: string;
  category: 'screening' | 'branching' | 'termination' | 'custom';
  iconName: string;
  tags: string[];
  isBuiltIn: boolean;
  isPublic: boolean;
  createdBy?: string;
  createdDate?: Date;
  usageCount?: number;
  rating?: number;
  rules: TemplateRule[];
}

export interface TemplateRule {
  id: string;
  order: number;
  conditionType: string;
  conditionValue: string;
  conditionValue2?: string;
  actionType: string;
  targetType?: 'question' | 'section' | 'none';
  message?: string;
  description: string;
}

@Component({
  selector: 'app-logic-templates',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatChipsModule,
    MatTabsModule,
    MatTooltipModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressBarModule,
    MatCheckboxModule
  ],
  templateUrl: './logic-templates.component.html',
  styleUrl: './logic-templates.component.scss'
})
export class LogicTemplatesComponent implements OnInit {
  @Input() questions: any[] = [];
  @Input() sections: any[] = [];
  @Output() templateApplied = new EventEmitter<LogicTemplate>();
  @Output() templateEdited = new EventEmitter<LogicTemplate>();
  
  // Template management
  templates: LogicTemplate[] = [];
  filteredTemplates: LogicTemplate[] = [];
  selectedCategory = 'all';
  searchQuery = '';
  
  // Built-in templates
  builtInTemplates: LogicTemplate[] = [
    {
      id: 'age-screening',
      name: 'Age Screening',
      description: 'Disqualify participants under a certain age',
      category: 'screening',
      iconName: 'block',
      tags: ['screening', 'age', 'disqualification'],
      isBuiltIn: true,
      isPublic: true,
      usageCount: 156,
      rating: 4.8,
      rules: [
        {
          id: 'age-check',
          order: 1,
          conditionType: 'less_than',
          conditionValue: '18',
          actionType: 'disqualify',
          message: 'Sorry, you must be 18 or older to participate in this survey.',
          description: 'If age is less than 18, disqualify participant'
        }
      ]
    },
    {
      id: 'yes-skip',
      name: 'Skip if Yes',
      description: 'Skip follow-up questions when user answers "Yes"',
      category: 'branching',
      iconName: 'skip_next',
      tags: ['skip', 'conditional', 'yes/no'],
      isBuiltIn: true,
      isPublic: true,
      usageCount: 234,
      rating: 4.6,
      rules: [
        {
          id: 'yes-condition',
          order: 1,
          conditionType: 'equals',
          conditionValue: 'Yes',
          actionType: 'jump_to_section',
          targetType: 'section',
          description: 'If answer is "Yes", skip to next section'
        }
      ]
    },
    {
      id: 'product-satisfaction',
      name: 'Product Satisfaction Flow',
      description: 'Branch based on satisfaction rating',
      category: 'branching',
      iconName: 'thumbs_up_down',
      tags: ['satisfaction', 'rating', 'feedback'],
      isBuiltIn: true,
      isPublic: true,
      usageCount: 89,
      rating: 4.4,
      rules: [
        {
          id: 'low-satisfaction',
          order: 1,
          conditionType: 'less_than',
          conditionValue: '3',
          actionType: 'show_questions',
          targetType: 'question',
          description: 'If rating < 3, show feedback questions'
        },
        {
          id: 'high-satisfaction',
          order: 2,
          conditionType: 'greater_than',
          conditionValue: '4',
          actionType: 'show_questions',
          targetType: 'question',
          description: 'If rating > 4, show recommendation questions'
        }
      ]
    },
    {
      id: 'demographic-screening',
      name: 'Demographic Screening',
      description: 'Screen participants by location and employment',
      category: 'screening',
      iconName: 'location_on',
      tags: ['demographics', 'location', 'employment'],
      isBuiltIn: true,
      isPublic: true,
      usageCount: 67,
      rating: 4.2,
      rules: [
        {
          id: 'location-check',
          order: 1,
          conditionType: 'not_in_list',
          conditionValue: 'US,CA,UK',
          actionType: 'disqualify',
          message: 'This survey is only available in certain regions.',
          description: 'If location not in target countries, disqualify'
        }
      ]
    },
    {
      id: 'survey-completion',
      name: 'Early Completion',
      description: 'End survey early based on response patterns',
      category: 'termination',
      iconName: 'done',
      tags: ['completion', 'early', 'termination'],
      isBuiltIn: true,
      isPublic: true,
      usageCount: 45,
      rating: 4.0,
      rules: [
        {
          id: 'completion-trigger',
          order: 1,
          conditionType: 'equals',
          conditionValue: 'Not Applicable',
          actionType: 'end_survey',
          message: 'Thank you for your time. You have completed the survey.',
          description: 'If answer is "Not Applicable", end survey'
        }
      ]
    }
  ];
  
  categories = [
    { value: 'all', label: 'All Templates', icon: 'view_list' },
    { value: 'screening', label: 'Screening', icon: 'filter_list' },
    { value: 'branching', label: 'Branching', icon: 'call_split' },
    { value: 'termination', label: 'Termination', icon: 'stop' },
    { value: 'custom', label: 'Custom', icon: 'build' }
  ];
  
  constructor(
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}
  
  ngOnInit(): void {
    this.loadTemplates();
    this.filterTemplates();
  }
  
  private loadTemplates(): void {
    // In real implementation, load from API
    this.templates = [...this.builtInTemplates];
    
    // Add some mock custom templates
    const customTemplates: LogicTemplate[] = [
      {
        id: 'custom-1',
        name: 'Brand Awareness Flow',
        description: 'Custom flow for brand awareness studies',
        category: 'custom',
        iconName: 'business',
        tags: ['brand', 'awareness', 'custom'],
        isBuiltIn: false,
        isPublic: false,
        createdBy: 'Current User',
        createdDate: new Date(2024, 0, 15),
        usageCount: 12,
        rating: 4.5,
        rules: [
          {
            id: 'brand-recognition',
            order: 1,
            conditionType: 'equals',
            conditionValue: 'Never heard of it',
            actionType: 'jump_to_section',
            targetType: 'section',
            description: 'If never heard of brand, skip to awareness section'
          }
        ]
      }
    ];
    
    this.templates.push(...customTemplates);
  }
  
  filterTemplates(): void {
    let filtered = this.templates;
    
    // Filter by category
    if (this.selectedCategory !== 'all') {
      filtered = filtered.filter(t => t.category === this.selectedCategory);
    }
    
    // Filter by search query
    if (this.searchQuery) {
      const query = this.searchQuery.toLowerCase();
      filtered = filtered.filter(t => 
        t.name.toLowerCase().includes(query) ||
        t.description.toLowerCase().includes(query) ||
        t.tags.some(tag => tag.toLowerCase().includes(query))
      );
    }
    
    // Sort by usage count and rating
    filtered.sort((a, b) => {
      if (a.isBuiltIn && !b.isBuiltIn) return -1;
      if (!a.isBuiltIn && b.isBuiltIn) return 1;
      return (b.usageCount || 0) - (a.usageCount || 0);
    });
    
    this.filteredTemplates = filtered;
  }
  
  onCategoryChange(category: string): void {
    this.selectedCategory = category;
    this.filterTemplates();
  }
  
  onSearchChange(query: string): void {
    this.searchQuery = query;
    this.filterTemplates();
  }
  
  applyTemplate(template: LogicTemplate): void {
    this.templateApplied.emit(template);
    this.snackBar.open(`Applied template: ${template.name}`, 'Close', { duration: 3000 });
  }
  
  editTemplate(template: LogicTemplate): void {
    const dialogRef = this.dialog.open(TemplateEditorDialog, {
      width: '800px',
      data: { template: { ...template }, questions: this.questions, sections: this.sections }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.templateEdited.emit(result);
        this.loadTemplates(); // Refresh templates
        this.filterTemplates();
        this.snackBar.open('Template updated successfully', 'Close', { duration: 3000 });
      }
    });
  }
  
  duplicateTemplate(template: LogicTemplate): void {
    const duplicated: LogicTemplate = {
      ...template,
      id: `${template.id}-copy-${Date.now()}`,
      name: `${template.name} (Copy)`,
      isBuiltIn: false,
      isPublic: false,
      createdBy: 'Current User',
      createdDate: new Date(),
      usageCount: 0
    };
    
    this.editTemplate(duplicated);
  }
  
  deleteTemplate(template: LogicTemplate): void {
    if (template.isBuiltIn) {
      this.snackBar.open('Built-in templates cannot be deleted', 'Close', { duration: 3000 });
      return;
    }
    
    if (confirm(`Are you sure you want to delete "${template.name}"?`)) {
      this.templates = this.templates.filter(t => t.id !== template.id);
      this.filterTemplates();
      this.snackBar.open('Template deleted successfully', 'Close', { duration: 3000 });
    }
  }
  
  createNewTemplate(): void {
    const newTemplate: LogicTemplate = {
      id: `custom-${Date.now()}`,
      name: 'New Template',
      description: 'Custom logic template',
      category: 'custom',
      iconName: 'build',
      tags: ['custom'],
      isBuiltIn: false,
      isPublic: false,
      createdBy: 'Current User',
      createdDate: new Date(),
      usageCount: 0,
      rules: []
    };
    
    this.editTemplate(newTemplate);
  }
  
  getTemplateIcon(template: LogicTemplate): string {
    return template.iconName || 'help_outline';
  }
  
  getCategoryIcon(category: string): string {
    const cat = this.categories.find(c => c.value === category);
    return cat ? cat.icon : 'help_outline';
  }
  
  formatRating(rating: number | undefined): string {
    return rating ? rating.toFixed(1) : 'N/A';
  }
  
  formatUsageCount(count: number | undefined): string {
    if (!count) return '0';
    if (count < 1000) return count.toString();
    return `${(count / 1000).toFixed(1)}k`;
  }
}

// Template Editor Dialog
@Component({
  selector: 'template-editor-dialog',
  template: `
    <h2 mat-dialog-title>{{ data.template.isBuiltIn ? 'View' : 'Edit' }} Template</h2>
    
    <mat-dialog-content>
      <form [formGroup]="templateForm" class="template-form">
        
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Template Name</mat-label>
          <input matInput formControlName="name" [readonly]="data.template.isBuiltIn">
        </mat-form-field>
        
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Description</mat-label>
          <textarea matInput formControlName="description" rows="2" [readonly]="data.template.isBuiltIn"></textarea>
        </mat-form-field>
        
        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Category</mat-label>
            <mat-select formControlName="category" [disabled]="data.template.isBuiltIn">
              <mat-option value="screening">Screening</mat-option>
              <mat-option value="branching">Branching</mat-option>
              <mat-option value="termination">Termination</mat-option>
              <mat-option value="custom">Custom</mat-option>
            </mat-select>
          </mat-form-field>
          
          <mat-form-field appearance="outline">
            <mat-label>Icon</mat-label>
            <input matInput formControlName="iconName" [readonly]="data.template.isBuiltIn">
          </mat-form-field>
        </div>
        
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Tags (comma-separated)</mat-label>
          <input matInput [value]="getTags()" (input)="setTags($event.target.value)" [readonly]="data.template.isBuiltIn">
        </mat-form-field>
        
        <div *ngIf="!data.template.isBuiltIn" class="template-settings">
          <mat-checkbox formControlName="isPublic">Make template public</mat-checkbox>
        </div>
        
        <!-- Rules Section -->
        <div class="rules-section">
          <div class="section-header">
            <h3>Logic Rules</h3>
            <button 
              *ngIf="!data.template.isBuiltIn"
              mat-icon-button 
              type="button"
              (click)="addRule()">
              <mat-icon>add</mat-icon>
            </button>
          </div>
          
          <div class="rules-list">
            <div 
              *ngFor="let rule of templateRules; let i = index"
              class="rule-item">
              <div class="rule-header">
                <span class="rule-order">{{ i + 1 }}</span>
                <span class="rule-description">{{ rule.description }}</span>
                <div *ngIf="!data.template.isBuiltIn" class="rule-actions">
                  <button mat-icon-button type="button" (click)="editRule(i)">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button type="button" (click)="deleteRule(i)">
                    <mat-icon>delete</mat-icon>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
        
      </form>
    </mat-dialog-content>
    
    <mat-dialog-actions>
      <button mat-button (click)="cancel()">{{ data.template.isBuiltIn ? 'Close' : 'Cancel' }}</button>
      <button 
        *ngIf="!data.template.isBuiltIn"
        mat-raised-button 
        color="primary" 
        (click)="save()"
        [disabled]="templateForm.invalid">
        Save Template
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .template-form {
      min-width: 500px;
      
      .full-width {
        width: 100%;
        margin-bottom: 16px;
      }
      
      .form-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 16px;
        margin-bottom: 16px;
      }
      
      .template-settings {
        margin: 16px 0;
      }
      
      .rules-section {
        margin-top: 24px;
        
        .section-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: 16px;
          
          h3 {
            margin: 0;
            font-size: 16px;
            font-weight: 600;
          }
        }
        
        .rules-list {
          .rule-item {
            padding: 12px;
            background: #f8f9fa;
            border-radius: 6px;
            margin-bottom: 8px;
            
            .rule-header {
              display: flex;
              align-items: center;
              gap: 8px;
              
              .rule-order {
                width: 24px;
                height: 24px;
                background: #2196f3;
                color: white;
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 12px;
                font-weight: bold;
              }
              
              .rule-description {
                flex: 1;
                font-size: 14px;
                color: #333;
              }
              
              .rule-actions {
                display: flex;
                gap: 4px;
              }
            }
          }
        }
      }
    }
  `],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatIconModule
  ]
})
export class TemplateEditorDialog {
  templateForm: FormGroup;
  templateRules: TemplateRule[] = [];
  
  constructor(
    public dialogRef: MatDialogRef<TemplateEditorDialog>,
    private fb: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public data: { template: LogicTemplate; questions: any[]; sections: any[] }
  ) {
    this.templateForm = this.fb.group({
      name: [data.template.name, Validators.required],
      description: [data.template.description, Validators.required],
      category: [data.template.category, Validators.required],
      iconName: [data.template.iconName],
      isPublic: [data.template.isPublic]
    });
    
    this.templateRules = [...(data.template.rules || [])];
  }
  
  getTags(): string {
    return this.data.template.tags.join(', ');
  }
  
  setTags(value: string): void {
    this.data.template.tags = value.split(',').map(tag => tag.trim()).filter(tag => tag);
  }
  
  addRule(): void {
    const newRule: TemplateRule = {
      id: `rule-${Date.now()}`,
      order: this.templateRules.length + 1,
      conditionType: 'equals',
      conditionValue: '',
      actionType: 'show_question',
      description: 'New rule'
    };
    
    this.templateRules.push(newRule);
  }
  
  editRule(index: number): void {
    // TODO: Open rule editor dialog
    console.log('Edit rule at index:', index);
  }
  
  deleteRule(index: number): void {
    this.templateRules.splice(index, 1);
    // Update order
    this.templateRules.forEach((rule, i) => {
      rule.order = i + 1;
    });
  }
  
  cancel(): void {
    this.dialogRef.close();
  }
  
  save(): void {
    if (this.templateForm.valid) {
      const updatedTemplate: LogicTemplate = {
        ...this.data.template,
        ...this.templateForm.value,
        rules: this.templateRules
      };
      
      this.dialogRef.close(updatedTemplate);
    }
  }
}