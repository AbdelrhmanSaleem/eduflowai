import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { ApplicationService } from '../../data-access/application.service';
import { ApplicationsStore } from '../../data-access/applications.store';
import { ApplicantDashboardPage } from './applicant-dashboard-page';

describe('ApplicantDashboardPage', () => {
  let component: ApplicantDashboardPage;
  let fixture: ComponentFixture<ApplicantDashboardPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicantDashboardPage],
      providers: [
        ApplicationsStore,
        {
          provide: ApplicationService,
          useValue: {
            getDashboardSummary: () => of({}),
          },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ applicationId: 'application-1' }),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicantDashboardPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
