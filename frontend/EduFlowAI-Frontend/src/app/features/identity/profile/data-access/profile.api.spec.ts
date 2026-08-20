import {
  provideHttpClient,
  withInterceptorsFromDi,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { RuntimeConfig } from '../../../../core/config/runtime-config';
import {
  ApplicantProfile,
  UpdateApplicantProfileRequest,
} from '../models/profile.model';
import { ProfileApi } from './profile.api';

const apiBaseUrl = 'https://identity.example.test/api';

const profile: ApplicantProfile = {
  userId: 'user-1',
  email: 'applicant@example.com',
  phoneNumber: null,
  preferredLanguage: 'en',
  gmailNotificationsEnabled: true,
  isComplete: false,
  isProfileLocked: false,
  fullNameEn: null,
  fullNameAr: null,
  nationalId: null,
  nationality: null,
  dateOfBirth: null,
  gender: null,
  address: null,
  governorate: null,
  university: null,
  faculty: null,
  degreeLevel: null,
  major: null,
  graduationYear: null,
  cumulativeGrade: null,
  militaryStatus: null,
  createdAt: null,
  updatedAt: null,
};

describe('ProfileApi', () => {
  let api: ProfileApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ProfileApi,
        { provide: RuntimeConfig, useValue: { apiBaseUrl } },
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
      ],
    });

    api = TestBed.inject(ProfileApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the applicant profile through the configured API route', () => {
    let result: ApplicantProfile | undefined;

    api.getProfile().subscribe((value) => (result = value));

    const request = http.expectOne(`${apiBaseUrl}/profile`);
    expect(request.request.method).toBe('GET');
    request.flush(profile);
    expect(result).toEqual(profile);
  });

  it('sends the complete profile DTO with PUT', () => {
    const body: UpdateApplicantProfileRequest = {
      fullNameEn: 'Karim Ramadan',
      fullNameAr: 'كريم رمضان',
      nationalId: '30009060201856',
      nationality: 'EGY',
      dateOfBirth: '2000-07-23',
      gender: 'Male',
      address: null,
      governorate: 'Cairo',
      university: 'Cairo University',
      faculty: 'Engineering',
      degreeLevel: 'Bachelor',
      major: 'Computer Engineering',
      graduationYear: 2023,
      cumulativeGrade: 'VeryGood',
      militaryStatus: 'Completed',
      phoneNumber: '01282538431',
      preferredLanguage: 'en',
      gmailNotificationsEnabled: true,
    };

    api.updateProfile(body).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/profile`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush({ ...profile, ...body, isComplete: true });
  });
});
