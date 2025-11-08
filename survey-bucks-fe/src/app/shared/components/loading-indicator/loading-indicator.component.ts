import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { LoadingTimerService, LoadingState } from '../../../core/services/loading-timer.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-loading-indicator',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './loading-indicator.component.html',
  styleUrls: ['./loading-indicator.component.scss']
})
export class LoadingIndicatorComponent implements OnInit, OnDestroy {
  @Input() loadingKey!: string;
  @Input() message: string = 'Loading...';
  @Input() showElapsedTime: boolean = true;
  @Input() showCancelButton: boolean = false;
  @Input() type: 'spinner' | 'bar' = 'spinner';
  @Input() size: 'small' | 'medium' | 'large' = 'medium';

  @Output() cancel = new EventEmitter<void>();

  loadingState: LoadingState = {
    isLoading: false,
    elapsedSeconds: 0,
    message: '',
    showCancel: false
  };

  private destroy$ = new Subject<void>();

  constructor(private loadingTimerService: LoadingTimerService) {}

  ngOnInit(): void {
    if (this.loadingKey) {
      this.loadingTimerService.getLoadingState(this.loadingKey)
        .pipe(takeUntil(this.destroy$))
        .subscribe(state => {
          this.loadingState = state;
        });
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onCancel(): void {
    this.cancel.emit();
  }

  getSpinnerDiameter(): number {
    switch (this.size) {
      case 'small': return 30;
      case 'medium': return 50;
      case 'large': return 70;
      default: return 50;
    }
  }
}
