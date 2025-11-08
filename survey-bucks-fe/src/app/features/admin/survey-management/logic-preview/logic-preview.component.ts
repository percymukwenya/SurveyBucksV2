import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatSliderModule } from '@angular/material/slider';
import { MatTabsModule } from '@angular/material/tabs';
import { LogicBlock } from '../logic-builder/logic-builder.component';

interface PreviewQuestion {
  id: number;
  questionText: string;
  questionType: string;
  options?: string[];
  isVisible: boolean;
  isAnswered: boolean;
  answer?: any;
  order: number;
}

interface PreviewScenario {
  id: string;
  name: string;
  description: string;
  testResponses: Record<number, any>;
}

interface LogicExecutionStep {
  blockId: string;
  blockType: 'condition' | 'action' | 'group';
  description: string;
  result: boolean | string;
  timestamp: number;
}

@Component({
  selector: 'app-logic-preview',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatRadioModule,
    MatCheckboxModule,
    MatSliderModule,
    MatChipsModule,
    MatTabsModule
  ],
  templateUrl: './logic-preview.component.html',
  styleUrl: './logic-preview.component.scss'
})
export class LogicPreviewComponent implements OnChanges {
  @Input() logicBlocks: LogicBlock[] = [];
  @Input() questions: any[] = [];
  @Input() sections: any[] = [];
  @Output() previewClosed = new EventEmitter<void>();
  
  // Preview state
  previewQuestions: PreviewQuestion[] = [];
  currentResponses: Record<number, any> = {};
  executionLog: LogicExecutionStep[] = [];
  isExecuting = false;
  
  // Test scenarios
  testScenarios: PreviewScenario[] = [
    {
      id: 'empty',
      name: 'No Responses',
      description: 'Test with no questions answered',
      testResponses: {}
    },
    {
      id: 'yes_scenario',
      name: 'All Yes',
      description: 'Answer "Yes" to all Yes/No questions',
      testResponses: {}
    },
    {
      id: 'no_scenario',
      name: 'All No',
      description: 'Answer "No" to all Yes/No questions',
      testResponses: {}
    },
    {
      id: 'mixed_scenario',
      name: 'Mixed Responses',
      description: 'Random mix of responses',
      testResponses: {}
    }
  ];
  
  selectedScenario: PreviewScenario | null = null;
  
