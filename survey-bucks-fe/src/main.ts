import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';
import { tokenInterceptor } from './app/core/http/token.interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(
      withFetch(),
      withInterceptors([tokenInterceptor])
    ),
    provideAnimations(),
    provideRouter(routes),
  ]
});