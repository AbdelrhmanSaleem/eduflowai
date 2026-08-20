import { Routes } from '@angular/router';

export const APPLICANT_DOCUMENTS_ROUTES: Routes = [
    {
        path: '',
        loadComponent: () =>
            import('./pages/required-documents-page/required-documents-page').then(
                (m) => m.RequiredDocumentsPage,
            ),
    },
];