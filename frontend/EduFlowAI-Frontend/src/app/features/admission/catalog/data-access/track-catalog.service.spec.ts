import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { RuntimeConfig } from '../../../../core/config/runtime-config';
import { TrackCatalogItem } from '../models/track-catalog.model';
import { TrackCatalogService } from './track-catalog.service';

const apiBaseUrl = 'https://admission.example.test/api';

describe('TrackCatalogService', () => {
  let service: TrackCatalogService;
  let http: HttpTestingController;

  const track: TrackCatalogItem = {
    id: '11111111-1111-1111-1111-111111111111',
    programId: '22222222-2222-2222-2222-222222222222',
    officialTrackId: 'cf198247-911f-4df1-a66e-70beb76cfc09',
    officialTrackUrl:
      'https://iti.gov.eg/intakes/de3fa682-88c3-45e1-aa0c-e42bf47d5071/tracks/cf198247-911f-4df1-a66e-70beb76cfc09',
    isOfficialIntake47: true,
    intake: 47,
    year: 2026,
    name: '.NET Enterprise Solutions Development & Architecture Foundations with AI Integration',
    description: 'Build production-ready web applications.',
    category: 'Software Engineering & Agentic AI Development',
    totalHours: 1313,
    minimumGrade: 'Good',
    eligibilitySummary: 'Open to eligible graduates from relevant disciplines.',
    graduationYearLimitYears: null,
    prerequisiteTopics: ['C#', 'SQL'],
    isActive: true,
    locations: [
      { branchId: 'assiut', branchName: 'Assiut', governorate: 'Assiut' },
      { branchId: 'aswan', branchName: 'Aswan', governorate: 'Aswan' },
      {
        branchId: 'smart-village',
        branchName: 'Smart Village',
        governorate: 'Giza',
      },
      { branchId: 'mansoura', branchName: 'Mansoura', governorate: 'Dakahlia' },
      { branchId: 'menofia', branchName: 'Menofia', governorate: 'Menofia' },
      {
        branchId: 'alexandria',
        branchName: 'Alexandria',
        governorate: 'Alexandria',
      },
      { branchId: 'ismailia', branchName: 'Ismailia', governorate: 'Ismailia' },
      { branchId: 'tanta', branchName: 'Tanta', governorate: 'Gharbia' },
    ],
    offerings: [
      {
        offeringId: '33333333-3333-3333-3333-333333333333',
        branchId: 'alexandria',
        branchName: 'Alexandria',
        governorate: 'Alexandria',
        capacity: 40,
      },
    ],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TrackCatalogService,
        { provide: RuntimeConfig, useValue: { apiBaseUrl } },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(TrackCatalogService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads and unwraps the public track catalog', () => {
    let result: readonly TrackCatalogItem[] | undefined;

    service.getTracks().subscribe((tracks) => {
      result = tracks;
    });

    const request = http.expectOne(`${apiBaseUrl}/tracks`);
    expect(request.request.method).toBe('GET');

    request.flush({
      isSuccess: true,
      data: [track],
      statusCode: 200,
      message: '',
    });

    expect(result).toEqual([track]);
  });

  it('loads and unwraps one public track', () => {
    let result: TrackCatalogItem | undefined;

    service.getTrack(track.id).subscribe((item) => {
      result = item;
    });

    const request = http.expectOne(`${apiBaseUrl}/tracks/${track.id}`);
    expect(request.request.method).toBe('GET');

    request.flush({
      isSuccess: true,
      data: track,
      statusCode: 200,
      message: '',
    });

    expect(result).toEqual(track);
  });

  it('normalizes additive catalog fields from an older backend response', () => {
    let result: readonly TrackCatalogItem[] | undefined;
    const legacyTrack = {
      id: track.id,
      programId: track.programId,
      name: 'Legacy track',
      description: null,
      prerequisiteTopics: undefined,
      isActive: true,
      locations: [],
      offerings: track.offerings,
    } as unknown as TrackCatalogItem;

    service.getTracks().subscribe((tracks) => {
      result = tracks;
    });

    const request = http.expectOne(`${apiBaseUrl}/tracks`);
    request.flush({
      isSuccess: true,
      data: [legacyTrack],
      statusCode: 200,
      message: '',
    });

    expect(result?.[0]).toEqual(
      expect.objectContaining({
        officialTrackId: null,
        officialTrackUrl: null,
        isOfficialIntake47: false,
        intake: null,
        year: null,
        category: null,
        totalHours: null,
        minimumGrade: null,
        eligibilitySummary: null,
        graduationYearLimitYears: null,
        prerequisiteTopics: [],
        locations: [
          {
            branchId: 'alexandria',
            branchName: 'Alexandria',
            governorate: 'Alexandria',
          },
        ],
      }),
    );
  });
});
