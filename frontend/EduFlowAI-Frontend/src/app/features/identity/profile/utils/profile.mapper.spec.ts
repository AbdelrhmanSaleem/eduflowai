import {
  ApplicantProfile,
  ApplicantProfileFormValue,
} from '../models/profile.model';
import {
  emptyProfileFormValue,
  formValueToRequest,
  profileToFormValue,
} from './profile.mapper';

const completeValue: ApplicantProfileFormValue = {
  fullNameEn: '  Karim Ramadan  ',
  fullNameAr: '  كريم رمضان  ',
  nationalId: '30009060201856',
  nationality: 'egy',
  dateOfBirth: '2000-07-23',
  gender: 'Male',
  address: '  Cairo  ',
  governorate: '',
  university: '  Cairo University ',
  faculty: ' Engineering ',
  degreeLevel: ' Bachelor ',
  major: ' Computer Engineering ',
  graduationYear: 2023,
  cumulativeGrade: 'VeryGood',
  militaryStatus: 'Completed',
  phoneNumber: ' 01282538431 ',
  preferredLanguage: 'en',
  gmailNotificationsEnabled: true,
};

describe('profile mapper', () => {
  it('starts a new profile with the EGY wire code', () => {
    expect(emptyProfileFormValue('ar')).toMatchObject({
      nationality: 'EGY',
      preferredLanguage: 'ar',
      gmailNotificationsEnabled: true,
    });
  });

  it('normalizes text while preserving backend enum names', () => {
    const request = formValueToRequest(completeValue);

    expect(request).toEqual({
      fullNameEn: 'Karim Ramadan',
      fullNameAr: 'كريم رمضان',
      nationalId: '30009060201856',
      nationality: 'EGY',
      dateOfBirth: '2000-07-23',
      gender: 'Male',
      address: 'Cairo',
      governorate: null,
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
    });
  });

  it('always sends null military status for a female applicant', () => {
    const request = formValueToRequest({
      ...completeValue,
      gender: 'Female',
      militaryStatus: 'Exempted',
    });

    expect(request.militaryStatus).toBeNull();
  });

  it('maps the incomplete GET response into editable defaults', () => {
    const response: ApplicantProfile = {
      userId: 'user-1',
      email: 'applicant@example.com',
      phoneNumber: null,
      preferredLanguage: 'en',
      gmailNotificationsEnabled: false,
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

    expect(profileToFormValue(response)).toMatchObject({
      nationality: 'EGY',
      gender: '',
      cumulativeGrade: '',
      militaryStatus: '',
      graduationYear: null,
    });
  });
});
