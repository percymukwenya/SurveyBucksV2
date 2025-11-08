import { CommonModule } from '@angular/common';
import { Component, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterModule } from '@angular/router';
import { finalize, Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../../core/authentication/auth.service';
import { ErrorHandlerService } from '../../../core/utils/error-handler.service';

@Component({
  selector: 'app-forgot-password',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    RouterModule
  ],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent implements OnDestroy {
  forgotPasswordForm!: FormGroup;
  loading = false;
  emailSent = false;
  resendLoading = false;
  resendCooldown = 0;
  private destroy$ = new Subject<void>();
  private cooldownInterval?: any;

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private errorHandler: ErrorHandlerService
  ) {
    this.initializeForm();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.cooldownInterval) {
      clearInterval(this.cooldownInterval);
    }
  }

  private initializeForm(): void {
    this.forgotPasswordForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit(): void {
    if (this.forgotPasswordForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.loading = true;
    const { email } = this.forgotPasswordForm.value;

    this.authService.forgotPassword(email)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.loading = false)
      )
      .subscribe({
        next: () => {
          this.emailSent = true;
          this.startResendCooldown();
        },
        error: (error) => {
          // Don't show the actual error for security reasons
          this.emailSent = true;
          this.startResendCooldown();
        }
      });
  }

  resendEmail(): void {
    if (this.resendCooldown > 0) return;

    this.resendLoading = true;
    const { email } = this.forgotPasswordForm.value;

    this.authService.forgotPassword(email)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.resendLoading = false)
      )
      .subscribe({
        next: () => {
          this.errorHandler.handleSuccess('Reset email sent again!');
          this.startResendCooldown();
        },
        error: () => {
          this.errorHandler.handleSuccess('Reset email sent again!');
          this.startResendCooldown();
        }
      });
  }

  private startResendCooldown(): void {
    this.resendCooldown = 60; // 60 seconds cooldown
    this.cooldownInterval = setInterval(() => {
      this.resendCooldown--;
      if (this.resendCooldown <= 0) {
        clearInterval(this.cooldownInterval);
      }
    }, 1000);
  }

  private markFormGroupTouched(): void {
    Object.keys(this.forgotPasswordForm.controls).forEach(key => {
      const control = this.forgotPasswordForm.get(key);
      control?.markAsTouched();
    });
  }
}
