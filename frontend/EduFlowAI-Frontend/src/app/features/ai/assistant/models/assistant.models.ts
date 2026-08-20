export interface AssistantMessageRequest {
  message: string;
  sessionId: string | null;
  language: 'en' | 'ar';
  recommendation: RecommendationQuestionnaireProgress | null;
  recommendWithAvailableData: boolean;
}

export interface RecommendationQuestionnaireProgress {
  major: string | null;
  technicalCourses: string[] | null;
  skills: string[] | null;
  interests: string[] | null;
  preferredActivities: string[] | null;
  careerGoals: string[] | null;
  additionalContext: string | null;
  skippedFields: string[];
}

export interface AssistantResponse {
  sessionId: string;
  requiresClarification: boolean;
  clarificationMessage: string | null;
  results: AssistantResult[];
  timestamp: string;
}

export interface AssistantResult {
  intent:
    | 'knowledge'
    | 'recommendation'
    | 'application_status'
    | 'document_status'
    | 'unknown';

  title: string;
  content: string;
  sources: string[];
  metadata: AssistantResultMetadata;
}

export interface AssistantResultMetadata {
  state?: 'collecting_answers' | 'completed';

  questionKey?: RecommendationQuestionKey;
  questionNumber?: number;
  totalQuestions?: number;
  canRecommendNow?: boolean;

  missingContext?: string[];

  recommendations?: RecommendedTrack[];
  usedFallback?: boolean;
  basedOnAvailableData?: boolean;
  advisory?: boolean;
  advisoryNotice?: string;

  requiresLogin?: boolean;

  applicationFound?: boolean;
  applicationId?: string;
  currentStatus?: string;
  lastUpdatedAt?: string;
  statusMessage?: string | null;

  documentsFound?: boolean;
  documents?: ApplicantDocumentStatus[];

  [key: string]: unknown;
}

export type RecommendationQuestionKey =
  | 'major'
  | 'careerGoals'
  | 'interests'
  | 'skills';

export interface RecommendedTrack {
  trackId: string;
  trackName: string;
  rank: number;
  reason: string;
}

export interface ApplicantDocumentStatus {
  id?: string;
  fileName?: string;
  documentType?: string;
  status?: string;

  [key: string]: unknown;
}

export type ChatMessage =
  | UserChatMessage
  | AssistantChatMessage;

export interface UserChatMessage {
  id: string;
  role: 'user';
  content: string;
  timestamp: string;
}

export interface AssistantChatMessage {
  id: string;
  role: 'assistant';
  response: AssistantResponse;
  timestamp: string;
}