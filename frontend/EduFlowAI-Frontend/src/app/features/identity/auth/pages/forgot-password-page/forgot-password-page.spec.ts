import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ForgotPasswordPage } from './forgot-password-page';

describe('ForgotPasswordPage', () => {
  it('requires a valid email address', async () => {
    await TestBed.configureTestingModule({
      imports: [ForgotPasswordPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    const component = TestBed.createComponent(
      ForgotPasswordPage,
    ).componentInstance;
    component.form.controls.email.setValue('not-an-email');

    expect(component.form.invalid).toBe(true);

    component.form.controls.email.setValue('applicant@example.com');
    expect(component.form.valid).toBe(true);
  });
});
