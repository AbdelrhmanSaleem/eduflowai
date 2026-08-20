import {
  AbstractControl,
  ValidationErrors,
  ValidatorFn,
} from '@angular/forms';

const LETTER_PATTERN = /^\p{Letter}$/u;
const MARK_PATTERN = /^\p{Mark}$/u;
const LATIN_SCRIPT_PATTERN = /^\p{Script=Latin}$/u;
const ARABIC_SCRIPT_PATTERN = /^\p{Script=Arabic}$/u;
const NAME_SEPARATORS = new Set([
  ' ',
  "'",
  '-',
  '.',
  '\u02bc',
  '\u2010',
  '\u2011',
  '\u2019',
]);

export function notBlankValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null =>
    String(control.value ?? '').trim().length > 0
      ? null
      : { required: true };
}

export function englishNameValidator(): ValidatorFn {
  return nameScriptValidator(LATIN_SCRIPT_PATTERN, 'englishName');
}

export function arabicNameValidator(): ValidatorFn {
  return nameScriptValidator(ARABIC_SCRIPT_PATTERN, 'arabicName');
}

export function pastDateValidator(today = new Date()): ValidatorFn {
  const maximum = new Date(
    Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate()),
  );

  return (control: AbstractControl): ValidationErrors | null => {
    const value = String(control.value ?? '');

    if (!value) {
      return null;
    }

    const parsed = new Date(`${value}T00:00:00Z`);
    return Number.isFinite(parsed.getTime()) && parsed < maximum
      ? null
      : { pastDate: true };
  };
}

export function graduationYearValidator(
  currentYear = new Date().getUTCFullYear(),
): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const year = control.value as number | null;

    if (year === null) {
      return null;
    }

    return Number.isInteger(year) && year >= 1900 && year <= currentYear
      ? null
      : { graduationYear: { min: 1900, max: currentYear } };
  };
}

function nameScriptValidator(
  expectedScript: RegExp,
  errorKey: 'englishName' | 'arabicName',
): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = String(control.value ?? '').trim();

    if (!value) {
      return null;
    }

    return isValidNameForScript(value, expectedScript)
      ? null
      : { [errorKey]: true };
  };
}

function isValidNameForScript(value: string, expectedScript: RegExp): boolean {
  let hasLetter = false;
  let canAcceptMark = false;

  for (const character of value) {
    if (NAME_SEPARATORS.has(character)) {
      canAcceptMark = false;
      continue;
    }

    if (LETTER_PATTERN.test(character)) {
      if (!expectedScript.test(character)) {
        return false;
      }

      hasLetter = true;
      canAcceptMark = true;
      continue;
    }

    if (MARK_PATTERN.test(character)) {
      if (!canAcceptMark) {
        return false;
      }

      continue;
    }

    return false;
  }

  return hasLetter;
}
