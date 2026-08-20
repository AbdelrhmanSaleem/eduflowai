import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthSessionStore } from '../auth/auth-session.store';
import { RuntimeConfig } from '../config/runtime-config';
import { ApiError } from '../errors/api-problem';
import { apiErrorInterceptor } from './api-error-interceptor';
import { authInterceptor } from './auth-interceptor';
import { correlationIdInterceptor } from './correlation-id-interceptor';

@Component({ template: '' })
class EmptyTestPage {}

describe('HTTP interceptors', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let session: InstanceType<typeof AuthSessionStore>;
  let runtimeConfig: RuntimeConfig;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: 'auth/login',
            component: EmptyTestPage,
          },
        ]),
        provideHttpClient(
          withInterceptors([
            correlationIdInterceptor,
            authInterceptor,
            apiErrorInterceptor,
          ]),
        ),
        provideHttpClientTesting(),
      ],
    });

    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthSessionStore);
    runtimeConfig = TestBed.inject(RuntimeConfig);
    session.startSession(
      {
        accessToken: 'trusted-token',
        tokenType: 'Bearer',
        expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
        roles: ['Applicant'],
      },
      'applicant@example.com',
    );
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('adds authorization and correlation headers to trusted API calls', () => {
    const url = `${runtimeConfig.apiBaseUrl}/profile`;
    client.get(url).subscribe();

    const request = http.expectOne(url);
    expect(request.request.headers.get('Authorization')).toBe(
      'Bearer trusted-token',
    );
    expect(request.request.headers.get('X-Correlation-ID')).toBeTruthy();
    request.flush({});
  });

  it('does not expose credentials to an untrusted origin', () => {
    client.get('https://example.com/api/profile').subscribe();

    const request = http.expectOne('https://example.com/api/profile');
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(request.request.headers.has('X-Correlation-ID')).toBe(false);
    request.flush({});
  });

  it('does not expose credentials over plaintext to the configured host', () => {
    const configuredUrl = new URL(runtimeConfig.apiBaseUrl);
    configuredUrl.protocol = 'http:';
    const url = `${configuredUrl.toString().replace(/\/$/, '')}/profile`;
    client.get(url).subscribe();

    const request = http.expectOne(url);
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(request.request.headers.has('X-Correlation-ID')).toBe(false);
    request.flush({});
  });

  it('normalizes RFC validation problems and field keys', () => {
    let received: ApiError | null = null;

    client.put('/api/profile', {}).subscribe({
      error: (error: ApiError) => {
        received = error;
      },
    });

    http.expectOne('/api/profile').flush(
      {
        title: 'Validation failed',
        errors: {
          '$.NationalId': ['National ID is invalid.'],
        },
        traceId: 'trace-1',
      },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(received?.status).toBe(400);
    expect(received?.errors['nationalid']).toEqual([
      'National ID is invalid.',
    ]);
    expect(received?.traceId).toBe('trace-1');
  });

  it('clears an active session after a protected endpoint returns 401', () => {
    client.get('/api/profile').subscribe({ error: () => undefined });

    http.expectOne('/api/profile').flush(
      { title: 'Authentication required' },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(session.isAuthenticated()).toBe(false);
  });

  it('clears and redirects an expired session before a protected request', () => {
    session.startSession(
      {
        accessToken: 'expired-token',
        tokenType: 'Bearer',
        expiresAtUtc: new Date(Date.now() - 60_000).toISOString(),
        roles: ['Applicant'],
      },
      'applicant@example.com',
    );

    client.get('/api/profile').subscribe({ error: () => undefined });

    const request = http.expectOne('/api/profile');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(
      { title: 'Authentication required' },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(session.session()).toBeNull();
  });
});
