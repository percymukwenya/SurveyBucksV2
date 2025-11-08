import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { SelectionModel } from '@angular/cdk/collections';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

interface BankingDetail {
  id: number;
  userId: string;
  userEmail: string;
  userName: string;
  bankName: string;
  accountHolderName: string;
  accountNumber: string;
  accountType: string;
  branchCode: string;
  isPrimary: boolean;
  isVerified: boolean;
  verificationStatus: string;
  verificationNotes: string;
  verifiedDate: Date | null;
  verifiedBy: string;
  createdDate: Date;
  modifiedDate: Date;
}

@Component({
  selector: 'app-pending-banking',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatCardModule
  ],
  templateUrl: './pending-banking.component.html',
  styleUrls: ['./pending-banking.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PendingBankingComponent implements OnInit {
  bankingDetails: BankingDetail[] = [];
  filteredBankingDetails: BankingDetail[] = [];
  loading = true;
  selectedBankingDetails = new SelectionModel<BankingDetail>(true, []);
  selectedBankingDetail: BankingDetail | null = null;

  // Single verification
  verificationAction: 'approve' | 'reject' = 'approve';
  verificationNotes = '';

  // Batch verification
  batchAction: 'approve' | 'reject' = 'approve';
  batchNotes = '';

  // Filters
  filterBankName = '';
  filterAccountType = '';

  displayedColumns: string[] = [
    'select',
    'user',
    'bankName',
    'accountHolderName',
    'accountNumber',
    'accountType',
    'branchCode',
    'createdDate',
    'actions'
  ];

  private apiUrl = `${environment.apiUrl}/api/admin/banking-verification`;

  constructor(
    private http: HttpClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPendingBankingDetails();
  }

  loadPendingBankingDetails(): void {
    this.loading = true;
    this.http.get<BankingDetail[]>(`${this.apiUrl}/pending`)
      .subscribe({
        next: (details) => {
          this.bankingDetails = details;
          this.applyFilters();
          this.loading = false;
          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Error loading pending banking details', error);
          this.snackBar.open('Error loading banking details', 'Close', { duration: 3000 });
          this.loading = false;
          this.cdr.markForCheck();
        }
      });
  }

  applyFilters(): void {
    this.filteredBankingDetails = this.bankingDetails.filter(detail => {
      const matchesBankName = !this.filterBankName || 
        detail.bankName.toLowerCase().includes(this.filterBankName.toLowerCase());
      const matchesAccountType = !this.filterAccountType || 
        detail.accountType === this.filterAccountType;
      
      return matchesBankName && matchesAccountType;
    });
    this.cdr.markForCheck();
  }

  selectBankingDetail(detail: BankingDetail): void {
    this.selectedBankingDetail = detail;
    this.verificationNotes = '';
    this.verificationAction = 'approve';
  }

  verifyBankingDetail(): void {
    if (!this.selectedBankingDetail) return;

    const status = this.verificationAction === 'approve' ? 'Approved' : 'Rejected';
    
    if (this.verificationAction === 'reject' && !this.verificationNotes.trim()) {
      this.snackBar.open('Notes are required when rejecting banking details', 'Close', { duration: 3000 });
      return;
    }

    const request = {
      status: status,
      notes: this.verificationNotes || ''
    };

    this.http.post(`${this.apiUrl}/verify/${this.selectedBankingDetail.id}`, request)
      .subscribe({
        next: () => {
          this.snackBar.open(`Banking details ${status.toLowerCase()} successfully`, 'Close', { duration: 3000 });
          this.selectedBankingDetail = null;
          this.loadPendingBankingDetails();
        },
        error: (error) => {
          console.error('Error verifying banking details', error);
          this.snackBar.open('Error verifying banking details', 'Close', { duration: 3000 });
        }
      });
  }

  batchVerifyBankingDetails(): void {
    if (this.selectedBankingDetails.selected.length === 0) return;

    const status = this.batchAction === 'approve' ? 'Approved' : 'Rejected';
    
    if (this.batchAction === 'reject' && !this.batchNotes.trim()) {
      this.snackBar.open('Notes are required when rejecting banking details', 'Close', { duration: 3000 });
      return;
    }

    const request = {
      bankingVerifications: this.selectedBankingDetails.selected.map(detail => ({
        bankingDetailId: detail.id,
        status: status,
        notes: this.batchNotes || ''
      }))
    };

    this.http.post(`${this.apiUrl}/verify/batch`, request)
      .subscribe({
        next: () => {
          this.snackBar.open(`${this.selectedBankingDetails.selected.length} banking details ${status.toLowerCase()} successfully`, 'Close', { duration: 3000 });
          this.selectedBankingDetails.clear();
          this.batchNotes = '';
          this.loadPendingBankingDetails();
        },
        error: (error) => {
          console.error('Error batch verifying banking details', error);
          this.snackBar.open('Error verifying banking details', 'Close', { duration: 3000 });
        }
      });
  }

  isAllSelected(): boolean {
    const numSelected = this.selectedBankingDetails.selected.length;
    const numRows = this.filteredBankingDetails.length;
    return numSelected === numRows && numRows > 0;
  }

  masterToggle(): void {
    if (this.isAllSelected()) {
      this.selectedBankingDetails.clear();
    } else {
      this.filteredBankingDetails.forEach(row => this.selectedBankingDetails.select(row));
    }
  }

  maskAccountNumber(accountNumber: string): string {
    if (!accountNumber || accountNumber.length < 4) return accountNumber;
    const visibleDigits = accountNumber.slice(-4);
    const maskedPart = '*'.repeat(Math.max(0, accountNumber.length - 4));
    return maskedPart + visibleDigits;
  }

  getAccountTypeDisplayName(accountType: string): string {
    const types: { [key: string]: string } = {
      'Checking': 'Checking',
      'Savings': 'Savings',
      'BusinessChecking': 'Business Checking',
      'BusinessSavings': 'Business Savings'
    };
    return types[accountType] || accountType;
  }

  formatDate(date: Date | string): string {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
}