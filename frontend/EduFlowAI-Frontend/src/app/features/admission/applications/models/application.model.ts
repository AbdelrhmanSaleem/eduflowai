/**
 * Core Result Wrapper matching the C# Result<T> implementation
 */
export interface Result<T> {
    isSuccess: boolean;
    data: T;
    statusCode: number;
    message: string;
}

/**
 * Application Status Enum matching C# ApplicationStatus
 */
export enum ApplicationStatus {
    None = 0,
    Draft = 1,
    DocumentsRequired = 2,
    EligibilityFailed = 3,
    UnderDocumentVerification = 4,
    NeedsHumanReview = 5,
    DocumentRejected = 6,
    AssessmentInProgress = 7,
    Admitted = 8,
    Waitlisted = 9,
    NotSelected = 10,
    Withdrawn = 11,
    Expired = 12
}

/**
 * DTO for Track/Branch Preference
 */
export interface PreferenceDto {
    trackId: string;    // Guid maps to string in TS
    branchId: string;
    rank: number;       // short maps to number
}

/**
 * DTO for creating a new draft application
 */
export interface ApplicationRequestDto {
    cycleId: string;
    preferences: PreferenceDto[];
}

/**
 *  DTO for updating existing preferences
 */ 
export interface UpdateApplicationPreferencesDto {
    preferences: PreferenceDto[];
}

/**
 * DTO for application details returned from GET endpoint
 */
export interface ApplicationDetailsDto {
    id: string;
    applicantUserId: string;
    cycleId: string;
    cycleName: string;
    cycleDeadlineUtc: string;
    status: string;     // Kept as string to match C# JSON serialization, can be mapped to Enum if needed
    createdAt: string;  // DateTimeOffset maps to ISO string
    updatedAt: string;
    preferences: PreferenceDto[];
}

/**
 * DTO for Active Admission Cycle based on backend response
 */
export interface ActiveAdmissionCycleDto {
    cycleId: string;
    programName: string;
    programCode: string;
    programDescription: string;
    cycleLabel: string;
    startDate: string;
    deadlineUtc: string;
}

/**
 * DTO representing the result of an application status change (e.g., Withdrawal).
 * Maps directly to the backend ApplicationStatusDto record.
 */
export interface ApplicationStatusDto {
    applicationId: string;
    currentStatus: string;
    lastUpdatedAt: string;     // Maps from DateTimeOffset to ISO string in TS
    statusMessage?: string | null;
}

/*
 * Defines the shape of the lightweight application data used in the dashboard list view.
 * This directly maps to the ApplicationListDto in the backend.
 */
export interface ApplicationListDto {
    id: string;
    programName: string;
    intakeName: string;
    status: string;
    submittedAt?: string | null;
}

/**
 * Pagination query parameters matching C# QueryParameters
 */
export interface QueryParameters {
    page?: number;
    pageSize?: number;
    search?: string;
    status?: string;
    type?: string;
}

/**
 * Paginated result wrapper matching C# PaginatedResult<T>
 */
export interface PaginatedResult<T> {
    data: T[];
    filters?: any;
    currentPage: number;
    totalPages: number;
    pageSize: number;
    totalCount: number;
}

/**
 * DTO for returning eligibility results
 */
export interface EligibilityDetailsDto {
    passed: boolean;
    evaluatedAt: string; // Maps to DateTimeOffset
    failureReasons: string[]; // Parsed list of rejection reasons
}

/**
 * DTO to securely receive the evaluation request from the front-end
 */
export interface EvaluateEligibilityRequestDto {
    applicantId: string;
    cycleId: string;
    applicationId: string;
}

/**
 * Result returned after evaluating applicant eligibility
 */
export interface EligibilityResult {
    id: string;
    passed: boolean;
    failureReasonsJson: string; // JSON array string
    evaluatedAt: string;
    applicationId: string;
    // application entity omitted to prevent circular reference bloat
}

/**
 * DTO for Assessment Simulated Stages
 */
export interface SimulatedStageDto {
    stageId: string;
    stageType: string;
    title: string;
    description: string;
    score?: number | null;
    maxScore: number;
    result: string;
    updatedAt: string; // Maps to DateTimeOffset
}

/**
 * Enum for Reviewer Type mapping to C# Enum
 */
export enum ReviewerType {
    Human = 0,
    AI = 1
}

/**
 * DTO for Document Review Result
 */
export interface DocumentReviewResultDto {
    isAccepted: boolean;
    rejectionReason?: string | null;
    reviewerType: ReviewerType;
    isAgentUncertain: boolean;
    reviewedByUserId?: string | null;
}

/**
 * Represents a single task in the enrollment checklist.
 */
export interface EnrollmentTaskItemDto {
    id: string;
    title: string;
    status: string; 
    taskType: string; 
    subtextMessage?: string;
    actionUrl?: string;
}

/**
 * Represents the full enrollment checklist dashboard.
 */
export interface EnrollmentChecklistDto {
    completedTasksCount: number;
    totalTasksCount: number;
    tasks: EnrollmentTaskItemDto[];
}

/**
 * Enum representing the assessment stages, mapping directly to C# SelectionStage.
 */
export enum SelectionStage {
    None = 0,
    EnglishExamAndIq = 1,
    ProgrammingExam = 2,
    TechnicalInterview = 3,
    SoftSkillsInterview = 4
}