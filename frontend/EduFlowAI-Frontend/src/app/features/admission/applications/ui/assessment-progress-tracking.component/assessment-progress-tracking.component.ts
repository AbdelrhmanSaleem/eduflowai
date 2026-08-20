import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SimulatedStageDto } from '../../../applications/models/application.model';

// Interface to shape the data for our template rendering
interface StageViewInfo {
  type: string;
  title: string;
  description: string;
  data: SimulatedStageDto | undefined;
  status: 'Passed' | 'Failed' | 'InProgress' | 'Pending';
}

@Component({
  selector: 'app-assessment-progress-tracking',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './assessment-progress-tracking.component.html',
  styleUrl: './assessment-progress-tracking.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssessmentProgressTrackingComponent {
  // Receive the list of stage results from the parent page
  stages = input.required<SimulatedStageDto[]>();

  // Computed signal to dynamically build the UI state for the 4 stages
  stageViews = computed<StageViewInfo[]>(() => {
    const backendStages = this.stages() || [];

    return backendStages.map(backendData => {
      let status: 'Passed' | 'Failed' | 'InProgress' | 'Pending' = 'Pending';

      // Evaluate the status based on the exact backend string
      if (backendData.result === 'Passed') {
        status = 'Passed';
      } else if (backendData.result === 'NotPassed' || backendData.result === 'Missed') {
        status = 'Failed';
      } else if (backendData.result === 'Pending' || backendData.result === 'None') {
        status = 'InProgress';
      }

      return {
        type: backendData.stageType,
        title: backendData.title,             // Now coming directly from the backend!
        description: backendData.description, // Now coming directly from the backend!
        data: backendData,
        status: status
      };
    });
  });
}
