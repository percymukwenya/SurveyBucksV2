// src/app/features/notifications/notifications-list/notifications-list.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmationDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { MatProgressBar } from '@angular/material/progress-bar';

@Component({
  selector: 'app-notifications-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressBar
  ],
  templateUrl: './notifications-list.component.html',
  styleUrls: ['./notifications-list.component.scss']
})
export class NotificationsListComponent implements OnInit {
  notifications: any[] = [];
  unreadCount: number = 0;
  loading: boolean = true;
  
  constructor(
    private notificationService: NotificationService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) { }
  
  ngOnInit(): void {
    this.loadNotifications();
  }
  
  loadNotifications(): void {
    this.loading = true;
    
    this.notificationService.getUserNotifications().subscribe({
      next: (notifications: any[]) => {
        this.notifications = notifications;
        this.unreadCount = notifications.filter((n: { isRead: any; }) => !n.isRead).length;
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error loading notifications', error);
        this.loading = false;
      }
    });
  }
  
  markAsRead(notification: any): void {
    if (notification.isRead) return;
    
    this.notificationService.markNotificationAsRead(notification.id).subscribe({
      next: () => {
        notification.isRead = true;
        this.unreadCount--;
      },
      error: (error: any) => {
        console.error('Error marking notification as read', error);
        this.snackBar.open('Error marking notification as read. Please try again.', 'Close', {
          duration: 5000
        });
      }
    });
  }
  
  markAllAsRead(): void {
    if (this.unreadCount === 0) return;
    
    this.notificationService.markAllNotificationsAsRead().subscribe({
      next: () => {
        this.notifications.forEach(notification => notification.isRead = true);
        this.unreadCount = 0;
        this.snackBar.open('All notifications marked as read.', 'Close', {
          duration: 3000
        });
      },
      error: (error: any) => {
        console.error('Error marking all notifications as read', error);
        this.snackBar.open('Error marking all notifications as read. Please try again.', 'Close', {
          duration: 5000
        });
      }
    });
  }
  
  deleteNotification(notification: any): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '350px',
      data: {
        title: 'Delete Notification',
        message: 'Are you sure you want to delete this notification?',
        confirmText: 'Delete',
        cancelText: 'Cancel'
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.notificationService.deleteNotification(notification.id).subscribe({
          next: () => {
            this.notifications = this.notifications.filter(n => n.id !== notification.id);
            if (!notification.isRead) {
              this.unreadCount--;
            }
            this.snackBar.open('Notification deleted.', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error deleting notification', error);
            this.snackBar.open('Error deleting notification. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
  
  clearAllNotifications(): void {
    if (this.notifications.length === 0) return;
    
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '350px',
      data: {
        title: 'Clear All Notifications',
        message: 'Are you sure you want to delete all notifications? This action cannot be undone.',
        confirmText: 'Clear All',
        cancelText: 'Cancel',
        isDestructive: true
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.notificationService.clearAllNotifications().subscribe({
          next: () => {
            this.notifications = [];
            this.unreadCount = 0;
            this.snackBar.open('All notifications cleared.', 'Close', {
              duration: 3000
            });
          },
          error: (error: any) => {
            console.error('Error clearing notifications', error);
            this.snackBar.open('Error clearing notifications. Please try again.', 'Close', {
              duration: 5000
            });
          }
        });
      }
    });
  }
  
  getNotificationIcon(type: string): string {
    switch (type.toLowerCase()) {
      case 'survey': return 'assignment';
      case 'reward': return 'card_giftcard';
      case 'achievement': return 'emoji_events';
      case 'challenge': return 'flag';
      case 'system': return 'notifications';
      default: return 'notifications';
    }
  }
  
  getNotificationTypeClass(type: string): string {
    switch (type.toLowerCase()) {
      case 'survey': return 'type-survey';
      case 'reward': return 'type-reward';
      case 'achievement': return 'type-achievement';
      case 'challenge': return 'type-challenge';
      case 'system': return 'type-system';
      default: return '';
    }
  }
  
  navigateToReference(notification: any): void {
    if (!notification.deepLink) return;
    
    // Mark as read first
    if (!notification.isRead) {
      this.markAsRead(notification);
    }
    
    // Then navigate
    // The router will handle this via routerLink in the template
  }
}