import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);
  
  // Skip loading indicator for certain requests
  const skipLoadingUrls = ['/auth/me', '/api/notifications'];
  const shouldSkipLoading = skipLoadingUrls.some(url => req.url.includes(url));
  
  if (!shouldSkipLoading) {
    loadingService.setLoading(true);
  }
  
  return next(req).pipe(
    finalize(() => {
      if (!shouldSkipLoading) {
        loadingService.setLoading(false);
      }
    })
  );
};