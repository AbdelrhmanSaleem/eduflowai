import {
  errorContains,
  errorStatus,
  safeReturnUrl,
} from './auth-error.util';

describe('auth error utilities', () => {
  it('reads normalized status codes and validation messages', () => {
    const error = {
      status: 400,
      errors: {
        DuplicateEmail: ['Email is already registered.'],
      },
    };

    expect(errorStatus(error)).toBe(400);
    expect(errorContains(error, 'duplicate')).toBe(true);
    expect(errorContains(error, 'already registered')).toBe(true);
  });

  it('allows only safe internal return URLs outside auth', () => {
    expect(safeReturnUrl('/applicant/profile?step=2')).toBe(
      '/applicant/profile?step=2',
    );
    expect(safeReturnUrl('https://malicious.example')).toBeNull();
    expect(safeReturnUrl('//malicious.example')).toBeNull();
    expect(safeReturnUrl('/auth/login')).toBeNull();
  });
});
