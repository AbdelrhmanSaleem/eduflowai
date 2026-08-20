import { Component, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApplicationsStore } from '../../data-access/applications.store';
import { StatusTimeline } from '../../../../../shared/ui/status-timeline/status-timeline';
import { FinalResultCardComponent } from '../../ui/final-result-card.component/final-result-card.component';

@Component({
  selector: 'app-applicant-dashboard-page',
  standalone: true,
  imports: [CommonModule, StatusTimeline, RouterLink, FinalResultCardComponent],
  templateUrl: './applicant-dashboard-page.html',
  styleUrl: './applicant-dashboard-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush     // Enforce OnPush change detection strategy for better performance
})
export class ApplicantDashboardPage implements OnInit {
  readonly store = inject(ApplicationsStore);

  // Inject ActivatedRoute to read parameters from the URL
  private readonly route = inject(ActivatedRoute);

  // Store the real ID to use it for initial load and retries
  currentApplicationId: string | null = null;

  ngOnInit(): void {
    // Extract the 'applicationId' parameter from the active route
    this.currentApplicationId = this.route.snapshot.paramMap.get('id');

    if (this.currentApplicationId) {
      // Fetch the data using the real application ID
      this.store.loadDashboardSummary(this.currentApplicationId);
    } else {
      // Fallback or error handling if the ID is missing from the URL
      console.error('Application ID is missing from the route parameters!');
    }
  }

  /**
   * Helper method for the UI retry button.
   */
  retryLoading(): void {
    if (this.currentApplicationId) {
      // Retry using the same real ID
      this.store.loadDashboardSummary(this.currentApplicationId);
    }
  }

  /**
   * Helper method to determine the Call to Action (CTA) details 
   * based on the application's current status.
   */
  getActionConfig(status: string) {
    if (!this.currentApplicationId) return null;
    
    // Base path for the status page
    const statusPath = ['/applications', this.currentApplicationId, 'status'];

    switch (status) {
      case 'DocumentsRequired':
      case 'UnderDocumentVerification':
        return { 
          label: 'Upload Documents', 
          icon: 'upload_file', 
          path: statusPath
        };
      case 'EligibilityPassed':
      case 'EligibilityFailed':
        return { 
          label: 'View Result Details', 
          icon: 'fact_check', 
          path: statusPath 
        };
      case 'DocumentRejected':
        return {
          label: 'View Rejection Details',
          icon: 'find_in_page',
          path: statusPath
        };
      case 'AssessmentInProgress':
      case 'AssessmentCompleted':
        return {
          label: 'Track Progress',
          icon: 'monitoring', 
          path: statusPath
        };
      case 'English/IQ Pending':
      case 'Technical Pending':
        return { 
          label: 'Take Assessment', 
          icon: 'quiz', 
          path: statusPath 
        };
      case 'Interviews Pending':
        return { 
          label: 'View Interview Details', 
          icon: 'groups', 
          path: statusPath
        };
      // Return null if the status doesn't require a direct user action
      default:
        return null; 
    }
  }

}
