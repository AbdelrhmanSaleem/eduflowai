export const REPLACEMENT_REQUEST_STATUSES = ['Open', 'Fulfilled'] as const;

export type ReplacementRequestStatus = 'None' | (typeof REPLACEMENT_REQUEST_STATUSES)[number];

export interface ReplacementRequestItem {
  id: string;
  documentId: string;
  documentType: string | null;
  reason: string;
  status: ReplacementRequestStatus;
  requestedAt: string;
}

export interface PaginatedReplacementRequests {
  data: ReplacementRequestItem[];
  filters: unknown | null;
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalCount: number;
}

export interface ReplacementUploadResponse {
  documentId: string;
  message: string;
  replacementStatus: ReplacementRequestStatus;
  documentStatus: string;
}
