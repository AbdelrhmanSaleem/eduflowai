import { Component, effect, inject, signal } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ApplicationsStore } from './../../data-access/applications.store';
import { ValidationErrors } from '../../../../../shared/ui/validation-errors/validation-errors'

@Component({
  selector: 'app-application-create-page',
  imports: [DatePipe, NgClass, ValidationErrors, RouterLink],
  templateUrl: './application-create-page.html',
  styleUrl: './application-create-page.scss',
})
export class ApplicationCreatePage {
  readonly store = inject(ApplicationsStore);
  private readonly router = inject(Router);

  // Signal to hold the currently selected cycle/program ID
  selectedCycleId = signal<string | null>(null);

  constructor() {
    // 1. Fetch active programs (cycles) when the page loads
    this.store.loadActiveCycles();

    // 2. Listen for successful draft creation to navigate to the preferences page
    effect(() => {
      const details = this.store.applicationDetails();
      
      // If we have details and the status is newly set to 'Draft', navigate to the next step
      if (details && details.status === 'Draft') {
        this.router.navigate(['/applications', details.id, 'preferences']);
      }
    });
  }

  /**
   * Set the selected cycle ID when the user clicks a program card
   */
  selectCycle(cycleId: string) {
    this.selectedCycleId.set(cycleId);
  }

  /**
   * Trigger the API call to create the draft application
   */
  startApplication() {
    const cycleId = this.selectedCycleId();
    if (cycleId && !this.store.isLoading()) {
      this.store.createDraft({
        cycleId: cycleId,
        preferences: [] // Preferences are handled in the next step (Milestone 3)
      });
    }
  }
}
