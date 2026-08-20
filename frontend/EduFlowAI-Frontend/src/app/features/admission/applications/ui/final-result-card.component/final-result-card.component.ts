import { Component, ChangeDetectionStrategy, input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApplicationsStore } from '../../data-access/applications.store'; // Adjust path if needed

@Component({
  selector: 'app-final-result-card',
  imports: [CommonModule, RouterLink],
  templateUrl: './final-result-card.component.html',
  styleUrl: './final-result-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinalResultCardComponent {
  readonly store = inject(ApplicationsStore);
  
  // Receives the CurrentStatus and applicationId from ApplicationDashboardSummaryDto
  status = input.required<string>(); 
  applicationId = input.required<string | null>();

  // Mock data for the UI representation
  trackName = input<string>('Professional Web Development'); 
  branchName = input<string>('Alexandria'); 
  waitlistPosition = input<number>(5);

  withdrawApplication(): void {
    const id = this.applicationId();
    if (id) {
      if (confirm('Are you sure you want to withdraw your application? This action cannot be undone.')) {
        this.store.withdrawApplication(id);
      }
    }
  }
}
