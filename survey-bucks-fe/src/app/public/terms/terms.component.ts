import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';

interface LegalSubsection {
  subtitle?: string;
  text: string;
  list?: string[];
}

interface LegalSection {
  title: string;
  content: LegalSubsection[];
}

@Component({
  selector: 'app-terms',
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    RouterModule
  ],
  templateUrl: './terms.component.html',
  styleUrl: './terms.component.scss'
})

export class TermsComponent {
  termsSections: LegalSection[] = [
    {
      title: '1. Acceptance of Terms',
      content: [
        {
          text: 'By accessing and using SurveyBucks, you accept and agree to be bound by the terms and provision of this agreement. If you do not agree to abide by the above, please do not use this service.'
        }
      ]
    },
    {
      title: '2. Eligibility',
      content: [
        {
          text: 'To use SurveyBucks, you must:',
          list: [
            'Be at least 18 years old',
            'Provide accurate and truthful information',
            'Have legal capacity to enter into this agreement',
            'Not be prohibited from using our services under applicable law'
          ]
        }
      ]
    },
    {
      title: '3. Account Registration',
      content: [
        {
          subtitle: 'Account Creation',
          text: 'You must create an account to use our services. You are responsible for maintaining the confidentiality of your account and password.'
        },
        {
          subtitle: 'Account Information',
          text: 'You agree to provide accurate, current, and complete information during registration and to update such information to keep it accurate, current, and complete.'
        }
      ]
    },
    {
      title: '4. Survey Participation',
      content: [
        {
          subtitle: 'Honest Responses',
          text: 'You agree to provide honest, thoughtful, and accurate responses to all survey questions. Fraudulent or misleading responses may result in account termination.'
        },
        {
          subtitle: 'Survey Availability',
          text: 'Survey availability depends on various factors including demographics, location, and current research needs. We do not guarantee survey availability.'
        }
      ]
    },
    {
      title: '5. Rewards and Payments',
      content: [
        {
          subtitle: 'Earning Rewards',
          text: 'Rewards are earned upon successful completion of surveys. Incomplete surveys or surveys where you do not qualify may receive partial compensation as specified.'
        },
        {
          subtitle: 'Payment Processing',
          text: 'Payments are processed according to our published schedule. We reserve the right to verify account information before processing payments.'
        },
        {
          subtitle: 'Tax Responsibility',
          text: 'You are responsible for any applicable taxes on rewards received. We may be required to report payments to tax authorities.'
        }
      ]
    },
    {
      title: '6. Prohibited Activities',
      content: [
        {
          text: 'You agree not to:',
          list: [
            'Create multiple accounts',
            'Use automated tools or bots to complete surveys',
            'Share your account credentials with others',
            'Provide false or misleading information',
            'Attempt to manipulate the reward system',
            'Violate any applicable laws or regulations'
          ]
        }
      ]
    },
    {
      title: '7. Intellectual Property',
      content: [
        {
          text: 'All content on SurveyBucks, including text, graphics, logos, and software, is our property or the property of our licensors and is protected by copyright and other intellectual property laws.'
        }
      ]
    },
    {
      title: '8. Privacy and Data Protection',
      content: [
        {
          text: 'Your privacy is important to us. Please review our Privacy Policy, which also governs your use of the services, to understand our practices.'
        }
      ]
    },
    {
      title: '9. Account Termination',
      content: [
        {
          subtitle: 'Termination by You',
          text: 'You may terminate your account at any time by contacting our support team. Upon termination, you forfeit any unredeemed rewards.'
        },
        {
          subtitle: 'Termination by Us',
          text: 'We may terminate or suspend your account immediately, without prior notice, for conduct that we believe violates these Terms or is harmful to other users or our business.'
        }
      ]
    },
    {
      title: '10. Disclaimers and Limitation of Liability',
      content: [
        {
          subtitle: 'Service Availability',
          text: 'We provide our services "as is" and "as available." We do not guarantee uninterrupted or error-free operation of our services.'
        },
        {
          subtitle: 'Limitation of Liability',
          text: 'To the fullest extent permitted by law, SurveyBucks shall not be liable for any indirect, incidental, special, consequential, or punitive damages.'
        }
      ]
    },
    {
      title: '11. Changes to Terms',
      content: [
        {
          text: 'We reserve the right to modify these Terms at any time. Changes will be effective when posted on our website. Continued use of our services constitutes acceptance of the modified Terms.'
        }
      ]
    },
    {
      title: '12. Governing Law',
      content: [
        {
          text: 'These Terms shall be governed by and construed in accordance with the laws of the State of California, without regard to its conflict of law provisions.'
        }
      ]
    }
  ];
}
