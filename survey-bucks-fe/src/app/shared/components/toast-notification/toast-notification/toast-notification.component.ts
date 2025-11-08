// src/app/shared/components/toast-notification/toast-notification.component.ts
import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_SNACK_BAR_DATA, MatSnackBarRef, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-toast-notification',
  standalone: true,
  imports: [
    CommonModule,
    MatSnackBarModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="toast-container" [ngClass]="data.type">
      <mat-icon *ngIf="data.icon">{{data.icon}}</mat-icon>
      <span class="message">{{data.message}}</span>
      <button *ngIf="data.action" mat-button (click)="snackBarRef.dismiss()">
        {{data.action}}
      </button>
    </div>
  `,
  styles: [`
    .toast-container {
      display: flex;
      align-items: center;
      padding: 8px 16px;
      border-radius: 4px;
      
      mat-icon {
        margin-right: 12px;
      }
      
      .message {
        flex: 1;
      }
      
      &.success {
        background-color: #e8f5e9;
        color: #2e7d32;
        
        mat-icon {
          color: #2e7d32;
        }
      }
      
      &.info {
        background-color: #e3f2fd;
        color: #1565c0;
        
        mat-icon {
          color: #1565c0;
        }
      }
      
      &.warning {
        background-color: #fff8e1;
        color: #f57f17;
        
        mat-icon {
          color: #f57f17;
        }
      }
      
      &.error {
        background-color: #ffebee;
        color: #c62828;
        
        mat-icon {
          color: #c62828;
        }
      }
    }
  `]
})
export class ToastNotificationComponent {
  constructor(
    public snackBarRef: MatSnackBarRef<ToastNotificationComponent>,
    @Inject(MAT_SNACK_BAR_DATA) public data: any
  ) { }
}