export interface ApiResult<T> {
  isSuccess: boolean;
  data: T;
  statusCode: number;
  message: string;
}

export interface InstitutionDto {
  id: string;
  name: string;
  code: string;
  programCount: number;
}

export interface ProgramDto {
  id: string;
  institutionId: string;
  institutionName: string;
  name: string;
  code: string;
  durationMonths: number;
  trackCount: number;
  cycleCount: number;
}

export interface CreateInstitutionRequest {
  name: string;
  code: string;
}

export type UpdateInstitutionRequest = CreateInstitutionRequest;

export interface CreateProgramRequest {
  institutionId: string;
  name: string;
  code: string;
  durationMonths: number;
}

export interface UpdateProgramRequest {
  name: string;
  code: string;
  durationMonths: number;
}

export enum DocumentType {
  NationalId = 1,
  BirthCertificate = 2,
  GraduationCertificate = 3,
  MilitaryCertificate = 4,
}

export enum RequirementGender {
  Male = 1,
  Female = 2,
}

export interface ProgramDocumentRequirementDto {
  id: string;
  programId: string;
  documentType: DocumentType;
  requiredForGender: RequirementGender | null;
}

export interface ProgramDocumentRequirementInput {
  documentType: DocumentType;
  requiredForGender: RequirementGender | null;
}

export interface UpdateProgramDocumentRequirementsRequest {
  requirements: ProgramDocumentRequirementInput[];
}

export interface AdminTrackDto {
  id: string;
  programId: string;
  officialTrackId: string | null;
  officialTrackUrl: string | null;
  isOfficialIntake47: boolean;
  intake: number | null;
  year: number | null;
  name: string;
  description: string | null;
  category: string | null;
  totalHours: number | null;
  minimumGrade: string | null;
  eligibilitySummary: string | null;
  graduationYearLimitYears: number | null;
  prerequisiteTopics: string[];
  isActive: boolean;
  locations: TrackLocationDto[];
  offerings: BranchOfferingDto[];
}

export interface TrackLocationDto {
  branchId: string;
  branchName: string;
  governorate: string | null;
}

export interface BranchOfferingDto {
  offeringId: string;
  branchId: string;
  branchName: string;
  governorate: string | null;
  capacity: number;
}

export interface CreateTrackRequest {
  programId: string;
  name: string;
  description: string | null;
  prerequisiteTopics: string[];
  isActive: boolean;
}

export interface UpdateTrackRequest {
  name: string;
  description: string | null;
  prerequisiteTopics: string[];
  isActive: boolean;
}

export interface BranchDto {
  id: string;
  name: string;
  governorate: string | null;
  isActive: boolean;
  isOfficialIntake47Location: boolean;
}

export interface CreateBranchRequest {
  name: string;
  governorate: string | null;
  isActive: boolean;
}

export type UpdateBranchRequest = CreateBranchRequest;

export enum CycleStatus {
  Draft = 1,
  Active = 2,
  Closed = 3,
}

export enum CumulativeGrade {
  Acceptable = 1,
  Good = 2,
  VeryGood = 3,
  Excellent = 4,
}

export interface CycleEligibilityRuleDto {
  id: string;
  cycleId: string;
  requiredNationality: string;
  requiredDegreeLevel: string;
  maxYearsSinceGraduation: number;
  minGrade: CumulativeGrade;
}

export interface OfferingDto {
  id: string;
  cycleId: string;
  trackId: string;
  trackName: string;
  branchId: string;
  branchName: string;
  capacity: number;
}

export interface AdmissionCycleDto {
  id: string;
  programId: string;
  programName: string;
  label: string;
  startDate: string;
  deadlineUtc: string;
  status: CycleStatus;
  closedAt: string | null;
  rowVersion: number;
  eligibilityRule: CycleEligibilityRuleDto | null;
  offerings: OfferingDto[];
}

export interface CreateAdmissionCycleRequest {
  programId: string;
  label: string;
  startDate: string;
  deadlineUtc: string;
}

export interface UpdateCycleEligibilityRuleRequest {
  requiredNationality: string;
  requiredDegreeLevel: string;
  maxYearsSinceGraduation: number;
  minGrade: CumulativeGrade;
}

export interface CreateOfferingRequest {
  trackId: string;
  branchId: string;
  capacity: number;
}

export interface UpdateOfferingRequest {
  capacity: number;
}

export interface AdminAdmissionDashboardDto {
  institutionCount: number;
  programCount: number;
  activeTrackCount: number;
  activeBranchCount: number;
  draftCycleCount: number;
  closedCycleCount: number;
  applicationCount: number;
  activeCycle: AdmissionCycleDto | null;
  activeCycleOfferingCount: number;
  activeCycleCapacity: number;
}
