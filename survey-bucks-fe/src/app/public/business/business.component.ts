import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-business',
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatTabsModule,
    RouterModule
  ],
  templateUrl: './business.component.html',
  styleUrl: './business.component.scss'
})
export class BusinessComponent {
  
  researchSolutions = [
    {
      icon: 'insights',
      title: 'Product Development Research',
      description: 'Get feedback on new products, features, or concepts before launch',
      useCases: [
        'Product concept testing',
        'Feature prioritization',
        'Packaging and design feedback',
        'Price sensitivity analysis'
      ],
      timeframe: '1-2 weeks',
      sampleSize: '50-500 participants'
    },
    {
      icon: 'trending_up',
      title: 'Market Research & Analysis',
      description: 'Understand your target market, competition, and growth opportunities',
      useCases: [
        'Brand perception studies',
        'Competitive analysis',
        'Market segmentation',
        'Customer satisfaction tracking'
      ],
      timeframe: '2-3 weeks',
      sampleSize: '100-1000 participants'
    },
    {
      icon: 'campaign',
      title: 'Marketing & Advertising Testing',
      description: 'Test marketing campaigns, messaging, and creative content',
      useCases: [
        'Ad creative testing',
        'Message resonance',
        'Campaign effectiveness',
        'Brand positioning'
      ],
      timeframe: '1-2 weeks',
      sampleSize: '100-500 participants'
    },
    {
      icon: 'support_agent',
      title: 'Customer Experience Research',
      description: 'Improve customer journeys, service quality, and satisfaction',
      useCases: [
        'Service quality assessment',
        'Customer journey mapping',
        'Support experience evaluation',
        'Loyalty and retention insights'
      ],
      timeframe: '1-3 weeks',
      sampleSize: '50-300 participants'
    }
  ];

  targetAudiences = [
    {
      category: 'Demographics',
      options: [
        'Age groups (18-25, 26-35, 36-45, 46-55, 55+)',
        'Gender preferences',
        'Income levels',
        'Education levels',
        'Employment status'
      ]
    },
    {
      category: 'Location',
      options: [
        'Specific provinces (Gauteng, Western Cape, KZN, etc.)',
        'Urban vs rural populations',
        'Major cities (Johannesburg, Cape Town, Durban)',
        'Suburban vs township demographics'
      ]
    },
    {
      category: 'Lifestyle & Interests',
      options: [
        'Shopping preferences and habits',
        'Technology adoption levels',
        'Health and wellness focus',
        'Entertainment and media consumption',
        'Financial service usage'
      ]
    },
    {
      category: 'Business Specific',
      options: [
        'Your existing customers',
        'Competitor users',
        'Industry-specific professionals',
        'Decision makers and influencers'
      ]
    }
  ];

  whyChooseUs = [
    {
      icon: 'location_on',
      title: 'South African Focus',
      description: 'Deep understanding of local market dynamics, cultural nuances, and consumer behavior unique to South Africa.',
      benefits: [
        'Local market expertise',
        'Cultural sensitivity',
        'Language options (English, Afrikaans)',
        'Understanding of SA economic context'
      ]
    },
    {
      icon: 'person_search',
      title: 'Smart Participant Matching',
      description: 'Our advanced matching system ensures you reach exactly the right demographics for your research needs.',
      benefits: [
        'Precise demographic targeting',
        'Quality participant screening',
        'Reduced survey abandonment',
        'Higher response quality'
      ]
    },
    {
      icon: 'speed',
      title: 'Fast & Flexible',
      description: 'Quick turnaround times and flexible research design to meet your business timeline and budget.',
      benefits: [
        '48-hour project setup',
        'Real-time response monitoring',
        'Flexible survey lengths',
        'Agile methodology'
      ]
    },
    {
      icon: 'analytics',
      title: 'Actionable Insights',
      description: 'We don\'t just collect data - we provide clear, actionable insights that drive business decisions.',
      benefits: [
        'Professional analysis',
        'Visual reporting',
        'Executive summaries',
        'Recommendation framework'
      ]
    }
  ];

