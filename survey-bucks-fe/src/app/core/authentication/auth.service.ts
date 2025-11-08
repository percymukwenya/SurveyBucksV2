import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, catchError, map, Observable, tap, throwError } from 'rxjs';
import { User, UserRole } from '../models/user.model';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { isPlatformBrowser } from '@angular/common';
import { AuthResponse, ForgotPasswordRequest, LoginRequest, RegisterRequest, ResetPasswordRequest } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'currentUser';
  private readonly API_URL = `${environment.apiUrl}/api/auth`;

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  get currentUser(): User | null {
    return this.currentUserSubject.value;
  }
  
  get isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  constructor(
    private http: HttpClient, 
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {    
    // Initialize authentication state on service creation
    if (isPlatformBrowser(this.platformId) && this.hasValidToken()) {
      this.validateStoredToken();
    }
  }  
  
  login(email: string, password: string): Observable<User> {
    const loginRequest: LoginRequest = { email, password };
    
    return this.http.post<AuthResponse>(`${this.API_URL}/login`, loginRequest)
      .pipe(
        map(response => this.mapAuthResponseToUser(response)),
        tap(user => this.handleSuccessfulAuth(user)),
        catchError(error => this.handleAuthError(error))
      );
  }
  
  register(user: Partial<User>, password: string): Observable<any> {
    const registerRequest: RegisterRequest = {
      firstName: user.firstName!,
      lastName: user.lastName!,
      email: user.email!,
      password,
      confirmPassword: password,
      phoneNumber: user.phoneNumber
    };

    return this.http.post(`${this.API_URL}/register`, registerRequest)
      .pipe(
        catchError(error => this.handleAuthError(error))
      );
  }

  forgotPassword(email: string): Observable<any> {
    const request: ForgotPasswordRequest = { email };
    
    return this.http.post(`${this.API_URL}/forgot-password`, request)
      .pipe(
        catchError(error => this.handleAuthError(error))
      );
  }

  resetPassword(email: string, token: string, newPassword: string, confirmPassword: string): Observable<any> {
    const request: ResetPasswordRequest = { email, token, newPassword, confirmPassword };
    
    return this.http.post(`${this.API_URL}/reset-password`, request)
      .pipe(
        catchError(error => this.handleAuthError(error))
      );
  }
  
  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.USER_KEY);
    }
    
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/']);
  }

  loadCurrentUser(): Observable<User> {
    return this.http.get<any>(`${this.API_URL}/me`)
      .pipe(
        map(response => ({
          id: response.id,
          email: response.email,
          firstName: response.firstName,
          lastName: response.lastName,
          role: response.roles?.[0] || UserRole.Client
        } as User)),
        tap(user => {
          this.currentUserSubject.next(user);
          this.storeUser(user);
        }),
        catchError(error => {
          this.logout();
          return throwError(() => error);
        })
      );
  }

  hasRole(role: UserRole): boolean {
    return this.currentUser?.role === role;
  }

  isAdmin(): boolean {
    return this.hasRole(UserRole.Admin);
  }

  isClient(): boolean {
    return this.hasRole(UserRole.Client);
  }

  // Navigate based on user role
  redirectBasedOnRole(): void {
    const user = this.currentUser;
    
    if (!user) {
      this.router.navigate(['/auth/login']);
      return;
    }

    switch (user.role) {
      case UserRole.Admin:
        this.router.navigate(['/admin/dashboard']);
        break;
      case UserRole.Client:
        this.router.navigate(['/client/dashboard']);
        break;
      default:
        this.router.navigate(['/']);
    }
  }

  // Helper method for getting user's full name
  getUserFullName(): string {
    const user = this.currentUser;
    if (!user) return '';
    return `${user.firstName} ${user.lastName}`.trim();
  }
  
  // Helper method to get user detail (for navbar compatibility)
  getUserDetail(): { fullName: string; [key: string]: any } | null {
    const user = this.currentUser;
    if (!user) return null;
    
    return {
      ...user,
      fullName: this.getUserFullName()
    };
  }

  resendEmailConfirmation(email: string): Observable<any> {
  const request = { email };
  
  return this.http.post(`${this.API_URL}/resend-confirmation`, request)
    .pipe(
      catchError(error => this.handleAuthError(error))
    );
}

  // Check if token exists and is not expired  
  private hasValidToken(): boolean {
    if (!isPlatformBrowser(this.platformId)) {
      return false;
    }
    
    const token = localStorage.getItem(this.TOKEN_KEY);
    if (!token) return false;

    // Add JWT token expiration check here if needed
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expirationTime = payload.exp * 1000;
      return Date.now() < expirationTime;
    } catch {
      return false;
    }
  }

  private getStoredUser(): User | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }

    const storedUser = localStorage.getItem(this.USER_KEY);
    if (storedUser) {
      try {
        return JSON.parse(storedUser);
      } catch {
        return null;
      }
    }
    return null;
  }

  private validateStoredToken(): void {
    if (this.hasValidToken()) {
      this.loadCurrentUser().subscribe({
        error: () => this.logout()
      });
    } else {
      this.logout();
    }
  }

  private mapAuthResponseToUser(response: AuthResponse): User {
    return {
      id: response.id,
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
      role: response.role as UserRole,
      token: response.token
    };
  }

  private handleSuccessfulAuth(user: User): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(this.TOKEN_KEY, user.token || '');
      this.storeUser(user);
    }
    
    this.currentUserSubject.next(user);
    this.isAuthenticatedSubject.next(true);
  }

  private storeUser(user: User): void {
    if (isPlatformBrowser(this.platformId)) {
      const userToStore = { ...user };
      delete userToStore.token; // Don't store token in user object
      localStorage.setItem(this.USER_KEY, JSON.stringify(userToStore));
    }
  }

  private handleAuthError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An authentication error occurred';
    
    if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.error?.errors) {
      errorMessage = Array.isArray(error.error.errors) 
        ? error.error.errors.join(', ')
        : error.error.errors;
    } else if (error.status === 0) {
      errorMessage = 'Unable to connect to the server';
    } else if (error.status === 401) {
      errorMessage = 'Invalid email or password';
    } else if (error.status === 423) {
      errorMessage = 'Account is locked out';
    }

    return throwError(() => ({ 
      ...error, 
      error: { 
        ...error.error, 
        message: errorMessage 
      } 
    }));
  }
}