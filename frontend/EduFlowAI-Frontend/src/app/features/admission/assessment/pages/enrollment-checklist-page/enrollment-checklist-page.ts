import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ApplicationsStore } from '../../../applications/data-access/applications.store';
@Component({
  selector: 'app-enrollment-checklist-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './enrollment-checklist-page.html',
  styleUrl: './enrollment-checklist-page.scss',
  // Enforcing zoneless architecture for optimal performance
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentChecklistPage implements OnInit {
  readonly store = inject(ApplicationsStore);
  private readonly route = inject(ActivatedRoute);
  
  currentApplicationId: string | null = null;

  ngOnInit(): void {
    // Extract the application ID from the route parameters
    this.currentApplicationId = this.route.snapshot.paramMap.get('id');

    if (this.currentApplicationId) {
      // Trigger the store to fetch the checklist data
      this.store.loadEnrollmentChecklist(this.currentApplicationId);
    } else {
      console.error('Application ID is missing from the route parameters!');
    }
  }

  // Helper method to calculate progress percentage safely
  getProgressPercentage(completed: number, total: number): number {
    if (!total || total === 0) return 0;
    return Math.round((completed / total) * 100);
  }
}
