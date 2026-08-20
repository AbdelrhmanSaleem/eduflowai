import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AssessmentStore } from '../../data-access/assessment.store';
import { SelectionStage } from '../../../applications/models/application.model';
import { ValidationErrors } from '../../../../../shared/ui/validation-errors/validation-errors';

@Component({
  selector: 'app-simulate-stage-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ValidationErrors],
  templateUrl: './simulate-stage-page.html',
  styleUrl: './simulate-stage-page.scss',
  // Enforce optimized change detection
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SimulateStagePage implements OnInit {
  readonly store = inject(AssessmentStore);
  private readonly route = inject(ActivatedRoute);

  // Form state managed via Signals
  cycleId = signal<string>('');
  selectedStage = signal<SelectionStage>(SelectionStage.None);
  // Expose the Enum to the template so we can use its values in the select options
  SelectionStage = SelectionStage;

  // Track ALL completed stages using a Set to avoid duplicates
  completedStages = signal<Set<SelectionStage>>(new Set<SelectionStage>());

  // Computed signal to cleanly evaluate if the allocation card should appear
  canRunAllocation = computed(() => {
    const hasSuccessMsg = this.store.bulkSimulateSuccessMessage() !== null;
    const completedCount = this.store.completedStages().length;
    
    // Allocation button unlocks ONLY if all 4 stages are successfully completed in the backend
    return hasSuccessMsg && completedCount === 4;
  });

  ngOnInit(): void {
    // Auto-fill cycleId from query params (e.g., ?cycleId=123...) when navigating from the Dashboard
    const idFromQuery = this.route.snapshot.queryParamMap.get('cycleId');
    if (idFromQuery) {
      this.cycleId.set(idFromQuery);
    }
  }

  /**
   * Validates the form and dispatches the bulk simulation action to the Store.
   */
  submitBulkSimulation(): void {
    const id = this.cycleId().trim();
    const stage = Number(this.selectedStage());

    if (id && stage !== SelectionStage.None) {
      // We just dispatch the action. The Store will handle tracking it IF it succeeds.
      this.store.submitBulkSimulateStage({
        cycleId: id,
        stage: stage
      });
    }
  }

  /**
   * Admitt the applicants according to the total score of their assessments.
   */
  runAllocationEngine(): void {
    const id = this.cycleId().trim();
    if (id) {
      this.store.submitRunAllocation(id);
    }
  }

  /**
   * Resets the form inputs and clears any success/error states from the Store.
   */
  resetForm(): void {
    this.selectedStage.set(SelectionStage.None);
    this.store.resetBulkSimulateState();
  }
}
