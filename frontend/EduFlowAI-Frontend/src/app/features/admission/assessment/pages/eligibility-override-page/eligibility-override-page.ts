import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AssessmentStore } from '../../data-access/assessment.store';
import { ApplicationsStore } from '../../../applications/data-access/applications.store';

@Component({
  selector: 'app-eligibility-override-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './eligibility-override-page.html',
  styleUrl: './eligibility-override-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EligibilityOverridePage implements OnInit {
  readonly assessmentStore = inject(AssessmentStore);
  readonly applicationsStore = inject(ApplicationsStore);
  
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  applicationId: string | null = null;
  
  // Signal to hold the textarea input
  justification = signal<string>('');

  ngOnInit(): void {
    // Read the application ID from the route (e.g. /admin/applications/:id/override)
    this.applicationId = this.route.snapshot.paramMap.get('id');
    
    if (this.applicationId) {
      // Trigger loading of both application general details and the specific eligibility failure reasons
      this.applicationsStore.loadApplicationDetails(this.applicationId);
      this.applicationsStore.loadEligibilityDetails(this.applicationId);
    }
  }

  /**
   * Dispatches the override request to the store if validations pass.
   */
  submitOverride(): void {
    if (this.applicationId && this.justification().length >= 50) {
      this.assessmentStore.submitEligibilityOverride({
        applicationId: this.applicationId,
        reason: this.justification()
      });
    }
  }

  /**
   * Resets the store state and navigates away from the modal.
   */
  closeModal(): void {
    this.assessmentStore.resetOverrideState();
    
    // Fallback routing: navigate up one level, or to a specific admin dashboard
    // Adjust this path based on your admin routing structure
    this.router.navigate(['../'], { relativeTo: this.route });
  }

}
