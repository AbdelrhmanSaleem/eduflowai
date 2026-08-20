import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { RuntimeConfig } from '../../../../core/config/runtime-config';
import {
  DocumentReviewDto,
  DocumentStatus,
  DocumentType,
  HumanReviewDto,
  HumanReviewQuery,
  PaginatedResult,
  ReplacementRequestDto,
} from '../models/human-review.model';

type RawReview = Omit<HumanReviewDto, 'documentType' | 'status' | 'applicantName'> & {
  documentType: DocumentType | number | string;
  status: DocumentStatus | number | string;
  applicantName?: string | null;
};

type RawReviewDetails = Omit<DocumentReviewDto, 'documentType' | 'status'> & {
  documentType: DocumentType | number | string;
  status: DocumentStatus | number | string;
};

type RawPage = Omit<PaginatedResult<HumanReviewDto>, 'data'> & { data: RawReview[] };

const DOCUMENT_TYPE_BY_VALUE: Record<number, DocumentType> = {
  0: 'None',
  1: 'NationalId',
  2: 'BirthCertificate',
  3: 'GraduationCertificate',
  4: 'MilitaryCertificate',
};

const DOCUMENT_STATUS_BY_VALUE: Record<number, DocumentStatus> = {
  0: 'None',
  1: 'Uploaded',
  2: 'Verifying',
  3: 'Approved',
  4: 'NeedsHumanReview',
  5: 'ReplacementRequested',
  6: 'Rejected',
};

@Injectable({ providedIn: 'root' })
export class HumanReviewApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(RuntimeConfig).apiBaseUrl;
  private readonly baseUrl = `${this.apiBaseUrl}/operations/HumanReview`;

  getReviews(query: HumanReviewQuery): Observable<PaginatedResult<HumanReviewDto>> {
    let params = new HttpParams().set('Page', query.page).set('PageSize', query.pageSize);

    if (query.search.trim()) {
      params = params.set('Search', query.search.trim());
    }
    if (query.status) {
      params = params.set('Status', query.status);
    }
    if (query.type) {
      params = params.set('Type', query.type);
    }

    return this.http.get<RawPage>(`${this.baseUrl}/document-reviews`, { params }).pipe(
      map((page) => ({
        ...page,
        data: page.data.map(normalizeReview),
      })),
    );
  }

  getReview(documentId: string): Observable<DocumentReviewDto> {
    return this.http
      .get<RawReviewDetails>(`${this.baseUrl}/document-review/${documentId}`)
      .pipe(map(normalizeReviewDetails));
  }

  getDocumentFile(documentId: string): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/document-file/${documentId}`, {
      observe: 'response',
      responseType: 'blob',
    });
  }

  approve(documentId: string): Observable<string> {
    return this.http.post(`${this.baseUrl}/approve-review/${documentId}`, null, {
      responseType: 'text',
    });
  }

  reject(documentId: string, reason: string): Observable<string> {
    const params = new HttpParams().set('reason', reason);
    return this.http.post(`${this.baseUrl}/reject-review/${documentId}`, null, {
      params,
      responseType: 'text',
    });
  }

  requestReplacement(request: ReplacementRequestDto): Observable<string> {
    return this.http.post(
      `${this.apiBaseUrl}/ReplacementRequest/send-replacement-request`,
      request,
      {
        responseType: 'text',
      },
    );
  }
}

function normalizeReview(value: RawReview): HumanReviewDto {
  return {
    documentId: value.documentId,
    documentType: normalizeEnum(value.documentType, DOCUMENT_TYPE_BY_VALUE, 'None'),
    applicantName: typeof value.applicantName === 'string' ? value.applicantName : '',
    originalFileName: value.originalFileName,
    status: normalizeEnum(value.status, DOCUMENT_STATUS_BY_VALUE, 'None'),
    verificationDetailsJson: value.verificationDetailsJson,
  };
}

function normalizeReviewDetails(value: RawReviewDetails): DocumentReviewDto {
  return {
    documentId: value.documentId,
    applicationId: value.applicationId,
    applicantId: value.applicantId,
    documentType: normalizeEnum(value.documentType, DOCUMENT_TYPE_BY_VALUE, 'None'),
    originalFileName: value.originalFileName,
    status: normalizeEnum(value.status, DOCUMENT_STATUS_BY_VALUE, 'None'),
    verificationDetailsJson: value.verificationDetailsJson,
  };
}

function normalizeEnum<T extends string>(
  value: T | number | string,
  values: Record<number, T>,
  fallback: T,
): T {
  if (typeof value === 'number' || /^\d+$/.test(value)) {
    return values[Number(value)] ?? fallback;
  }

  return Object.values(values).includes(value as T) ? (value as T) : fallback;
}
