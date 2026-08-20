import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ReviewQueuePage } from './review-queue-page';

describe('ReviewQueuePage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReviewQueuePage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('renders the clear queue state', () => {
    const fixture = TestBed.createComponent(ReviewQueuePage);
    fixture.detectChanges();

    http
      .expectOne((request) => request.url.endsWith('/document-reviews'))
      .flush({
        data: [],
        filters: null,
        currentPage: 1,
        totalPages: 0,
        pageSize: 10,
        totalCount: 0,
      });
    fixture.detectChanges();

    expect(fixture.componentInstance).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Queue is clear');
  });

  it('shows only the required columns and review statuses', () => {
    const fixture = TestBed.createComponent(ReviewQueuePage);
    fixture.detectChanges();

    http
      .expectOne((request) => request.url.endsWith('/document-reviews'))
      .flush({
        data: [
          {
            documentId: 'document-1',
            documentType: 'NationalId',
            applicantName: 'Sarah Ahmed',
            originalFileName: 'national-id.png',
            status: 5,
            verificationDetailsJson: null,
          },
        ],
        filters: null,
        currentPage: 1,
        totalPages: 1,
        pageSize: 10,
        totalCount: 1,
      });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const headings = Array.from(element.querySelectorAll('thead th'), (heading) =>
      heading.textContent?.trim(),
    );
    const statusSelect = element.querySelectorAll<HTMLSelectElement>('.select-field select')[1];
    const statusOptions = Array.from(statusSelect.options, (option) => option.text.trim());

    expect(headings).toEqual(['Applicant name', 'Document type', 'Status', 'Action']);
    expect(statusOptions).toEqual(['All statuses', 'Needs human review', 'Replacement requested']);
    expect(element.querySelector('.status-pill')?.textContent).toContain('Replacement requested');
    expect(element.textContent).not.toContain('Original filename');
  });
});
