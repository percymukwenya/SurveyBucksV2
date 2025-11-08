import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

interface BankingVerificationStats {
  totalBankingDetails: number;
  pendingBankingDetails: number;
  approvedBankingDetails: number;
  rejectedBankingDetails: number;
  averageVerificationTimeHours: number;
  bankingDetailsSubmittedToday: number;
  bankingDetailsVerifiedToday: number;
  topBanks: Array<{
    bankName: string;
    count: number;
    approvalRate: number;
  }>;
}

@Component({
  selector: 'app-banking-verification-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  templateUrl: './banking-verification-dashboard.component.html',
  styleUrls: ['./banking-verification-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BankingVerificationDashboardComponent implements OnInit {
  stats: BankingVerificationStats | null = null;
  loading = true;
  
  displayedColumns: string[] = ['bankName', 'count', 'approvalRate'];
  
  private apiUrl = `${environment.apiUrl}/api/admin/banking-verification`;

  constructor(
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.loading = true;
    this.http.get<BankingVerificationStats>(`${this.apiUrl}/stats`)
      .subscribe({
        next: (stats) => {
          this.stats = stats;
          this.loading = false;
          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Error loading banking verification stats', error);
          this.loading = false;
          this.cdr.markForCheck();
        }
      });
  }

  getApprovalRate(): number {
    if (!this.stats || this.stats.totalBankingDetails === 0) return 0;
    return (this.stats.approvedBankingDetails / this.stats.totalBankingDetails) * 100;
  }

  getRejectionRate(): number {
    if (!this.stats || this.stats.totalBankingDetails === 0) return 0;
    return (this.stats.rejectedBankingDetails / this.stats.totalBankingDetails) * 100;
  }

  getPendingRate(): number {
    if (!this.stats || this.stats.totalBankingDetails === 0) return 0;
    return (this.stats.pendingBankingDetails / this.stats.totalBankingDetails) * 100;
  }
}