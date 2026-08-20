import { Routes } from '@angular/router';

export default [
  {
    path: '',
    title: 'Document Review Queue | EduFlow AI',
    loadComponent: () =>
      import('./pages/review-queue-page/review-queue-page').then(
        (component) => component.ReviewQueuePage,
      ),
  },
  {
    path: ':documentId',
    title: 'Document Review | EduFlow AI',
    loadComponent: () =>
      import('./pages/review-details-page/review-details-page').then(
        (component) => component.ReviewDetailsPage,
      ),
  },
] satisfies Routes;
