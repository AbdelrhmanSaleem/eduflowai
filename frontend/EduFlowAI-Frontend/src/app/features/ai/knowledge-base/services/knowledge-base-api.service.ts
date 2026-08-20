import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import {
  AddKnowledgeBaseTextRequest,
  KnowledgeBaseDocument,
  KnowledgeBaseDocumentStatus,
  KnowledgeBaseSyncResult,
} from '../models/knowledge-base.models';

@Injectable({
  providedIn: 'root',
})
export class KnowledgeBaseApiService {
  private readonly http = inject(HttpClient);

  // private readonly baseUrl = '/api/admin/knowledge-base';
  private readonly baseUrl = `${environment.apiUrl}/admin/knowledge-base`;

  getDocuments(): Observable<KnowledgeBaseDocument[]> {
    return this.http.get<KnowledgeBaseDocument[]>(
      this.baseUrl,
    );
  }

  getDocumentStatus(
    documentId: string,
  ): Observable<KnowledgeBaseDocumentStatus> {
    return this.http.get<KnowledgeBaseDocumentStatus>(
      `${this.baseUrl}/${documentId}/status`,
    );
  }

  uploadFile(file: File): Observable<string> {
    const formData = new FormData();

    formData.append('file', file);

    return this.http.post<string>(
      `${this.baseUrl}/upload`,
      formData,
    );
  }

  addText(
    request: AddKnowledgeBaseTextRequest,
  ): Observable<string> {
    return this.http.post<string>(
      `${this.baseUrl}/text`,
      request,
    );
  }

  deleteDocument(
    documentId: string,
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/${documentId}`,
    );
  }

  syncAll(): Observable<KnowledgeBaseSyncResult> {
    return this.http.post<KnowledgeBaseSyncResult>(
      `${this.baseUrl}/sync`,
      {},
    );
  }
}