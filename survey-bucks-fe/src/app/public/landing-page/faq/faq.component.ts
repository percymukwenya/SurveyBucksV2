import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

interface FaqItem {
  question: string;
  answer: string;
  isOpen: boolean;
}

@Component({
  selector: 'app-faq',
  imports: [CommonModule],
  templateUrl: './faq.component.html',
  styleUrl: './faq.component.scss'
})
export class FaqComponent {
faqItems: FaqItem[] = [
    {
      question: 'How Do I Get Started?',
      answer: 'Simply click the "Register Now" button and fill out your profile. You\'ll start receiving survey invitations right away!',
      isOpen: true
    },
    {
      question: 'How Are Rewards Distributed?',
      answer: 'Rewards are distributed via PayPal or gift cards. You can choose your preferred method in your account settings.',
      isOpen: false
    },
    {
      question: 'How Often Will I Receive Surveys?',
      answer: 'The frequency depends on your profile and the surveys available. Most users receive 5-10 surveys per month.',
      isOpen: false
    }
  ];
  
  toggleFaq(index: number): void {
    this.faqItems[index].isOpen = !this.faqItems[index].isOpen;
  }
}
