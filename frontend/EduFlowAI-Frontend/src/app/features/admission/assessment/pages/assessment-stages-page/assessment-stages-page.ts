import { Component, ChangeDetectionStrategy, OnInit, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssessmentStore } from '../../data-access/assessment.store';
// Adjust the path to your shared stage-stepper component
import { StageStepper } from '../../../../../shared/ui/stage-stepper/stage-stepper';

@Component({
  selector: 'app-assessment-stages-page',
  standalone: true,
  imports: [CommonModule, StageStepper],
  templateUrl: './assessment-stages-page.html',
  styleUrl: './assessment-stages-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,      // Enforcing zoneless compatibility
})
export class AssessmentStagesPage implements OnInit {
  readonly store = inject(AssessmentStore);

  // Assuming the applicationId is passed via router params (with bindToComponentInputs enabled)
  // If it comes from a parent store in your architecture, you can adjust this to read from that store instead
  applicationId = input.required<string>();

  ngOnInit(): void {
    // Trigger the API call when the page initializes
    this.store.loadStages(this.applicationId());
  }
}
