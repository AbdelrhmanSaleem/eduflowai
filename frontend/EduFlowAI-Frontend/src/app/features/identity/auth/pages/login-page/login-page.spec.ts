import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { LoginPage } from './login-page';

describe('LoginPage', () => {
  it('creates an invalid form until credentials are supplied', async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    expect(fixture.componentInstance.form.invalid).toBe(true);

    fixture.componentInstance.form.setValue({
      email: 'applicant@example.com',
      password: 'Password1',
    });

    expect(fixture.componentInstance.form.valid).toBe(true);
  });
});
