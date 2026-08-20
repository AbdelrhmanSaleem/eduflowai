import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  Route,
  Router,
  UrlSegment,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';

import { RuntimeConfig } from '../config/runtime-config';
import { authGuard } from './auth-guard';
import { APP_ROLES } from './auth-session.model';
import { AuthSessionStore } from './auth-session.store';
import { guestGuard } from './guest-guard';
import { profileCompleteGuard } from './profile-complete-guard';
import { roleGuard } from './role-guard';

const apiBaseUrl = 'https://identity.example.test/api';

function startApplicantSession(store: InstanceType<typeof AuthSessionStore>): void {
  store.startSession(
    {
      accessToken: 'token',
      tokenType: 'Bearer',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      roles: [APP_ROLES.applicant],
    },
    'applicant@example.com',
  );
}

describe('authentication guards', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: RuntimeConfig, useValue: { apiBaseUrl } },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('preserves the protected return URL for anonymous users', () => {
    const router = TestBed.inject(Router);
    const result = TestBed.runInInjectionContext(() =>
      authGuard(
        {} as Route,
        [new UrlSegment('applicant', {}), new UrlSegment('profile', {})],
      ),
    ) as UrlTree;

    expect(router.serializeUrl(result)).toBe(
      '/auth/login?returnUrl=%2Fapplicant%2Fprofile',
    );
  });

  it('redirects an authenticated user away from guest pages', () => {
    startApplicantSession(TestBed.inject(AuthSessionStore));
    const router = TestBed.inject(Router);
    const result = TestBed.runInInjectionContext(() =>
      guestGuard({} as never, {} as never),
    ) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/applicant/profile');
  });

  it('redirects a user without the requested role to their own shell', () => {
    startApplicantSession(TestBed.inject(AuthSessionStore));
    const router = TestBed.inject(Router);
    const guard = roleGuard(APP_ROLES.superAdmin);
    const result = TestBed.runInInjectionContext(() =>
      guard({} as Route, [new UrlSegment('admin', {})]),
    ) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/applicant/profile');
  });

  it('loads unknown profile completeness before allowing applications', async () => {
    const store = TestBed.inject(AuthSessionStore);
    startApplicantSession(store);
    const router = TestBed.inject(Router);
    const http = TestBed.inject(HttpTestingController);
    const result = TestBed.runInInjectionContext(() =>
      profileCompleteGuard({} as Route, []),
    ) as Observable<boolean | UrlTree>;
    const resolvedResult = firstValueFrom(result);

    http
      .expectOne(`${apiBaseUrl}/profile`)
      .flush({ isComplete: false });

    expect(router.serializeUrl((await resolvedResult) as UrlTree)).toBe(
      '/applicant/profile',
    );
    expect(store.profileComplete()).toBe(false);
  });
});
