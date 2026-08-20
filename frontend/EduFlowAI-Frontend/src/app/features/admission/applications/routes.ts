import { Routes } from '@angular/router';
import { ApplicationsStore } from './data-access/applications.store';

export default [
  {
    path: '',
    // Here we provide the Store. It will be available to all children routes,
    // but destroyed when the user leaves the applications feature!
    providers: [ApplicationsStore],
    children: [
      // Route for starting a new application
      {
        path: 'create',
        loadComponent: () =>
          import('./pages/application-create-page/application-create-page')
            .then(c => c.ApplicationCreatePage),
        title: 'Start Application'
      },
      // Route for viewing my applications
      {
        path: '',
        loadComponent: () =>
          import('./pages/my-applications-page/my-applications-page')
            .then(c => c.MyApplicationsPage),
        title: 'My Applications'
      },
      // Route for selecting preferences
      {
        path: ':id/preferences',
        loadComponent: () =>
          import('./pages/preference-selection-page/preference-selection-page')
            .then(c => c.PreferenceSelectionPage),
          title: 'Select Preferences'
        },
        // Route for editing an existing draft application
        {
          path: ':id/edit',
          loadComponent: () => 
            import('./pages/preference-selection-page/preference-selection-page')
            .then(c => c.PreferenceSelectionPage),
          title: 'Edit Application'
        },
        // Route for the applicant-documents feature (owned by Mansy — features/documents/applicant-documents/)
        {
          path: ':id/documents',
          loadChildren: () =>
            import('../../documents/applicant-documents/routes')
              .then(r => r.APPLICANT_DOCUMENTS_ROUTES),
          title: 'Document Uploads'
        },
        // Detailed status and document review page route
        {
          path: ':id/status',
          loadComponent: () => import('./pages/application-status-page/application-status-page')
            .then(c => c.ApplicationStatusPage),
          title: 'Application Status Details'
        },
        // Route for editing an existing draft application
        // {
        //   path: ':id/edit',
        //   loadComponent: () => 
        //     import('./pages/application-edit-page/application-edit-page')
        //     .then(c => c.ApplicationEditPage),
        //   title: 'Edit Application'
        // },
        // Route for the enrollment cheklist page of an admitted application
        {
          path: ':id/enrollment',
          loadComponent: () => 
            import('./../assessment/pages/enrollment-checklist-page/enrollment-checklist-page')
            .then(c => c.EnrollmentChecklistPage),
          title: 'Enrollment Checklist'
        },
        // The dynamic ':id' route MUST be at the very bottom
        {
          path: ':id',  // Add ':' to make it a dynamic route parameter
          // Lazy loading the standalone component
          loadComponent: () => import('./pages/applicant-dashboard-page/applicant-dashboard-page')
            .then(c => c.ApplicantDashboardPage),
          title: 'Application Dashboard'
        },
        // Other routes owned by Halim (like application-create-page) will go here
      ]
    }    
] as Routes;