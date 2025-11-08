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
  selector: 'app-privacy',
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    RouterModule
  ],
  templateUrl: './privacy.component.html',
  styleUrl: './privacy.component.scss'
})
export class PrivacyComponent {
  privacySections : LegalSection[] = [
    {
      title: '1. Information We Collect',
      content: [
        {
          subtitle: 'Personal Information',
          text: 'We collect information you provide directly to us, such as:',
          list: [
            'Name and contact information',
            'Demographic information (age, gender, location)',
            'Account credentials',
            'Payment information for reward processing',
            'Communication preferences'
          ]
        },
        {
          subtitle: 'Survey Responses',
          text: 'We collect your responses to surveys, including opinions, preferences, and feedback on various topics.'
        },
        {
          subtitle: 'Usage Information',
          text: 'We automatically collect information about how you use our services, including:',
          list: [
            'Device information and IP address',
            'Browser type and version',
            'Pages visited and time spent',
            'Survey completion rates and patterns'
          ]
        }
      ]
    },
    {
      title: '2. How We Use Your Information',
      content: [
        {
          text: 'We use your information to:',
          list: [
            'Provide and improve our services',
            'Match you with relevant surveys',
            'Process reward payments',
            'Communicate with you about your account',
            'Ensure the security and integrity of our platform',
            'Comply with legal obligations',
            'Conduct research and analytics'
          ]
        }
      ]
    },
    {
      title: '3. Information Sharing',
      content: [
        {
          subtitle: 'Survey Sponsors',
          text: 'We share anonymized survey responses with research sponsors. Your personal information is never included in these reports.'
        },
        {
          subtitle: 'Service Providers',
          text: 'We may share information with trusted third-party service providers who help us operate our platform, such as payment processors and email service providers.'
        },
        {
          subtitle: 'Legal Requirements',
          text: 'We may disclose information when required by law or to protect our rights, property, or safety.'
        },
        {
          subtitle: 'Business Transfers',
          text: 'In the event of a merger, acquisition, or sale of assets, your information may be transferred to the new entity.'
        }
      ]
    },
    {
      title: '4. Data Security',
      content: [
        {
          text: 'We implement appropriate security measures to protect your information:',
          list: [
            'Encryption of data in transit and at rest',
            'Regular security audits and assessments',
            'Access controls and authentication measures',
            'Secure data centers and infrastructure',
            'Employee training on data protection'
          ]
        }
      ]
    },
    {
      title: '5. Your Rights and Choices',
      content: [
        {
          subtitle: 'Access and Correction',
          text: 'You can access and update your personal information through your account settings or by contacting us.'
        },
        {
          subtitle: 'Data Deletion',
          text: 'You can request deletion of your personal information, subject to certain legal and operational requirements.'
        },
        {
          subtitle: 'Communication Preferences',
          text: 'You can opt out of marketing communications at any time through your account settings or unsubscribe links.'
        },
        {
          subtitle: 'Cookie Preferences',
          text: 'You can control cookie settings through your browser preferences.'
        }
      ]
    },
    {
      title: '6. International Data Transfers',
      content: [
        {
          text: 'Your information may be transferred to and processed in countries other than your country of residence. We ensure appropriate safeguards are in place for such transfers.'
        }
      ]
    },
    {
      title: '7. Data Retention',
      content: [
        {
          text: 'We retain your information for as long as necessary to provide our services and comply with legal obligations. Survey responses may be retained for research purposes in anonymized form.'
        }
      ]
    },
    {
      title: '8. Children\'s Privacy',
      content: [
        {
          text: 'Our services are not intended for individuals under 18 years of age. We do not knowingly collect personal information from children under 18.'
        }
      ]
    },
    {
      title: '9. Cookies and Tracking',
      content: [
        {
          subtitle: 'Cookies',
          text: 'We use cookies and similar technologies to enhance your experience, analyze usage, and provide personalized content.'
        },
        {
          subtitle: 'Analytics',
          text: 'We use analytics tools to understand how our services are used and to improve user experience.'
        },
        {
          subtitle: 'Advertising',
          text: 'We may use advertising networks that collect information about your browsing activities for targeted advertising.'
        }
      ]
    },
    {
      title: '10. Third-Party Links',
      content: [
        {
          text: 'Our services may contain links to third-party websites. We are not responsible for the privacy practices of these external sites.'
        }
      ]
    },
    {
      title: '11. California Privacy Rights',
      content: [
        {
          text: 'California residents have additional rights under the California Consumer Privacy Act (CCPA):',
          list: [
            'Right to know what personal information is collected',
            'Right to delete personal information',
            'Right to opt-out of the sale of personal information',
            'Right to non-discrimination for exercising privacy rights'
          ]
        }
      ]
    },
    {
      title: '12. European Privacy Rights',
      content: [
        {
          text: 'If you are in the European Economic Area, you have additional rights under GDPR:',
          list: [
            'Right of access to your personal data',
            'Right to rectification of inaccurate data',
            'Right to erasure of your data',
            'Right to restrict processing',
            'Right to data portability',
            'Right to object to processing'
          ]
        }
      ]
    },
    {
      title: '13. Changes to This Policy',
      content: [
        {
          text: 'We may update this Privacy Policy from time to time. We will notify you of any material changes by posting the new policy on our website and updating the "Last updated" date.'
        }
      ]
    }
  ];
}
