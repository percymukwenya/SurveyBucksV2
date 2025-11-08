import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../authentication/auth.service';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);
  const authService = inject(AuthService);
  
  return next(req).pipe(
    catchError(error => {
      let message = 'An unexpected error occurred';
      let shouldNavigate = false;
      let navigationPath = '/';

      switch (error.status) {
        case 400:
          message = error.error?.message || 'Bad request. Please check your input.';
          break;
          
        case 401:
          // Only logout if user was previously authenticated
          if (authService.isAuthenticated) {
            authService.logout();
            message = 'Your session has expired. Please log in again.';
            shouldNavigate = true;
            navigationPath = '/auth/login';
          } else {
            message = error.error?.message || 'Authentication required.';
          }
          break;
          
        case 403:
          message = 'You do not have permission to access this resource.';
          // Navigate back to appropriate dashboard based on user role
          if (authService.isAuthenticated) {
            shouldNavigate = true;
            if (authService.isAdmin()) {
              navigationPath = '/admin/dashboard';
            } else if (authService.isClient()) {
              navigationPath = '/client/dashboard';
            }
          }
          break;
          
        case 404:
          message = 'The requested resource was not found.';
          break;
          
        case 422:
          // Validation errors from server
          if (error.error?.errors) {
            message = Array.isArray(error.error.errors) 
              ? error.error.errors.join(', ')
              : error.error.errors;
          } else {
            message = error.error?.message || 'Validation failed.';
          }
          break;
          
        case 429:
          message = 'Too many requests. Please try again later.';
          break;
          
        case 500:
          message = 'A server error occurred. Please try again later.';
          break;
          
        case 503:
          message = 'Service temporarily unavailable. Please try again later.';
          break;
          
        case 0:
          message = 'Unable to connect to the server. Please check your internet connection.';
          break;
          
        default:
          message = error.error?.message || `HTTP Error ${error.status}: ${error.statusText}`;
      }

      // Show error message (unless it's a 401 during login attempt)
      const isLoginAttempt = req.url.includes('/auth/login') && error.status === 401;
      if (!isLoginAttempt) {
        snackBar.open(message, 'Close', {
          duration: error.status >= 500 ? 8000 : 5000,
          horizontalPosition: 'end',
          verticalPosition: 'top',
          panelClass: ['error-snackbar']
        });
      }

      // Navigate if needed
      if (shouldNavigate) {
        setTimeout(() => router.navigate([navigationPath]), 100);
      }

      // Return the original error for component handling
      return throwError(() => ({
        ...error,
        error: {
          ...error.error,
          message
        }
      }));
    })
  );
};