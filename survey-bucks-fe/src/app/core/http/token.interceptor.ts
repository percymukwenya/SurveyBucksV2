import { HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);
  
  // Only try to access localStorage in browser environment
  if (!isPlatformBrowser(platformId)) {
    return next(req);
  }
  
  const token = localStorage.getItem('auth_token');

  // Skip adding token for certain requests (like login/register)
  const skipTokenUrls = ['/auth/login', '/auth/register', '/auth/forgot-password'];
  const shouldSkipToken = skipTokenUrls.some(url => req.url.includes(url));
  
  if (token && !shouldSkipToken) {
    const cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(cloned);
  }

  return next(req);
};