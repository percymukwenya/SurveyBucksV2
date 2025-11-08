import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

/**
 * Reusable empty state component for consistent UX
 *
 * Usage:
 * <app-empty-state
 *   icon="assignment"
 *   title="No surveys available"
 *   description="Check back soon for new survey opportunities!"
 *   actionLabel="Refresh"
 *   (action)="refreshSurveys()">
 * </app-empty-state>
 */
@Component({
  selector: 'app-empty-state',
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
  /** Material icon name to display */
  @Input() icon: string = 'inbox';

  /** Main title text */
  @Input() title: string = 'No items found';

  /** Descriptive message */
  @Input() description: string = '';

  /** Primary action button label (optional) */
  @Input() actionLabel?: string;

  /** Secondary action button label (optional) */
  @Input() secondaryActionLabel?: string;

  /** Size variant */
  @Input() size: 'small' | 'medium' | 'large' = 'medium';

  /** Primary action click event */
  @Output() action = new EventEmitter<void>();

  /** Secondary action click event */
  @Output() secondaryAction = new EventEmitter<void>();

  onAction(): void {
    this.action.emit();
  }

  onSecondaryAction(): void {
    this.secondaryAction.emit();
  }
}
