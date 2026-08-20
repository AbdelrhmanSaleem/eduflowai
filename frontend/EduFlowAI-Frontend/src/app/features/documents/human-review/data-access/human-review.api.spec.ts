import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { HumanReviewApi } from './human-review.api';

describe('HumanReviewApi', () => {
  let api: HumanReviewApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [HumanReviewApi, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(HumanReviewApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the configured backend URL and supported queue query parameters', () => {
    api
      .getReviews({
        page: 2,
        pageSize: 10,
        search: 'sarah',
        type: 'NationalId',
        status: 'NeedsHumanReview',
      })
      .subscribe();

    const request = http.expectOne((candidate) => candidate.url.endsWith('/document-reviews'));
    expect(request.request.url).toContain('/api/operations/HumanReview/document-reviews');
    expect(request.request.url.startsWith('http')).toBe(true);
    expect(request.request.params.get('Page')).toBe('2');
    expect(request.request.params.get('PageSize')).toBe('10');
    expect(request.request.params.get('Search')).toBe('sarah');
    expect(request.request.params.get('Type')).toBe('NationalId');
    expect(request.request.params.get('Status')).toBe('NeedsHumanReview');
    expect(request.request.params.has('Sort')).toBe(false);
    request.flush({ data: [], currentPage: 2, totalPages: 2, pageSize: 10, totalCount: 12 });
  });

  it('sends the rejection reason as the current controller query parameter', () => {
    api.reject('document-1', 'The scan is too blurry to verify.').subscribe();

    const request = http.expectOne((candidate) =>
      candidate.url.endsWith('/reject-review/document-1'),
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    expect(request.request.params.get('reason')).toBe('The scan is too blurry to verify.');
    request.flush('Rejected');
  });

  it('maps only the exact replacement request DTO fields', () => {
    api
      .requestReplacement({
        documentId: 'document-1',
        applicantId: 'applicant-1',
        reason: 'The document has expired.',
      })
      .subscribe();

    const request = http.expectOne((candidate) =>
      candidate.url.endsWith('/api/ReplacementRequest/send-replacement-request'),
    );
    expect(request.request.url.startsWith('http')).toBe(true);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      documentId: 'document-1',
      applicantId: 'applicant-1',
      reason: 'The document has expired.',
    });
    request.flush('Requested');
  });
});
