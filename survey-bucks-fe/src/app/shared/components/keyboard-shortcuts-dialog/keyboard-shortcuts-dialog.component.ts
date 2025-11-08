import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';

interface KeyboardShortcut {
  keys: string[];
  description: string;
  category: string;
}

@Component({
  selector: 'app-keyboard-shortcuts-dialog',
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatDividerModule
  ],
  templateUrl: './keyboard-shortcuts-dialog.component.html',
  styleUrl: './keyboard-shortcuts-dialog.component.scss'
})
export class KeyboardShortcutsDialogComponent {
  shortcuts: KeyboardShortcut[] = [
    // General Navigation
    { keys: ['?'], description: 'Show keyboard shortcuts', category: 'General' },
    { keys: ['Esc'], description: 'Close dialogs and modals', category: 'General' },
    { keys: ['Tab'], description: 'Navigate to next element', category: 'General' },
    { keys: ['Shift', 'Tab'], description: 'Navigate to previous element', category: 'General' },
    { keys: ['Enter'], description: 'Activate button or link', category: 'General' },

    // Survey Taking
    { keys: ['Ctrl', 'S'], description: 'Save survey progress', category: 'Survey Taking' },
    { keys: ['Ctrl', '→'], description: 'Next section', category: 'Survey Taking' },
    { keys: ['Ctrl', '←'], description: 'Previous section', category: 'Survey Taking' },
    { keys: ['Ctrl', 'Enter'], description: 'Submit survey', category: 'Survey Taking' },

    // Dashboard & Lists
    { keys: ['r'], description: 'Refresh current page', category: 'Dashboard' },
    { keys: ['/'], description: 'Focus search (when available)', category: 'Dashboard' },
    { keys: ['n'], description: 'New item (context-dependent)', category: 'Dashboard' },

    // Admin Shortcuts
    { keys: ['Ctrl', 'n'], description: 'Create new survey', category: 'Admin' },
    { keys: ['Ctrl', 'p'], description: 'Preview survey', category: 'Admin' },
    { keys: ['Ctrl', 'Shift', 'p'], description: 'Publish survey', category: 'Admin' },
  ];

  get categories(): string[] {
    return [...new Set(this.shortcuts.map(s => s.category))];
  }

  getShortcutsByCategory(category: string): KeyboardShortcut[] {
    return this.shortcuts.filter(s => s.category === category);
  }

  formatKeys(keys: string[]): string {
    return keys.join(' + ');
  }
}
