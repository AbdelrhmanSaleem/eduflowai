import { Component, OnInit, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApplicationsStore } from '../../data-access/applications.store';
import { EligibilityResultComponent } from './../../ui/eligibility-result.component/eligibility-result.component';
import { AssessmentProgressTrackingComponent } from './../../ui/assessment-progress-tracking.component/assessment-progress-tracking.component';
import { DocumentUploadManagementComponent } from '../../ui/document-upload-management.component/document-upload-management.component';

@Component({
  selector: 'app-application-status-page',
  standalone: true,
  imports: [
    CommonModule, RouterLink, EligibilityResultComponent,
    AssessmentProgressTrackingComponent, DocumentUploadManagementComponent
  ],
  templateUrl: './application-status-page.html',
  styleUrls: ['./application-status-page.scss']
})
export class ApplicationStatusPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  readonly store = inject(ApplicationsStore);

  applicationId: string = '';

  constructor() {
    // Reactively load specific details based on the current status
    effect(() => {
      const summary = this.store.summaryData();
      
      if (summary && this.applicationId) {
        const status = summary.currentStatus;

        // Load specific data based on the active phase
        if (status === 'EligibilityFailed' || status === 'DocumentsRequired') {
          this.store.loadEligibilityDetails(this.applicationId);
        } else if (status === 'AssessmentInProgress' || status === 'AssessmentCompleted') {
          this.store.loadStagesResults(this.applicationId);
        }
      }
    });
  }

  ngOnInit(): void {
    this.applicationId = this.route.snapshot.paramMap.get('id') || '';
    
    if (this.applicationId) {
      // Ensure we have the latest summary to determine the status
      this.store.loadDashboardSummary(this.applicationId);
    }
  }
}