  pricingTiers = [
    {
      name: 'Startup Package',
      price: 'From R5,000',
      description: 'Perfect for small businesses and startups testing the waters',
      features: [
        'Up to 100 responses',
        'Basic demographic targeting',
        'Standard survey design',
        'PDF report with key findings',
        '1 week turnaround',
        'Email support'
      ],
      highlight: false,
      popular: false
    },
    {
      name: 'Business Package',
      price: 'From R15,000',
      description: 'Comprehensive research for growing businesses',
      features: [
        'Up to 500 responses',
        'Advanced demographic targeting',
        'Custom survey design',
        'Interactive dashboard',
        'Professional analysis report',
        '10-14 day turnaround',
        'Phone & email support'
      ],
      highlight: true,
      popular: true
    },
    {
      name: 'Enterprise Package',
      price: 'Custom Quote',
      description: 'Large-scale research with full-service support',
      features: [
        '500+ responses',
        'Multi-segment targeting',
        'Advanced survey logic',
        'Real-time dashboard',
        'Executive presentation',
        'Dedicated project manager',
        'Priority support'
      ],
      highlight: false,
      popular: false
    }
  ];

  processSteps = [
    {
      step: 1,
      title: 'Discovery Call',
      description: 'We discuss your research objectives, target audience, and timeline',
      icon: 'call',
      duration: '30-60 minutes'
    },
    {
      step: 2,
      title: 'Proposal & Quote',
      description: 'Receive a detailed proposal with methodology, timeline, and pricing',
      icon: 'description',
      duration: '1-2 business days'
    },
    {
      step: 3,
      title: 'Survey Design',
      description: 'Our team creates and refines your survey with your input',
      icon: 'design_services',
      duration: '2-3 business days'
    },
    {
      step: 4,
      title: 'Data Collection',
      description: 'We launch the survey and monitor responses in real-time',
      icon: 'poll',
      duration: '3-14 days'
    },
    {
      step: 5,
      title: 'Analysis & Reporting',
      description: 'Professional analysis with actionable insights and recommendations',
      icon: 'analytics',
      duration: '2-5 business days'
    },
    {
      step: 6,
      title: 'Results Presentation',
      description: 'Present findings and discuss implications for your business',
      icon: 'present_to_all',
      duration: '1 hour meeting'
    }
  ];

  industriesServed = [
    { name: 'Retail & E-commerce', icon: 'shopping_cart', count: 'Multiple projects' },
    { name: 'Financial Services', icon: 'account_balance', count: 'Growing portfolio' },
    { name: 'Technology & Software', icon: 'computer', count: 'Active partnerships' },
    { name: 'Healthcare & Wellness', icon: 'local_hospital', count: 'Specialized research' },
    { name: 'Food & Beverage', icon: 'restaurant', count: 'Consumer insights' },
    { name: 'Automotive', icon: 'directions_car', count: 'Market studies' },
    { name: 'Property & Real Estate', icon: 'home', count: 'Market analysis' },
    { name: 'Education & Training', icon: 'school', count: 'Research projects' }
  ];

  caseStudyHighlights = [
    {
      industry: 'Retail Chain',
      challenge: 'Needed to understand customer satisfaction across multiple store locations',
      solution: 'Conducted comprehensive customer experience survey with 300+ responses',
      result: 'Identified key improvement areas leading to 15% increase in customer satisfaction scores',
      icon: 'store'
    },
    {
      industry: 'Tech Startup',
      challenge: 'Required product-market fit validation for new mobile app',
      solution: 'Targeted research with 200 potential users testing app concept and features',
      result: 'Refined product features based on feedback, leading to successful beta launch',
      icon: 'smartphone'
    },
    {
      industry: 'Financial Services',
      challenge: 'Needed to understand barriers to digital banking adoption',
      solution: 'In-depth research with 400 participants across different age groups',
      result: 'Developed targeted marketing strategy increasing digital adoption by 25%',
      icon: 'account_balance'
    }
  ];
}
