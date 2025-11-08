import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { AuthService } from './auth.service';

export const roleGuard = (allowedRoles: string[]) => {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);
    
    return authService.isAuthenticated$.pipe(
      map((isAuthenticated) => {
        // First check authentication
        if (!isAuthenticated) {
          router.navigate(['/auth/login']);
          return false;
        }
        
        // Then check role
        const user = authService.currentUser;
        if (!user || !allowedRoles.includes(user.role)) {
          // Redirect based on user's actual role
          if (user?.role === 'Client') {
            router.navigate(['/client/dashboard']);
          } else if (user?.role === 'Admin') {
            router.navigate(['/admin/dashboard']);
          } else {
            router.navigate(['/']);
          }
          return false;
        }
        
        return true;
      })
    );
  };
};