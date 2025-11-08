import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { Stat } from '../../../core/services/data.service';

@Component({
  selector: 'app-hero',
  imports: [CommonModule, RouterLink, MatButtonModule],
  templateUrl: './hero.component.html',
  styleUrl: './hero.component.scss',
})
export class HeroComponent {
  @Input() stats: Stat[] = [];
}
