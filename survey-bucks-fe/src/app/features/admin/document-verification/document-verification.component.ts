import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-document-verification',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './document-verification.component.html',
  styleUrl: './document-verification.component.scss'
})
export class DocumentVerificationComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}