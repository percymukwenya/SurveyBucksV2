// src/app/features/rewards/rewards-history/rewards-history.component.ts
import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { RewardsService } from '../../../core/services/rewards.service';
import { MatProgressBar } from '@angular/material/progress-bar';

@Component({
  selector: 'app-rewards-history',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTabsModule,
    MatProgressBar
  ],
  templateUrl: './rewards-history.component.html',
  styleUrls: ['./rewards-history.component.scss']
})
export class RewardsHistoryComponent implements OnInit {
  userPoints: any = { currentPoints: 0, totalPointsEarned: 0 };
  pointTransactions: any[] = [];
  userRewards: any[] = [];
  loading: boolean = true;
  
  // Table columns
  transactionColumns: string[] = ['date', 'description', 'type', 'amount', 'balance'];
  rewardsColumns: string[] = ['date', 'name', 'type', 'points', 'status'];
  
  @ViewChild('transactionTable') transactionTable!: MatTable<any>;
  @ViewChild('rewardsTable') rewardsTable!: MatTable<any>;
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  
  constructor(private rewardsService: RewardsService) { }
  
  ngOnInit(): void {
    this.loadRewardsData();
  }
  
  loadRewardsData(): void {
    this.loading = true;
    
    // Get user points
    this.rewardsService.getUserPoints().subscribe({
      next: (points) => {
        this.userPoints = points;
      },
      error: (error) => {
        console.error('Error loading user points', error);
      }
    });
    
    // Get point transactions
    this.rewardsService.getPointTransactions(100, 0).subscribe({
      next: (transactions) => {
        this.pointTransactions = transactions;
        this.loading = false;
        
        // Refresh table if available
        if (this.transactionTable) {
          this.transactionTable.renderRows();
        }
      },
      error: (error) => {
        console.error('Error loading point transactions', error);
        this.loading = false;
      }
    });
    
    // Get user rewards
    this.rewardsService.getUserRewards().subscribe({
      next: (rewards) => {
        this.userRewards = rewards;
        
        // Refresh table if available
        if (this.rewardsTable) {
          this.rewardsTable.renderRows();
        }
      },
      error: (error) => {
        console.error('Error loading user rewards', error);
      }
    });
  }
  
  getTransactionTypeClass(type: string): string {
    switch (type.toLowerCase()) {
      case 'earned': return 'type-earned';
      case 'redeemed': return 'type-redeemed';
      case 'bonus': return 'type-bonus';
      case 'adjustment': return 'type-adjustment';
      default: return '';
    }
  }
  
  getTransactionTypeIcon(type: string): string {
    switch (type.toLowerCase()) {
      case 'earned': return 'add_circle';
      case 'redeemed': return 'remove_circle';
      case 'bonus': return 'stars';
      case 'adjustment': return 'published_with_changes';
      default: return 'help';
    }
  }
  
  getRewardStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'ready': return 'status-ready';
      case 'pending': return 'status-pending';
      case 'claimed': return 'status-claimed';
      case 'expired': return 'status-expired';
      default: return '';
    }
  }
}
