import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormGroup, FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTabsModule } from '@angular/material/tabs';

@Component({
  selector: 'app-admin-settings',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatTabsModule,
    MatDividerModule
  ],
  templateUrl: './admin-settings.component.html',
  styleUrl: './admin-settings.component.scss'
})
export class AdminSettingsComponent {
  generalForm: FormGroup;
  emailForm: FormGroup;
  securityForm: FormGroup;
  notificationForm: FormGroup;
  
  constructor(private fb: FormBuilder) {
    this.generalForm = this.fb.group({
      siteName: ['SurveyBucks', [Validators.required]],
      adminEmail: ['admin@surveybucks.com', [Validators.required, Validators.email]],
      siteDescription: ['A platform for creating and taking surveys with rewards', [Validators.required]],
      defaultLanguage: ['en', [Validators.required]],
      defaultTimezone: ['UTC', [Validators.required]]
    });
    
    this.emailForm = this.fb.group({
      smtpHost: ['smtp.example.com', [Validators.required]],
      smtpPort: [587, [Validators.required]],
      smtpUsername: ['user@example.com', [Validators.required]],
      smtpPassword: ['password', [Validators.required]],
      enableSsl: [true],
      welcomeEmail: ['Welcome {name}, Thank you for registering with SurveyBucks!', [Validators.required]]
    });
    
    this.securityForm = this.fb.group({
      enableTwoFactor: [false],
      passwordPolicy: ['medium', [Validators.required]],
      sessionTimeout: [30, [Validators.required]],
      enableApiRateLimit: [true],
      apiRateLimit: [100, [Validators.required]]
    });
    
    this.notificationForm = this.fb.group({
      enableUserRegistration: [true],
      enableSurveyCompletion: [true],
      enablePaymentProcessing: [true],
      enableSystemErrors: [true],
      enableEmailNotification: [true],
      enablePushNotification: [false],
      enableSlackNotification: [false]
    });
  }
}
