export type PreferredLanguage = 'en' | 'ar';

export interface RegisterRequest {
  email: string;
  password: string;
  preferredLanguage: PreferredLanguage;
}

export interface RegisterResponse {
  userId: string;
  email: string;
  requiresEmailConfirmation: boolean;
  developmentConfirmationToken?: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  roles: string[];
}

export interface ConfirmEmailRequest {
  email: string;
  token: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  message: string;
  developmentResetToken?: string | null;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export type AuthRequestState =
  | 'idle'
  | 'submitting'
  | 'success'
  | 'invalid-link'
  | 'error';
