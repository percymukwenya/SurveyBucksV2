import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { AuthService } from './auth.service';

export const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  return authService.isAuthenticated$.pipe(
    map((isAuthenticated) => {
      if (!isAuthenticated) {
        router.navigate(['/auth/login']);
        return false;
      }
      return true;
    })
  );
};