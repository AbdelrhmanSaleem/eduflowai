export interface KnowledgeBaseDocument {
  documentId: string;
  fileName: string;
  status: string;
  errorMessage: string | null;
  createdAt: string;
}

export interface KnowledgeBaseDocumentStatus {
  documentId: string;
  fileName: string;
  status: string;
  errorMessage: string | null;
  chunkCount: number;
  createdAt: string;
}

export interface KnowledgeBaseSyncResult {
  totalDocuments: number;
  indexed: number;
  failed: number;
}

export interface AddKnowledgeBaseTextRequest {
  title?: string | null;
  content: string;
}