export const DOCUMENT_TYPES = [
  'NationalId',
  'BirthCertificate',
  'GraduationCertificate',
  'MilitaryCertificate',
] as const;

export const DOCUMENT_STATUSES = [
  'Uploaded',
  'Verifying',
  'Approved',
  'NeedsHumanReview',
  'ReplacementRequested',
  'Rejected',
] as const;

export type DocumentType = 'None' | (typeof DOCUMENT_TYPES)[number];
export type DocumentStatus = 'None' | (typeof DOCUMENT_STATUSES)[number];

export const REVIEW_QUEUE_STATUSES = [
  'NeedsHumanReview',
  'ReplacementRequested',
] as const satisfies readonly DocumentStatus[];

export interface HumanReviewDto {
  documentId: string;
  documentType: DocumentType;
  applicantName: string;
  originalFileName: string;
  status: DocumentStatus;
  verificationDetailsJson?: string | null;
}

export interface DocumentReviewDto {
  documentId: string;
  applicationId: string;
  applicantId: string;
  documentType: DocumentType;
  originalFileName: string;
  status: DocumentStatus;
  verificationDetailsJson?: string | null;
}

export interface PaginatedResult<T> {
  data: T[];
  filters: unknown;
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalCount: number;
}

export interface HumanReviewQuery {
  page: number;
  pageSize: number;
  search: string;
  status: string;
  type: string;
}

export interface ReplacementRequestDto {
  documentId: string;
  applicantId: string;
  reason: string;
}

export interface VerificationField {
  notes?: string | null;
  isMatch: boolean;
  fieldName: string;
  expectedValue?: string | null;
  extractedValue?: string | null;
}

export interface VerificationDetails {
  fields: VerificationField[];
  warnings: string[];
  modelName: string;
  missingFields: string[];
  confidenceScore: number;
}
