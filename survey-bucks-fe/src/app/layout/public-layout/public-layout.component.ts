import { Component, Inject, PLATFORM_ID } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/authentication/auth.service';
import { FooterComponent } from "../footer/footer.component";
import { NavbarComponent } from "../navbar/navbar/navbar.component";
import { CommonModule, isPlatformBrowser } from '@angular/common';

@Component({
  selector: 'app-public-layout',
  imports: [
    CommonModule,
    RouterModule,
    FooterComponent,
    NavbarComponent
],
  templateUrl: './public-layout.component.html',
  styleUrl: './public-layout.component.scss'
})
export class PublicLayoutComponent {
  currentYear = new Date().getFullYear();
  isMobileView = false;
  private resizeListener?: () => void;
  
  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    // Only access window in browser environment
    if (isPlatformBrowser(this.platformId)) {
      this.checkMobileView();
      this.setupResizeListener();
    }
  }

  private checkMobileView(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.isMobileView = window.innerWidth < 768;
    }
  }

  private setupResizeListener(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.resizeListener = () => {
        this.checkMobileView();
      };
      window.addEventListener('resize', this.resizeListener);
    }
  }
  
  ngOnDestroy(): void {
    if (isPlatformBrowser(this.platformId) && this.resizeListener) {
      window.removeEventListener('resize', this.resizeListener);
    }
  }
}
