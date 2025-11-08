import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/authentication/auth.service';
import { finalize, Subject, takeUntil } from 'rxjs';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { ErrorHandlerService } from '../../../core/utils/error-handler.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatCheckboxModule,
    MatIconModule,
    MatDividerModule,
    RouterModule,
    FormsModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit, OnDestroy {
  loginForm!: FormGroup;
  loading = false;
  hidePassword = true;
  serverErrors: string[] = [];
  showServerErrors = false;
  isAccountLocked = false;
  lockoutMinutes = 0;
  requiresEmailConfirmation = false;
  userEmail = '';
  resendingConfirmation = false;
  private destroy$ = new Subject<void>();
  
  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private errorHandler: ErrorHandlerService,
    private snackBar: MatSnackBar
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    // Pre-fill email from query params (e.g., from registration)
    this.route.queryParams.subscribe(params => {
      if (params['email']) {
        this.loginForm.patchValue({ email: params['email'] });
      }
      if (params['registered']) {
        this.snackBar.open('Account created! Please check your email to verify your account.', 'Close', {
          duration: 6000,
          panelClass: ['success-snackbar']
        });
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false]
    });
  }

  // Enhanced error message methods
  getEmailError(): string {
    const control = this.loginForm.get('email');
    if (control?.hasError('required')) {
      return 'Email address is required';
    }
    if (control?.hasError('email')) {
      return 'Please enter a valid email address';
    }
    return '';
  }

  getPasswordError(): string {
    const control = this.loginForm.get('password');
    if (control?.hasError('required')) {
      return 'Password is required';
    }
    if (control?.hasError('minlength')) {
      return 'Password must be at least 6 characters';
    }
    return '';
  }

  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  clearServerErrors(): void {
    this.serverErrors = [];
    this.showServerErrors = false;
    this.isAccountLocked = false;
    this.requiresEmailConfirmation = false;
  }

  onSubmit(): void {
    this.clearServerErrors();
    
    if (this.loginForm.invalid) {
      this.markFormGroupTouched();
      this.snackBar.open('Please correct the errors in the form', 'Close', {
        duration: 4000,
        panelClass: ['error-snackbar']
      });
      return;
    }

    this.loading = true;
    const { email, password } = this.loginForm.value;
    this.userEmail = email.trim().toLowerCase();

    this.authService.login(this.userEmail, password)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.loading = false)
      )
      .subscribe({
        next: (user) => {
          this.snackBar.open(`Welcome back to SurveyBucks, ${user.firstName}!`, 'Close', {
            duration: 4000,
            panelClass: ['success-snackbar']
          });
          
          // Small delay to show success message before redirect
          setTimeout(() => {
            this.authService.redirectBasedOnRole();
          }, 500);
        },
        error: (error) => {
          this.handleLoginError(error);
        }
      });
  }

  private handleLoginError(error: any): void {
    this.serverErrors = [];
    this.isAccountLocked = false;
    this.requiresEmailConfirmation = false;

    if (error.status === 423) {
      // Account locked
      this.isAccountLocked = true;
      this.lockoutMinutes = error.error?.lockoutMinutes || 0;
      this.serverErrors.push(
        `Your account has been temporarily locked due to multiple failed login attempts. ` +
        `Please try again in ${this.lockoutMinutes} minutes or reset your password.`
      );
    } else if (error.error?.requiresEmailConfirmation) {
      // Email confirmation required
      this.requiresEmailConfirmation = true;
      this.serverErrors.push('Please confirm your email address before signing in.');
    } else if (error.error?.message) {
      // Server provided error message
      this.serverErrors.push(error.error.message);
    } else if (error.status === 401) {
      this.serverErrors.push('Invalid email or password. Please check your credentials and try again.');
    } else if (error.status === 400) {
      this.serverErrors.push('Please check your email and password.');
    } else if (error.status === 0) {
      this.serverErrors.push('Unable to connect to SurveyBucks servers. Please check your internet connection.');
    } else {
      this.serverErrors.push('Sign in failed. Please try again.');
    }

    this.showServerErrors = this.serverErrors.length > 0;
    
    // Show error in snackbar
    const errorMessage = this.serverErrors.length > 0 ? 
      this.serverErrors[0] : 
      'Sign in failed. Please try again.';
    
    this.snackBar.open(errorMessage, 'Close', {
      duration: 6000,
      panelClass: ['error-snackbar']
    });
  }

  onResendConfirmation(): void {
    if (!this.userEmail) {
      this.snackBar.open('Please enter your email address', 'Close', {
        duration: 4000,
        panelClass: ['error-snackbar']
      });
      return;
    }

    this.resendingConfirmation = true;
    
    // You'll need to implement this method in your AuthService
    this.authService.resendEmailConfirmation(this.userEmail)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.resendingConfirmation = false)
      )
      .subscribe({
        next: () => {
          this.snackBar.open('Confirmation email sent! Please check your inbox.', 'Close', {
            duration: 6000,
            panelClass: ['success-snackbar']
          });
        },
        error: (error) => {
          this.snackBar.open('Failed to send confirmation email. Please try again.', 'Close', {
            duration: 4000,
            panelClass: ['error-snackbar']
          });
        }
      });
  }

  private markFormGroupTouched(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      const control = this.loginForm.get(key);
      control?.markAsTouched();
    });
  }
}