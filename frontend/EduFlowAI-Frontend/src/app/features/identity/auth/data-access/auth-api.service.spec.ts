import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { RuntimeConfig } from '../../../../core/config/runtime-config';
import { AuthApiService } from './auth-api.service';

const apiBaseUrl = 'https://identity.example.test/api';

describe('AuthApiService', () => {
  let api: AuthApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthApiService,
        { provide: RuntimeConfig, useValue: { apiBaseUrl } },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    api = TestBed.inject(AuthApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts the login wire shape', () => {
    const response = {
      accessToken: 'token',
      tokenType: 'Bearer',
      expiresAtUtc: '2030-01-01T00:00:00Z',
      roles: ['Applicant'],
    };

    api
      .login({ email: 'user@example.com', password: 'Password1' })
      .subscribe((result) => expect(result).toEqual(response));

    const request = http.expectOne(`${apiBaseUrl}/auth/login`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'user@example.com',
      password: 'Password1',
    });
    request.flush(response);
  });

  it('posts the registration language and accepts a development token', () => {
    const response = {
      userId: 'user-id',
      email: 'user@example.com',
      requiresEmailConfirmation: true,
      developmentConfirmationToken: 'confirmation-token',
    };

    api
      .register({
        email: 'user@example.com',
        password: 'Password1',
        preferredLanguage: 'ar',
      })
      .subscribe((result) => expect(result).toEqual(response));

    const request = http.expectOne(`${apiBaseUrl}/auth/register`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'user@example.com',
      password: 'Password1',
      preferredLanguage: 'ar',
    });
    request.flush(response);
  });

  it('posts confirmation, recovery, and reset requests to their contracts', () => {
    api
      .confirmEmail({ email: 'user@example.com', token: 'confirm' })
      .subscribe();
    const confirmation = http.expectOne(`${apiBaseUrl}/auth/confirm-email`);
    expect(confirmation.request.body).toEqual({
      email: 'user@example.com',
      token: 'confirm',
    });
    confirmation.flush(null, { status: 204, statusText: 'No Content' });

    api.forgotPassword({ email: 'user@example.com' }).subscribe();
    const recovery = http.expectOne(`${apiBaseUrl}/auth/forgot-password`);
    expect(recovery.request.body).toEqual({ email: 'user@example.com' });
    recovery.flush({ message: 'If an account exists, an email was sent.' });

    api
      .resetPassword({
        email: 'user@example.com',
        token: 'reset',
        newPassword: 'NewPassword1',
      })
      .subscribe();
    const reset = http.expectOne(`${apiBaseUrl}/auth/reset-password`);
    expect(reset.request.body).toEqual({
      email: 'user@example.com',
      token: 'reset',
      newPassword: 'NewPassword1',
    });
    reset.flush(null, { status: 204, statusText: 'No Content' });
  });
});
