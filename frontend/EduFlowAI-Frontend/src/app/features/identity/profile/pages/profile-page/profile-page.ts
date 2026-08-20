import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';

import { LocaleStore } from '../../../../../core/i18n/locale.store';
import { ProfileStore } from '../../data-access/profile.store';
import {
  DEGREE_LEVELS,
  EGYPTIAN_UNIVERSITIES,
  FACULTIES,
  LocalizedAcademicOption,
  MAJORS,
} from '../../models/profile-academic-options';
import {
  ApplicantProfile,
  ApplicantProfileFormValue,
  CUMULATIVE_GRADES,
  MILITARY_STATUSES,
  PROFILE_FIELD_NAMES,
  PROTECTED_PROFILE_FIELDS,
  ProfileFieldName,
  ProfileGender,
  PreferredLanguage,
} from '../../models/profile.model';
import {
  emptyProfileFormValue,
  formValueToRequest,
  profileToFormValue,
} from '../../utils/profile.mapper';
import {
  arabicNameValidator,
  englishNameValidator,
  graduationYearValidator,
  notBlankValidator,
  pastDateValidator,
} from '../../validators/profile.validators';
import { PROFILE_COPY } from './profile.copy';

type WizardStep = 1 | 2 | 3;

type ProfileFormControls = {
  fullNameEn: FormControl<string>;
  fullNameAr: FormControl<string>;
  nationalId: FormControl<string>;
  nationality: FormControl<string>;
  dateOfBirth: FormControl<string>;
  gender: FormControl<ProfileGender | ''>;
  address: FormControl<string>;
  governorate: FormControl<string>;
  university: FormControl<string>;
  faculty: FormControl<string>;
  degreeLevel: FormControl<string>;
  major: FormControl<string>;
  graduationYear: FormControl<number | null>;
  cumulativeGrade: FormControl<
    ApplicantProfileFormValue['cumulativeGrade']
  >;
  militaryStatus: FormControl<ApplicantProfileFormValue['militaryStatus']>;
  phoneNumber: FormControl<string>;
  preferredLanguage: FormControl<PreferredLanguage>;
  gmailNotificationsEnabled: FormControl<boolean>;
};

const STEP_FIELDS: Record<WizardStep, readonly ProfileFieldName[]> = {
  1: [
    'fullNameEn',
    'fullNameAr',
    'nationalId',
    'nationality',
    'dateOfBirth',
    'gender',
    'militaryStatus',
  ],
  2: [
    'phoneNumber',
    'address',
    'governorate',
    'preferredLanguage',
    'gmailNotificationsEnabled',
  ],
  3: [
    'university',
    'faculty',
    'degreeLevel',
    'major',
    'graduationYear',
    'cumulativeGrade',
  ],
};

