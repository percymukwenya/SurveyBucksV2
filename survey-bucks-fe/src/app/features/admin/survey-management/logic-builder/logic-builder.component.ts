import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

export interface LogicBlock {
  id: string;
  type: 'condition' | 'action' | 'group';
  order: number;
  data: {
    conditionType?: string;
    conditionValue?: string;
    conditionValue2?: string;
    actionType?: string;
    targetQuestionId?: number;
    targetQuestionIds?: number[];
    targetSectionId?: number;
    message?: string;
    operator?: 'AND' | 'OR';
    children?: LogicBlock[];
  };
  isActive: boolean;
  isExpanded?: boolean;
}

export interface DragDropList {
  id: string;
  title: string;
  items: LogicBlock[];
  acceptedTypes: string[];
}

@Component({
  selector: 'app-logic-builder',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    DragDropModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatChipsModule,
    MatTooltipModule,
    MatDialogModule,
    MatSnackBarModule,
    MatExpansionModule,
    MatSlideToggleModule
  ],
  templateUrl: './logic-builder.component.html',
  styleUrl: './logic-builder.component.scss'
})
export class LogicBuilderComponent implements OnInit {
  @Input() questionId?: number;
  @Input() questions: any[] = [];
  @Input() sections: any[] = [];
  @Output() logicChanged = new EventEmitter<LogicBlock[]>();
  @Output() previewRequested = new EventEmitter<LogicBlock[]>();
  
  // Drag and drop lists
  toolbox: DragDropList = {
    id: 'toolbox',
    title: 'Logic Elements',
    items: [],
    acceptedTypes: []
  };
  
  workspace: DragDropList = {
    id: 'workspace',
    title: 'Logic Flow',
    items: [],
    acceptedTypes: ['condition', 'action', 'group']
  };
  
  // Form for editing logic blocks
  editForm: FormGroup;
  editingBlock: LogicBlock | null = null;
  showEditPanel = false;
  
  // Available options
  conditionTypes = [
    { value: 'equals', label: 'Equals', icon: '=' },
    { value: 'not_equals', label: 'Not Equals', icon: '≠' },
    { value: 'greater_than', label: 'Greater Than', icon: '>' },
    { value: 'less_than', label: 'Less Than', icon: '<' },
    { value: 'between', label: 'Between', icon: '≤≥' },
    { value: 'contains', label: 'Contains', icon: '⊃' },
    { value: 'in_list', label: 'In List', icon: '∈' },
    { value: 'regex_match', label: 'Regex Match', icon: '.*' }
  ];
  
  actionTypes = [
    { value: 'show_question', label: 'Show Question', icon: 'visibility', color: '#4caf50' },
    { value: 'hide_question', label: 'Hide Question', icon: 'visibility_off', color: '#ff9800' },
    { value: 'show_questions', label: 'Show Multiple', icon: 'view_list', color: '#4caf50' },
    { value: 'hide_questions', label: 'Hide Multiple', icon: 'view_list', color: '#ff9800' },
    { value: 'jump_to_section', label: 'Jump to Section', icon: 'call_made', color: '#2196f3' },
    { value: 'jump_to_question', label: 'Jump to Question', icon: 'redo', color: '#2196f3' },
    { value: 'end_survey', label: 'End Survey', icon: 'stop', color: '#f44336' },
    { value: 'disqualify', label: 'Disqualify', icon: 'block', color: '#f44336' }
  ];
  
