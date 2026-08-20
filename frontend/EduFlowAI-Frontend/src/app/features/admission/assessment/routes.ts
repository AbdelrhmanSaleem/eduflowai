import { Routes } from '@angular/router';
import { AssessmentStore } from './data-access/assessment.store';

export default [
  {
    path: '',
    // Provide the AssessmentStore at the feature level
    // so it's available to all admin assessment operations
    providers: [AssessmentStore],
    children: [
      // Admin route for manual eligibility override
      {
        path: ':id/override',
        loadComponent: () =>
          import('./pages/eligibility-override-page/eligibility-override-page')
            .then(c => c.EligibilityOverridePage),
        title: 'Eligibility Override'
      },
      // Admin route for demo stage simulation
      {
        path: 'simulate',
        loadComponent: () =>
          import('./pages/simulate-stage-page/simulate-stage-page')
            .then(c => c.SimulateStagePage),
        title: 'Bulk Simulate Stage'
      }
      // Future Milestone 6 routes (like simulate-stage) will be added here
    ]
  }
] as Routes;