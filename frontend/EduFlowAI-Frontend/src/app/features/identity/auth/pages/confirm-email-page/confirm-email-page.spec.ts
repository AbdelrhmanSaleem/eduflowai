import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { throwError } from 'rxjs';

import { AuthApiService } from '../../data-access/auth-api.service';
import { ConfirmEmailPage } from './confirm-email-page';

describe('ConfirmEmailPage', () => {
  it('shows an invalid-link state when query parameters are missing', async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmEmailPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ConfirmEmailPage);
    fixture.detectChanges();

    expect(fixture.componentInstance.state()).toBe('invalid-link');
  });

  it('keeps transient server failures retryable', async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmEmailPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({
                email: 'applicant@example.com',
                token: 'confirmation-token',
              }),
            },
          },
        },
        {
          provide: AuthApiService,
          useValue: {
            confirmEmail: () =>
              throwError(() => ({
                status: 500,
                title: 'Unexpected server error',
              })),
          },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ConfirmEmailPage);
    fixture.detectChanges();

    expect(fixture.componentInstance.state()).toBe('error');
    expect(fixture.componentInstance.canRetry()).toBe(true);
    expect(fixture.componentInstance.errorKey()).toBe(
      'confirmationTechnicalFailure',
    );
  });
});
