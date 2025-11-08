import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-about',
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    RouterModule
  ],
  templateUrl: './about.component.html',
  styleUrl: './about.component.scss'
})
export class AboutComponent {
  values = [
    {
      icon: 'visibility',
      title: 'Transparency',
      description: 'We\'re honest about being new, our current capabilities, and what we\'re building. No fake statistics or misleading claims.'
    },
    {
      icon: 'security',
      title: 'Data Protection',
      description: 'Your personal information is protected according to South Africa\'s POPI Act and never sold to third parties.'
    },
    {
      icon: 'handshake',
      title: 'Fair Rewards',
      description: 'We believe in fair compensation for your time and valuable opinions, with transparent point systems and honest payout timelines.'
    },
    {
      icon: 'trending_up',
      title: 'Growth Together',
      description: 'As we grow our business network, our community benefits with more surveys, better rewards, and enhanced features.'
    }
  ];

  workProcess = [
    {
      icon: 'business',
      title: 'Build Business Partnerships',
      description: 'We actively seek partnerships with South African companies who need authentic consumer insights.'
    },
    {
      icon: 'psychology',
      title: 'Create Meaningful Research',
      description: 'We design surveys that provide genuine value to businesses while being engaging and respectful of your time.'
    },
    {
      icon: 'person_search',
      title: 'Smart Participant Matching',
      description: 'Our system matches surveys to participants based on demographics and interests, ensuring relevant opportunities.'
    },
    {
      icon: 'insights',
      title: 'Deliver Real Impact',
      description: 'Your feedback helps South African businesses improve their products and services while you earn rewards.'
    }
  ];

  platformFeatures = [
    {
      icon: 'smartphone',
      title: 'Mobile-Friendly Platform',
      description: 'Take surveys on any device, anywhere in South Africa with internet access.'
    },
    {
      icon: 'translate',
      title: 'Local Language Support',
      description: 'Surveys available in English and Afrikaans, with more South African languages planned.'
    },
    {
      icon: 'account_balance',
      title: 'SA Banking Integration',
      description: 'Direct EFT payments to all major South African banks with secure processing.'
    },
    {
      icon: 'diversity_3',
      title: 'Inclusive Community',
      description: 'Welcome to all South African residents regardless of citizenship status or background.'
    }
  ];

  whyWereBuilding = [
    {
      title: 'Gap in the SA Market',
      description: 'South African consumers deserve fair compensation for their valuable insights, but most international platforms don\'t serve our market well.',
      icon: 'location_on'
    },
    {
      title: 'Local Business Needs',
      description: 'SA businesses need authentic local consumer feedback to compete globally while serving local markets effectively.',
      icon: 'store'
    },
    {
      title: 'Economic Opportunity',
      description: 'Creating income opportunities for South Africans while supporting local business growth and innovation.',
      icon: 'payments'
    }
  ];

  securityFeatures = [
    {
      icon: 'lock',
      title: 'POPI Act Compliance',
      description: 'We comply with South Africa\'s Protection of Personal Information Act for responsible data handling.'
    },
    {
      icon: 'verified_user',
      title: 'Anonymous Responses',
      description: 'Your survey responses are anonymized and never linked back to your personal identity.'
    },
    {
      icon: 'privacy_tip',
      title: 'Minimal Data Collection',
      description: 'We only collect information necessary for survey matching and secure payments.'
    },
    {
      icon: 'https',
      title: 'Secure Platform',
      description: 'All data transmission is encrypted and we use secure South African banking partners.'
    }
  ];

  contactMethods = [
    {
      icon: 'support_agent',
      title: 'General Support',
      description: 'Questions about your account, surveys, or payments',
      link: 'mailto:support@yourplatform.co.za',
      linkText: 'support@yourplatform.co.za'
    },
    {
      icon: 'business',
      title: 'Business Partnerships',
      description: 'South African businesses interested in consumer research',
      link: 'mailto:partnerships@yourplatform.co.za',
      linkText: 'partnerships@yourplatform.co.za'
    },
    {
      icon: 'feedback',
      title: 'Platform Feedback',
      description: 'Help us improve by sharing your suggestions',
      link: 'mailto:feedback@yourplatform.co.za',
      linkText: 'feedback@yourplatform.co.za'
    }
  ];

  currentStatus = {
    stage: 'Early Growth',
    launched: '2024',
    focusAreas: [
      'Building business partnerships',
      'Growing user community',
      'Developing mobile app',
      'Expanding reward options'
    ]
  };
}
