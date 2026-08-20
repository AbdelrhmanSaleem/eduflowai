import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { RuntimeConfig } from '../../../../core/config/runtime-config';
import { DocumentType, RequirementGender } from '../models/admission-admin.model';
import { AdmissionAdminApiService } from './admission-admin-api.service';

const apiBaseUrl = 'https://admission.example.test/api';

describe('AdmissionAdminApiService', () => {
  let service: AdmissionAdminApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdmissionAdminApiService,
        { provide: RuntimeConfig, useValue: { apiBaseUrl } },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(AdmissionAdminApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the Super Admin dashboard for the selected program', () => {
    const programId = '11111111-1111-1111-1111-111111111111';

    service.getDashboard(programId).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/admin/dashboard?programId=${programId}`);
    expect(request.request.method).toBe('GET');
    request.flush({ isSuccess: true, data: {}, statusCode: 200, message: '' });
  });

  it('normalizes additive track fields from an older backend response', () => {
    let locations: readonly unknown[] | undefined;

    service.getTracks().subscribe((tracks) => {
      locations = tracks[0]?.locations;
      expect(tracks[0]).toEqual(
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
          offerings: [],
        }),
      );
    });

    const request = http.expectOne(`${apiBaseUrl}/admin/tracks`);
    request.flush({
      isSuccess: true,
      data: [
        {
          id: '11111111-1111-1111-1111-111111111111',
          programId: '22222222-2222-2222-2222-222222222222',
          name: 'Legacy track',
          description: null,
          isActive: true,
        },
      ],
      statusCode: 200,
      message: '',
    });

    expect(locations).toEqual([]);
  });

  it('deletes the selected program and its unlocked configuration', () => {
    const programId = '11111111-1111-1111-1111-111111111111';

    service.deleteProgram(programId).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/admin/programs/${programId}`);
    expect(request.request.method).toBe('DELETE');
    expect(request.request.body).toBeNull();
    request.flush({ isSuccess: true, data: true, statusCode: 200, message: '' });
  });

  it('replaces program document requirements with the backend contract', () => {
    const programId = '11111111-1111-1111-1111-111111111111';
    const body = {
      requirements: [
        {
          documentType: DocumentType.MilitaryCertificate,
          requiredForGender: RequirementGender.Male,
        },
      ],
    };

    service.updateProgramRequirements(programId, body).subscribe();

    const request = http.expectOne(
      `${apiBaseUrl}/admin/programs/${programId}/document-requirements`,
    );
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush({ isSuccess: true, data: [], statusCode: 200, message: '' });
  });

  it('creates one offering without resending existing offerings', () => {
    const cycleId = '22222222-2222-2222-2222-222222222222';
    const body = {
      trackId: '33333333-3333-3333-3333-333333333333',
      branchId: '44444444-4444-4444-4444-444444444444',
      capacity: 40,
    };

    service.createOffering(cycleId, body).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/admin/cycles/${cycleId}/offerings`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ isSuccess: true, data: {}, statusCode: 201, message: '' });
  });

  it('updates only the selected offering capacity', () => {
    const cycleId = '22222222-2222-2222-2222-222222222222';
    const offeringId = '55555555-5555-5555-5555-555555555555';
    const body = { capacity: 75 };

    service.updateOffering(cycleId, offeringId, body).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/admin/cycles/${cycleId}/offerings/${offeringId}`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush({ isSuccess: true, data: {}, statusCode: 200, message: '' });
  });

  it('deletes only the selected offering', () => {
    const cycleId = '22222222-2222-2222-2222-222222222222';
    const offeringId = '55555555-5555-5555-5555-555555555555';

    service.deleteOffering(cycleId, offeringId).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/admin/cycles/${cycleId}/offerings/${offeringId}`);
    expect(request.request.method).toBe('DELETE');
    expect(request.request.body).toBeNull();
    request.flush({ isSuccess: true, data: true, statusCode: 200, message: '' });
  });

  it('activates a cycle with the expected endpoint', () => {
    const cycleId = '66666666-6666-6666-6666-666666666666';

    service.activateCycle(cycleId).subscribe();

    const request = http.expectOne(`${apiBaseUrl}/admin/cycles/${cycleId}/activate`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush({ isSuccess: true, data: {}, statusCode: 200, message: '' });
  });
});
