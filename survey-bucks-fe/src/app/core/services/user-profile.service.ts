import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserProfileService {
  private apiUrl = `${environment.apiUrl}/api/UserProfile`;
  private apiUrl2 = `${environment.apiUrl}/api/ProfileCompletion`;
  
  constructor(private http: HttpClient) { }
  
  getUserDemographics(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/demographics`);
  }
  
  updateDemographics(demographics: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/demographics`, demographics);
  }
  
  getProfileCompletion(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl2}`);
  }
  
  getUserInterests(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/interests`);
  }
  
  addUserInterest(interestData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/interests`, interestData);
  }
  
  removeUserInterest(interestId: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/interests/${interestId}`);
  }
  
  getUserEngagement(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/engagement`);
  }
  
  getUserDashboard(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/dashboard`);
  }

  getDetailedProfileCompletion(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl2}/detailed`);
  }
}
