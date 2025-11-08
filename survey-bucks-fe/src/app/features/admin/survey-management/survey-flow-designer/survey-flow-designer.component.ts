import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';
import { AdminSurveyService } from '../../../../core/services/admin-survey.service';

interface FlowNode {
  id: string;
  type: 'question' | 'section' | 'decision' | 'termination';
  x: number;
  y: number;
  width: number;
  height: number;
  data: {
    questionId?: number;
    sectionId?: number;
    title: string;
    subtitle?: string;
    questionType?: string;
    hasLogic?: boolean;
    isTerminal?: boolean;
  };
  connections: FlowConnection[];
}

interface FlowConnection {
  id: string;
  sourceNodeId: string;
  targetNodeId: string;
  condition?: string;
  label?: string;
  path: { x: number; y: number }[];
}

interface SurveyFlowData {
  nodes: FlowNode[];
  connections: FlowConnection[];
  sections: any[];
  questions: any[];
  logicRules: any[];
}

@Component({
  selector: 'app-survey-flow-designer',
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './survey-flow-designer.component.html',
  styleUrl: './survey-flow-designer.component.scss'
})
export class SurveyFlowDesignerComponent implements OnInit {
  @ViewChild('flowCanvas', { static: true }) flowCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('flowContainer', { static: true }) flowContainer!: ElementRef<HTMLDivElement>;
  
  surveyId!: number;
  survey: any;
  loading = false;
  
  // Canvas properties
  private canvas!: HTMLCanvasElement;
  private ctx!: CanvasRenderingContext2D;
  private scale = 1;
  private offsetX = 0;
  private offsetY = 0;
  private isDragging = false;
  private dragStartX = 0;
  private dragStartY = 0;
  
  // Flow data
  flowData: SurveyFlowData = {
    nodes: [],
    connections: [],
    sections: [],
    questions: [],
    logicRules: []
  };
  
  selectedNode: FlowNode | null = null;
  hoveredNode: FlowNode | null = null;
  
