import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserPoints, PointTransaction, UserReward, AvailableReward } from '../models/rewards.models';

@Injectable({
  providedIn: 'root'
})
export class RewardsService {
  private apiUrl = `${environment.apiUrl}/api/rewards`;
  private gamificationApiUrl = `${environment.apiUrl}/api/gamification`;
  
  constructor(private http: HttpClient) { }
  
  getUserPoints(): Observable<UserPoints> {
    return this.http.get<UserPoints>(`${this.apiUrl}/points`);
  }

  getUserLevel(): Observable<any> {
    return this.http.get<any>(`${this.gamificationApiUrl}/level`);
  }  
  
  getPointTransactions(take: number = 20, skip: number = 0): Observable<PointTransaction[]> {
    const params = new HttpParams()
      .set('take', take.toString())
      .set('skip', skip.toString());
    
    return this.http.get<PointTransaction[]>(`${this.apiUrl}/transactions`, { params });
  }
  
  // User Rewards
  getUserRewards(): Observable<UserReward[]> {
    return this.http.get<UserReward[]>(`${this.apiUrl}/user-rewards`);
  }
  
  getAvailableRewards(): Observable<AvailableReward[]> {
    return this.http.get<AvailableReward[]>(`${this.apiUrl}/available`);
  }
  
  getAvailableRewardsByCategory(category: string): Observable<AvailableReward[]> {
    const params = new HttpParams().set('category', category);
    return this.http.get<AvailableReward[]>(`${this.apiUrl}/available`, { params });
  }
  
  // Reward Actions
  redeemReward(rewardId: number): Observable<UserReward> {
    return this.http.post<UserReward>(`${this.apiUrl}/redeem/${rewardId}`, {});
  }
  
  claimReward(userRewardId: number): Observable<{ success: boolean; message: string; rewardCode?: string }> {
    return this.http.post<{ success: boolean; message: string; rewardCode?: string }>(`${this.apiUrl}/claim/${userRewardId}`, {});
  }

  // Additional helper methods
  getRewardCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/categories`);
  }

  getRewardDetails(rewardId: number): Observable<AvailableReward> {
    return this.http.get<AvailableReward>(`${this.apiUrl}/${rewardId}`);
  }

  getUserRewardHistory(status?: string): Observable<UserReward[]> {
    const params = status ? new HttpParams().set('status', status) : new HttpParams();
    return this.http.get<UserReward[]>(`${this.apiUrl}/user-rewards/history`, { params });
  }
}

