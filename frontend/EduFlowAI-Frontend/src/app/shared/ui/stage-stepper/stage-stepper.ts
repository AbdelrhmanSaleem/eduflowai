import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

// Define the possible statuses for any assessment stage
export type AssessmentStageStatus = 'Pending' | 'Passed' | 'NotPassed' | 'InProgress';

// Define the shape of a single step to use in our HTML loop
interface StepperItem {
  id: string;
  name: string;
  status: AssessmentStageStatus;
}

@Component({
  selector: 'app-stage-stepper',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stage-stepper.html',
  styleUrl: './stage-stepper.scss',
})

export class StageStepper {
  // We need an input for each exam stage so the parent component can control them individually
  englishStatus = input.required<AssessmentStageStatus>();
  iqStatus = input.required<AssessmentStageStatus>();
  technicalStatus = input.required<AssessmentStageStatus>();
  interviewStatus = input.required<AssessmentStageStatus>();

  // A computed signal that builds the array automatically whenever any input changes
  steps = computed<StepperItem[]>(() => [
    { id: 'en', name: 'English Exam', status: this.englishStatus() },
    { id: 'iq', name: 'IQ Exam', status: this.iqStatus() },
    { id: 'tech', name: 'Technical Exam', status: this.technicalStatus() },
    { id: 'hr', name: 'HR Interview', status: this.interviewStatus() }
  ]);
}