  // Layout constants
  private readonly NODE_WIDTH = 200;
  private readonly NODE_HEIGHT = 80;
  private readonly SECTION_HEIGHT = 120;
  private readonly HORIZONTAL_SPACING = 300;
  private readonly VERTICAL_SPACING = 150;
  
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adminSurveyService: AdminSurveyService,
    private snackBar: MatSnackBar
  ) {}
  
  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.surveyId = +params['id'];
        this.initializeCanvas();
        this.loadSurveyFlowData();
      }
    });
  }
  
  ngAfterViewInit(): void {
    this.setupEventListeners();
  }
  
  private initializeCanvas(): void {
    this.canvas = this.flowCanvas.nativeElement;
    this.ctx = this.canvas.getContext('2d')!;
    this.resizeCanvas();
  }
  
  private setupEventListeners(): void {
    // Canvas event listeners
    this.canvas.addEventListener('mousedown', this.onMouseDown.bind(this));
    this.canvas.addEventListener('mousemove', this.onMouseMove.bind(this));
    this.canvas.addEventListener('mouseup', this.onMouseUp.bind(this));
    this.canvas.addEventListener('wheel', this.onWheel.bind(this));
    this.canvas.addEventListener('click', this.onClick.bind(this));
    
    // Window resize
    window.addEventListener('resize', this.resizeCanvas.bind(this));
  }
  
  private resizeCanvas(): void {
    const container = this.flowContainer.nativeElement;
    this.canvas.width = container.clientWidth;
    this.canvas.height = container.clientHeight;
    this.render();
  }
  
  private loadSurveyFlowData(): void {
    this.loading = true;
    
    this.adminSurveyService.getSurveyById(this.surveyId)
      .pipe(
        switchMap(survey => {
          this.survey = survey;
          return this.adminSurveyService.getSurveyQuestions(this.surveyId);
        }),
        switchMap(questions => {
          this.flowData.questions = questions;
          // TODO: Load sections and logic rules
          return of([]);
        }),
        catchError(error => {
          console.error('Error loading survey flow data', error);
          this.snackBar.open('Error loading survey flow data. Please try again.', 'Close', {
            duration: 5000
          });
          return of(null);
        }),
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe(() => {
        this.generateFlowNodes();
        this.render();
      });
  }
  
  private generateFlowNodes(): void {
    this.flowData.nodes = [];
    this.flowData.connections = [];
    
    let currentX = 50;
    let currentY = 50;
    
    // Group questions by section
    const sectionGroups = this.groupQuestionsBySection();
    
    for (const [sectionId, questions] of sectionGroups.entries()) {
      // Create section header node
      const sectionNode: FlowNode = {
        id: `section-${sectionId}`,
        type: 'section',
        x: currentX,
        y: currentY,
        width: this.NODE_WIDTH,
        height: this.SECTION_HEIGHT,
        data: {
          sectionId: sectionId,
          title: `Section ${sectionId}`,
          subtitle: `${questions.length} questions`
        },
        connections: []
      };
      this.flowData.nodes.push(sectionNode);
      
      currentY += this.SECTION_HEIGHT + 20;
      
      // Create question nodes for this section
      for (let i = 0; i < questions.length; i++) {
        const question = questions[i];
        const questionNode: FlowNode = {
          id: `question-${question.id}`,
          type: 'question',
          x: currentX + 50,
          y: currentY,
          width: this.NODE_WIDTH,
          height: this.NODE_HEIGHT,
          data: {
            questionId: question.id,
            title: `Q${question.order}`,
            subtitle: this.truncateText(question.questionText, 30),
            questionType: question.questionType,
            hasLogic: false // TODO: Check from logic rules
          },
          connections: []
        };
        this.flowData.nodes.push(questionNode);
        
        // Connect to next question or create decision nodes for branching
        if (i < questions.length - 1) {
          const connection: FlowConnection = {
            id: `conn-${question.id}-${questions[i + 1].id}`,
            sourceNodeId: questionNode.id,
            targetNodeId: `question-${questions[i + 1].id}`,
            path: this.calculateConnectionPath(questionNode, questionNode) // Will be recalculated
          };
          questionNode.connections.push(connection);
          this.flowData.connections.push(connection);
        }
        
        currentY += this.NODE_HEIGHT + this.VERTICAL_SPACING;
      }
      
      currentX += this.HORIZONTAL_SPACING;
      currentY = 50;
    }
    
    // Create termination nodes
    this.createTerminationNodes();
  }
  
  private groupQuestionsBySection(): Map<number, any[]> {
    const groups = new Map<number, any[]>();
    
    for (const question of this.flowData.questions) {
      const sectionId = question.sectionId || 1;
      if (!groups.has(sectionId)) {
        groups.set(sectionId, []);
      }
      groups.get(sectionId)!.push(question);
    }
    
    return groups;
  }
  
  private createTerminationNodes(): void {
    const lastColumn = Math.max(...this.flowData.nodes.map(n => n.x)) + this.HORIZONTAL_SPACING;
    
    // Survey completion node
    const completionNode: FlowNode = {
      id: 'completion',
      type: 'termination',
      x: lastColumn,
      y: 200,
      width: this.NODE_WIDTH,
      height: this.NODE_HEIGHT,
      data: {
        title: 'Survey Complete',
        subtitle: 'Thank you for participating',
        isTerminal: true
      },
      connections: []
    };
    this.flowData.nodes.push(completionNode);
    
    // Disqualification node
    const disqualNode: FlowNode = {
      id: 'disqualification',
      type: 'termination',
      x: lastColumn,
      y: 350,
      width: this.NODE_WIDTH,
      height: this.NODE_HEIGHT,
      data: {
        title: 'Disqualified',
        subtitle: 'Thank you for your time',
        isTerminal: true
      },
      connections: []
    };
    this.flowData.nodes.push(disqualNode);
  }
  
  private calculateConnectionPath(source: FlowNode, target: FlowNode): { x: number; y: number }[] {
    const startX = source.x + source.width;
    const startY = source.y + source.height / 2;
    const endX = target.x;
    const endY = target.y + target.height / 2;
    
    // Simple straight line for now
    return [
      { x: startX, y: startY },
      { x: endX, y: endY }
    ];
  }
  
  private render(): void {
    if (!this.ctx) return;
    
    // Clear canvas
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    
    // Apply transformations
    this.ctx.save();
    this.ctx.scale(this.scale, this.scale);
    this.ctx.translate(this.offsetX, this.offsetY);
    
    // Draw grid
    this.drawGrid();
    
    // Draw connections first (so they appear behind nodes)
    this.drawConnections();
    
    // Draw nodes
    this.drawNodes();
    
    this.ctx.restore();
  }
  
  private drawGrid(): void {
    const gridSize = 50;
    const startX = Math.floor(-this.offsetX / gridSize) * gridSize;
    const startY = Math.floor(-this.offsetY / gridSize) * gridSize;
    const endX = startX + (this.canvas.width / this.scale) + gridSize;
    const endY = startY + (this.canvas.height / this.scale) + gridSize;
    
    this.ctx.strokeStyle = '#f0f0f0';
    this.ctx.lineWidth = 1;
    
    // Vertical lines
    for (let x = startX; x < endX; x += gridSize) {
      this.ctx.beginPath();
      this.ctx.moveTo(x, startY);
      this.ctx.lineTo(x, endY);
      this.ctx.stroke();
    }
    
    // Horizontal lines
    for (let y = startY; y < endY; y += gridSize) {
      this.ctx.beginPath();
      this.ctx.moveTo(startX, y);
      this.ctx.lineTo(endX, y);
      this.ctx.stroke();
    }
  }
  
  private drawConnections(): void {
    for (const connection of this.flowData.connections) {
      this.drawConnection(connection);
    }
  }
  
  private drawConnection(connection: FlowConnection): void {
    if (connection.path.length < 2) return;
    
    this.ctx.strokeStyle = '#666';
    this.ctx.lineWidth = 2;
    this.ctx.beginPath();
    
    const start = connection.path[0];
    this.ctx.moveTo(start.x, start.y);
    
    for (let i = 1; i < connection.path.length; i++) {
      const point = connection.path[i];
      this.ctx.lineTo(point.x, point.y);
    }
    
    this.ctx.stroke();
    
    // Draw arrow head
    if (connection.path.length >= 2) {
      const end = connection.path[connection.path.length - 1];
      const beforeEnd = connection.path[connection.path.length - 2];
      this.drawArrowHead(beforeEnd.x, beforeEnd.y, end.x, end.y);
    }
    
    // Draw label if exists
    if (connection.label) {
      const midPoint = this.getMidPoint(connection.path);
      this.drawConnectionLabel(midPoint.x, midPoint.y, connection.label);
    }
  }
  
  private drawArrowHead(fromX: number, fromY: number, toX: number, toY: number): void {
    const angle = Math.atan2(toY - fromY, toX - fromX);
    const arrowLength = 10;
    const arrowAngle = Math.PI / 6;
    
    this.ctx.fillStyle = '#666';
    this.ctx.beginPath();
    this.ctx.moveTo(toX, toY);
    this.ctx.lineTo(
      toX - arrowLength * Math.cos(angle - arrowAngle),
      toY - arrowLength * Math.sin(angle - arrowAngle)
    );
    this.ctx.lineTo(
      toX - arrowLength * Math.cos(angle + arrowAngle),
      toY - arrowLength * Math.sin(angle + arrowAngle)
    );
    this.ctx.closePath();
    this.ctx.fill();
  }
  
  private getMidPoint(path: { x: number; y: number }[]): { x: number; y: number } {
    const midIndex = Math.floor(path.length / 2);
    return path[midIndex];
  }
  
  private drawConnectionLabel(x: number, y: number, label: string): void {
    this.ctx.fillStyle = 'white';
    this.ctx.strokeStyle = '#ccc';
    this.ctx.lineWidth = 1;
    
    const padding = 4;
    const textWidth = this.ctx.measureText(label).width;
    const rectWidth = textWidth + padding * 2;
    const rectHeight = 20;
    
    // Draw background
    this.ctx.fillRect(x - rectWidth / 2, y - rectHeight / 2, rectWidth, rectHeight);
    this.ctx.strokeRect(x - rectWidth / 2, y - rectHeight / 2, rectWidth, rectHeight);
    
    // Draw text
    this.ctx.fillStyle = '#333';
    this.ctx.font = '12px Arial';
    this.ctx.textAlign = 'center';
    this.ctx.textBaseline = 'middle';
    this.ctx.fillText(label, x, y);
  }
  
  private drawNodes(): void {
    for (const node of this.flowData.nodes) {
      this.drawNode(node);
    }
  }
  
  private drawNode(node: FlowNode): void {
    const isSelected = this.selectedNode?.id === node.id;
    const isHovered = this.hoveredNode?.id === node.id;
    
    // Node background
    this.ctx.fillStyle = this.getNodeBackgroundColor(node, isSelected, isHovered);
    this.ctx.strokeStyle = this.getNodeBorderColor(node, isSelected);
    this.ctx.lineWidth = isSelected ? 3 : 1;
    
    this.ctx.fillRect(node.x, node.y, node.width, node.height);
    this.ctx.strokeRect(node.x, node.y, node.width, node.height);
    
    // Node icon/indicator
    this.drawNodeIcon(node);
    
    // Node text
    this.drawNodeText(node);
    
    // Logic indicator
    if (node.data.hasLogic) {
      this.drawLogicIndicator(node);
    }
  }
  
  private getNodeBackgroundColor(node: FlowNode, isSelected: boolean, isHovered: boolean): string {
    if (isSelected) return '#e3f2fd';
    if (isHovered) return '#f3f8ff';
    
    switch (node.type) {
      case 'section': return '#fff3e0';
      case 'question': return '#ffffff';
      case 'decision': return '#f1f8e9';
      case 'termination': return node.data.isTerminal ? '#ffebee' : '#e8f5e8';
      default: return '#ffffff';
    }
  }
  
  private getNodeBorderColor(node: FlowNode, isSelected: boolean): string {
    if (isSelected) return '#2196f3';
    
    switch (node.type) {
      case 'section': return '#ff9800';
      case 'question': return '#e0e0e0';
      case 'decision': return '#4caf50';
      case 'termination': return node.data.isTerminal ? '#f44336' : '#4caf50';
      default: return '#e0e0e0';
    }
  }
  
  private drawNodeIcon(node: FlowNode): void {
    const iconX = node.x + 8;
    const iconY = node.y + 8;
    const iconSize = 16;
    
    this.ctx.fillStyle = this.getNodeBorderColor(node, false);
    this.ctx.font = '16px Material Icons';
    this.ctx.textAlign = 'left';
    this.ctx.textBaseline = 'top';
    
    let icon = '';
    switch (node.type) {
      case 'section': icon = '📁'; break;
      case 'question': icon = '❓'; break;
      case 'decision': icon = '🔀'; break;
      case 'termination': icon = node.data.isTerminal ? '🚫' : '✅'; break;
    }
    
    this.ctx.fillText(icon, iconX, iconY);
  }
  
  private drawNodeText(node: FlowNode): void {
    const textX = node.x + 8;
    const titleY = node.y + 35;
    const subtitleY = node.y + 55;
    
    // Title
    this.ctx.fillStyle = '#333';
    this.ctx.font = 'bold 14px Arial';
    this.ctx.textAlign = 'left';
    this.ctx.textBaseline = 'top';
    this.ctx.fillText(this.truncateText(node.data.title, 20), textX, titleY);
    
    // Subtitle
    if (node.data.subtitle) {
      this.ctx.fillStyle = '#666';
      this.ctx.font = '12px Arial';
      this.ctx.fillText(this.truncateText(node.data.subtitle, 25), textX, subtitleY);
    }
  }
  
  private drawLogicIndicator(node: FlowNode): void {
    const indicatorX = node.x + node.width - 20;
    const indicatorY = node.y + 8;
    
    this.ctx.fillStyle = '#4caf50';
    this.ctx.beginPath();
    this.ctx.arc(indicatorX, indicatorY, 6, 0, 2 * Math.PI);
    this.ctx.fill();
    
    this.ctx.fillStyle = 'white';
    this.ctx.font = '10px Arial';
    this.ctx.textAlign = 'center';
    this.ctx.textBaseline = 'middle';
    this.ctx.fillText('L', indicatorX, indicatorY);
  }
  
  private truncateText(text: string, maxLength: number): string {
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
  }
  
  // Event handlers
  private onMouseDown(event: MouseEvent): void {
    const rect = this.canvas.getBoundingClientRect();
    const x = (event.clientX - rect.left) / this.scale - this.offsetX;
    const y = (event.clientY - rect.top) / this.scale - this.offsetY;
    
    const clickedNode = this.getNodeAt(x, y);
    if (clickedNode) {
      this.selectedNode = clickedNode;
    } else {
      this.selectedNode = null;
      this.isDragging = true;
      this.dragStartX = event.clientX;
      this.dragStartY = event.clientY;
    }
    
    this.render();
  }
  
  private onMouseMove(event: MouseEvent): void {
    const rect = this.canvas.getBoundingClientRect();
    const x = (event.clientX - rect.left) / this.scale - this.offsetX;
    const y = (event.clientY - rect.top) / this.scale - this.offsetY;
    
    if (this.isDragging) {
      const deltaX = (event.clientX - this.dragStartX) / this.scale;
      const deltaY = (event.clientY - this.dragStartY) / this.scale;
      
      this.offsetX += deltaX;
      this.offsetY += deltaY;
      
      this.dragStartX = event.clientX;
      this.dragStartY = event.clientY;
      
      this.render();
    } else {
      const hoveredNode = this.getNodeAt(x, y);
      if (hoveredNode !== this.hoveredNode) {
        this.hoveredNode = hoveredNode;
        this.render();
      }
      
      // Update cursor
      this.canvas.style.cursor = hoveredNode ? 'pointer' : 'grab';
    }
  }
  
  private onMouseUp(event: MouseEvent): void {
    this.isDragging = false;
    this.canvas.style.cursor = 'default';
  }
  
  private onWheel(event: WheelEvent): void {
    event.preventDefault();
    
    const zoomFactor = 0.1;
    const rect = this.canvas.getBoundingClientRect();
    const mouseX = event.clientX - rect.left;
    const mouseY = event.clientY - rect.top;
    
    const wheel = event.deltaY < 0 ? 1 : -1;
    const zoom = Math.exp(wheel * zoomFactor);
    
    this.scale *= zoom;
    this.scale = Math.max(0.1, Math.min(3, this.scale));
    
    this.render();
  }
  
  private onClick(event: MouseEvent): void {
    // Handle double-click to edit node
    if (event.detail === 2 && this.selectedNode) {
      this.editNode(this.selectedNode);
    }
  }
  
  private getNodeAt(x: number, y: number): FlowNode | null {
    for (const node of this.flowData.nodes) {
      if (x >= node.x && x <= node.x + node.width &&
          y >= node.y && y <= node.y + node.height) {
        return node;
      }
    }
    return null;
  }
  
  // Public methods
  resetView(): void {
    this.scale = 1;
    this.offsetX = 0;
    this.offsetY = 0;
    this.render();
  }
  
  fitToView(): void {
    if (this.flowData.nodes.length === 0) return;
    
    const bounds = this.getFlowBounds();
    const padding = 50;
    
    const scaleX = (this.canvas.width - padding * 2) / bounds.width;
    const scaleY = (this.canvas.height - padding * 2) / bounds.height;
    this.scale = Math.min(scaleX, scaleY, 1);
    
    this.offsetX = padding / this.scale - bounds.minX;
    this.offsetY = padding / this.scale - bounds.minY;
    
    this.render();
  }
  
  private getFlowBounds(): { minX: number; minY: number; width: number; height: number } {
    const xs = this.flowData.nodes.map(n => n.x);
    const ys = this.flowData.nodes.map(n => n.y);
    const widths = this.flowData.nodes.map(n => n.x + n.width);
    const heights = this.flowData.nodes.map(n => n.y + n.height);
    
    const minX = Math.min(...xs);
    const minY = Math.min(...ys);
    const maxX = Math.max(...widths);
    const maxY = Math.max(...heights);
    
    return {
      minX,
      minY,
      width: maxX - minX,
      height: maxY - minY
    };
  }
  
  editNode(node: FlowNode): void {
    if (node.type === 'question') {
      // Navigate to logic manager for this question
      this.router.navigate(['/admin/surveys/logic', this.surveyId], {
        queryParams: { questionId: node.data.questionId }
      });
    }
  }
  
  exportFlow(): void {
    // TODO: Implement flow export functionality
    this.snackBar.open('Flow export functionality coming soon!', 'Close', { duration: 3000 });
  }
  
  zoomIn(): void {
    this.scale = Math.min(3, this.scale * 1.2);
    this.render();
  }
  
  zoomOut(): void {
    this.scale = Math.max(0.1, this.scale / 1.2);
    this.render();
  }
  
  getQuestionCount(): number {
    return this.flowData.nodes.filter(n => n.type === 'question').length;
  }
  
  getSectionCount(): number {
    return this.flowData.nodes.filter(n => n.type === 'section').length;
  }
  
  getLogicRuleCount(): number {
    return this.flowData.logicRules.length;
  }
  
  getConnectionCount(): number {
    return this.flowData.connections.length;
  }
  
  goBack(): void {
    this.router.navigate(['/admin/surveys/edit', this.surveyId]);
  }
}