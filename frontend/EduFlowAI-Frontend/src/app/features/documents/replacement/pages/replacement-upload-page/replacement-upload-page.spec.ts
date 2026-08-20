import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';

import { ReplacementUploadPage } from './replacement-upload-page';

describe('ReplacementUploadPage', () => {
  let component: ReplacementUploadPage;
  let fixture: ComponentFixture<ReplacementUploadPage>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReplacementUploadPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => 'request-1' },
              queryParams: {},
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReplacementUploadPage);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http
      .expectOne('/api/replacement/ReplacementRequest/applicant/replacement-requests/request-1')
      .flush({
        id: 'request-1',
        documentId: 'document-1',
        documentType: 'NationalId',
        reason: 'Upload a valid copy.',
        status: 'Open',
        requestedAt: '2026-08-01T10:00:00Z',
      });
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('submits a valid selected file and shows the fulfilled state', () => {
    const file = new File(['replacement'], 'national-id.pdf', {
      type: 'application/pdf',
    });
    (component as any).acceptFile(file);
    (component as any).submit();

    const request = http.expectOne('/api/replacement-requests/request-1/upload');
    expect(request.request.method).toBe('POST');
    request.flush({
      documentId: 'document-1',
      message: 'Uploaded',
      replacementStatus: 'Fulfilled',
      documentStatus: 'Verifying',
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.fulfilled-state')).not.toBeNull();
  });
});
