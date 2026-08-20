import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AuthSessionStore } from '../../../../core/auth/auth-session.store';
import { ApiError } from '../../../../core/errors/api-problem';
import { LocaleStore } from '../../../../core/i18n/locale.store';
import {
  ApplicantProfile,
  UpdateApplicantProfileRequest,
} from '../models/profile.model';
import { ProfileApi } from './profile.api';
import { ProfileStore } from './profile.store';

const profile: ApplicantProfile = {
  userId: 'user-1',
  email: 'applicant@example.com',
  phoneNumber: '01282538431',
  preferredLanguage: 'ar',
  gmailNotificationsEnabled: true,
  isComplete: true,
  isProfileLocked: false,
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

const updateRequest: UpdateApplicantProfileRequest = {
  fullNameEn: profile.fullNameEn!,
  fullNameAr: profile.fullNameAr!,
  nationalId: profile.nationalId!,
  nationality: profile.nationality!,
  dateOfBirth: profile.dateOfBirth!,
  gender: profile.gender!,
  address: profile.address,
  governorate: profile.governorate,
  university: profile.university!,
  faculty: profile.faculty!,
  degreeLevel: profile.degreeLevel!,
  major: profile.major!,
  graduationYear: profile.graduationYear!,
  cumulativeGrade: profile.cumulativeGrade!,
  militaryStatus: profile.militaryStatus,
  phoneNumber: profile.phoneNumber,
  preferredLanguage: profile.preferredLanguage,
  gmailNotificationsEnabled: profile.gmailNotificationsEnabled,
};

describe('ProfileStore', () => {
  let api: {
    getProfile: ReturnType<typeof vi.fn>;
    updateProfile: ReturnType<typeof vi.fn>;
  };
  let markProfileComplete: ReturnType<typeof vi.fn>;
  let setLocale: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    api = {
      getProfile: vi.fn(() => of(profile)),
      updateProfile: vi.fn(() => of(profile)),
    };
    markProfileComplete = vi.fn();
    setLocale = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        ProfileStore,
        { provide: ProfileApi, useValue: api },
        {
          provide: AuthSessionStore,
          useValue: { markProfileComplete },
        },
        {
          provide: LocaleStore,
          useValue: { setLocale },
        },
      ],
    });
  });

  it('loads profile state and synchronizes session and locale', () => {
    const store = TestBed.inject(ProfileStore);

    store.load(undefined);

    expect(store.profile()).toEqual(profile);
    expect(store.isComplete()).toBe(true);
    expect(markProfileComplete).toHaveBeenCalledWith(true);
    expect(setLocale).toHaveBeenCalledWith('ar');
  });

  it('updates profile state and synchronizes session and locale', () => {
    const store = TestBed.inject(ProfileStore);

    store.save(updateRequest);

    expect(api.updateProfile).toHaveBeenCalledWith(updateRequest);
    expect(store.profile()).toEqual(profile);
    expect(store.savedAt()).not.toBeNull();
    expect(markProfileComplete).toHaveBeenCalledWith(true);
    expect(setLocale).toHaveBeenCalledWith('ar');
  });

  it('maps case-insensitive backend field errors to controls', () => {
    const error: ApiError = {
      status: 409,
      title: 'Profile update conflict',
      errors: {
        NationalId: [
          'This National ID is already associated with an account.',
        ],
      },
    };
    api.updateProfile = vi.fn(() => throwError(() => error));
    const store = TestBed.inject(ProfileStore);

    store.save(updateRequest);

    expect(store.validationErrors().nationalId).toEqual(
      error.errors['NationalId'],
    );
  });

  it('surfaces a locked-profile conflict as a general message', () => {
    const error: ApiError = {
      status: 409,
      title: 'Profile update conflict',
      errors: {
        profile: [
          'Eligibility fields are permanently locked after submission.',
        ],
      },
    };
    api.updateProfile = vi.fn(() => throwError(() => error));
    const store = TestBed.inject(ProfileStore);

    store.save(updateRequest);

    expect(store.saveError()).toBe(error.errors['profile'][0]);
  });
});
