import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, interval, takeWhile } from 'rxjs';
import { map } from 'rxjs/operators';

export interface LoadingState {
  isLoading: boolean;
  elapsedSeconds: number;
  message: string;
  showCancel: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class LoadingTimerService {
  private loadingStates = new Map<string, BehaviorSubject<LoadingState>>();
  private timers = new Map<string, any>();

  /**
   * Start tracking loading time for a specific operation
   * @param key Unique identifier for the loading operation
   * @param initialMessage Initial loading message
   * @param showCancelAfterSeconds Show cancel button after X seconds (default: 10)
   */
  startLoading(key: string, initialMessage: string = 'Loading...', showCancelAfterSeconds: number = 10): void {
    // Clean up existing timer if any
    this.stopLoading(key);

    // Initialize state
    const state$ = new BehaviorSubject<LoadingState>({
      isLoading: true,
      elapsedSeconds: 0,
      message: initialMessage,
      showCancel: false
    });
    this.loadingStates.set(key, state$);

    // Start timer
    let elapsed = 0;
    const timer = interval(1000)
      .pipe(takeWhile(() => {
        const currentState = this.loadingStates.get(key);
        return currentState !== undefined && currentState.value.isLoading;
      }))
      .subscribe(() => {
        elapsed++;
        const currentState = this.loadingStates.get(key);
        if (currentState) {
          currentState.next({
            isLoading: true,
            elapsedSeconds: elapsed,
            message: this.getMessageForElapsedTime(elapsed, initialMessage),
            showCancel: elapsed >= showCancelAfterSeconds
          });
        }
      });

    this.timers.set(key, timer);
  }

  /**
   * Stop tracking loading time
   * @param key Unique identifier for the loading operation
   */
  stopLoading(key: string): void {
    const timer = this.timers.get(key);
    if (timer) {
      timer.unsubscribe();
      this.timers.delete(key);
    }

    const state$ = this.loadingStates.get(key);
    if (state$) {
      state$.next({
        isLoading: false,
        elapsedSeconds: state$.value.elapsedSeconds,
        message: '',
        showCancel: false
      });
      // Keep the state for a moment so components can read final value
      setTimeout(() => {
        state$.complete();
        this.loadingStates.delete(key);
      }, 100);
    }
  }

  /**
   * Get observable for loading state
   * @param key Unique identifier for the loading operation
   */
  getLoadingState(key: string): Observable<LoadingState> {
    if (!this.loadingStates.has(key)) {
      // Return a default non-loading state
      return new BehaviorSubject<LoadingState>({
        isLoading: false,
        elapsedSeconds: 0,
        message: '',
        showCancel: false
      }).asObservable();
    }
    return this.loadingStates.get(key)!.asObservable();
  }

  /**
   * Check if currently loading
   * @param key Unique identifier for the loading operation
   */
  isLoading(key: string): boolean {
    const state$ = this.loadingStates.get(key);
    return state$ ? state$.value.isLoading : false;
  }

  /**
   * Get current elapsed time
   * @param key Unique identifier for the loading operation
   */
  getElapsedSeconds(key: string): number {
    const state$ = this.loadingStates.get(key);
    return state$ ? state$.value.elapsedSeconds : 0;
  }

  /**
   * Generate appropriate message based on elapsed time
   */
  private getMessageForElapsedTime(seconds: number, initialMessage: string): string {
    if (seconds < 3) {
      return initialMessage;
    } else if (seconds < 5) {
      return `${initialMessage} (${seconds}s)`;
    } else if (seconds < 10) {
      return `Still loading... (${seconds}s)`;
    } else if (seconds < 20) {
      return `This is taking longer than expected... (${seconds}s)`;
    } else {
      return `Please wait, still processing... (${seconds}s)`;
    }
  }

  /**
   * Format elapsed time as human-readable string
   * @param seconds Elapsed seconds
   */
  formatElapsedTime(seconds: number): string {
    if (seconds < 60) {
      return `${seconds}s`;
    } else {
      const minutes = Math.floor(seconds / 60);
      const remainingSeconds = seconds % 60;
      return `${minutes}m ${remainingSeconds}s`;
    }
  }

  /**
   * Clean up all timers (call on service destroy)
   */
  ngOnDestroy(): void {
    this.timers.forEach(timer => timer.unsubscribe());
    this.timers.clear();
    this.loadingStates.forEach(state$ => state$.complete());
    this.loadingStates.clear();
  }
}
