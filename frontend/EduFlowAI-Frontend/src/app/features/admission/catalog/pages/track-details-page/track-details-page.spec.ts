import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
  provideRouter,
} from '@angular/router';

import { RuntimeConfig } from '../../../../../core/config/runtime-config';
import { TrackDetailsPage } from './track-details-page';

describe('TrackDetailsPage', () => {
  let component: TrackDetailsPage;
  let fixture: ComponentFixture<TrackDetailsPage>;
  let http: HttpTestingController;

  const trackId = '11111111-1111-1111-1111-111111111111';

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrackDetailsPage],
      providers: [
        provideRouter([]),
        { provide: RuntimeConfig, useValue: { apiBaseUrl: '/api' } },
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ trackId }),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TrackDetailsPage);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates and loads the selected public track', () => {
    fixture.detectChanges();

    const request = http.expectOne(`/api/tracks/${trackId}`);
    request.flush({
      isSuccess: true,
      data: {
        id: trackId,
        programId: '22222222-2222-2222-2222-222222222222',
        officialTrackId: '59d2e6c7-7221-4024-fe29-08dbe75ac461',
        officialTrackUrl:
          'https://iti.gov.eg/intakes/de3fa682-88c3-45e1-aa0c-e42bf47d5071/tracks/59d2e6c7-7221-4024-fe29-08dbe75ac461',
        isOfficialIntake47: true,
        intake: 47,
        year: 2026,
        name: 'Industrial Automation',
        description: 'Build and maintain industrial automation systems.',
        category: 'Industrial Systems',
        totalHours: null,
        minimumGrade: 'Good',
        eligibilitySummary:
          'Applicants must satisfy the official academic eligibility requirements.',
        graduationYearLimitYears: 5,
        prerequisiteTopics: ['Control systems'],
        isActive: true,
        locations: [
          {
            branchId: 'smart-village',
            branchName: 'Smart Village',
            governorate: 'Giza',
          },
        ],
        offerings: [],
      },
      statusCode: 200,
      message: '',
    });
    fixture.detectChanges();

    expect(component).toBeTruthy();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('Industrial Automation');
    expect(root.textContent).toContain('Industrial Systems');
    expect(root.textContent).toContain('Not published');
    expect(root.textContent).toContain('Within the last 5 years');
    expect(root.textContent).toContain('Smart Village');
    expect(root.textContent).toContain('No active-cycle capacity configured');
    expect(
      root.querySelector<HTMLAnchorElement>('.details__official-link')?.href,
    ).toContain('59d2e6c7-7221-4024-fe29-08dbe75ac461');
  });
});
