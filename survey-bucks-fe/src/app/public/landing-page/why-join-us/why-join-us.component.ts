import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-why-join-us',
  imports: [CommonModule],
  templateUrl: './why-join-us.component.html',
  styleUrl: './why-join-us.component.scss'
})
export class WhyJoinUsComponent {
  @Input() variant: 'first' | 'second' = 'first';

  getImagePath(): string {
    return this.variant === 'first' 
      ? 'assets/images/woman-tablet.png' 
      : 'assets/images/person-desktop.jpg';
  }
}
