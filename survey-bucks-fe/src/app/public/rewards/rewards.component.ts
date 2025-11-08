import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-rewards',
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatTabsModule,
    RouterModule
  ],
  templateUrl: './rewards.component.html',
  styleUrl: './rewards.component.scss'
})
export class RewardsComponent {
  cashRewards = [
    {
      name: 'EFT Bank Transfer',
      description: 'Direct transfer to your South African bank account',
      value: 'R50 minimum',
      icon: 'account_balance',
      features: ['All major SA banks', '24-48 hour processing', 'No transfer fees']
    },
    {
      name: 'Capitec Pay',
      description: 'Instant payments via Capitec Pay',
      value: 'R20 minimum',
      icon: 'phone_android',
      features: ['Instant transfer', 'Mobile banking', 'Available 24/7']
    },
    {
      name: 'PayPal (if available)',
      description: 'International payment option',
      value: 'R75 minimum',
      icon: 'payment',
      features: ['Global platform', 'Quick processing', 'Currency conversion available']
    }
  ];

  futureGiftCards = [
    {
      name: 'Woolworths Vouchers',
      description: 'Premium groceries and lifestyle products',
      value: 'Coming Soon',
      icon: 'shopping_cart',
      features: ['Quality products', 'Food & fashion', 'Nationwide stores'],
      status: 'partnership_pending'
    },
    {
      name: 'Pick n Pay Vouchers',
      description: 'Groceries and household essentials',
      value: 'Coming Soon',
      icon: 'local_grocery_store',
      features: ['Everyday essentials', 'Smart Shopper points', 'Nationwide network'],
      status: 'partnership_pending'
    },
    {
      name: 'Takealot Vouchers',
      description: 'South Africa\'s largest online retailer',
      value: 'Coming Soon',
      icon: 'shopping_bag',
      features: ['Electronics & more', 'Online shopping', 'Wide product range'],
      status: 'partnership_pending'
    },
    {
      name: 'Uber/Bolt Credits',
      description: 'Transport and food delivery credits',
      value: 'Coming Soon',
      icon: 'directions_car',
      features: ['Transport credits', 'Food delivery', 'Convenient travel'],
      status: 'partnership_pending'
    }
  ];

  currentRewards = [
    {
      name: 'Platform Points',
      icon: 'stars',
      description: 'Earn points for every survey completed and platform activity',
      value: '10-100 points per survey',
      features: ['Immediate earning', 'Bonus multipliers', 'Referral bonuses']
    },
    {
      name: 'Loyalty Levels',
      icon: 'military_tech',
      description: 'Unlock higher earning potential as you complete more surveys',
      value: 'Up to 50% bonus',
      features: ['Bronze, Silver, Gold tiers', 'Exclusive survey access', 'Priority matching']
    },
    {
      name: 'Referral Rewards',
      icon: 'group_add',
      description: 'Earn bonus points when friends join and complete surveys',
      value: '50-200 points per referral',
      features: ['Unlimited referrals', 'Social sharing tools', 'Friend activity bonuses']
    },
    {
      name: 'Early Access Perks',
      icon: 'trending_up',
      description: 'Be first in line for premium surveys and new features',
      value: 'Exclusive opportunities',
      features: ['Beta testing access', 'Premium survey invites', 'Platform feedback rewards']
    }
  ];

  rewardProcess = [
    {
      icon: 'assignment_turned_in',
      title: 'Complete Surveys',
      description: 'Earn points immediately after completing matched surveys'
    },
    {
      icon: 'account_balance_wallet',
      title: 'Build Your Balance',
      description: 'Watch your points accumulate with bonuses and referrals'
    },
    {
      icon: 'redeem',
      title: 'Choose Payout Method',
      description: 'Select from available cash options or save for future rewards'
    },
    {
      icon: 'rocket_launch',
      title: 'Get Rewarded',
      description: 'Receive your earnings quickly and securely'
    }
  ];

  earningProjections = [
    {
      name: 'Getting Started',
      monthlyEarning: 'R50-150',
      description: 'Perfect for learning the platform and earning pocket money',
      surveys: '3-8 surveys per month',
      features: [
        'Profile building phase',
        'Basic survey matching',
        'Standard point rates',
        'Learning the system'
      ],
      highlight: false
    },
    {
      name: 'Regular Participant',
      monthlyEarning: 'R150-400',
      description: 'Great for covering small monthly expenses',
      surveys: '8-15 surveys per month',
      features: [
        'Better survey matching',
        'Referral opportunities',
        'Bonus point events',
        'Loyalty tier progression'
      ],
      highlight: true
    },
    {
      name: 'Active Contributor',
      monthlyEarning: 'R400+',
      description: 'For dedicated participants as our network grows',
      surveys: '15+ surveys per month',
      features: [
        'Premium survey access',
        'Maximum point multipliers',
        'Early feature access',
        'Community leadership opportunities'
      ],
      highlight: false
    }
  ];

  platformUpdates = [
    {
      title: 'Building Partnerships',
      description: 'We\'re actively negotiating with major South African retailers to bring you gift card options.',
      icon: 'handshake',
      status: 'in_progress'
    },
    {
      title: 'Mobile App Development',
      description: 'Native mobile app coming soon for easier survey participation on the go.',
      icon: 'phone_android',
      status: 'planned'
    },
    {
      title: 'Business Network Growth',
      description: 'Expanding our business partner network to increase survey frequency and variety.',
      icon: 'business',
      status: 'ongoing'
    }
  ];

  rewardsFaq = [
    {
      question: 'What rewards are currently available?',
      answer: 'Right now, we offer cash payouts via EFT to South African bank accounts and our points-based system with gamification features. We\'re working on partnerships for gift cards and other rewards.'
    },
    {
      question: 'How quickly will I receive cash payments?',
      answer: 'EFT payments are typically processed within 24-48 hours to your South African bank account. We aim to make this even faster as we grow.'
    },
    {
      question: 'What\'s the minimum payout amount?',
      answer: 'Currently, the minimum for EFT is R50. We\'re working to lower this as we establish more payout partnerships.'
    },
    {
      question: 'When will gift cards be available?',
      answer: 'We\'re in discussions with major South African retailers. Gift card options should become available as we grow our user base and establish these partnerships.'
    },
    {
      question: 'How do points convert to cash?',
      answer: 'Our points system is designed to be transparent. The exact conversion rate depends on survey complexity and your loyalty level, but we always show point values upfront.'
    },
    {
      question: 'What happens to my points if reward options change?',
      answer: 'Your earned points will always retain their value. If we add new reward options, you\'ll be able to use existing points for those rewards too.'
    }
  ];
}
