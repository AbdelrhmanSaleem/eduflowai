import { Routes } from '@angular/router';

export const TRACK_CATALOG_ROUTES: Routes = [
  {
    path: '',
    title: 'Training tracks | ITI Admissions',
    loadComponent: () =>
      import('./pages/track-catalog-page/track-catalog-page').then(
        (component) => component.TrackCatalogPage,
      ),
  },
  {
    path: ':trackId',
    title: 'Track details | ITI Admissions',
    loadComponent: () =>
      import('./pages/track-details-page/track-details-page').then(
        (component) => component.TrackDetailsPage,
      ),
  },
];

export default TRACK_CATALOG_ROUTES;
