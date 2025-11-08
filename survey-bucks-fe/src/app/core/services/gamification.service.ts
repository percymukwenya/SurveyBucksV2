import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  UserLevel, 
  Achievement, 
  Challenge, 
  LeaderboardInfo, 
  Leaderboard 
} from '../models/gamification.models';

@Injectable({
  providedIn: 'root'
})
export class GamificationService {
  private apiUrl = `${environment.apiUrl}/api/gamification`;
  
  constructor(private http: HttpClient) { }
  
  // User Level & Progress
  getUserLevel(): Observable<UserLevel> {
    return this.http.get<UserLevel>(`${this.apiUrl}/level`);
  }
  
  // Achievements
  getUserAchievements(): Observable<Achievement[]> {
    return this.http.get<Achievement[]>(`${this.apiUrl}/achievements`);
  }

  getUnlockedAchievements(): Observable<Achievement[]> {
    const params = new HttpParams().set('unlocked', 'true');
    return this.http.get<Achievement[]>(`${this.apiUrl}/achievements`, { params });
  }

  getAvailableAchievements(): Observable<Achievement[]> {
    const params = new HttpParams().set('available', 'true');
    return this.http.get<Achievement[]>(`${this.apiUrl}/achievements`, { params });
  }

  getAchievementsByCategory(category: string): Observable<Achievement[]> {
    const params = new HttpParams().set('category', category);
    return this.http.get<Achievement[]>(`${this.apiUrl}/achievements`, { params });
  }
  
  // Challenges
  getActiveChallenges(): Observable<Challenge[]> {
    return this.http.get<Challenge[]>(`${this.apiUrl}/challenges`);
  }

  getAllChallenges(): Observable<Challenge[]> {
    const params = new HttpParams().set('includeInactive', 'true');
    return this.http.get<Challenge[]>(`${this.apiUrl}/challenges`, { params });
  }

  getChallengeProgress(challengeId: number): Observable<Challenge> {
    return this.http.get<Challenge>(`${this.apiUrl}/challenges/${challengeId}`);
  }
  
  // Leaderboards
  getAvailableLeaderboards(): Observable<LeaderboardInfo[]> {
    return this.http.get<LeaderboardInfo[]>(`${this.apiUrl}/leaderboards`);
  }
  
  getLeaderboard(leaderboardId: number, top: number = 10): Observable<Leaderboard> {
    const params = new HttpParams().set('top', top.toString());
    return this.http.get<Leaderboard>(`${this.apiUrl}/leaderboards/${leaderboardId}`, { params });
  }

  getUserRankInLeaderboard(leaderboardId: number): Observable<{ rank: number; totalParticipants: number }> {
    return this.http.get<{ rank: number; totalParticipants: number }>(`${this.apiUrl}/leaderboards/${leaderboardId}/user-rank`);
  }

  // Statistics and Progress
  getUserStats(): Observable<{
    totalSurveys: number;
    totalPoints: number;
    currentStreak: number;
    longestStreak: number;
    achievementsUnlocked: number;
    challengesCompleted: number;
  }> {
    return this.http.get<any>(`${this.apiUrl}/stats`);
  }

  getProgressSummary(): Observable<{
    level: UserLevel;
    recentAchievements: Achievement[];
    activeChallenges: Challenge[];
    nextMilestone: { type: string; target: number; current: number; };
  }> {
    return this.http.get<any>(`${this.apiUrl}/progress-summary`);
  }
}