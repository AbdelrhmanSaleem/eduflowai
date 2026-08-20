// features/documents/applicant-documents/models/document.model.ts

/** Mirrors EduFlowAI.Admission.Domain.Enums.DocumentType */
export enum DocumentType {
    None = 0,
    NationalId = 1,
    BirthCertificate = 2,
    GraduationCertificate = 3,
    MilitaryCertificate = 4,
}

/** Mirrors EduFlowAI.Documents.Domain.Enums.DocumentStatus */
export enum DocumentStatus {
    None = 0,
    Uploaded = 1,
    Verifying = 2,
    Approved = 3,
    NeedsHumanReview = 4,
    Rejected = 5,
}

/** Response shape of GET /api/applications/{applicationId}/documents (one item) */
export interface ApplicantDocumentDto {
    id: string;
    documentType: DocumentType;
    originalFileName: string;
    status: DocumentStatus;
    createdAt: string; // ISO date string
}

/** Response of POST /api/applications/{applicationId}/documents */
export interface UploadDocumentResponse {
    documentId: string;
    message: string;
}

/** Response of POST /api/applications/{applicationId}/documents/submit */
export interface SubmitPackageResponse {
    message: string;
    submittedCount: number;
}

/** Response of GET /api/applications/{applicationId}/documents/required */
export interface RequiredDocumentsResponse {
    documentTypes: DocumentType[];
}

/** Shape of a failed response body across all five endpoints */
export interface ApiErrorResponse {
    error: string;
}