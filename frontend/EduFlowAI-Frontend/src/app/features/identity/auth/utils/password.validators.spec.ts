import { FormControl, FormGroup } from '@angular/forms';

import {
  matchingPasswordsValidator,
  strongPasswordValidator,
} from './password.validators';

describe('password validators', () => {
  it('requires length, uppercase, lowercase, and a number', () => {
    const weak = new FormControl('password');
    const strong = new FormControl('Password1');

    expect(strongPasswordValidator(weak)).toEqual({
      passwordUppercase: true,
      passwordDigit: true,
    });
    expect(strongPasswordValidator(strong)).toBeNull();
  });

  it('detects mismatched confirmation values', () => {
    const form = new FormGroup({
      password: new FormControl('Password1'),
      confirmPassword: new FormControl('Different1'),
    });

    expect(matchingPasswordsValidator(form)).toEqual({
      passwordMismatch: true,
    });

    form.controls.confirmPassword.setValue('Password1');
    expect(matchingPasswordsValidator(form)).toBeNull();
  });
});
