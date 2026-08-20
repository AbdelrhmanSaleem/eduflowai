export interface ApiResult<T> {
  isSuccess: boolean;
  data: T;
  statusCode: number;
  message: string;
}

export interface BranchOffering {
  offeringId: string;
  branchId: string;
  branchName: string;
  governorate: string | null;
  capacity: number;
}

export interface TrackLocation {
  branchId: string;
  branchName: string;
  governorate: string | null;
}

export interface TrackCatalogItem {
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
  locations: TrackLocation[];
  offerings: BranchOffering[];
}
