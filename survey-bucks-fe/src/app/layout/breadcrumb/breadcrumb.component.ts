import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { filter, map, mergeMap } from 'rxjs';

interface Breadcrumb {
  label: string;
  url: string;
  icon?: string;
}

@Component({
  selector: 'breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule],
  templateUrl: './breadcrumb.component.html',
  styleUrls: ['./breadcrumb.component.scss']
})
export class BreadcrumbComponent {
  breadcrumbs: Breadcrumb[] = [];

  // Route label mapping
  private routeLabels: { [key: string]: { label: string; icon?: string } } = {
    'client': { label: 'Dashboard', icon: 'dashboard' },
    'dashboard': { label: 'Dashboard', icon: 'dashboard' },
    'profile': { label: 'Profile', icon: 'person' },
    'surveys': { label: 'Surveys', icon: 'assignment' },
    'rewards': { label: 'Rewards', icon: 'card_giftcard' },
    'notifications': { label: 'Notifications', icon: 'notifications' },
    'settings': { label: 'Settings', icon: 'settings' },
    'take': { label: 'Take Survey', icon: 'quiz' },
    'results': { label: 'Results', icon: 'poll' },
    'redemptions': { label: 'Redemptions', icon: 'redeem' },
    'wallet': { label: 'Wallet', icon: 'account_balance_wallet' },
    'help': { label: 'Help & Support', icon: 'help' },
    'achievements': { label: 'Achievements', icon: 'emoji_events' },
    'leaderboard': { label: 'Leaderboard', icon: 'leaderboard' }
  };

  constructor(
    private router: Router,
    private activatedRoute: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        map(() => this.activatedRoute),
        map(route => {
          while (route.firstChild) {
            route = route.firstChild;
          }
          return route;
        }),
        mergeMap(route => route.data)
      )
      .subscribe(() => {
        this.buildBreadcrumbs();
      });

    // Build initial breadcrumbs
    this.buildBreadcrumbs();
  }

  private buildBreadcrumbs(): void {
    const url = this.router.url;
    const segments = url.split('/').filter(segment => segment.length > 0);
    
    this.breadcrumbs = [];
    let currentUrl = '';

    segments.forEach((segment, index) => {
      currentUrl += `/${segment}`;
      
      // Skip numeric IDs in breadcrumbs (like /surveys/123)
      if (this.isNumeric(segment)) {
        return;
      }

      const routeConfig = this.routeLabels[segment];
      
      if (routeConfig) {
        this.breadcrumbs.push({
          label: routeConfig.label,
          url: currentUrl,
          icon: index === 0 ? routeConfig.icon : undefined // Only show icon for first item
        });
      } else {
        // Fallback: capitalize the segment
        this.breadcrumbs.push({
          label: this.capitalizeFirstLetter(segment),
          url: currentUrl
        });
      }
    });

    // Handle special cases
    this.handleSpecialCases(segments);
  }

  private handleSpecialCases(segments: string[]): void {
    // Handle survey details page
    if (segments.includes('surveys') && segments.length > 2) {
      const surveyIndex = segments.indexOf('surveys');
      if (surveyIndex !== -1 && surveyIndex + 1 < segments.length) {
        const nextSegment = segments[surveyIndex + 1];
        
        if (this.isNumeric(nextSegment)) {
          // This is a survey details page
          this.breadcrumbs.push({
            label: 'Survey Details',
            url: this.router.url,
            icon: 'description'
          });
        } else if (nextSegment === 'take') {
          // This is taking a survey
          this.breadcrumbs.push({
            label: 'Take Survey',
            url: this.router.url,
            icon: 'quiz'
          });
        }
      }
    }

    // Handle profile sub-pages
    if (segments.includes('profile')) {
      const profileIndex = segments.indexOf('profile');
      if (profileIndex !== -1 && profileIndex + 1 < segments.length) {
        const subPage = segments[profileIndex + 1];
        
        switch (subPage) {
          case 'edit':
            this.breadcrumbs.push({
              label: 'Edit Profile',
              url: this.router.url
            });
            break;
          case 'security':
            this.breadcrumbs.push({
              label: 'Security Settings',
              url: this.router.url
            });
            break;
        }
      }
    }
  }

  private isNumeric(value: string): boolean {
    return !isNaN(Number(value));
  }

  private capitalizeFirstLetter(string: string): string {
    return string.charAt(0).toUpperCase() + string.slice(1);
  }
}
