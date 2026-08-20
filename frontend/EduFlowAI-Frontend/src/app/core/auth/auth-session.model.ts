export const APP_ROLES = {
  applicant: 'Applicant',
  operationsManager: 'OperationsManager',
  superAdmin: 'SuperAdmin',
} as const;

export type AppRole = (typeof APP_ROLES)[keyof typeof APP_ROLES];

export interface AuthSession {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  roles: AppRole[];
  email: string;
  profileComplete: boolean | null;
}

export interface LoginSession {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  roles: string[];
}
