import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ReplacementRequestApi } from './replacement-request.api';

describe('ReplacementRequestApi', () => {
  let api: ReplacementRequestApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ReplacementRequestApi, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(ReplacementRequestApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads every backend page and normalizes numeric statuses', () => {
    let statuses: string[] = [];
    api.getAll().subscribe((requests) => {
      statuses = requests.map((request) => request.status);
    });

    const firstPage = http.expectOne(
      (request) =>
        request.url === '/api/ReplacementRequest/applicant/replacement-requests' &&
        request.params.get('Page') === '1' &&
        request.params.get('PageSize') === '100',
    );
    firstPage.flush(page([replacement('request-1', 1)], 1, 2));

    http
      .expectOne(
        (request) =>
          request.url === '/api/ReplacementRequest/applicant/replacement-requests' &&
          request.params.get('Page') === '2',
      )
      .flush(page([replacement('request-2', 2)], 2, 2));

    expect(statuses).toEqual(['Open', 'Fulfilled']);
  });

  it('loads one replacement request by id', () => {
    let id: string | undefined;
    api.getById('request-1').subscribe((request) => {
      id = request.id;
    });

    http
      .expectOne('/api/ReplacementRequest/applicant/replacement-requests/request-1')
      .flush(replacement('request-1', 'Open'));

    expect(id).toBe('request-1');
  });

  it('uploads the selected file as multipart form data', () => {
    const file = new File(['replacement'], 'national-id.pdf', {
      type: 'application/pdf',
    });
    api.upload('request-1', file).subscribe();

    const request = http.expectOne('/api/replacement-requests/request-1/upload');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeInstanceOf(FormData);
    const uploadedFile = (request.request.body as FormData).get('file') as File;
    expect(uploadedFile.name).toBe(file.name);
    expect(uploadedFile.type).toBe(file.type);
    request.flush({
      documentId: 'document-1',
      message: 'Uploaded',
      replacementStatus: 'Fulfilled',
      documentStatus: 'Verifying',
    });
  });

  function replacement(id: string, status: number | string) {
    return {
      id,
      documentId: 'document-1',
      documentType: 'NationalId',
      reason: 'Upload a clearer copy.',
      status,
      requestedAt: '2026-08-01T10:00:00Z',
    };
  }

  function page(data: object[], currentPage: number, totalPages: number) {
    return {
      data,
      filters: null,
      currentPage,
      totalPages,
      pageSize: 100,
      totalCount: data.length * totalPages,
    };
  }
});
