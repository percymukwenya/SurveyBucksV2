// src/app/features/admin/users/user-management/user-management.component.ts
import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ConfirmationDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { AdminUserService } from '../../../../core/services/admin-user.service';
import { MatProgressBar } from '@angular/material/progress-bar';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatMenuModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressBar,
    RouterModule
  ],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.scss']
})
export class UserManagementComponent implements OnInit {
  users: any[] = [];
  filteredUsers: any[] = [];
  loading: boolean = true;
  
  displayedColumns: string[] = [
    'name',
    'email',
    'status',
    'role',
    'joinDate',
    'lastActive',
    'profileCompletion',
    'actions'
  ];
  
  searchControl = new FormControl('');
  statusFilter = new FormControl('');
  roleFilter = new FormControl('');
  
  statusOptions = [
    { value: 'active', label: 'Active' },
    { value: 'inactive', label: 'Inactive' },
    { value: 'pending', label: 'Pending Verification' },
    { value: 'banned', label: 'Banned' }
  ];
  
  roleOptions = [
    { value: 'admin', label: 'Administrator' },
    { value: 'moderator', label: 'Moderator' },
    { value: 'client', label: 'Client' },
    { value: 'participant', label: 'Participant' }
  ];
  
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatTable) table!: MatTable<any>;
  
  constructor(
    private adminUserService: AdminUserService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) { }
  
  ngOnInit(): void {
    this.loadUsers();
    
    this.searchControl.valueChanges.subscribe(() => {
      this.applyFilters();
    });
    
    this.statusFilter.valueChanges.subscribe(() => {
      this.applyFilters();
    });
    
    this.roleFilter.valueChanges.subscribe(() => {
      this.applyFilters();
    });
  }
  
  loadUsers(): void {
    this.loading = true;
    
    this.adminUserService.getAllUsers().subscribe({
      next: (users: any[]) => {
        this.users = users;
        this.filteredUsers = [...this.users];
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error loading users', error);
        this.loading = false;
      }
    });
  }
  
  applyFilters(): void {
    const searchTerm = this.searchControl.value?.toLowerCase() || '';
    const statusFilter = this.statusFilter.value || '';
    const roleFilter = this.roleFilter.value || '';
    
    this.filteredUsers = this.users.filter(user => {
      // Search term filter
      const fullName = `${user.firstName} ${user.lastName}`.toLowerCase();
      const matchesSearch = !searchTerm || 
        fullName.includes(searchTerm) ||
        user.email.toLowerCase().includes(searchTerm);
      
      // Status filter
      const matchesStatus = !statusFilter || user.status.toLowerCase() === statusFilter;
      
      // Role filter
      const matchesRole = !roleFilter || user.role.toLowerCase() === roleFilter;
      
      return matchesSearch && matchesStatus && matchesRole;
    });
    
    // Reset pagination to first page
    if (this.paginator) {
      this.paginator.firstPage();
    }
    
    // Refresh table
    if (this.table) {
      this.table.renderRows();
    }
  }
  
  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusFilter.setValue('');
    this.roleFilter.setValue('');
  }
  
  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'active': return 'status-active';
      case 'inactive': return 'status-inactive';
      case 'pending': return 'status-pending';
      case 'banned': return 'status-banned';
      default: return '';
    }
  }
  
  getProfileCompletionClass(percentage: number): string {
    if (percentage >= 80) return 'high-completion';
    if (percentage >= 50) return 'medium-completion';
    return 'low-completion';
  }
  
  activateUser(userId: string): void {
    this.adminUserService.updateUserStatus(userId, 'active').subscribe({
      next: () => {
        const user = this.users.find(u => u.id === userId);
        if (user) {
          user.status = 'Active';
          this.snackBar.open('User activated successfully!', 'Close', {
            duration: 3000
          });
        }
      },
      error: (error: any) => {
        console.error('Error activating user', error);
        this.snackBar.open('Error activating user. Please try again.', 'Close', {
          duration: 5000
        });
      }
    });
  }
  
  deactivateUser(userId: string): void {
    const user = this.users.find(u => u.id === userId);
    
    if (!user) return;
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Deactivate User',
        message: `Are you sure you want to deactivate the account for ${user.firstName} ${user.lastName}? They will no longer be able to log in.`,
        confirmText: 'Deactivate',
        cancelText: 'Cancel'
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.adminUserService.updateUserStatus(userId, 'inactive').subscribe({
          next: () => {
            user.status = 'Inactive';
            this.snackBar.open('User deactivated successfully!', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error deactivating user', error);
            this.snackBar.open('Error deactivating user. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
  
  banUser(userId: string): void {
    const user = this.users.find(u => u.id === userId);
    
    if (!user) return;
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Ban User',
        message: `Are you sure you want to ban ${user.firstName} ${user.lastName}? This will prevent them from accessing the platform and participating in any surveys.`,
        confirmText: 'Ban User',
        cancelText: 'Cancel',
        isDestructive: true
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.adminUserService.updateUserStatus(userId, 'banned').subscribe({
          next: () => {
            user.status = 'Banned';
            this.snackBar.open('User banned successfully!', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error banning user', error);
            this.snackBar.open('Error banning user. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
  
  deleteUser(userId: string): void {
    const user = this.users.find(u => u.id === userId);
    
    if (!user) return;
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete User',
        message: `Are you sure you want to permanently delete the account for ${user.firstName} ${user.lastName}? This action cannot be undone and all associated data will be removed.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        isDestructive: true
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.adminUserService.deleteUser(userId).subscribe({
          next: () => {
            // Remove from local data
            this.users = this.users.filter(u => u.id !== userId);
            this.filteredUsers = this.filteredUsers.filter(u => u.id !== userId);
            this.snackBar.open('User deleted successfully!', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error deleting user', error);
            this.snackBar.open('Error deleting user. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
}