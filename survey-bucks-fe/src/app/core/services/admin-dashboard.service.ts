// src/app/core/services/admin-dashboard.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminDashboardService {
  private apiUrl = `${environment.apiUrl}/api/AdminDashboard`;
  
  constructor(private http: HttpClient) { }
  
  getDashboardStats(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/stats`);
  }
  
  getRecentActivity(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/activity`);
  }
}