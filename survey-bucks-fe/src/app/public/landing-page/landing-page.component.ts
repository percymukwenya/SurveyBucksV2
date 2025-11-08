import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { trigger, transition, style, animate } from '@angular/animations';
import { HeroComponent } from "./hero/hero.component";
import { StepsComponent } from "./steps/steps.component";
import { WhyJoinUsComponent } from "./why-join-us/why-join-us.component";
import { FeaturedSurveysComponent } from "./featured-surveys/featured-surveys.component";
import { BusinessStepsComponent } from "./business-steps/business-steps.component";
import { TestimonialsComponent } from "./testimonials/testimonials.component";
import { FaqComponent } from "./faq/faq.component";
import { DataService, Stat } from '../../core/services/data.service';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [
    CommonModule,
    HeroComponent,
    StepsComponent,
    WhyJoinUsComponent,
    FeaturedSurveysComponent,
    BusinessStepsComponent,
    TestimonialsComponent,
    FaqComponent
],
  animations: [
    trigger('fadeInUp', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('0.5s ease-out')
      ])
    ])
  ],
  templateUrl: './landing-page.component.html',
  styleUrls: ['./landing-page.component.scss']
})
export class LandingPageComponent {
  currentYear = new Date().getFullYear();

  stats: Stat[] = [];
  
  constructor(private dataService: DataService) {}

  ngOnInit(): void {
    this.stats = this.dataService.getStats();
  }
}
