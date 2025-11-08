import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService {
  constructor(private snackBar: MatSnackBar) {}

  handleError(error: any, defaultMessage: string = 'An error occurred'): void {
    let message = defaultMessage;
    
    if (error instanceof HttpErrorResponse) {
      // Handle HTTP errors
      if (error.error?.message) {
        message = error.error.message;
      } else if (error.error?.errors && Array.isArray(error.error.errors)) {
        message = error.error.errors.join(', ');
      } else if (error.status === 0) {
        message = 'Unable to connect to the server. Please check your internet connection.';
      } else if (error.status >= 500) {
        message = 'Server error. Please try again later.';
      }
    } else if (error?.message) {
      message = error.message;
    }

    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }

  handleSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass: ['success-snackbar']
    });
  }
}