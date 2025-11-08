import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/authentication/auth.service';
import { finalize, Subject, takeUntil } from 'rxjs';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { UserRole } from '../../../core/models/user.model';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { ErrorHandlerService } from '../../../core/utils/error-handler.service';
import { CustomValidators } from '../../../core/validators/custom-validators';

@Component({
  selector: 'app-register',
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
    RouterModule
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnDestroy {
  registerForm!: FormGroup;
  loading = false;
  hidePassword = true;
  hideConfirmPassword = true;
  serverErrors: string[] = [];
  showServerErrors = false;
  private destroy$ = new Subject<void>();
  
  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private errorHandler: ErrorHandlerService,
    private snackBar: MatSnackBar
  ) {
    this.initializeForm();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.registerForm = this.formBuilder.group({
      firstName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
      phoneNumber: ['', [Validators.pattern(/^[\+]?[1-9][\d]{0,15}$/)]],
      password: ['', [
        Validators.required, 
        Validators.minLength(6),
        Validators.maxLength(100)
        // Uncomment for stronger password validation
        // CustomValidators.strongPassword()
      ]],
      confirmPassword: ['', Validators.required],
      terms: [false, Validators.requiredTrue]
    }, {
      validators: CustomValidators.passwordMatch('password', 'confirmPassword')
    });
  }

  // Enhanced error message methods
  getFirstNameError(): string {
    const control = this.registerForm.get('firstName');
    if (control?.hasError('required')) {
      return 'First name is required';
    }
    if (control?.hasError('minlength')) {
      return 'First name must be at least 2 characters';
    }
    if (control?.hasError('maxlength')) {
      return 'First name cannot exceed 50 characters';
    }
    return '';
  }

  getLastNameError(): string {
    const control = this.registerForm.get('lastName');
    if (control?.hasError('required')) {
      return 'Last name is required';
    }
    if (control?.hasError('minlength')) {
      return 'Last name must be at least 2 characters';
    }
    if (control?.hasError('maxlength')) {
      return 'Last name cannot exceed 50 characters';
    }
    return '';
  }

  getEmailError(): string {
    const control = this.registerForm.get('email');
    if (control?.hasError('required')) {
      return 'Email address is required';
    }
    if (control?.hasError('email')) {
      return 'Please enter a valid email address';
    }
    if (control?.hasError('maxlength')) {
      return 'Email cannot exceed 100 characters';
    }
    return '';
  }

  getPhoneError(): string {
    const control = this.registerForm.get('phoneNumber');
    if (control?.hasError('pattern')) {
      return 'Please enter a valid phone number (e.g., +1234567890)';
    }
    return '';
  }

  getPasswordError(): string {
    const control = this.registerForm.get('password');
    if (control?.hasError('required')) {
      return 'Password is required';
    }
    if (control?.hasError('minlength')) {
      return 'Password must be at least 6 characters long';
    }
    if (control?.hasError('maxlength')) {
      return 'Password cannot exceed 100 characters';
    }
    if (control?.hasError('strongPassword')) {
      return 'Password must contain uppercase, lowercase, number, and special character';
    }
    return '';
  }

  getConfirmPasswordError(): string {
    const control = this.registerForm.get('confirmPassword');
    if (control?.hasError('required')) {
      return 'Please confirm your password';
    }
    if (control?.hasError('passwordMismatch')) {
      return 'Passwords do not match';
    }
    return '';
  }

  getTermsError(): string {
    const control = this.registerForm.get('terms');
    if (control?.hasError('required')) {
      return 'You must agree to the terms and conditions';
    }
    return '';
  }
  
  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword = !this.hideConfirmPassword;
  }

  clearServerErrors(): void {
    this.serverErrors = [];
    this.showServerErrors = false;
  }
  
  onSubmit(): void {
    this.clearServerErrors();
    
    if (this.registerForm.invalid) {
      this.markFormGroupTouched();
      this.snackBar.open('Please correct the errors in the form', 'Close', {
        duration: 4000,
        panelClass: ['error-snackbar']
      });
      return;
    }
    
    this.loading = true;    
    const { firstName, lastName, email, phoneNumber, password } = this.registerForm.value;
    
    // By default, register as a Client role
    const newUser = {
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      email: email.trim().toLowerCase(),
      phoneNumber: phoneNumber?.trim() || undefined // Only include if provided
    };
    
    this.authService.register(newUser, password)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.loading = false)
      )
      .subscribe({
        next: (response) => {
          this.snackBar.open(
            'Registration successful! Please check your email to verify your account.',
            'Close',
            {
              duration: 6000,
              panelClass: ['success-snackbar']
            }
          );
          
          // Navigate to login page after successful registration
          setTimeout(() => {
            this.router.navigate(['/auth/login'], {
              queryParams: { email: email.trim().toLowerCase(), registered: 'true' }
            });
          }, 2000);
        },
        error: (error) => {
          this.handleRegistrationError(error);
        }
      });
  }

  private handleRegistrationError(error: any): void {
    this.serverErrors = [];
    
    if (error.error?.errors) {
      // Handle validation errors from server
      if (Array.isArray(error.error.errors)) {
        this.serverErrors = error.error.errors;
      } else if (typeof error.error.errors === 'object') {
        // Handle ASP.NET ModelState errors
        Object.values(error.error.errors).forEach((errorArray: any) => {
          if (Array.isArray(errorArray)) {
            this.serverErrors.push(...errorArray);
          } else {
            this.serverErrors.push(errorArray);
          }
        });
      } else {
        this.serverErrors.push(error.error.errors);
      }
    } else if (error.error?.message) {
      this.serverErrors.push(error.error.message);
    } else if (error.status === 400) {
      this.serverErrors.push('Invalid registration data. Please check your information.');
    } else if (error.status === 409) {
      this.serverErrors.push('An account with this email already exists.');
    } else if (error.status === 0) {
      this.serverErrors.push('Unable to connect to the server. Please try again later.');
    } else {
      this.serverErrors.push('Registration failed. Please try again.');
    }

    this.showServerErrors = this.serverErrors.length > 0;
    
    // Show error in snackbar as well
    const errorMessage = this.serverErrors.length > 0 ? 
      this.serverErrors[0] : 
      'Registration failed. Please try again.';
    
    this.snackBar.open(errorMessage, 'Close', {
      duration: 6000,
      panelClass: ['error-snackbar']
    });
  }

  private markFormGroupTouched(): void {
    Object.keys(this.registerForm.controls).forEach(key => {
      const control = this.registerForm.get(key);
      control?.markAsTouched();
    });
  }
}