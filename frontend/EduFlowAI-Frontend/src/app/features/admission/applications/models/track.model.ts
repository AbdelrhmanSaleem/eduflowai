export interface BranchOfferingDto {
    offeringId: string;
    branchId: string;
    branchName: string;
    governorate: string | null;
    capacity: number;
}

export interface TrackDto {
    id: string;
    programId: string;
    name: string;
    description: string | null;
    prerequisiteTopics: string[];
    isActive: boolean;
    offerings: BranchOfferingDto[];
}