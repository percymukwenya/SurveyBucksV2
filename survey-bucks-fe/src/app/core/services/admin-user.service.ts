import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminUserService {
  private apiUrl = `${environment.apiUrl}/api/admin/users`;
  
  constructor(private http: HttpClient) { }
  
  getAllUsers(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }
  
  getUserById(id: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }
  
  createUser(userData: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, userData);
  }
  
  updateUser(id: string, userData: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, userData);
  }
  
  updateUserStatus(id: string, status: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/status`, { status });
  }
  
  deleteUser(id: string): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }
  
  getUserSurveyHistory(id: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${id}/surveys`);
  }
  
  getUserRewardHistory(id: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${id}/rewards`);
  }
}