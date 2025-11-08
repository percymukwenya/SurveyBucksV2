import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private apiUrl = `${environment.apiUrl}/api/notification`;
  
  constructor(private http: HttpClient) { }
  
  getUserNotifications(unreadOnly: boolean = false): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}?unreadOnly=${unreadOnly}`);
  }
  
  getUnreadNotificationCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/count`);
  }
  
  markNotificationAsRead(notificationId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${notificationId}/read`, {});
  }
  
  markAllNotificationsAsRead(): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/read-all`, {});
  }

  deleteNotification(notificationId: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${notificationId}`);
  }
  
  clearAllNotifications(): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/clear-all`);
  }
}
