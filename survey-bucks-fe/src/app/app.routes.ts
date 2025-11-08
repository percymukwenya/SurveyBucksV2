import { Routes } from '@angular/router';
import { roleGuard } from './core/authentication/role.guard';
import { publicGuard } from './core/authentication/public.guard';

export const routes: Routes = [
  // Public pages with PublicLayout
  {
    path: '',
    loadComponent: () => import('./layout/public-layout/public-layout.component').then(m => m.PublicLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./public/landing-page/landing-page.component').then(m => m.LandingPageComponent)
      },
      {
        path: 'how-it-works',
        loadComponent: () => import('./public/how-it-works/how-it-works.component').then(m => m.HowItWorksComponent)
      },
      {
        path: 'rewards',
        loadComponent: () => import('./public/rewards/rewards.component').then(m => m.RewardsComponent)
      },
      {
        path: 'about',
        loadComponent: () => import('./public/about/about.component').then(m => m.AboutComponent)
      },
      {
        path: 'business',
        loadComponent: () => import('./public/business/business.component').then(m => m.BusinessComponent)
      },
      {
        path: 'contact',
        loadComponent: () => import('./public/contact/contact.component').then(m => m.ContactComponent)
      },
      {
        path: 'help',
        loadComponent: () => import('./public/help/help.component').then(m => m.HelpComponent)
      },
      {
        path: 'testimonials',
        loadComponent: () => import('./public/testimonials/testimonials.component').then(m => m.TestimonialsComponent)
      },
      {
        path: 'faq',
        loadComponent: () => import('./public/faq/faq.component').then(m => m.FaqComponent)
      },
      {
        path: 'terms',
        loadComponent: () => import('./public/terms/terms.component').then(m => m.TermsComponent)
      },
      {
        path: 'privacy',
        loadComponent: () => import('./public/privacy/privacy.component').then(m => m.PrivacyComponent)
      },
      {
        path: 'cookies',
        loadComponent: () => import('./public/cookies/cookies.component').then(m => m.CookiesComponent)
      }
    ]
  },
  
  // Authentication routes
  {
    path: 'auth',
    canActivate: [publicGuard],
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
      },
      {
        path: 'forgot-password',
        loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent)
      },
      {
        path: '', // This handles /auth route
        redirectTo: 'login',
        pathMatch: 'full'
      }
    ]
  },
  
  // Admin routes
  {
    path: 'admin',
    canActivate: [roleGuard(['Admin'])],
    loadComponent: () => import('./layout/admin-layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./features/admin/users/user-management/user-management.component').then(m => m.UserManagementComponent)
      },
      {
        path: 'surveys',
        loadComponent: () => import('./features/admin/survey-management/survey-management.component').then(m => m.SurveyManagementComponent)
      },
      {
        path: 'surveys/create',
        loadComponent: () => import('./features/admin/survey-management/survey-editor/survey-editor.component').then(m => m.SurveyEditorComponent)
      },
      {
        path: 'surveys/edit/:id',
        loadComponent: () => import('./features/admin/survey-management/survey-editor/survey-editor.component').then(m => m.SurveyEditorComponent)
      },
      {
        path: 'surveys/results/:id',
        loadComponent: () => import('./features/admin/survey-management/survey-results/survey-results.component').then(m => m.SurveyResultsComponent)
      },
      {
        path: 'rewards',
        loadComponent: () => import('./features/admin/reward-management/reward-management.component').then(m => m.RewardManagementComponent)
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/admin/reports/report-dashboard/report-dashboard.component').then(m => m.ReportDashboardComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/admin/settings/admin-settings/admin-settings.component').then(m => m.AdminSettingsComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/admin/profile/admin-profile/admin-profile.component').then(m => m.AdminProfileComponent)
      },
      {
        path: 'notifications',
        loadComponent: () => import('./features/admin/notifications/notification-center/notification-center.component').then(m => m.NotificationCenterComponent)
      },
      {
        path: 'document-verification',
        loadComponent: () => import('./features/admin/document-verification/verification-dashboard/verification-dashboard.component').then(m => m.VerificationDashboardComponent)
      },
      {
        path: 'pending',
        loadComponent: () => import('./features/admin/document-verification/pending-documents/pending-documents.component').then(m => m.PendingDocumentsComponent)
      },
      {
        path: 'banking-verification',
        loadComponent: () => import('./features/admin/banking-verification/banking-verification.component').then(m => m.BankingVerificationComponent),
        children: [
          {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full'
          },
          {
            path: 'dashboard',
            loadComponent: () => import('./features/admin/banking-verification/verification-dashboard/banking-verification-dashboard.component').then(m => m.BankingVerificationDashboardComponent)
          },
          {
            path: 'pending',
            loadComponent: () => import('./features/admin/banking-verification/pending-banking/pending-banking.component').then(m => m.PendingBankingComponent)
          }
        ]
      }
    ]
  },
  
  // Client routes
  {
    path: 'client',
    canActivate: [roleGuard(['Client'])],
    loadComponent: () => import('./layout/main-layout/main-layout.component').then(m => m.MainLayoutComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile-management/profile-management/profile-management.component').then(m => m.ProfileManagementComponent)
      },
      {
        path: 'surveys',
        loadComponent: () => import('./features/surveys/survey-list/survey-list.component').then(m => m.SurveyListComponent)
      },      
      {
        path: 'surveys/take/:id',
        loadComponent: () => import('./features/surveys/survey-taking/survey-taking.component').then(m => m.SurveyTakeComponent)
      },
      {
        path: 'surveys/:id',
        loadComponent: () => import('./features/surveys/survey-details/survey-details.component').then(m => m.SurveyDetailsComponent)
      },
      {
        path: 'rewards',
        loadComponent: () => import('./features/rewards/rewards-catalog/rewards-catalog.component').then(m => m.RewardsCatalogComponent)
      },
      {
        path: 'notifications',
        loadComponent: () => import('./features/notifications/notifications-list/notifications-list.component').then(m => m.NotificationsListComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/settings/user-settings/user-settings.component').then(m => m.UserSettingsComponent)
      }
    ]
  },
  
  // 404 route
  {
    path: '**',
    loadComponent: () => import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];