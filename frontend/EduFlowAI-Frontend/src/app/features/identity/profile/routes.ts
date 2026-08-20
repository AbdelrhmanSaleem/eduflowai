import { Routes } from '@angular/router';

import { ProfileStore } from './data-access/profile.store';

export const PROFILE_ROUTES: Routes = [
  {
    path: '',
    providers: [ProfileStore],
    loadComponent: () =>
      import('./pages/profile-page/profile-page').then(
        (component) => component.ProfilePage,
      ),
  },
];

export default PROFILE_ROUTES;
