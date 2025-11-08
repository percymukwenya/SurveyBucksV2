import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { KeyboardShortcutsService } from './core/services/keyboard-shortcuts.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'survey-bucks';

  // Initialize keyboard shortcuts globally
  private keyboardShortcuts = inject(KeyboardShortcutsService);
}
