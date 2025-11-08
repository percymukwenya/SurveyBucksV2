import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-banking-verification',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './banking-verification.component.html',
  styleUrl: './banking-verification.component.scss'
})
export class BankingVerificationComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}