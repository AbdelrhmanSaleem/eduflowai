import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { RuntimeConfig } from '../../../../../core/config/runtime-config';
import { TrackCatalogItem } from '../../models/track-catalog.model';
import { TrackCatalogPage } from './track-catalog-page';

const dotNetTrack: TrackCatalogItem = {
  id: 'dotnet-track',
  programId: 'professional-program',
  officialTrackId: 'cf198247-911f-4df1-a66e-70beb76cfc09',
  officialTrackUrl:
    'https://iti.gov.eg/intakes/de3fa682-88c3-45e1-aa0c-e42bf47d5071/tracks/cf198247-911f-4df1-a66e-70beb76cfc09',
  isOfficialIntake47: true,
  intake: 47,
  year: 2026,
  name: '.NET Enterprise Solutions Development & Architecture Foundations with AI Integration',
  description: 'Build enterprise solutions with .NET and AI integration.',
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
  offerings: [],
};

const industrialAutomationTrack: TrackCatalogItem = {
  id: 'industrial-automation',
  programId: 'professional-program',
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
  eligibilitySummary: 'Applicants must satisfy the official academic requirements.',
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
};

describe('TrackCatalogPage', () => {
  let component: TrackCatalogPage;
  let fixture: ComponentFixture<TrackCatalogPage>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrackCatalogPage],
      providers: [
        provideRouter([]),
        { provide: RuntimeConfig, useValue: { apiBaseUrl: '/api' } },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TrackCatalogPage);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates and loads the public catalog', () => {
    fixture.detectChanges();

    const request = http.expectOne('/api/tracks');
    request.flush({
      isSuccess: true,
      data: [],
      statusCode: 200,
      message: '',
    });
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('No tracks are available yet');
  });

  it('renders official metadata, nullable hours, and canonical multi-location data', () => {
    fixture.detectChanges();

    const request = http.expectOne('/api/tracks');
    request.flush({
      isSuccess: true,
      data: [dotNetTrack, industrialAutomationTrack],
      statusCode: 200,
      message: '',
    });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const cards = Array.from(root.querySelectorAll<HTMLElement>('.track-card'));
    const dotNetCard = cards.find((card) => card.textContent?.includes(dotNetTrack.name));
    const industrialCard = cards.find((card) =>
      card.textContent?.includes(industrialAutomationTrack.name),
    );

    expect(dotNetCard?.textContent).toContain('Software Engineering & Agentic AI Development');
    expect(dotNetCard?.textContent).toContain('1,313 hours');
    expect(dotNetCard?.textContent).toContain('8 locations');
    expect(industrialCard?.textContent).toContain('Industrial Systems');
    expect(industrialCard?.textContent).toContain('Not published');
    expect(industrialCard?.textContent).toContain('No active-cycle capacity configured');

    const search = root.querySelector<HTMLInputElement>('#track-search');
    search!.value = 'Industrial Systems';
    search!.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(root.querySelectorAll('.track-card')).toHaveLength(1);
    expect(root.textContent).toContain('Industrial Automation');
  });

  it('renders a nonofficial offered track with available-branch semantics', () => {
    const customTrack: TrackCatalogItem = {
      id: 'custom-track',
      programId: 'custom-program',
      officialTrackId: null,
      officialTrackUrl: null,
      isOfficialIntake47: false,
      intake: null,
      year: null,
      name: 'Custom Program Track',
      description: 'A separately configured program track.',
      category: null,
      totalHours: null,
      minimumGrade: null,
      eligibilitySummary: null,
      graduationYearLimitYears: null,
      prerequisiteTopics: [],
      isActive: true,
      locations: [],
      offerings: [
        {
          offeringId: 'custom-offering',
          branchId: 'custom-branch',
          branchName: 'Custom Branch',
          governorate: 'Cairo',
          capacity: 12,
        },
      ],
    };

    fixture.detectChanges();
    http.expectOne('/api/tracks').flush({
      isSuccess: true,
      data: [customTrack],
      statusCode: 200,
      message: '',
    });
    fixture.detectChanges();

    const card = fixture.nativeElement.querySelector('.track-card') as HTMLElement;
    expect(card.textContent).toContain('Active program track');
    expect(card.textContent).toContain('Custom Branch');
    expect(card.textContent).toContain('12 seats');
    expect(card.textContent).not.toContain('Official ITI track');
    expect(card.textContent).not.toContain('Minimum grade');
  });
});
