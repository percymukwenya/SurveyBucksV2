// src/app/features/rewards/reward-claim/reward-claim.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatStepperModule } from '@angular/material/stepper';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { RewardsService } from '../../../core/services/rewards.service';
import { MatProgressBar } from '@angular/material/progress-bar';

@Component({
  selector: 'app-reward-claim',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatStepperModule,
    MatSnackBarModule,
    MatProgressBar
  ],
  templateUrl: './reward-claim.component.html',
  styleUrls: ['./reward-claim.component.scss']
})
export class RewardClaimComponent implements OnInit {
  rewardId!: number;
  userReward: any = null;
  loading: boolean = true;
  claiming: boolean = false;
  claimed: boolean = false;
  
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private rewardsService: RewardsService,
    private snackBar: MatSnackBar
  ) { }
  
  ngOnInit(): void {
    this.rewardId = +this.route.snapshot.paramMap.get('id')!;
    this.loadRewardDetails();
  }
  
  loadRewardDetails(): void {
    this.loading = true;
    
    this.rewardsService.getUserRewards().subscribe({
      next: (rewards) => {
        const reward = rewards.find(r => r.id === this.rewardId);
        
        if (reward) {
          this.userReward = reward;
          this.claimed = reward.redemptionStatus === 'Claimed';
        } else {
          this.snackBar.open('Reward not found or you do not have access to this reward.', 'Close', {
            duration: 5000
          });
          this.router.navigate(['/client/rewards']);
        }
        
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading reward details', error);
        this.snackBar.open('Error loading reward details. Please try again.', 'Close', {
          duration: 5000
        });
        this.loading = false;
        this.router.navigate(['/client/rewards']);
      }
    });
  }
  
  claimReward(): void {
    if (this.claimed) return;
    
    this.claiming = true;
    
    this.rewardsService.claimReward(this.rewardId).subscribe({
      next: () => {
        this.claiming = false;
        this.claimed = true;
        this.snackBar.open('Reward claimed successfully!', 'Close', {
          duration: 3000
        });
        
        // Reload the reward to get updated status
        this.loadRewardDetails();
      },
      error: (error) => {
        this.claiming = false;
        console.error('Error claiming reward', error);
        this.snackBar.open(error.error?.message || 'Error claiming reward. Please try again.', 'Close', {
          duration: 5000
        });
      }
    });
  }
}