import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EligibilityDetailsDto } from '../../models/application.model';

@Component({
  selector: 'app-eligibility-result',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './eligibility-result.component.html',
  styleUrls: ['./eligibility-result.component.scss']
})
export class EligibilityResultComponent {
  // The details object coming from the smart component
  details = input.required<EligibilityDetailsDto | null>();
  // To use it for the return button routing
  applicationId = input.required<string>(); 
}