  constructor(
    private fb: FormBuilder,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {
    this.editForm = this.createEditForm();
  }
  
  ngOnInit(): void {
    this.initializeToolbox();
  }
  
  private createEditForm(): FormGroup {
    return this.fb.group({
      conditionType: [''],
      conditionValue: [''],
      conditionValue2: [''],
      actionType: [''],
      targetQuestionId: [''],
      targetQuestionIds: [[]],
      targetSectionId: [''],
      message: [''],
      operator: ['AND'],
      isActive: [true]
    });
  }
  
  private initializeToolbox(): void {
    this.toolbox.items = [
      // Condition blocks
      ...this.conditionTypes.map(condition => ({
        id: `condition-${condition.value}`,
        type: 'condition' as const,
        order: 0,
        data: {
          conditionType: condition.value,
          conditionValue: '',
          conditionValue2: ''
        },
        isActive: true
      })),
      
      // Action blocks
      ...this.actionTypes.map(action => ({
        id: `action-${action.value}`,
        type: 'action' as const,
        order: 0,
        data: {
          actionType: action.value,
          targetQuestionId: undefined,
          targetQuestionIds: [],
          targetSectionId: undefined,
          message: ''
        },
        isActive: true
      })),
      
      // Group block
      {
        id: 'group-container',
        type: 'group' as const,
        order: 0,
        data: {
          operator: 'AND' as const,
          children: []
        },
        isActive: true,
        isExpanded: true
      }
    ];
  }
  
  // Drag and drop handlers
  onDrop(event: CdkDragDrop<LogicBlock[]>): void {
    if (event.previousContainer === event.container) {
      // Reorder within same container
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      // Transfer between containers
      if (event.previousContainer.id === 'toolbox') {
        // Clone the item from toolbox instead of moving it
        const clonedItem = this.cloneLogicBlock(event.previousContainer.data[event.previousIndex]);
        clonedItem.id = this.generateUniqueId();
        clonedItem.order = event.currentIndex;
        
        event.container.data.splice(event.currentIndex, 0, clonedItem);
      } else {
        // Move between workspace containers
        transferArrayItem(
          event.previousContainer.data,
          event.container.data,
          event.previousIndex,
          event.currentIndex
        );
      }
    }
    
    this.updateOrder();
    this.emitLogicChanged();
  }
  
  private cloneLogicBlock(block: LogicBlock): LogicBlock {
    return {
      ...block,
      id: this.generateUniqueId(),
      data: { ...block.data }
    };
  }
  
  private generateUniqueId(): string {
    return `block-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }
  
  private updateOrder(): void {
    this.workspace.items.forEach((item, index) => {
      item.order = index;
    });
  }
  
  // Block management
  editBlock(block: LogicBlock): void {
    this.editingBlock = block;
    this.editForm.patchValue(block.data);
    this.showEditPanel = true;
  }
  
  saveBlockEdit(): void {
    if (!this.editingBlock) return;
    
    if (this.editForm.valid) {
      this.editingBlock.data = { ...this.editingBlock.data, ...this.editForm.value };
      this.showEditPanel = false;
      this.editingBlock = null;
      this.editForm.reset();
      this.emitLogicChanged();
      
      this.snackBar.open('Logic block updated successfully', 'Close', { duration: 3000 });
    } else {
      this.snackBar.open('Please fill in all required fields', 'Close', { duration: 3000 });
    }
  }
  
  cancelBlockEdit(): void {
    this.showEditPanel = false;
    this.editingBlock = null;
    this.editForm.reset();
  }
  
  deleteBlock(block: LogicBlock): void {
    const index = this.workspace.items.findIndex(item => item.id === block.id);
    if (index !== -1) {
      this.workspace.items.splice(index, 1);
      this.updateOrder();
      this.emitLogicChanged();
      
      this.snackBar.open('Logic block deleted', 'Close', { duration: 3000 });
    }
  }
  
  duplicateBlock(block: LogicBlock): void {
    const clonedBlock = this.cloneLogicBlock(block);
    const index = this.workspace.items.findIndex(item => item.id === block.id);
    this.workspace.items.splice(index + 1, 0, clonedBlock);
    this.updateOrder();
    this.emitLogicChanged();
    
    this.snackBar.open('Logic block duplicated', 'Close', { duration: 3000 });
  }
  
  toggleBlock(block: LogicBlock): void {
    block.isActive = !block.isActive;
    this.emitLogicChanged();
  }
  
  // Group management
  addToGroup(block: LogicBlock, groupBlock: LogicBlock): void {
    if (!groupBlock.data.children) {
      groupBlock.data.children = [];
    }
    
    // Remove from main workspace
    const index = this.workspace.items.findIndex(item => item.id === block.id);
    if (index !== -1) {
      this.workspace.items.splice(index, 1);
      groupBlock.data.children.push(block);
      this.updateOrder();
      this.emitLogicChanged();
    }
  }
  
  removeFromGroup(block: LogicBlock, groupBlock: LogicBlock): void {
    if (!groupBlock.data.children) return;
    
    const index = groupBlock.data.children.findIndex(item => item.id === block.id);
    if (index !== -1) {
      groupBlock.data.children.splice(index, 1);
      
      // Add back to main workspace
      const groupIndex = this.workspace.items.findIndex(item => item.id === groupBlock.id);
      this.workspace.items.splice(groupIndex + 1, 0, block);
      this.updateOrder();
      this.emitLogicChanged();
    }
  }
  
  // Utility methods
  getBlockDisplayText(block: LogicBlock): string {
    switch (block.type) {
      case 'condition':
        const condition = this.conditionTypes.find(c => c.value === block.data.conditionType);
        const conditionLabel = condition ? condition.label : block.data.conditionType;
        if (block.data.conditionType === 'between') {
          return `${conditionLabel} ${block.data.conditionValue || '?'} and ${block.data.conditionValue2 || '?'}`;
        }
        return `${conditionLabel} "${block.data.conditionValue || '?'}"`;
        
      case 'action':
        const action = this.actionTypes.find(a => a.value === block.data.actionType);
        let actionText = action ? action.label : block.data.actionType;
        
        if (block.data.targetQuestionId) {
          const question = this.questions.find(q => q.id === block.data.targetQuestionId);
          if (question) {
            actionText += `: Q${question.order}`;
          }
        } else if (block.data.targetSectionId) {
          actionText += `: Section ${block.data.targetSectionId}`;
        } else if (block.data.targetQuestionIds && block.data.targetQuestionIds.length > 0) {
          actionText += `: ${block.data.targetQuestionIds.length} questions`;
        }
        
        return actionText;
        
      case 'group':
        const childCount = block.data.children ? block.data.children.length : 0;
        return `${block.data.operator} Group (${childCount} items)`;
        
      default:
        return 'Unknown Block';
    }
  }
  
  getBlockIcon(block: LogicBlock): string {
    switch (block.type) {
      case 'condition':
        const condition = this.conditionTypes.find(c => c.value === block.data.conditionType);
        return condition ? condition.icon : '?';
        
      case 'action':
        const action = this.actionTypes.find(a => a.value === block.data.actionType);
        return action ? action.icon : 'settings';
        
      case 'group':
        return block.data.operator === 'AND' ? 'check_box' : 'radio_button_checked';
        
      default:
        return 'help';
    }
  }
  
  getBlockColor(block: LogicBlock): string {
    switch (block.type) {
      case 'condition':
        return '#2196f3';
        
      case 'action':
        const action = this.actionTypes.find(a => a.value === block.data.actionType);
        return action ? action.color : '#666';
        
      case 'group':
        return '#9c27b0';
        
      default:
        return '#666';
    }
  }
  
  isBlockConfigured(block: LogicBlock): boolean {
    switch (block.type) {
      case 'condition':
        return !!(block.data.conditionType && block.data.conditionValue);
        
      case 'action':
        if (!block.data.actionType) return false;
        
        const action = this.actionTypes.find(a => a.value === block.data.actionType);
        if (!action) return false;
        
        // Check if required targets are set
        if (['show_question', 'hide_question', 'jump_to_question'].includes(block.data.actionType!)) {
          return !!block.data.targetQuestionId;
        } else if (['show_questions', 'hide_questions'].includes(block.data.actionType!)) {
          return !!(block.data.targetQuestionIds && block.data.targetQuestionIds.length > 0);
        } else if (block.data.actionType === 'jump_to_section') {
          return !!block.data.targetSectionId;
        }
        
        return true;
        
      case 'group':
        return !!(block.data.children && block.data.children.length > 0);
        
      default:
        return false;
    }
  }
  
  // Form validation helpers
  onConditionTypeChange(): void {
    const conditionType = this.editForm.get('conditionType')?.value;
    const conditionValue2Control = this.editForm.get('conditionValue2');
    
    if (conditionType === 'between') {
      conditionValue2Control?.setValidators([Validators.required]);
    } else {
      conditionValue2Control?.clearValidators();
    }
    conditionValue2Control?.updateValueAndValidity();
  }
  
  onActionTypeChange(): void {
    const actionType = this.editForm.get('actionType')?.value;
    
    // Reset target fields
    this.editForm.patchValue({
      targetQuestionId: null,
      targetQuestionIds: [],
      targetSectionId: null
    });
    
    // Update validators based on action type
    const targetQuestionIdControl = this.editForm.get('targetQuestionId');
    const targetQuestionIdsControl = this.editForm.get('targetQuestionIds');
    const targetSectionIdControl = this.editForm.get('targetSectionId');
    
    // Clear all validators
    targetQuestionIdControl?.clearValidators();
    targetQuestionIdsControl?.clearValidators();
    targetSectionIdControl?.clearValidators();
    
    // Set validators based on action type
    if (['show_question', 'hide_question', 'jump_to_question'].includes(actionType)) {
      targetQuestionIdControl?.setValidators([Validators.required]);
    } else if (['show_questions', 'hide_questions'].includes(actionType)) {
      targetQuestionIdsControl?.setValidators([Validators.required]);
    } else if (actionType === 'jump_to_section') {
      targetSectionIdControl?.setValidators([Validators.required]);
    }
    
    // Update validity
    targetQuestionIdControl?.updateValueAndValidity();
    targetQuestionIdsControl?.updateValueAndValidity();
    targetSectionIdControl?.updateValueAndValidity();
  }
  
  // Multi-select helpers
  onQuestionSelectionChange(questionId: number, selected: boolean): void {
    const currentIds = this.editForm.get('targetQuestionIds')?.value || [];
    
    if (selected) {
      if (!currentIds.includes(questionId)) {
        this.editForm.patchValue({
          targetQuestionIds: [...currentIds, questionId]
        });
      }
    } else {
      this.editForm.patchValue({
        targetQuestionIds: currentIds.filter((id: number) => id !== questionId)
      });
    }
  }
  
  isQuestionSelected(questionId: number): boolean {
    const selectedIds = this.editForm.get('targetQuestionIds')?.value || [];
    return selectedIds.includes(questionId);
  }
  
  // Output events
  private emitLogicChanged(): void {
    this.logicChanged.emit(this.workspace.items);
  }
  
  previewLogic(): void {
    this.previewRequested.emit(this.workspace.items);
  }
  
  // Import/Export
  exportLogic(): string {
    return JSON.stringify(this.workspace.items, null, 2);
  }
  
  importLogic(jsonString: string): void {
    try {
      const imported = JSON.parse(jsonString);
      if (Array.isArray(imported)) {
        this.workspace.items = imported;
        this.updateOrder();
        this.emitLogicChanged();
        this.snackBar.open('Logic imported successfully', 'Close', { duration: 3000 });
      } else {
        throw new Error('Invalid format');
      }
    } catch (error) {
      this.snackBar.open('Failed to import logic. Please check the format.', 'Close', { duration: 5000 });
    }
  }
  
  clearWorkspace(): void {
    if (confirm('Are you sure you want to clear all logic blocks?')) {
      this.workspace.items = [];
      this.emitLogicChanged();
      this.snackBar.open('Workspace cleared', 'Close', { duration: 3000 });
    }
  }
  
  trackByBlockId(index: number, block: LogicBlock): string {
    return block.id;
  }
}