import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export interface ConfirmationDialogData {
  title: string;
  message: string;
  confirmText: string;
  cancelText: string;
  isDestructive?: boolean;
}

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule
  ],
  template: `
    <h2 mat-dialog-title>{{data.title}}</h2>
    <mat-dialog-content>
      <p>{{data.message}}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{data.cancelText}}</button>
      <button 
        mat-flat-button 
        [color]="data.isDestructive ? 'warn' : 'primary'" 
        [mat-dialog-close]="true">
        {{data.confirmText || 'Confirm'}}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 {
      margin: 0;
    }
    
    mat-dialog-content {
      margin: 16px 0;
    }
    
    mat-dialog-actions {
      padding-bottom: 16px;
    }
  `]
})
export class ConfirmationDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<ConfirmationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmationDialogData
  ) {}
}
