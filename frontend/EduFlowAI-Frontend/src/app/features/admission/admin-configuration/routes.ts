import { Routes } from '@angular/router';

export const ADMISSION_ADMIN_ROUTES: Routes = [
  {
    path: '',
    title: 'Admission overview | EduFlow AI',
    loadComponent: () =>
      import('./admission-overview/admission-overview').then(
        (component) => component.AdmissionOverview,
      ),
  },
  {
    path: 'institutions',
    title: 'Institutions | EduFlow AI',
    loadComponent: () =>
      import('./institution-management/institution-management-page').then(
        (component) => component.InstitutionManagementPage,
      ),
  },
  {
    path: 'programs',
    title: 'Programs | EduFlow AI',
    loadComponent: () =>
      import('./program-management/program-management-page').then(
        (component) => component.ProgramManagementPage,
      ),
  },
  {
    path: 'document-requirements',
    title: 'Document Requirements | EduFlow AI',
    loadComponent: () =>
      import('./document-requirements/document-requirements-page').then(
        (component) => component.DocumentRequirementsPage,
      ),
  },
  {
    path: 'tracks',
    title: 'Tracks | EduFlow AI',
    loadComponent: () =>
      import('./track-management/track-management-page').then(
        (component) => component.TrackManagementPage,
      ),
  },
  {
    path: 'branches',
    title: 'Branches | EduFlow AI',
    loadComponent: () =>
      import('./branch-management/branch-management-page').then(
        (component) => component.BranchManagementPage,
      ),
  },
  {
    path: 'catalog',
    redirectTo: 'tracks',
    pathMatch: 'full',
  },
  {
    path: 'cycles',
    title: 'Admission cycles | EduFlow AI',
    loadComponent: () =>
      import('./cycle-management/cycle-management-page').then(
        (component) => component.CycleManagementPage,
      ),
  },
  // Load the assessment routes lazily when the URL contains 'assessment'
  {
    path: 'assessment',
    title: 'Assessment & Operations',
    loadChildren: () =>
      import('../assessment/routes').then((m) => m.default),
  }
];

export default ADMISSION_ADMIN_ROUTES;
