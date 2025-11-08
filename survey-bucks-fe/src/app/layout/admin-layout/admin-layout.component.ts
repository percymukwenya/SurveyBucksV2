import { CommonModule } from '@angular/common';
import { Component, ViewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { AuthService } from '../../core/authentication/auth.service';
import { MatBadgeModule } from '@angular/material/badge';
import { MatRippleModule } from '@angular/material/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { ThemeService } from '../../core/services/theme.service';
import { filter } from 'rxjs';
import { FormsModule } from '@angular/forms';

interface NavItem {
  label: string;
  icon: string;
  route?: string;
  description?: string;
  children?: NavItem[];
  expanded?: boolean;
  badge?: number | string;
  badgeColor?: string;
}

@Component({
  selector: 'app-admin-layout',
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    FormsModule,
    RouterLinkActive,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatToolbarModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule,
    MatTooltipModule,
    MatBadgeModule,
    MatRippleModule,
    MatExpansionModule,
    BreadcrumbComponent
  ],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent {
  @ViewChild('sidenav') sidenav!: MatSidenav;
  
  isExpanded = true;
  isMobileView = false;
  isDarkMode = false;
  currentRoute = '';
  pageTitle = 'Dashboard';
  searchQuery = '';

  navItems: NavItem[] = [
    { 
      label: 'Dashboard', 
      icon: 'dashboard', 
      route: '/admin/dashboard',
      description: 'Overview and key metrics'
    },
    { 
      label: 'User Management', 
      icon: 'people', 
      route: '/admin/users',
      description: 'Manage platform users',
      badge: 5,
      badgeColor: 'accent'
    },
    { 
      label: 'Document Verification', 
      icon: 'verified', 
      route: '/admin/document-verification',
      description: 'Review user documents'
    },
    { 
      label: 'Banking Verification', 
      icon: 'account_balance', 
      route: '/admin/banking-verification',
      description: 'Verify user banking details'
    },
    { 
      label: 'Content', 
      icon: 'article', 
      description: 'Manage all content',
      children: [
        { 
          label: 'Surveys', 
          icon: 'assignment', 
          route: '/admin/surveys',
          description: 'Manage survey templates'
        },
        { 
          label: 'Rewards', 
          icon: 'card_giftcard', 
          route: '/admin/rewards',
          description: 'Configure reward options'
        }
      ],
      expanded: false
    },
    { 
      label: 'Analytics', 
      icon: 'insights', 
      children: [
        { 
          label: 'Reports', 
          icon: 'bar_chart', 
          route: '/admin/reports',
          description: 'View detailed reports'
        },
        { 
          label: 'Statistics', 
          icon: 'analytics', 
          route: '/admin/statistics',
          description: 'Platform statistics'
        }
      ],
      expanded: false
    },
    { 
      label: 'System', 
      icon: 'settings_applications', 
      children: [
        { 
          label: 'Settings', 
          icon: 'settings', 
          route: '/admin/settings',
          description: 'Platform configuration'
        },
        { 
          label: 'Security', 
          icon: 'security', 
          route: '/admin/security',
          description: 'Security settings and logs'
        },
        { 
          label: 'API', 
          icon: 'code', 
          route: '/admin/api',
          description: 'API management'
        }
      ],
      expanded: false
    }
  ];
  
// Add this property to provide the current year to the template
public currentYear: number = new Date().getFullYear();

  quickActions = [
    { label: 'New Survey', icon: 'add_circle', route: '/admin/surveys/create' },
    { label: 'Add User', icon: 'person_add', route: '/admin/users/create' },
    { label: 'Generate Report', icon: 'summarize', route: '/admin/reports/create' }
  ];

  systemStatus = {
    status: 'Operational',
    uptime: '99.98%',
    lastUpdated: new Date()
  };

  adminStats = {
    activeUsers: 1254,
    surveysToday: 47,
    pendingRewards: 18
  };

  constructor(
    public authService: AuthService,
    private themeService: ThemeService,
    private router: Router) {}

    ngOnInit() {
    this.checkScreenSize();
    window.addEventListener('resize', () => this.checkScreenSize());
    
    // Subscribe to route changes
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.currentRoute = event.url;
      this.updatePageTitle();
      
      // Close sidenav on navigation in mobile view
      if (this.isMobileView) {
        this.isExpanded = false;
      }
    });
    
    // Subscribe to theme changes
    this.themeService.isDarkMode$.subscribe(isDark => {
      this.isDarkMode = isDark;
    });
  }

  checkScreenSize() {
    this.isMobileView = window.innerWidth < 960;
    
    // Automatically collapse on mobile
    if (this.isMobileView) {
      this.isExpanded = false;
    } else {
      this.isExpanded = true;
    }
  }
  
  toggleSidebar() {
    this.isExpanded = !this.isExpanded;
  }
  
  toggleTheme() {
    this.themeService.toggleDarkMode();
  }
  
  toggleNavGroup(item: NavItem) {
    item.expanded = !item.expanded;
  }

  updatePageTitle() {
    // Find the matching nav item
    for (const item of this.navItems) {
      if (item.route && this.currentRoute.includes(item.route)) {
        this.pageTitle = item.label;
        return;
      }
      
      if (item.children) {
        for (const child of item.children) {
          if (child.route && this.currentRoute.includes(child.route)) {
            this.pageTitle = child.label;
            return;
          }
        }
      }
    }
    
    // Default fallback
    this.pageTitle = 'Dashboard';
  }
  
  logout() {
    this.authService.logout();
  }
}
