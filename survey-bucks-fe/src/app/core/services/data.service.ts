import { Injectable } from '@angular/core';

export interface Testimonial {
  name: string;
  content: string;
  rating: number;
}

export interface Step {
  number: number;
  title: string;
  description: string;
}

export interface Stat {
  value: string;
  label: string;
}

@Injectable({
  providedIn: 'root'
})
export class DataService {
  getStats(): Stat[] {
    return [
      { value: '5+', label: 'Active Users' },
      { value: 'R50+', label: 'Rewards Paid' },
      { value: '2+', label: 'Monthly Surveys' }
    ];
  }

  getHowItWorksSteps(): Step[] {
    return [
      { number: 1, title: 'Sign Up', description: 'Create your free account in minutes' },
      { number: 2, title: 'Complete Profile', description: 'Tell us about yourself to match surveys' },
      { number: 3, title: 'Take Surveys', description: 'Share your opinion on various topics' },
      { number: 4, title: 'Get Paid', description: 'Cash out via PayPal or gift cards' }
    ];
  }

  getBusinessSteps(): Step[] {
    return [
      { number: 1, title: 'Sign Up', description: 'Create your free account in minutes' },
      { number: 2, title: 'Fill in your details', description: 'Tell us about your business' },
      { number: 3, title: 'Create a request', description: 'What data do you need to collect' },
      { number: 4, title: 'Pay Now', description: 'Cash out via PayPal or credit card' },
      { number: 5, title: 'Receive The Results For You', description: 'Get vital insight for your business' }
    ];
  }

  getTestimonials(): Testimonial[] {
    return [
      {
        name: 'Alex M',
        content: 'I\'ve discovered fascinating insights about myself through these surveys.',
        rating: 5
      },
      {
        name: 'John S',
        content: 'The rewards are great, but the real value is in knowing my opinion matters.',
        rating: 5
      },
      {
        name: 'Sarah J',
        content: 'SurveyBucks provided me with a great way to earn extra income during my free time.',
        rating: 5
      }
    ];
  }

  getBenefits(): string[] {
    return [
      'Influence product development and services',
      'Earn rewards for your valuable feedback',
      'Join a community of honest reviewers',
      'Participate in surveys that match your interests'
    ];
  }

  getFeaturedSurveys(): string[] {
    return [
      'Tech Trends 2025',
      'Future of Remote Work',
      'Sustainable Living Habits'
    ];
  }
}