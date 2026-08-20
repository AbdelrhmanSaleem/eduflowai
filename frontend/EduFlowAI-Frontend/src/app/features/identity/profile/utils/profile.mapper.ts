import {
  ApplicantProfile,
  ApplicantProfileFormValue,
  CUMULATIVE_GRADES,
  MILITARY_STATUSES,
  PROFILE_GENDERS,
  PreferredLanguage,
  UpdateApplicantProfileRequest,
} from '../models/profile.model';

function isOneOf<T extends string>(
  value: string | null,
  values: readonly T[],
): value is T {
  return value !== null && values.some((item) => item === value);
}

function optionalText(value: string): string | null {
  const normalized = value.trim();
  return normalized.length > 0 ? normalized : null;
}

export function emptyProfileFormValue(
  preferredLanguage: PreferredLanguage,
): ApplicantProfileFormValue {
  return {
    fullNameEn: '',
    fullNameAr: '',
    nationalId: '',
    nationality: 'EGY',
    dateOfBirth: '',
    gender: '',
    address: '',
    governorate: '',
    university: '',
    faculty: '',
    degreeLevel: '',
    major: '',
    graduationYear: null,
    cumulativeGrade: '',
    militaryStatus: '',
    phoneNumber: '',
    preferredLanguage,
    gmailNotificationsEnabled: true,
  };
}

export function profileToFormValue(
  profile: ApplicantProfile,
): ApplicantProfileFormValue {
  return {
    fullNameEn: profile.fullNameEn ?? '',
    fullNameAr: profile.fullNameAr ?? '',
    nationalId: profile.nationalId ?? '',
    nationality: profile.nationality?.toUpperCase() || 'EGY',
    dateOfBirth: profile.dateOfBirth ?? '',
    gender: isOneOf(profile.gender, PROFILE_GENDERS) ? profile.gender : '',
    address: profile.address ?? '',
    governorate: profile.governorate ?? '',
    university: profile.university ?? '',
    faculty: profile.faculty ?? '',
    degreeLevel: profile.degreeLevel ?? '',
    major: profile.major ?? '',
    graduationYear: profile.graduationYear,
    cumulativeGrade: isOneOf(
      profile.cumulativeGrade,
      CUMULATIVE_GRADES,
    )
      ? profile.cumulativeGrade
      : '',
    militaryStatus: isOneOf(profile.militaryStatus, MILITARY_STATUSES)
      ? profile.militaryStatus
      : '',
    phoneNumber: profile.phoneNumber ?? '',
    preferredLanguage: profile.preferredLanguage,
    gmailNotificationsEnabled: profile.gmailNotificationsEnabled,
  };
}

export function formValueToRequest(
  value: ApplicantProfileFormValue,
): UpdateApplicantProfileRequest {
  if (
    value.gender === '' ||
    value.cumulativeGrade === '' ||
    value.graduationYear === null
  ) {
    throw new Error('Cannot map an incomplete applicant profile.');
  }

  return {
    fullNameEn: value.fullNameEn.trim(),
    fullNameAr: value.fullNameAr.trim(),
    nationalId: value.nationalId.trim(),
    nationality: value.nationality.trim().toUpperCase(),
    dateOfBirth: value.dateOfBirth,
    gender: value.gender,
    address: optionalText(value.address),
    governorate: optionalText(value.governorate),
    university: value.university.trim(),
    faculty: value.faculty.trim(),
    degreeLevel: value.degreeLevel.trim(),
    major: value.major.trim(),
    graduationYear: value.graduationYear,
    cumulativeGrade: value.cumulativeGrade,
    militaryStatus:
      value.gender === 'Female' || value.militaryStatus === ''
        ? null
        : value.militaryStatus,
    phoneNumber: optionalText(value.phoneNumber),
    preferredLanguage: value.preferredLanguage,
    gmailNotificationsEnabled: value.gmailNotificationsEnabled,
  };
}
