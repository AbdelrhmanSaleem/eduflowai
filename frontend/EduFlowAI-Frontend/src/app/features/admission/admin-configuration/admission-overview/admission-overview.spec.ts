import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { RuntimeConfig } from '../../../../core/config/runtime-config';
import { AdmissionOverview } from './admission-overview';

const apiBaseUrl = 'https://admission.example.test/api';

describe('AdmissionOverview', () => {
  let component: AdmissionOverview;
  let fixture: ComponentFixture<AdmissionOverview>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdmissionOverview],
      providers: [
        { provide: RuntimeConfig, useValue: { apiBaseUrl } },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdmissionOverview);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads dashboard counts for the first available program', () => {
    const programId = '11111111-1111-1111-1111-111111111111';

    fixture.detectChanges();

    const programsRequest = http.expectOne(`${apiBaseUrl}/admin/programs`);
    programsRequest.flush({
      isSuccess: true,
      data: [
        {
          id: programId,
          institutionId: '22222222-2222-2222-2222-222222222222',
          institutionName: 'ITI',
          name: '9-Month Program',
          code: '9M',
          durationMonths: 9,
          trackCount: 10,
          cycleCount: 1,
        },
      ],
      statusCode: 200,
      message: '',
    });

    const dashboardRequest = http.expectOne(
      `${apiBaseUrl}/admin/dashboard?programId=${programId}`,
    );
    dashboardRequest.flush({
      isSuccess: true,
      data: {
        institutionCount: 1,
        programCount: 1,
        activeTrackCount: 10,
        activeBranchCount: 5,
        draftCycleCount: 1,
        closedCycleCount: 0,
        applicationCount: 3,
        activeCycle: null,
        activeCycleOfferingCount: 0,
        activeCycleCapacity: 0,
      },
      statusCode: 200,
      message: '',
    });
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Admission Overview');
    expect(
      (fixture.nativeElement.querySelector('select') as HTMLSelectElement).value,
    ).toBe(programId);
  });
});
