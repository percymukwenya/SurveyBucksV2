import { Component, Inject, OnDestroy, OnInit, PLATFORM_ID, ViewChild } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/authentication/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserProfileService } from '../../core/services/user-profile.service';
import { User } from '../../core/models/user.model';
import { NotificationDropdownComponent } from '../../shared/components/notification-dropdown/notification-dropdown/notification-dropdown.component';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatRippleModule } from '@angular/material/core';
import { filter, Subject, takeUntil } from 'rxjs';
import { ThemeService } from '../../core/services/theme.service';
import { BreadcrumbComponent } from "../breadcrumb/breadcrumb.component";

interface NavigationItem {
  label: string;
  icon: string;
  route: string;
  description: string;
  badge?: number;
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatSidenavModule,
    MatListModule,
    MatBadgeModule,
    MatMenuModule,
    MatTooltipModule,
    MatRippleModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    NotificationDropdownComponent,
    BreadcrumbComponent
],
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss'],
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  @ViewChild('sidenav') sidenav!: MatSidenav;

  private destroy$ = new Subject<void>();

  currentUser: User | null = null;
  unreadNotifications = 0;
  isMobileView = false;
  sidenavOpen = false;
  isDarkMode = false;
  activePage = '';
  profileCompletionPercentage = 0;
  profileCompletionLoading = false;

  navigation: NavigationItem[] = [
    {
      label: 'Dashboard',
      icon: 'dashboard',
      route: '/client/dashboard',
      description: 'Your personalized dashboard',
    },
    {
      label: 'Surveys',
      icon: 'assignment',
      route: '/client/surveys',
      description: 'Available and completed surveys',
    },
    {
      label: 'Rewards',
      icon: 'card_giftcard',
      route: '/client/rewards',
      description: 'Your rewards and redemption options',
    },
    {
      label: 'Notifications',
      icon: 'notifications',
      route: '/client/notifications',
      description: 'Your alerts and messages',
    },
    {
      label: 'Profile',
      icon: 'person',
      route: '/client/profile',
      description: 'Manage your account details',
    },
  ];
currentYear: any;

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private userProfileService: UserProfileService,
    private themeService: ThemeService,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    this.initializeLayout();
    this.subscribeToServices();
    this.setupRouterEvents();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    
    if (isPlatformBrowser(this.platformId)) {
      window.removeEventListener('resize', this.onResize);
    }
  }

  private initializeLayout(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.checkScreenSize();
      window.addEventListener('resize', this.onResize);
    }
  }

  private subscribeToServices(): void {
    // Subscribe to auth state
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe((user) => {
        this.currentUser = user;
        if (user) {
          this.loadUserSpecificData();
        }
      });

    // Subscribe to theme changes
    this.themeService.isDarkMode$
      .pipe(takeUntil(this.destroy$))
      .subscribe((isDark: boolean) => {
        this.isDarkMode = isDark;
      });

    // Subscribe to notification count
    this.notificationService.getUnreadNotificationCount()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (count) => {
          this.unreadNotifications = count;
          this.updateNavigationBadges();
        },
        error: (error) => {
          console.error('Error loading unread notification count', error);
        }
      });
  }

  private setupRouterEvents(): void {
    this.router.events
      .pipe(
        filter((event: any) => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((event: NavigationEnd) => {
        this.activePage = event.url;
        this.handleRouteChange();
      });
  }

  private loadUserSpecificData(): void {
    // Load any user-specific data that affects the layout
    this.loadUnreadNotificationCount();
    this.loadProfileCompletion();
  }

  private handleRouteChange(): void {
    // Auto-close sidenav on navigation in mobile view
    if (this.isMobileView && this.sidenavOpen) {
      this.closeSidenav();
    }
  }

  private updateNavigationBadges(): void {
    // Update the notifications badge in navigation
    const notificationNav = this.navigation.find(nav => nav.route === '/client/notifications');
    if (notificationNav) {
      notificationNav.badge = this.unreadNotifications;
    }
  }

  private onResize = (): void => {
    this.checkScreenSize();
  };

  checkScreenSize(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const previousMobileView = this.isMobileView;
    this.isMobileView = window.innerWidth < 960;
    
    // Handle transition from mobile to desktop
    if (previousMobileView && !this.isMobileView) {
      this.sidenavOpen = true;
    }
    // Handle transition from desktop to mobile
    else if (!previousMobileView && this.isMobileView) {
      this.sidenavOpen = false;
    }
    // Initial load for desktop
    else if (!this.isMobileView && !previousMobileView) {
      this.sidenavOpen = true;
    }
  }

  toggleSidenav(): void {
    this.sidenavOpen = !this.sidenavOpen;
  }

  closeSidenav(): void {
    if (this.isMobileView) {
      this.sidenavOpen = false;
    }
  }

  loadUnreadNotificationCount(): void {
    this.notificationService.getUnreadNotificationCount()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (count) => {
          this.unreadNotifications = count;
          this.updateNavigationBadges();
        },
        error: (error) => {
          console.error('Error loading unread notification count', error);
        }
      });
  }

  onNotificationRead(notificationId: number): void {
    if (notificationId === -1) {
      // All notifications marked as read
      this.unreadNotifications = 0;
    } else {
      // Single notification marked as read
      this.unreadNotifications = Math.max(0, this.unreadNotifications - 1);
    }
    this.updateNavigationBadges();
  }

  toggleTheme(): void {
    this.themeService.toggleDarkMode();
  }

  logout(): void {
    this.authService.logout();
  }

  getUserInitials(): string {
    if (!this.currentUser) return '??';
    
    const firstInitial = this.currentUser.firstName?.charAt(0) || '';
    const lastInitial = this.currentUser.lastName?.charAt(0) || '';
    
    return (firstInitial + lastInitial).toUpperCase();
  }

  getUserFullName(): string {
    if (!this.currentUser) return 'Guest User';
    
    return `${this.currentUser.firstName || ''} ${this.currentUser.lastName || ''}`.trim();
  }

  isActiveRoute(route: string): boolean {
    return this.activePage === route || this.activePage.startsWith(route + '/');
  }

  // Navigation helpers
  navigateToRoute(route: string): void {
    this.router.navigate([route]);
    this.closeSidenav();
  }

  // Search functionality (placeholder)
  onSearch(): void {
    // Implement search functionality
    console.log('Search functionality to be implemented');
  }

  // Profile completion
  loadProfileCompletion(): void {
    this.profileCompletionLoading = true;
    this.userProfileService.getProfileCompletion()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (completion) => {
          this.profileCompletionPercentage = completion.completionPercentage || 0;
          this.profileCompletionLoading = false;
        },
        error: (error) => {
          console.error('Error loading profile completion', error);
          this.profileCompletionLoading = false;
        }
      });
  }

  navigateToProfile(): void {
    this.router.navigate(['/client/profile']);
  }

  getProfileCompletionColor(): string {
    if (this.profileCompletionPercentage >= 100) return 'primary';
    if (this.profileCompletionPercentage >= 75) return 'accent';
    return 'warn';
  }

  isProfileIncomplete(): boolean {
    return this.profileCompletionPercentage < 100;
  }
}
