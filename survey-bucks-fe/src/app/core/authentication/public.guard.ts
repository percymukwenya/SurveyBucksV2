import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { AuthService } from './auth.service';

export const publicGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  return authService.isAuthenticated$.pipe(
    map((isAuthenticated) => {
      if (isAuthenticated) {
        const user = authService.currentUser;
        // Redirect to appropriate dashboard based on role
        if (user?.role === 'Admin') {
          router.navigate(['/admin/dashboard']);
        } else if (user?.role === 'Client') {
          router.navigate(['/client/dashboard']);
        }
        return false;
      }
      return true;
    })
  );
};