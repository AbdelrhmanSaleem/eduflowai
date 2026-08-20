import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { LocaleStore } from '../../../../../core/i18n/locale.store';
import { ProfileStore } from '../../data-access/profile.store';
import {
  ApplicantProfile,
  ProfileFieldName,
  UpdateApplicantProfileRequest,
} from '../../models/profile.model';
import { ProfilePage } from './profile-page';

const lockedProfile: ApplicantProfile = {
  userId: 'user-1',
  email: 'applicant@example.com',
  phoneNumber: '01282538431',
  preferredLanguage: 'en',
  gmailNotificationsEnabled: true,
  isComplete: true,
  isProfileLocked: true,
  fullNameEn: 'Karim Ramadan',
  fullNameAr: 'كريم رمضان',
  nationalId: '30009060201856',
  nationality: 'EGY',
  dateOfBirth: '2000-07-23',
  gender: 'Male',
  address: 'Cairo',
  governorate: 'Cairo',
  university: 'Cairo University',
  faculty: 'Engineering',
  degreeLevel: 'Bachelor',
  major: 'Computer Engineering',
  graduationYear: 2023,
  cumulativeGrade: 'VeryGood',
  militaryStatus: 'Completed',
  createdAt: '2026-07-01T00:00:00Z',
  updatedAt: '2026-07-30T00:00:00Z',
};

describe('ProfilePage', () => {
  let fixture: ComponentFixture<ProfilePage>;
  let component: ProfilePage;
  let save: ReturnType<typeof vi.fn>;
  let clearSaveFeedback: ReturnType<typeof vi.fn>;
  let navigateByUrl: ReturnType<typeof vi.fn>;
  let profile: WritableSignal<ApplicantProfile | null>;
  let isLocked: WritableSignal<boolean>;
  let savedAt: WritableSignal<number | null>;

  beforeEach(async () => {
    save = vi.fn();
    const validationErrors = signal<
      Partial<Record<ProfileFieldName, string[]>>
    >({});
    profile = signal<ApplicantProfile | null>(lockedProfile);
    isLocked = signal(true);
    savedAt = signal<number | null>(null);
    clearSaveFeedback = vi.fn();
    navigateByUrl = vi.fn(() => Promise.resolve(true));
    const store = {
      profile,
      isLoading: signal(false),
      isSaving: signal(false),
      hasLoaded: signal(true),
      loadError: signal<string | null>(null),
      saveError: signal<string | null>(null),
      validationErrors,
      savedAt,
      isComplete: signal(true),
      isLocked,
      load: vi.fn(),
      save,
      clearFieldError: vi.fn(),
      clearSaveFeedback,
    };
    const locale = {
      locale: signal<'en' | 'ar'>('en'),
      setLocale: vi.fn(),
      toggle: vi.fn(),
      t: (key: string) => key,
    };

    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [
        { provide: ProfileStore, useValue: store },
        { provide: LocaleStore, useValue: locale },
        { provide: Router, useValue: { navigateByUrl } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfilePage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the locked profile presentation', () => {
    expect(component).toBeTruthy();
    expect(
      fixture.nativeElement.querySelector('.locked-banner'),
    ).not.toBeNull();
  });

  it('disables protected fields but leaves contact fields editable', () => {
    expect(component.form.controls.nationalId.disabled).toBe(true);
    expect(component.form.controls.university.disabled).toBe(true);
    expect(component.form.controls.phoneNumber.enabled).toBe(true);
    expect(component.form.controls.address.enabled).toBe(true);
  });

  it('submits locked fields unchanged through getRawValue', () => {
    component.form.controls.phoneNumber.setValue('01000000000');

    component.submit();

    expect(save).toHaveBeenCalledOnce();
    const request = save.mock.calls[0][0] as UpdateApplicantProfileRequest;
    expect(request.nationalId).toBe(lockedProfile.nationalId);
    expect(request.university).toBe(lockedProfile.university);
    expect(request.phoneNumber).toBe('01000000000');
  });

  it('shows validation feedback for editable locked contact fields', () => {
    component.form.controls.address.setValue('x'.repeat(2001));
    component.form.controls.address.markAsTouched();

    component.submit();
    fixture.detectChanges();

    expect(save).not.toHaveBeenCalled();
    expect(component.fieldMessages('address')).toContain(
      'This value is too long.',
    );
    expect(
      fixture.nativeElement.querySelector('#locked-address')?.getAttribute(
        'aria-invalid',
      ),
    ).toBe('true');
  });

  it('renders academic fields as dropdowns and preserves legacy values', () => {
    isLocked.set(false);
    profile.set({
      ...lockedProfile,
      isProfileLocked: false,
      university: 'Alexandria',
      degreeLevel: "Bachelor's",
      major: 'Communication',
    });
    component.currentStep.set(3);
    fixture.detectChanges();

    const university = fixture.nativeElement.querySelector(
      '#university',
    ) as HTMLSelectElement;
    const faculty = fixture.nativeElement.querySelector(
      '#faculty',
    ) as HTMLSelectElement;
    const degree = fixture.nativeElement.querySelector(
      '#degree-level',
    ) as HTMLSelectElement;
    const major = fixture.nativeElement.querySelector(
      '#major',
    ) as HTMLSelectElement;

    expect(university.tagName).toBe('SELECT');
    expect(faculty.tagName).toBe('SELECT');
    expect(degree.tagName).toBe('SELECT');
    expect(major.tagName).toBe('SELECT');
    expect(university.value).toBe('Alexandria');
    expect(degree.value).toBe("Bachelor's");
    expect(major.value).toBe('Communication');
  });

  it('navigates to applications only after an unlocked profile save succeeds', () => {
    isLocked.set(false);
    profile.set({ ...lockedProfile, isProfileLocked: false });
    fixture.detectChanges();

    component.submit();
    savedAt.set(Date.now());
    fixture.detectChanges();

    expect(navigateByUrl).toHaveBeenCalledOnce();
    expect(navigateByUrl).toHaveBeenCalledWith('/applications');
    expect(clearSaveFeedback).toHaveBeenCalledOnce();
  });

  it('ignores save feedback retained from an earlier profile visit', () => {
    isLocked.set(false);
    profile.set({ ...lockedProfile, isProfileLocked: false });
    savedAt.set(Date.now());
    fixture.detectChanges();

    expect(navigateByUrl).not.toHaveBeenCalled();
    expect(clearSaveFeedback).not.toHaveBeenCalled();
  });

  it('stays on the profile when saving editable fields after profile lock', () => {
    component.submit();
    savedAt.set(Date.now());
    fixture.detectChanges();

    expect(navigateByUrl).not.toHaveBeenCalled();
  });
});
