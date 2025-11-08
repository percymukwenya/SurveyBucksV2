import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-how-it-works',
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    RouterModule
  ],
  templateUrl: './how-it-works.component.html',
  styleUrl: './how-it-works.component.scss'
})
export class HowItWorksComponent {
  steps = [
    {
      icon: 'person_add',
      title: 'Create Your Account',
      description: 'Quick and simple registration to join our community',
      details: [
        'Basic personal information',
        'Email verification',
        'Set up your profile preferences',
        'Completely free to join'
      ]
    },
    {
      icon: 'assignment',
      title: 'Complete Your Profile',
      description: 'Help us match you with relevant survey opportunities',
      details: [
        'Demographics and interests',
        'Location and lifestyle preferences',
        'Shopping and product preferences',
        'Privacy settings you control'
      ]
    },
    {
      icon: 'quiz',
      title: 'Get Matched to Surveys',
      description: 'We send you surveys that match your profile',
      details: [
        'Surveys matched to your demographics',
        'Invitations sent via email',
        'Only relevant surveys for your profile',
        'Quality matching ensures better experience'
      ]
    },
    {
      icon: 'stars',
      title: 'Earn Rewards',
      description: 'Get rewarded through our gamified system',
      details: [
        'Points for each completed survey',
        'Bonus points for referrals',
        'Achievement badges and levels',
        'Multiple redemption options'
      ]
    }
  ];

  surveyTypes = [
    {
      icon: 'shopping_cart',
      title: 'Product Research',
      description: 'Help companies understand consumer preferences for new and existing products',
      earningRange: 'Earn points + potential cash rewards'
    },
    {
      icon: 'business',
      title: 'Service Feedback',
      description: 'Share experiences with various services to help improve customer satisfaction',
      earningRange: 'Earn points + bonus opportunities'
    },
    {
      icon: 'phone_android',
      title: 'App & Website Testing',
      description: 'Test digital platforms and provide usability feedback',
      earningRange: 'Higher point values for longer sessions'
    },
    {
      icon: 'lightbulb',
      title: 'Innovation Insights',
      description: 'Contribute to product development and innovation processes',
      earningRange: 'Premium points for specialized feedback'
    },
    {
      icon: 'trending_up',
      title: 'Market Trends',
      description: 'Share insights on industry trends and market preferences',
      earningRange: 'Variable rewards based on survey complexity'
    },
    {
      icon: 'group',
      title: 'Community Research',
      description: 'Participate in community-focused research projects',
      earningRange: 'Collaborative rewards and group bonuses'
    }
  ];

  faqs = [
    {
      question: 'How does the rewards system work?',
      answer: 'We use a points-based system with gamification elements. Earn points for surveys, referrals, and platform engagement. Points can be redeemed for various rewards as we establish partnerships with retailers and service providers.'
    },
    {
      question: 'How do I get survey invitations?',
      answer: 'Our matching system analyzes your profile and demographics to send you surveys that are relevant to you. You\'ll receive email invitations for surveys where you match the target audience that our business partners are looking for.'
    },
    {
      question: 'Why don\'t I qualify for some surveys?',
      answer: 'Surveys have specific demographic requirements set by our business partners. If you don\'t match the target profile (age, location, interests, etc.), you won\'t be invited to that particular survey. This ensures better quality responses for researchers.'
    },
    {
      question: 'How long do surveys typically take?',
      answer: 'Survey length varies based on the research requirements of our business partners. We always show estimated time and reward before you start, typically ranging from 5-30 minutes.'
    },
    {
      question: 'How do referrals work?',
      answer: 'Invite friends and family to join our platform. You earn bonus points when they sign up and complete their first survey. Both you and your referral benefit from our referral program.'
    },
    {
      question: 'What payout options will be available?',
      answer: 'We\'re developing multiple redemption options including bank transfers, mobile money, and gift vouchers. As we grow, we\'ll add more partners and payout methods based on user preferences.'
    },
    {
      question: 'Who can participate?',
      answer: 'Anyone 18 or older residing in South Africa can join, regardless of citizenship status. You need valid identification (ID, passport, or permit) and a South African bank account or mobile money account for future payouts.'
    }
  ];

  requirements = [
    {
      title: 'Age Requirement',
      description: 'Must be 18 years or older to participate',
      required: true
    },
    {
      title: 'South African Resident',
      description: 'Currently residing in South Africa',
      required: true
    },
    {
      title: 'Valid Identification',
      description: 'South African ID, passport, or valid permit',
      required: true
    },
    {
      title: 'Active Email',
      description: 'Valid email address for communications',
      required: true
    },
    {
      title: 'Internet Access',
      description: 'Reliable internet to complete surveys',
      required: true
    },
    {
      title: 'Banking Details',
      description: 'SA bank account or mobile money for future payouts',
      required: false
    }
  ];

  gamificationFeatures = [
    {
      icon: 'military_tech',
      title: 'Achievement Badges',
      description: 'Unlock badges for completing surveys and reaching milestones'
    },
    {
      icon: 'trending_up',
      title: 'Level Progression',
      description: 'Advance through levels and unlock premium survey opportunities'
    },
    {
      icon: 'group_add',
      title: 'Referral Rewards',
      description: 'Earn bonus points for every friend you bring to the platform'
    },
    {
      icon: 'event',
      title: 'Daily Challenges',
      description: 'Complete daily tasks for extra points and rewards'
    }
  ];
}
