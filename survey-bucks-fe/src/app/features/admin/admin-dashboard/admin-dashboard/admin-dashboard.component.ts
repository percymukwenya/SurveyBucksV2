// src/app/features/admin/admin-dashboard/admin-dashboard.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { AdminDashboardService } from '../../../../core/services/admin-dashboard.service';
import { MatProgressBar } from '@angular/material/progress-bar';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatProgressBar
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  dashboardStats: any = null;
  recentActivity: any[] = [];
  loading: boolean = true;
  
  constructor(private adminDashboardService: AdminDashboardService) { }
  
  ngOnInit(): void {
    this.loadDashboardData();
  }
  
  loadDashboardData(): void {
    this.loading = true;
    
    this.adminDashboardService.getDashboardStats().subscribe({
      next: (stats: any) => {
        this.dashboardStats = stats;
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error loading dashboard stats', error);
        this.loading = false;
      }
    });
    
    this.adminDashboardService.getRecentActivity().subscribe({
      next: (activity: any[]) => {
        this.recentActivity = activity;
      },
      error: (error: any) => {
        console.error('Error loading recent activity', error);
      }
    });
  }
  
  getActivityIcon(activityType: string): string {
    switch (activityType) {
      case 'Survey_Created': return 'add_circle';
      case 'Survey_Published': return 'publish';
      case 'Survey_Completed': return 'check_circle';
      case 'User_Registered': return 'person_add';
      case 'Reward_Redeemed': return 'card_giftcard';
      case 'Payment_Processed': return 'payment';
      default: return 'event_note';
    }
  }
  
  getActivityClass(activityType: string): string {
    switch (activityType) {
      case 'Survey_Created':
      case 'Survey_Published': return 'activity-survey';
      case 'User_Registered': return 'activity-user';
      case 'Reward_Redeemed': return 'activity-reward';
      case 'Payment_Processed': return 'activity-payment';
      default: return '';
    }
  }
}