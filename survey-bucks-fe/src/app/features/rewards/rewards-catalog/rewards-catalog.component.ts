// src/app/features/rewards/rewards-catalog/rewards-catalog.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatBadgeModule } from '@angular/material/badge';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { RewardsService } from '../../../core/services/rewards.service';
import { ConfirmationDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-rewards-catalog',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTabsModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
    MatBadgeModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressBarModule
  ],
  templateUrl: './rewards-catalog.component.html',
  styleUrls: ['./rewards-catalog.component.scss']
})
export class RewardsCatalogComponent implements OnInit {
  availableRewards: any[] = [];
  userRewards: any[] = [];
  userPoints: any = { currentPoints: 0, totalPointsEarned: 0 };
  userLevel: any = { currentLevel: 1, pointsRequiredForNextLevel: 1000, currentPoints: 0 };
  loading: boolean = true;
  
  // Filter options
  selectedCategory: string = 'all';
  sortOption: string = 'popular';
  
  constructor(
    private rewardsService: RewardsService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) { }
  
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
    
    // Get user level
    this.rewardsService.getUserLevel().subscribe({
      next: (level: any) => {
        this.userLevel = level;
      },
      error: (error: any) => {
        console.error('Error loading user level', error);
      }
    });
    
    // Get available rewards
    this.rewardsService.getAvailableRewards().subscribe({
      next: (rewards) => {
        this.availableRewards = rewards;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading available rewards', error);
        this.loading = false;
      }
    });
    
    // Get user rewards
    this.rewardsService.getUserRewards().subscribe({
      next: (rewards) => {
        this.userRewards = rewards;
      },
      error: (error) => {
        console.error('Error loading user rewards', error);
      }
    });
  }
  
  filterRewards(): any[] {
    // First filter by category
    let filtered = this.availableRewards;
    if (this.selectedCategory !== 'all') {
      filtered = filtered.filter(reward => reward.rewardCategory === this.selectedCategory);
    }
    
    // Then sort according to selected option
    switch (this.sortOption) {
      case 'popular':
        return filtered.sort((a, b) => (b.popularityRank || 0) - (a.popularityRank || 0));
      case 'points-low':
        return filtered.sort((a, b) => (a.pointsCost || 0) - (b.pointsCost || 0));
      case 'points-high':
        return filtered.sort((a, b) => (b.pointsCost || 0) - (a.pointsCost || 0));
      case 'newest':
        return filtered.sort((a, b) => new Date(b.dateAdded).getTime() - new Date(a.dateAdded).getTime());
      default:
        return filtered;
    }
  }
  
  canRedeem(reward: any): boolean {
    return (this.userPoints.currentPoints >= reward.pointsCost) && 
           (this.userLevel.currentLevel >= (reward.minimumUserLevel || 1));
  }
  
  getLevelLockMessage(reward: any): string {
    if (this.userLevel.currentLevel < reward.minimumUserLevel) {
      return `Unlocks at Level ${reward.minimumUserLevel}`;
    }
    return '';
  }
  
  redeemReward(reward: any): void {
    if (!this.canRedeem(reward)) {
      if (this.userLevel.currentLevel < reward.minimumUserLevel) {
        this.snackBar.open(`This reward requires Level ${reward.minimumUserLevel}. Keep participating to level up!`, 'Close', {
          duration: 5000
        });
      } else {
        this.snackBar.open(`You need ${reward.pointsCost - this.userPoints.currentPoints} more points to redeem this reward.`, 'Close', {
          duration: 5000
        });
      }
      return;
    }
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Redeem Reward',
        message: `Are you sure you want to redeem "${reward.name}" for ${reward.pointsCost} points?`,
        confirmText: 'Redeem',
        cancelText: 'Cancel'
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.rewardsService.redeemReward(reward.id).subscribe({
          next: () => {
            // Update points
            this.userPoints.currentPoints -= reward.pointsCost;
            
            // Add to user rewards
            this.loadRewardsData(); // Reload to get updated data
            
            this.snackBar.open('Reward redeemed successfully!', 'Close', {
              duration: 3000
            });
          },
          error: (error) => {
            console.error('Error redeeming reward', error);
            this.snackBar.open(error.error?.message || 'Error redeeming reward. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
}
