import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { RegisterPage } from './register-page';

describe('RegisterPage', () => {
  it('accepts only matching passwords that meet the backend policy', async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    const component = TestBed.createComponent(RegisterPage).componentInstance;
    component.form.setValue({
      email: 'applicant@example.com',
      password: 'Password1',
      confirmPassword: 'Password1',
    });

    expect(component.form.valid).toBe(true);

    component.form.controls.confirmPassword.setValue('Different1');
    expect(component.form.hasError('passwordMismatch')).toBe(true);
  });
});
