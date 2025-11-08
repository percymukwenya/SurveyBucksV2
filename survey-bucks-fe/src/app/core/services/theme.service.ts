import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private _isDarkMode = new BehaviorSubject<boolean>(false);
  isDarkMode$ = this._isDarkMode.asObservable();
  
  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    this.initializeTheme();
  }
  
  private initializeTheme(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    // Load user preference from localStorage
    const savedTheme = localStorage.getItem('theme');
    
    if (savedTheme) {
      this._isDarkMode.next(savedTheme === 'dark');
    } else {
      // Check for system preference
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      this._isDarkMode.next(prefersDark);
    }
    
    // Apply initial theme
    this.applyTheme(this._isDarkMode.value);
    
    // Listen for system preference changes
    if (window.matchMedia) {
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
        if (!localStorage.getItem('theme')) {
          this._isDarkMode.next(e.matches);
          this.applyTheme(e.matches);
        }
      });
    }
  }
  
  toggleDarkMode(): void {
    const newValue = !this._isDarkMode.value;
    this._isDarkMode.next(newValue);
    
    if (isPlatformBrowser(this.platformId)) {
      // Save user preference
      localStorage.setItem('theme', newValue ? 'dark' : 'light');
    }
    
    // Apply the theme
    this.applyTheme(newValue);
  }
  
  setDarkMode(isDark: boolean): void {
    this._isDarkMode.next(isDark);
    
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('theme', isDark ? 'dark' : 'light');
    }
    
    this.applyTheme(isDark);
  }
  
  get isDarkMode(): boolean {
    return this._isDarkMode.value;
  }
  
  private applyTheme(isDark: boolean): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    // Apply to document body
    if (isDark) {
      document.body.classList.add('dark-theme');
    } else {
      document.body.classList.remove('dark-theme');
    }
    
    // Apply to meta theme-color for mobile browsers
    const metaThemeColor = document.querySelector('meta[name="theme-color"]');
    if (metaThemeColor) {
      metaThemeColor.setAttribute('content', isDark ? '#1e1e1e' : '#673ab7');
    }
    
    // Apply CSS custom properties for dynamic theming
    const root = document.documentElement;
    if (isDark) {
      root.style.setProperty('--primary-color', '#7b52d3');
      root.style.setProperty('--accent-color', '#26c6da');
      root.style.setProperty('--background-color', '#121212');
      root.style.setProperty('--surface-color', '#1e1e1e');
      root.style.setProperty('--text-primary', '#e0e0e0');
      root.style.setProperty('--text-secondary', '#b0b0b0');
      root.style.setProperty('--border-color', '#333333');
    } else {
      root.style.setProperty('--primary-color', '#673ab7');
      root.style.setProperty('--accent-color', '#16d2cb');
      root.style.setProperty('--background-color', '#f7f9fc');
      root.style.setProperty('--surface-color', '#ffffff');
      root.style.setProperty('--text-primary', '#333333');
      root.style.setProperty('--text-secondary', '#666666');
      root.style.setProperty('--border-color', '#e0e0e0');
    }
  }
}