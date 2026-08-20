import {
  AbstractControl,
  ValidationErrors,
  ValidatorFn,
} from '@angular/forms';

export const PASSWORD_MIN_LENGTH = 8;

export const strongPasswordValidator: ValidatorFn = (
  control: AbstractControl<string>,
): ValidationErrors | null => {
  const value = control.value ?? '';
  const errors: ValidationErrors = {};

  if (value.length < PASSWORD_MIN_LENGTH) {
    errors['passwordLength'] = true;
  }

  if (!/[A-Z]/.test(value)) {
    errors['passwordUppercase'] = true;
  }

  if (!/[a-z]/.test(value)) {
    errors['passwordLowercase'] = true;
  }

  if (!/\d/.test(value)) {
    errors['passwordDigit'] = true;
  }

  return Object.keys(errors).length > 0 ? errors : null;
};

export const matchingPasswordsValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmation = control.get('confirmPassword')?.value;

  return password && confirmation && password !== confirmation
    ? { passwordMismatch: true }
    : null;
};
