import { Routes } from '@angular/router';

export default [
  {
    path: '',
    title: 'Replacement Requests | EduFlow AI',
    loadComponent: () =>
      import('./pages/replacement-requests-list-page/replacement-requests-list-page').then(
        (component) => component.ReplacementRequestsListPage,
      ),
  },
  {
    path: ':id',
    title: 'Replacement Request | EduFlow AI',
    loadComponent: () =>
      import('./pages/replacement-upload-page/replacement-upload-page').then(
        (component) => component.ReplacementUploadPage,
      ),
  },
] as Routes;
