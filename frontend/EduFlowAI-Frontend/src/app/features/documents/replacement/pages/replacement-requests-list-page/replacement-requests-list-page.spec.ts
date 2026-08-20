import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ReplacementRequestsListPage } from './replacement-requests-list-page';

describe('ReplacementRequestsListPage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReplacementRequestsListPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('keeps document search and status filtering working together', async () => {
    const fixture = TestBed.createComponent(ReplacementRequestsListPage);
    fixture.detectChanges();

    http
      .expectOne((request) => request.params.get('Page') === '1')
      .flush({
        data: [
          replacement('request-1', 'NationalId', 'Open'),
          replacement('request-2', 'NationalId', 'Fulfilled'),
          replacement('request-3', 'BirthCertificate', 'Open'),
        ],
        filters: null,
        currentPage: 1,
        totalPages: 1,
        pageSize: 100,
        totalCount: 3,
      });
    fixture.detectChanges();

    const component = fixture.componentInstance as any;
    component.searchControl.setValue('national');
    component.statusControl.setValue('Fulfilled');
    await new Promise((resolve) => setTimeout(resolve, 275));
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('.request-card');
    expect(cards).toHaveLength(1);
    expect(cards[0].textContent).toContain('National ID');
    expect(cards[0].textContent).toContain('Fulfilled');
  });

  function replacement(id: string, documentType: string, status: string) {
    return {
      id,
      documentId: `document-${id}`,
      documentType,
      reason: 'Please upload a new copy.',
      status,
      requestedAt: '2026-08-01T10:00:00Z',
    };
  }
});
