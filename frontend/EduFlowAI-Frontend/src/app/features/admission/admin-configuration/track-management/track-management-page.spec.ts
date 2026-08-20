import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AdmissionAdminCopy } from '../admission-admin.copy';
import { AdmissionAdminApiService } from '../data-access/admission-admin-api.service';
import { AdminTrackDto, ProgramDto } from '../models/admission-admin.model';
import { TrackManagementPage } from './track-management-page';

describe('TrackManagementPage', () => {
  const program: ProgramDto = {
    id: 'iti-9m',
    institutionId: 'iti',
    institutionName: 'Information Technology Institute',
    name: '9-Month Professional Training Program',
    code: '9M',
    durationMonths: 9,
    trackCount: 31,
    cycleCount: 1,
  };

  const track: AdminTrackDto = {
    id: 'industrial-automation',
    programId: program.id,
    officialTrackId: '59d2e6c7-7221-4024-fe29-08dbe75ac461',
    officialTrackUrl: 'https://iti.gov.eg/tracks/industrial-automation',
    isOfficialIntake47: true,
    intake: 47,
    year: 2026,
    name: 'Industrial Automation',
    description: 'Official description',
    category: 'Industrial Systems',
    totalHours: null,
    minimumGrade: 'Good',
    eligibilitySummary: 'Official eligibility',
    graduationYearLimitYears: 5,
    prerequisiteTopics: [],
    isActive: true,
    locations: [
      {
        branchId: 'smart-village',
        branchName: 'Smart Village',
        governorate: 'Giza',
      },
    ],
    offerings: [],
  };

  afterEach(() => TestBed.resetTestingModule());

  it('keeps official 9M tracks editable and allows custom track creation in that program', async () => {
    await TestBed.configureTestingModule({
      imports: [TrackManagementPage],
      providers: [
        {
          provide: AdmissionAdminApiService,
          useValue: {
            getPrograms: () => of([program]),
            getTracks: () => of([track]),
          },
        },
        {
          provide: AdmissionAdminCopy,
          useValue: {
            text: (english: string) => english,
            isRtl: () => false,
          },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(TrackManagementPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(root.querySelectorAll<HTMLButtonElement>('button'));
    const addTrack = buttons.find((button) => button.textContent?.includes('Add Track'));

    expect(root.textContent).toContain('Industrial Automation');
    expect(root.textContent).not.toContain('Source-managed');
    expect(addTrack?.disabled).toBe(false);
    expect(buttons.some((button) => button.textContent?.includes('Edit'))).toBe(true);
  });
});
