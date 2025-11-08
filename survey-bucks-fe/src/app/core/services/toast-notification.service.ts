// src/app/core/services/toast-notification.service.ts
import { Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';
import { Observable, Subject } from 'rxjs';

export interface ToastConfig extends MatSnackBarConfig {
  message: string;
  action?: string;
  icon?: string;
  type?: 'info' | 'success' | 'warning' | 'error';
}

@Injectable({
  providedIn: 'root'
})
export class ToastNotificationService {
  private notificationSubject = new Subject<ToastConfig>();
  public notification$: Observable<ToastConfig> = this.notificationSubject.asObservable();
  
  constructor(private snackBar: MatSnackBar) { }
  
  show(config: ToastConfig): void {
    // Default configuration
    const defaultConfig: MatSnackBarConfig = {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: config.type ? [`toast-${config.type}`] : []
    };
    
    // Merge with provided config
    const snackBarConfig = { ...defaultConfig, ...config };
    
    // Display the notification
    this.snackBar.open(config.message, config.action || 'Close', snackBarConfig);
    
    // Emit the notification for components that are listening
    this.notificationSubject.next(config);
  }
  
  success(message: string, action?: string, config?: Partial<MatSnackBarConfig>): void {
    this.show({
      message,
      action,
      type: 'success',
      icon: 'check_circle',
      ...config
    });
  }
  
  info(message: string, action?: string, config?: Partial<MatSnackBarConfig>): void {
    this.show({
      message,
      action,
      type: 'info',
      icon: 'info',
      ...config
    });
  }
  
  warning(message: string, action?: string, config?: Partial<MatSnackBarConfig>): void {
    this.show({
      message,
      action,
      type: 'warning',
      icon: 'warning',
      ...config
    });
  }
  
  error(message: string, action?: string, config?: Partial<MatSnackBarConfig>): void {
    this.show({
      message,
      action,
      type: 'error',
      icon: 'error',
      ...config
    });
  }
}