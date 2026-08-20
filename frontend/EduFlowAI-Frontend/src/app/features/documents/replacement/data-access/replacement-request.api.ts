import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { forkJoin, map, Observable, of, switchMap } from 'rxjs';
import { RuntimeConfig } from '../../../../core/config/runtime-config';
import {
  PaginatedReplacementRequests,
  ReplacementRequestItem,
  ReplacementRequestStatus,
  ReplacementUploadResponse,
} from '../models/replacement-request.model';

type RawReplacementRequest = Omit<ReplacementRequestItem, 'status'> & {
  status: ReplacementRequestStatus | number | string;
};

type RawPage = Omit<PaginatedReplacementRequests, 'data'> & {
  data: RawReplacementRequest[];
};

const STATUS_BY_VALUE: Record<number, ReplacementRequestStatus> = {
  0: 'None',
  1: 'Open',
  2: 'Fulfilled',
};

@Injectable({ providedIn: 'root' })
export class ReplacementRequestApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(RuntimeConfig).apiBaseUrl;
  private readonly applicantRequestsUrl =
    `${this.apiBaseUrl}/ReplacementRequest/applicant/replacement-requests`;

  getAll(): Observable<ReplacementRequestItem[]> {
    const pageSize = 100;

    return this.getPage(1, pageSize).pipe(
      switchMap((firstPage) => {
        if (firstPage.totalPages <= 1) {
          return of(firstPage.data);
        }

        const remainingPages = Array.from({ length: firstPage.totalPages - 1 }, (_, index) =>
          this.getPage(index + 2, pageSize),
        );

        return forkJoin(remainingPages).pipe(
          map((pages) => [firstPage, ...pages].flatMap((page) => page.data)),
        );
      }),
    );
  }

  getById(requestId: string): Observable<ReplacementRequestItem> {
    return this.http
      .get<RawReplacementRequest>(`${this.applicantRequestsUrl}/${requestId}`)
      .pipe(map(normalizeRequest));
  }

  upload(requestId: string, file: File): Observable<ReplacementUploadResponse> {
    const body = new FormData();
    body.append('file', file, file.name);

    return this.http.post<ReplacementUploadResponse>(
      `${this.apiBaseUrl}/replacement-requests/${requestId}/upload`,
      body,
    );
  }

  private getPage(page: number, pageSize: number): Observable<PaginatedReplacementRequests> {
    const params = new HttpParams().set('Page', page).set('PageSize', pageSize);

    return this.http.get<RawPage>(this.applicantRequestsUrl, { params }).pipe(
      map((result) => ({
        ...result,
        data: result.data.map(normalizeRequest),
      })),
    );
  }
}

function normalizeRequest(request: RawReplacementRequest): ReplacementRequestItem {
  return {
    ...request,
    documentType: request.documentType || null,
    status: normalizeStatus(request.status),
  };
}

function normalizeStatus(
  value: ReplacementRequestStatus | number | string,
): ReplacementRequestStatus {
  if (typeof value === 'number') {
    return STATUS_BY_VALUE[value] ?? 'None';
  }

  if (/^\d+$/.test(value)) {
    return STATUS_BY_VALUE[Number(value)] ?? 'None';
  }

  return Object.values(STATUS_BY_VALUE).includes(value as ReplacementRequestStatus)
    ? (value as ReplacementRequestStatus)
    : 'None';
}
