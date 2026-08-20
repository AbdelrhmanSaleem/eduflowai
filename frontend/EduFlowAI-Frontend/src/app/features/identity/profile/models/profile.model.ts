export const PROFILE_GENDERS = ['Male', 'Female'] as const;
export type ProfileGender = (typeof PROFILE_GENDERS)[number];

export const CUMULATIVE_GRADES = [
  'Acceptable',
  'Good',
  'VeryGood',
  'Excellent',
] as const;
export type CumulativeGrade = (typeof CUMULATIVE_GRADES)[number];

export const MILITARY_STATUSES = [
  'Completed',
  'Exempted',
  'Postponed',
  'CurrentlyServing',
] as const;
export type MilitaryStatus = (typeof MILITARY_STATUSES)[number];

export type PreferredLanguage = 'en' | 'ar';

export interface ApplicantProfile {
  userId: string;
  email: string;
  phoneNumber: string | null;
  preferredLanguage: PreferredLanguage;
  gmailNotificationsEnabled: boolean;
  isComplete: boolean;
  isProfileLocked: boolean;
  fullNameEn: string | null;
  fullNameAr: string | null;
  nationalId: string | null;
  nationality: string | null;
  dateOfBirth: string | null;
  gender: ProfileGender | null;
  address: string | null;
  governorate: string | null;
  university: string | null;
  faculty: string | null;
  degreeLevel: string | null;
  major: string | null;
  graduationYear: number | null;
  cumulativeGrade: CumulativeGrade | null;
  militaryStatus: MilitaryStatus | null;
  createdAt: string | null;
  updatedAt: string | null;
}

export interface UpdateApplicantProfileRequest {
  fullNameEn: string;
  fullNameAr: string;
  nationalId: string;
  nationality: string;
  dateOfBirth: string;
  gender: ProfileGender;
  address: string | null;
  governorate: string | null;
  university: string;
  faculty: string;
  degreeLevel: string;
  major: string;
  graduationYear: number;
  cumulativeGrade: CumulativeGrade;
  militaryStatus: MilitaryStatus | null;
  phoneNumber: string | null;
  preferredLanguage: PreferredLanguage;
  gmailNotificationsEnabled: boolean;
}

export interface ApplicantProfileFormValue {
  fullNameEn: string;
  fullNameAr: string;
  nationalId: string;
  nationality: string;
  dateOfBirth: string;
  gender: ProfileGender | '';
  address: string;
  governorate: string;
  university: string;
  faculty: string;
  degreeLevel: string;
  major: string;
  graduationYear: number | null;
  cumulativeGrade: CumulativeGrade | '';
  militaryStatus: MilitaryStatus | '';
  phoneNumber: string;
  preferredLanguage: PreferredLanguage;
  gmailNotificationsEnabled: boolean;
}

export type ProfileFieldName = keyof ApplicantProfileFormValue;

export const PROFILE_FIELD_NAMES: readonly ProfileFieldName[] = [
  'fullNameEn',
  'fullNameAr',
  'nationalId',
  'nationality',
  'dateOfBirth',
  'gender',
  'address',
  'governorate',
  'university',
  'faculty',
  'degreeLevel',
  'major',
  'graduationYear',
  'cumulativeGrade',
  'militaryStatus',
  'phoneNumber',
  'preferredLanguage',
  'gmailNotificationsEnabled',
];

export const PROTECTED_PROFILE_FIELDS: readonly ProfileFieldName[] = [
  'fullNameEn',
  'fullNameAr',
  'nationalId',
  'nationality',
  'dateOfBirth',
  'gender',
  'university',
  'faculty',
  'degreeLevel',
  'major',
  'graduationYear',
  'cumulativeGrade',
  'militaryStatus',
];
