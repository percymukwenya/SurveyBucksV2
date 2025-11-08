import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { fromEvent } from 'rxjs';
import { filter } from 'rxjs/operators';
import { KeyboardShortcutsDialogComponent } from '../../shared/components/keyboard-shortcuts-dialog/keyboard-shortcuts-dialog.component';

/**
 * Service to handle global keyboard shortcuts
 *
 * Usage:
 * 1. Inject in app.component.ts constructor to initialize
 * 2. Press '?' anywhere to show keyboard shortcuts dialog
 */
@Injectable({
  providedIn: 'root'
})
export class KeyboardShortcutsService {
  private dialog = inject(MatDialog);

  constructor() {
    this.initializeGlobalShortcuts();
  }

  /**
   * Initialize global keyboard shortcuts
   */
  private initializeGlobalShortcuts(): void {
    // Listen for '?' key to show help dialog
    fromEvent<KeyboardEvent>(document, 'keydown')
      .pipe(
        filter(event => {
          // Don't trigger if user is typing in an input
          const target = event.target as HTMLElement;
          const isInput = target.tagName === 'INPUT' ||
                         target.tagName === 'TEXTAREA' ||
                         target.isContentEditable;

          return event.key === '?' &&
                 !isInput &&
                 !event.ctrlKey &&
                 !event.altKey &&
                 !event.metaKey;
        })
      )
      .subscribe((event) => {
        event.preventDefault();
        this.showKeyboardShortcutsDialog();
      });
  }

  /**
   * Show the keyboard shortcuts dialog
   */
  showKeyboardShortcutsDialog(): void {
    this.dialog.open(KeyboardShortcutsDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      panelClass: 'keyboard-shortcuts-dialog-panel',
      autoFocus: true,
      restoreFocus: true
    });
  }
}
