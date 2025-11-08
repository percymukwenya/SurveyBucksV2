// src/app/shared/components/notification-dropdown/notification-dropdown.component.ts
import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'app-notification-dropdown',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatBadgeModule,
    MatMenuModule,
    MatDividerModule
  ],
  templateUrl: './notification-dropdown.component.html',
  styleUrls: ['./notification-dropdown.component.scss']
})
export class NotificationDropdownComponent implements OnInit {
  @Input() maxItems: number = 5;
  @Output() markAsRead = new EventEmitter<number>();
  
  notifications: any[] = [];
  unreadCount: number = 0;
  loading: boolean = true;
  
  constructor(private notificationService: NotificationService) { }
  
  ngOnInit(): void {
    this.loadNotifications();
  }
  
  loadNotifications(): void {
    this.loading = true;
    
    this.notificationService.getUserNotifications().subscribe({
      next: (notifications) => {
        this.notifications = notifications.slice(0, this.maxItems);
        this.unreadCount = notifications.filter(n => !n.isRead).length;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading notifications', error);
        this.loading = false;
      }
    });
  }
  
  onMarkAsRead(notification: any): void {
    if (notification.isRead) return;
    
    this.notificationService.markNotificationAsRead(notification.id).subscribe({
      next: () => {
        notification.isRead = true;
        this.unreadCount--;
        this.markAsRead.emit(notification.id);
      },
      error: (error: any) => {
        console.error('Error marking notification as read', error);
      }
    });
  }
  
  onMarkAllAsRead(): void {
    if (this.unreadCount === 0) return;
    
    this.notificationService.markAllNotificationsAsRead().subscribe({
      next: () => {
        this.notifications.forEach(notification => notification.isRead = true);
        this.unreadCount = 0;
        this.markAsRead.emit(-1); // -1 indicates all notifications
      },
      error: (error: any) => {
        console.error('Error marking all notifications as read', error);
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
  
  refreshNotifications(): void {
    this.loadNotifications();
  }
}