  // Execution state
  currentStep = 0;
  isStepByStep = false;
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['questions'] || changes['logicBlocks']) {
      this.initializePreview();
      this.generateTestScenarios();
    }
  }
  
  private initializePreview(): void {
    this.previewQuestions = this.questions.map(q => ({
      id: q.id,
      questionText: q.questionText,
      questionType: q.questionType,
      options: q.options || this.getDefaultOptions(q.questionType),
      isVisible: true,
      isAnswered: false,
      answer: undefined,
      order: q.order
    }));
    
    this.currentResponses = {};
    this.executionLog = [];
    this.currentStep = 0;
  }
  
  private getDefaultOptions(questionType: string): string[] | undefined {
    switch (questionType.toLowerCase()) {
      case 'yesno':
        return ['Yes', 'No'];
      case 'radio':
        return ['Option 1', 'Option 2', 'Option 3'];
      case 'checkbox':
        return ['Choice A', 'Choice B', 'Choice C'];
      case 'likert':
        return ['Strongly Disagree', 'Disagree', 'Neutral', 'Agree', 'Strongly Agree'];
      default:
        return undefined;
    }
  }
  
  private generateTestScenarios(): void {
    // Generate responses for predefined scenarios
    this.testScenarios.forEach(scenario => {
      scenario.testResponses = {};
      
      this.previewQuestions.forEach(question => {
        switch (scenario.id) {
          case 'yes_scenario':
            if (question.questionType.toLowerCase() === 'yesno') {
              scenario.testResponses[question.id] = 'Yes';
            } else if (question.questionType.toLowerCase() === 'radio' && question.options) {
              scenario.testResponses[question.id] = question.options[0];
            }
            break;
            
          case 'no_scenario':
            if (question.questionType.toLowerCase() === 'yesno') {
              scenario.testResponses[question.id] = 'No';
            } else if (question.questionType.toLowerCase() === 'radio' && question.options) {
              scenario.testResponses[question.id] = question.options[question.options.length - 1];
            }
            break;
            
          case 'mixed_scenario':
            if (question.questionType.toLowerCase() === 'yesno') {
              scenario.testResponses[question.id] = Math.random() > 0.5 ? 'Yes' : 'No';
            } else if (question.questionType.toLowerCase() === 'radio' && question.options) {
              const randomIndex = Math.floor(Math.random() * question.options.length);
              scenario.testResponses[question.id] = question.options[randomIndex];
            } else if (question.questionType.toLowerCase() === 'text') {
              scenario.testResponses[question.id] = `Sample answer ${question.id}`;
            } else if (question.questionType.toLowerCase() === 'number') {
              scenario.testResponses[question.id] = Math.floor(Math.random() * 100) + 1;
            }
            break;
        }
      });
    });
  }
  
  // Scenario management
  applyScenario(scenario: PreviewScenario): void {
    this.selectedScenario = scenario;
    this.currentResponses = { ...scenario.testResponses };
    
    // Update preview questions with scenario responses
    this.previewQuestions.forEach(question => {
      if (scenario.testResponses[question.id] !== undefined) {
        question.answer = scenario.testResponses[question.id];
        question.isAnswered = true;
      } else {
        question.answer = undefined;
        question.isAnswered = false;
      }
    });
    
    this.executeLogic();
  }
  
  clearScenario(): void {
    this.selectedScenario = null;
    this.currentResponses = {};
    this.previewQuestions.forEach(question => {
      question.answer = undefined;
      question.isAnswered = false;
      question.isVisible = true;
    });
    this.executionLog = [];
  }
  
  // Logic execution
  executeLogic(): void {
    this.isExecuting = true;
    this.executionLog = [];
    
    // Reset question visibility
    this.previewQuestions.forEach(question => {
      question.isVisible = true;
    });
    
    setTimeout(() => {
      this.processLogicBlocks();
      this.isExecuting = false;
    }, 500);
  }
  
  private processLogicBlocks(): void {
    for (const block of this.logicBlocks) {
      if (!block.isActive) continue;
      
      this.executeBlock(block);
    }
  }
  
  private executeBlock(block: LogicBlock): void {
    const step: LogicExecutionStep = {
      blockId: block.id,
      blockType: block.type,
      description: this.getBlockDescription(block),
      result: false,
      timestamp: Date.now()
    };
    
    switch (block.type) {
      case 'condition':
        step.result = this.evaluateCondition(block);
        break;
        
      case 'action':
        if (this.shouldExecuteAction(block)) {
          step.result = this.executeAction(block);
        } else {
          step.result = 'Skipped';
        }
        break;
        
      case 'group':
        step.result = this.evaluateGroup(block);
        break;
    }
    
    this.executionLog.push(step);
  }
  
  private evaluateCondition(block: LogicBlock): boolean {
    // Find the question this condition applies to
    const questionId = this.findQuestionIdForBlock(block);
    if (!questionId || this.currentResponses[questionId] === undefined) {
      return false;
    }
    
    const response = this.currentResponses[questionId];
    const { conditionType, conditionValue, conditionValue2 } = block.data;
    
    switch (conditionType) {
      case 'equals':
        return String(response).toLowerCase() === String(conditionValue).toLowerCase();
        
      case 'not_equals':
        return String(response).toLowerCase() !== String(conditionValue).toLowerCase();
        
      case 'greater_than':
        return Number(response) > Number(conditionValue);
        
      case 'less_than':
        return Number(response) < Number(conditionValue);
        
      case 'between':
        const num = Number(response);
        return num >= Number(conditionValue) && num <= Number(conditionValue2);
        
      case 'contains':
        return String(response).toLowerCase().includes(String(conditionValue).toLowerCase());
        
      case 'in_list':
        const list = String(conditionValue).split(',').map(v => v.trim().toLowerCase());
        return list.includes(String(response).toLowerCase());
        
      case 'regex_match':
        try {
          const regex = new RegExp(conditionValue || '');
          return regex.test(String(response));
        } catch {
          return false;
        }
        
      default:
        return false;
    }
  }
  
  private shouldExecuteAction(block: LogicBlock): boolean {
    // For simple preview, assume action should execute if there's a preceding condition that's true
    // In real implementation, this would check the actual logic flow
    return true;
  }
  
  private executeAction(block: LogicBlock): string {
    const { actionType, targetQuestionId, targetQuestionIds, message } = block.data;
    
    switch (actionType) {
      case 'show_question':
        if (targetQuestionId) {
          this.setQuestionVisibility(targetQuestionId, true);
          return `Question ${targetQuestionId} shown`;
        }
        break;
        
      case 'hide_question':
        if (targetQuestionId) {
          this.setQuestionVisibility(targetQuestionId, false);
          return `Question ${targetQuestionId} hidden`;
        }
        break;
        
      case 'show_questions':
        if (targetQuestionIds && targetQuestionIds.length > 0) {
          targetQuestionIds.forEach(id => this.setQuestionVisibility(id, true));
          return `${targetQuestionIds.length} questions shown`;
        }
        break;
        
      case 'hide_questions':
        if (targetQuestionIds && targetQuestionIds.length > 0) {
          targetQuestionIds.forEach(id => this.setQuestionVisibility(id, false));
          return `${targetQuestionIds.length} questions hidden`;
        }
        break;
        
      case 'jump_to_section':
        return `Jump to section ${block.data.targetSectionId}`;
        
      case 'jump_to_question':
        return `Jump to question ${targetQuestionId}`;
        
      case 'end_survey':
        return `Survey ended: ${message || 'Thank you!'}`;
        
      case 'disqualify':
        return `Participant disqualified: ${message || 'You do not qualify'}`;
        
      default:
        return 'Unknown action';
    }
    
    return 'Action executed';
  }
  
  private evaluateGroup(block: LogicBlock): boolean {
    if (!block.data.children || block.data.children.length === 0) {
      return false;
    }
    
    const results = block.data.children.map(child => {
      if (child.type === 'condition') {
        return this.evaluateCondition(child);
      }
      return false;
    });
    
    return block.data.operator === 'AND' 
      ? results.every(r => r) 
      : results.some(r => r);
  }
  
  private setQuestionVisibility(questionId: number, visible: boolean): void {
    const question = this.previewQuestions.find(q => q.id === questionId);
    if (question) {
      question.isVisible = visible;
    }
  }
  
  private findQuestionIdForBlock(block: LogicBlock): number | null {
    // In a real implementation, this would be stored in the block data
    // For preview purposes, assume it applies to the first question with responses
    const answeredQuestions = Object.keys(this.currentResponses).map(Number);
    return answeredQuestions.length > 0 ? answeredQuestions[0] : null;
  }
  
  private getBlockDescription(block: LogicBlock): string {
    switch (block.type) {
      case 'condition':
        const condition = block.data.conditionType;
        const value = block.data.conditionValue;
        return `Check if answer ${condition} "${value}"`;
        
      case 'action':
        const action = block.data.actionType;
        return `Execute ${action?.replace('_', ' ')}`;
        
      case 'group':
        return `Evaluate ${block.data.operator} group with ${block.data.children?.length || 0} conditions`;
        
      default:
        return 'Unknown block';
    }
  }
  
  // Response handling
  onResponseChange(questionId: number, value: any): void {
    this.currentResponses[questionId] = value;
    
    const question = this.previewQuestions.find(q => q.id === questionId);
    if (question) {
      question.answer = value;
      question.isAnswered = value !== undefined && value !== null && value !== '';
    }
    
    // Auto-execute logic on response change
    if (!this.isStepByStep) {
      this.executeLogic();
    }
  }
  
  // Step-by-step execution
  toggleStepByStep(): void {
    this.isStepByStep = !this.isStepByStep;
    this.currentStep = 0;
    
    if (!this.isStepByStep) {
      this.executeLogic();
    }
  }
  
  nextStep(): void {
    if (this.currentStep < this.logicBlocks.length) {
      const block = this.logicBlocks[this.currentStep];
      this.executeBlock(block);
      this.currentStep++;
    }
  }
  
  resetExecution(): void {
    this.executionLog = [];
    this.currentStep = 0;
    this.initializePreview();
  }
  
  // Utility methods
  getQuestionTypeIcon(questionType: string): string {
    switch (questionType.toLowerCase()) {
      case 'text':
      case 'email':
        return 'text_fields';
      case 'number':
        return 'numbers';
      case 'yesno':
        return 'help_outline';
      case 'radio':
        return 'radio_button_checked';
      case 'checkbox':
        return 'check_box';
      case 'likert':
        return 'linear_scale';
      case 'matrix':
        return 'grid_on';
      default:
        return 'help_outline';
    }
  }
  
  getExecutionStepIcon(step: LogicExecutionStep): string {
    switch (step.blockType) {
      case 'condition':
        return step.result ? 'check_circle' : 'cancel';
      case 'action':
        return step.result === 'Skipped' ? 'skip_next' : 'play_arrow';
      case 'group':
        return step.result ? 'check_circle' : 'cancel';
      default:
        return 'help_outline';
    }
  }
  
  getExecutionStepColor(step: LogicExecutionStep): string {
    switch (step.blockType) {
      case 'condition':
        return step.result ? '#4caf50' : '#f44336';
      case 'action':
        return step.result === 'Skipped' ? '#ff9800' : '#2196f3';
      case 'group':
        return step.result ? '#4caf50' : '#f44336';
      default:
        return '#666';
    }
  }
  
  exportExecutionLog(): void {
    const logData = {
      timestamp: new Date().toISOString(),
      scenario: this.selectedScenario?.name || 'Manual Testing',
      responses: this.currentResponses,
      executionLog: this.executionLog,
      finalState: this.previewQuestions.map(q => ({
        id: q.id,
        isVisible: q.isVisible,
        isAnswered: q.isAnswered
      }))
    };
    
    const blob = new Blob([JSON.stringify(logData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `logic-execution-log-${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }
  
  onCheckboxChange(questionId: number, option: string, checked: boolean): void {
    const question = this.previewQuestions.find(q => q.id === questionId);
    if (!question) return;
    
    let currentAnswers = question.answer || [];
    if (!Array.isArray(currentAnswers)) {
      currentAnswers = [];
    }
    
    if (checked) {
      if (!currentAnswers.includes(option)) {
        currentAnswers.push(option);
      }
    } else {
      const index = currentAnswers.indexOf(option);
      if (index > -1) {
        currentAnswers.splice(index, 1);
      }
    }
    
    this.onResponseChange(questionId, currentAnswers);
  }
  
  getConditionCount(): number {
    return this.executionLog.filter(step => step.blockType === 'condition').length;
  }
  
  getActionCount(): number {
    return this.executionLog.filter(step => step.blockType === 'action' && step.result !== 'Skipped').length;
  }
  
  getVisibleQuestionCount(): number {
    return this.previewQuestions.filter(q => q.isVisible).length;
  }

  close(): void {
    this.previewClosed.emit();
  }
}