@Component({
  selector: 'app-profile-page',
  imports: [ReactiveFormsModule],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage implements OnInit {
  readonly store = inject(ProfileStore);
  readonly locale = inject(LocaleStore);

  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private hydratedProfile: ApplicantProfile | null = null;
  private awaitingSaveCompletion = false;

  readonly currentStep = signal<WizardStep>(1);
  readonly currentYear = new Date().getUTCFullYear();
  readonly maximumBirthDate = new Date().toISOString().slice(0, 10);
  readonly grades = CUMULATIVE_GRADES;
  readonly militaryStatuses = MILITARY_STATUSES;
  readonly universities = EGYPTIAN_UNIVERSITIES;
  readonly faculties = FACULTIES;
  readonly degreeLevels = DEGREE_LEVELS;
  readonly majors = MAJORS;
  readonly copy = computed(() => PROFILE_COPY[this.locale.locale()]);
  readonly isArabic = computed(() => this.locale.locale() === 'ar');
  readonly steps = computed(
    () =>
      [
        { number: 1, label: this.copy().personalStep },
        { number: 2, label: this.copy().contactStep },
        { number: 3, label: this.copy().academicStep },
      ] as const,
  );
  readonly hasValidationErrors = computed(
    () => Object.keys(this.store.validationErrors()).length > 0,
  );

  readonly form = new FormGroup<ProfileFormControls>({
    fullNameEn: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        notBlankValidator(),
        englishNameValidator(),
        Validators.maxLength(300),
      ],
    }),
    fullNameAr: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        notBlankValidator(),
        arabicNameValidator(),
        Validators.maxLength(300),
      ],
    }),
    nationalId: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.pattern(/^\d{14}$/),
      ],
    }),
    nationality: new FormControl('EGY', {
      nonNullable: true,
      validators: [
        Validators.required,
        notBlankValidator(),
        Validators.maxLength(5),
      ],
    }),
    dateOfBirth: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, pastDateValidator()],
    }),
    gender: new FormControl<ProfileGender | ''>('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    address: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(2000)],
    }),
    governorate: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(100)],
    }),
    university: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        notBlankValidator(),
        Validators.maxLength(200),
      ],
    }),
    faculty: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        notBlankValidator(),
        Validators.maxLength(200),
      ],
    }),
    degreeLevel: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        notBlankValidator(),
        Validators.maxLength(30),
      ],
    }),
    major: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        notBlankValidator(),
        Validators.maxLength(200),
      ],
    }),
    graduationYear: new FormControl<number | null>(null, {
      validators: [Validators.required, graduationYearValidator()],
    }),
    cumulativeGrade: new FormControl<
      ApplicantProfileFormValue['cumulativeGrade']
    >('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    militaryStatus: new FormControl<
      ApplicantProfileFormValue['militaryStatus']
    >('', {
      nonNullable: true,
    }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.maxLength(50),
        Validators.pattern(/^\+?[0-9][0-9\s()-]{6,49}$/),
      ],
    }),
    preferredLanguage: new FormControl<PreferredLanguage>('en', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    gmailNotificationsEnabled: new FormControl(true, {
      nonNullable: true,
    }),
  });

  private readonly hydrateFromProfile = effect(() => {
    const profile = this.store.profile();

    if (!profile || profile === this.hydratedProfile) {
      return;
    }

    this.hydratedProfile = profile;
    this.form.reset(profileToFormValue(profile), { emitEvent: false });
    this.configureMilitaryStatus(profile.gender);
    this.applyLockedState(profile.isProfileLocked);
  });

  private readonly revealServerErrorStep = effect(() => {
    const errors = this.store.validationErrors();
    const firstField = PROFILE_FIELD_NAMES.find(
      (field) => (errors[field]?.length ?? 0) > 0,
    );

    if (firstField && !this.store.isLocked()) {
      this.currentStep.set(this.stepForField(firstField));
    }
  });

  private readonly redirectAfterSuccessfulCompletion = effect(() => {
    const savedAt = this.store.savedAt();

    if (!this.awaitingSaveCompletion || savedAt === null) {
      return;
    }

    this.awaitingSaveCompletion = false;

    if (this.store.isComplete() && !this.store.isLocked()) {
      this.store.clearSaveFeedback();
      void this.router.navigateByUrl('/applications');
    }
  });

  constructor() {
    this.form.controls.gender.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((gender) => this.configureMilitaryStatus(gender));

    for (const field of PROFILE_FIELD_NAMES) {
      this.form
        .get(field)
        ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.store.clearFieldError(field));
    }
  }

  ngOnInit(): void {
    if (!this.store.hasLoaded()) {
      const initial = emptyProfileFormValue(this.locale.locale());
      this.form.reset(initial, { emitEvent: false });
      this.store.load(undefined);
    }
  }

  retryLoad(): void {
    this.store.load(undefined);
  }

  goToStep(step: WizardStep): void {
    if (step <= this.currentStep()) {
      this.currentStep.set(step);
      return;
    }

    if (this.validateStep(this.currentStep())) {
      this.currentStep.set(step);
    }
  }

  nextStep(): void {
    const current = this.currentStep();

    if (this.validateStep(current) && current < 3) {
      this.currentStep.set((current + 1) as WizardStep);
    }
  }

  previousStep(): void {
    const current = this.currentStep();

    if (current > 1) {
      this.currentStep.set((current - 1) as WizardStep);
    }
  }

  submit(): void {
    this.form.markAllAsTouched();
    this.configureMilitaryStatus(this.form.controls.gender.value);

    if (this.form.invalid) {
      if (!this.store.isLocked()) {
        this.currentStep.set(this.firstInvalidStep());
      }

      return;
    }

    this.awaitingSaveCompletion = true;
    this.store.save(formValueToRequest(this.form.getRawValue()));
  }

  fieldMessages(field: ProfileFieldName): string[] {
    const serverMessages = this.store.validationErrors()[field];

    if (serverMessages?.length) {
      return serverMessages;
    }

    const control = this.form.get(field);

    if (!control || (!control.touched && !control.dirty) || !control.errors) {
      return [];
    }

    const copy = this.copy();

    if (control.hasError('required')) {
      return [copy.required];
    }

    if (field === 'fullNameEn' && control.hasError('englishName')) {
      return [copy.englishNameOnly];
    }

    if (field === 'fullNameAr' && control.hasError('arabicName')) {
      return [copy.arabicNameOnly];
    }

    if (field === 'nationalId' && control.hasError('pattern')) {
      return [copy.nationalIdFormat];
    }

    if (field === 'dateOfBirth' && control.hasError('pastDate')) {
      return [copy.pastDate];
    }

    if (
      field === 'graduationYear' &&
      control.hasError('graduationYear')
    ) {
      return [`${copy.graduationYearRange} ${this.currentYear}.`];
    }

    if (field === 'phoneNumber' && control.hasError('pattern')) {
      return [copy.phoneFormat];
    }

    if (control.hasError('maxlength')) {
      return [copy.maximumLength];
    }

    return [copy.required];
  }

  gradeLabel(
    grade: ApplicantProfileFormValue['cumulativeGrade'],
  ): string {
    const copy = this.copy();

    switch (grade) {
      case 'Acceptable':
        return copy.acceptable;
      case 'Good':
        return copy.good;
      case 'VeryGood':
        return copy.veryGood;
      case 'Excellent':
        return copy.excellent;
      default:
        return copy.selectOption;
    }
  }

  militaryLabel(
    status: ApplicantProfileFormValue['militaryStatus'],
  ): string {
    const copy = this.copy();

    switch (status) {
      case 'Completed':
        return copy.completed;
      case 'Exempted':
        return copy.exempted;
      case 'Postponed':
        return copy.postponed;
      case 'CurrentlyServing':
        return copy.currentlyServing;
      default:
        return copy.selectOption;
    }
  }

  academicOptionLabel(option: LocalizedAcademicOption): string {
    return option[this.locale.locale()];
  }

  hasUnlistedAcademicOption(
    options: readonly LocalizedAcademicOption[],
    value: string,
  ): boolean {
    return value.length > 0 && !options.some((option) => option.value === value);
  }

  private configureMilitaryStatus(gender: ProfileGender | null | ''): void {
    const military = this.form.controls.militaryStatus;

    if (gender === 'Male') {
      military.setValidators([Validators.required]);
    } else {
      military.clearValidators();

      if (gender === 'Female' && military.value !== '') {
        military.setValue('', { emitEvent: false });
      }
    }

    military.updateValueAndValidity({ emitEvent: false });
  }

  private applyLockedState(isLocked: boolean): void {
    for (const field of PROTECTED_PROFILE_FIELDS) {
      const control = this.form.get(field);

      if (isLocked) {
        control?.disable({ emitEvent: false });
      } else {
        control?.enable({ emitEvent: false });
      }
    }
  }

  private validateStep(step: WizardStep): boolean {
    const fields = STEP_FIELDS[step];

    for (const field of fields) {
      const control = this.form.get(field);
      control?.markAsTouched();
      control?.updateValueAndValidity({ emitEvent: false });
    }

    return fields.every((field) => !this.form.get(field)?.invalid);
  }

  private firstInvalidStep(): WizardStep {
    for (const step of [1, 2, 3] as const) {
      if (STEP_FIELDS[step].some((field) => this.form.get(field)?.invalid)) {
        return step;
      }
    }

    return 1;
  }

  private stepForField(field: ProfileFieldName): WizardStep {
    if (STEP_FIELDS[1].includes(field)) {
      return 1;
    }

    if (STEP_FIELDS[2].includes(field)) {
      return 2;
    }

    return 3;
  }
}
