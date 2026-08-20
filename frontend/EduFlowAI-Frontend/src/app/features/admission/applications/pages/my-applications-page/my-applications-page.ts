import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApplicationsStore } from '../../data-access/applications.store';
import { ApplicationListDto } from '../../models/application.model';
import { RouterLink  } from '@angular/router';

@Component({
  selector: 'app-my-applications-page',
  imports: [CommonModule, RouterLink],
  templateUrl: './my-applications-page.html',
  styleUrl: './my-applications-page.scss',
})
export class MyApplicationsPage implements OnInit {
  readonly store = inject(ApplicationsStore);

  // Local state for the withdrawal modal
  selectedAppForWithdrawal = signal<ApplicationListDto | null>(null);

  ngOnInit(): void {
    // Load initial page of applications
    this.store.loadMyApplications(this.store.listQueryParams());
  }

  /**
   * Handles navigation to a specific page
   */
  goToPage(page: number): void {
    const currentParams = this.store.listQueryParams();
    this.store.loadMyApplications({ ...currentParams, page });
  }

  /**
   * Opens the withdrawal confirmation modal
   */
  openWithdrawModal(app: ApplicationListDto): void {
    this.selectedAppForWithdrawal.set(app);
  }

  /**
   * Closes the withdrawal confirmation modal
   */
  closeWithdrawModal(): void {
    if (!this.store.isWithdrawing()) {
        this.selectedAppForWithdrawal.set(null);
    }
  }

  /**
   * Confirms withdrawal and triggers the store method
   */
  confirmWithdrawal(): void {
    const app = this.selectedAppForWithdrawal();
    if (app) {
      this.store.withdrawApplication(app.id);
      // Optional: You might want to close the modal only on success, 
      // but for simplicity, we close it immediately or wait for the store state.
      this.selectedAppForWithdrawal.set(null); 
    }
  }

  /**
   * Returns appropriate Tailwind classes based on application status
   */
  getStatusBadgeClasses(status: string): string {
    const baseClasses = 'inline-flex items-center px-2.5 py-0.5 rounded-full font-label-sm text-label-sm';
    
    switch (status) {
      case 'Draft':
        return `${baseClasses} bg-surface-container-highest text-on-surface-variant border border-outline-variant`;
      case 'DocumentsRequired':
      case 'AssessmentInProgress':
      case 'UnderDocumentVerification':
        return `${baseClasses} bg-primary-container text-on-primary shadow-sm gap-1.5`;
      case 'Admitted':
        return `${baseClasses} bg-[#D1FAE5] text-[#065F46] border border-[#059669]/20`;
      case 'NotSelected':
      case 'DocumentRejected':
      case 'EligibilityFailed':
      case 'Withdrawn':
        return `${baseClasses} bg-surface-variant text-on-surface-variant border border-outline-variant`;
      default:
        return `${baseClasses} bg-surface-container-high text-on-surface-variant border border-outline-variant`;
    }
  }
}
