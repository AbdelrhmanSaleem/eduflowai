import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ResetPasswordPage } from './reset-password-page';

describe('ResetPasswordPage', () => {
  it('rejects a reset page without email and token query parameters', async () => {
    await TestBed.configureTestingModule({
      imports: [ResetPasswordPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    const component = TestBed.createComponent(
      ResetPasswordPage,
    ).componentInstance;

    expect(component.state()).toBe('invalid-link');
  });
});
