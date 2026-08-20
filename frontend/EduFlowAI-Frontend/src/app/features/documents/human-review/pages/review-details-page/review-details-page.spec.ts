import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';

import { ReviewDetailsPage } from './review-details-page';

describe('ReviewDetailsPage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReviewDetailsPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ documentId: 'document-1' }) },
          },
        },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads details and safely handles a file error', () => {
    const fixture = TestBed.createComponent(ReviewDetailsPage);
    fixture.detectChanges();

    http
      .expectOne((request) => request.url.endsWith('/document-review/document-1'))
      .flush({
        documentId: 'document-1',
        applicationId: 'application-1',
        applicantId: 'applicant-1',
        documentType: 'NationalId',
        originalFileName: 'national-id.png',
        status: 'NeedsHumanReview',
        verificationDetailsJson: null,
      });
    http
      .expectOne((request) => request.url.endsWith('/document-file/document-1'))
      .flush(null, {
        status: 404,
        statusText: 'Not Found',
      });
    fixture.detectChanges();

    expect(fixture.componentInstance).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Verification details unavailable');
    expect(fixture.nativeElement.textContent).not.toContain('Reviewer notes');
  });

  it('shows the replacement-requested state and removes review actions after sending', () => {
    const fixture = TestBed.createComponent(ReviewDetailsPage);
    fixture.detectChanges();

    http
      .expectOne((request) => request.url.endsWith('/document-review/document-1'))
      .flush({
        documentId: 'document-1',
        applicationId: 'application-1',
        applicantId: 'applicant-1',
        documentType: 'NationalId',
        originalFileName: 'national-id.png',
        status: 'NeedsHumanReview',
        verificationDetailsJson: null,
      });
    http
      .expectOne((request) => request.url.endsWith('/document-file/document-1'))
      .flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    element.querySelector<HTMLButtonElement>('.review-actions .button--primary')?.click();
    fixture.detectChanges();
    submitReason(element, 'The uploaded document is expired.');

    const request = http.expectOne('/api/ReplacementRequest/send-replacement-request');
    expect(request.request.body).toEqual({
      documentId: 'document-1',
      applicantId: 'applicant-1',
      reason: 'The uploaded document is expired.',
    });
    request.flush('Requested');
    fixture.detectChanges();

    expect(element.textContent).toContain('Replacement requested');
    expect(element.textContent).toContain('Replacement request was sent.');
    expect(element.querySelector('.button--approve')).toBeNull();
    expect(element.querySelector('.button--reject')).toBeNull();
  });

  it('submits the rejection reason from the reason dialog', () => {
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(ReviewDetailsPage);
    fixture.detectChanges();

    http
      .expectOne((request) => request.url.endsWith('/document-review/document-1'))
      .flush({
        documentId: 'document-1',
        applicationId: 'application-1',
        applicantId: 'applicant-1',
        documentType: 'NationalId',
        originalFileName: 'national-id.png',
        status: 'NeedsHumanReview',
        verificationDetailsJson: null,
      });
    http
      .expectOne((request) => request.url.endsWith('/document-file/document-1'))
      .flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    element.querySelector<HTMLButtonElement>('.review-actions .button--reject')?.click();
    fixture.detectChanges();
    submitReason(element, 'The uploaded document is too blurry.');

    const request = http.expectOne((candidate) =>
      candidate.url.endsWith('/reject-review/document-1'),
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    expect(request.request.params.get('reason')).toBe('The uploaded document is too blurry.');
    request.flush('Rejected');
  });

  it('revokes the document object URL when destroyed', () => {
    const createObjectUrl = vi.fn(() => 'blob:document-preview');
    const revokeObjectUrl = vi.fn();
    const originalCreate = URL.createObjectURL;
    const originalRevoke = URL.revokeObjectURL;
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: createObjectUrl });
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: revokeObjectUrl });

    try {
      const fixture = TestBed.createComponent(ReviewDetailsPage);
      fixture.detectChanges();
      http
        .expectOne((request) => request.url.endsWith('/document-review/document-1'))
        .flush({
          documentId: 'document-1',
          applicationId: 'application-1',
          applicantId: 'applicant-1',
          documentType: 'NationalId',
          originalFileName: 'national-id.png',
          status: 'NeedsHumanReview',
          verificationDetailsJson: null,
        });
      http
        .expectOne((request) => request.url.endsWith('/document-file/document-1'))
        .flush(new Blob(['image'], { type: 'image/png' }), {
          headers: { 'Content-Type': 'image/png' },
        });

      fixture.destroy();
      expect(createObjectUrl).toHaveBeenCalledOnce();
      expect(revokeObjectUrl).toHaveBeenCalledWith('blob:document-preview');
    } finally {
      Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: originalCreate });
      Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: originalRevoke });
    }
  });
});

function submitReason(element: HTMLElement, reason: string): void {
  const textarea = element.querySelector<HTMLTextAreaElement>('app-reason-dialog textarea');
  expect(textarea).toBeTruthy();
  textarea!.value = reason;
  textarea!.dispatchEvent(new Event('input', { bubbles: true }));
  element
    .querySelector<HTMLFormElement>('app-reason-dialog form')
    ?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
}
