import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { RuntimeConfig } from '../../../../core/config/runtime-config';
import { LocaleStore } from '../../../../core/i18n/locale.store';
import { TrackCatalogItem } from '../../../admission/catalog/models/track-catalog.model';
import { LandingPage } from './landing-page';

const aiTrack: TrackCatalogItem = {
  id: '0d58476e-3be9-4d40-b454-08dbe75ac461',
  programId: '9m-program',
  officialTrackId: 'official-ai',
  officialTrackUrl: 'https://iti.gov.eg/tracks/ai',
  isOfficialIntake47: true,
  intake: 47,
  year: 2026,
  name: 'AI and Machine Learning',
  description: 'Build intelligent systems using modern machine-learning workflows.',
  category: 'Artificial Intelligence',
  totalHours: 960,
  minimumGrade: 'Good',
  eligibilitySummary: 'Official eligibility reference.',
  graduationYearLimitYears: 5,
  prerequisiteTopics: ['Python', 'Linear Algebra'],
  isActive: true,
  locations: [
    {
      branchId: 'reference-only',
      branchName: 'Reference Only Branch',
      governorate: 'Cairo',
    },
  ],
  offerings: [
    {
      offeringId: 'offering-ai-smart-village',
      branchId: 'smart-village',
      branchName: 'Smart Village',
      governorate: 'Giza',
      capacity: 40,
    },
    {
      offeringId: 'offering-ai-alexandria',
      branchId: 'alexandria',
      branchName: 'Alexandria',
      governorate: 'Alexandria',
      capacity: 30,
    },
  ],
};

const digitalIcTrack: TrackCatalogItem = {
  ...aiTrack,
  id: '7b32092e-6cf8-4c3b-a197-08dbe75ac461',
  officialTrackId: 'official-digital-ic',
  officialTrackUrl: 'https://iti.gov.eg/tracks/digital-ic',
  name: 'Digital IC Design',
  description: 'Design and verify digital integrated circuits.',
  category: 'Electronics & Embedded Systems',
  totalHours: 1080,
  prerequisiteTopics: ['Digital Logic', 'Electronics'],
  locations: [],
  offerings: [
    {
      offeringId: 'offering-digital-smart-village',
      branchId: 'smart-village',
      branchName: 'Smart Village',
      governorate: 'Giza',
      capacity: 25,
    },
  ],
};

function createTrackSet(count: number): TrackCatalogItem[] {
  return Array.from({ length: count }, (_, index) => {
    const trackNumber = index + 1;

    return {
      ...digitalIcTrack,
      id: `api-track-${trackNumber}`,
      officialTrackId: `official-track-${trackNumber}`,
      officialTrackUrl: `https://iti.gov.eg/tracks/${trackNumber}`,
      name: `API Track ${trackNumber}`,
      description: `Description for API Track ${trackNumber}.`,
      category: trackNumber % 2 === 0 ? 'Category B' : 'Category A',
      prerequisiteTopics: [`Topic ${trackNumber}`],
      locations: [],
      offerings: [
        {
          offeringId: `offering-${trackNumber}`,
          branchId: `branch-${trackNumber}`,
          branchName: `Branch ${trackNumber}`,
          governorate: null,
          capacity: 20 + trackNumber,
        },
      ],
    };
  });
}

describe('LandingPage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [LandingPage],
      providers: [
        provideRouter([]),
        { provide: RuntimeConfig, useValue: { apiBaseUrl: '/api' } },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.inject(LocaleStore).setLocale('en');
    localStorage.clear();
  });

  function createWithTracks(tracks: readonly TrackCatalogItem[] = [aiTrack, digitalIcTrack]) {
    const fixture = TestBed.createComponent(LandingPage);
    const request = http.expectOne('/api/tracks');

    expect(request.request.method).toBe('GET');
    request.flush({
      isSuccess: true,
      data: tracks,
      statusCode: 200,
      message: '',
    });
    fixture.detectChanges();

    return fixture;
  }

  it('keeps the existing static program section intact', () => {
    const fixture = createWithTracks();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('.program-card')).toHaveLength(2);
    expect(root.querySelector('.program-card h3')?.textContent).toContain(
      'Professional Training Program (9 Months)',
    );
  });

  it('renders TrackCatalogService data instead of the six hardcoded demo tracks', () => {
    const fixture = createWithTracks();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('.track-card')).toHaveLength(2);
    expect(root.textContent).toContain(aiTrack.name);
    expect(root.textContent).toContain(aiTrack.description!);
    expect(root.textContent).toContain(aiTrack.category!);
    expect(root.textContent).toContain('960h');
    expect(root.textContent).toContain('Python');
    expect(root.textContent).not.toContain('Full Stack Web Development');
    expect(root.textContent).not.toContain('Cybersecurity');
  });

  it('renders multiple active offering branches on one card and ignores locations', () => {
    const fixture = createWithTracks();
    const cards = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLElement>('.track-card'),
    );
    const card = cards.find((item) => item.textContent?.includes(aiTrack.name));

    expect(cards.filter((item) => item.textContent?.includes(aiTrack.name))).toHaveLength(1);
    expect(card?.textContent).toContain('Smart Village');
    expect(card?.textContent).toContain('Alexandria');
    expect(card?.textContent).not.toContain('Reference Only Branch');
  });

  it('searches API-backed track data and active offering branches', () => {
    const fixture = createWithTracks();
    const root = fixture.nativeElement as HTMLElement;
    const search = root.querySelector<HTMLInputElement>('.search-box input')!;

    search.value = 'Linear Algebra';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(root.querySelectorAll('.track-card')).toHaveLength(1);
    expect(root.querySelector('.track-card')?.textContent).toContain(aiTrack.name);

    search.value = 'Smart Village';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(root.querySelectorAll('.track-card')).toHaveLength(2);
  });

  it('derives category filters from API-backed categories', () => {
    const fixture = createWithTracks();
    const root = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(root.querySelectorAll<HTMLButtonElement>('.filter-btn'));

    expect(buttons.map((button) => button.textContent?.trim())).toEqual([
      'All tracks',
      'Artificial Intelligence',
      'Electronics & Embedded Systems',
    ]);

    buttons.find((button) => button.textContent?.includes('Electronics'))!.click();
    fixture.detectChanges();
    expect(root.querySelectorAll('.track-card')).toHaveLength(1);
    expect(root.querySelector('.track-card')?.textContent).toContain('Digital IC Design');
  });

  it('shows every returned track and no expansion control when there are nine or fewer tracks', () => {
    const tracks = createTrackSet(9);
    const fixture = createWithTracks(tracks);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('.track-card')).toHaveLength(9);
    expect(root.querySelector('button[aria-controls="landing-track-grid"]')).toBeNull();
    expect(fixture.componentInstance.tracks()).toHaveLength(9);
  });

  it('shows only the first nine tracks and Show more when more than nine tracks are returned', () => {
    const tracks = createTrackSet(12);
    const fixture = createWithTracks(tracks);
    const root = fixture.nativeElement as HTMLElement;
    const toggle = root.querySelector<HTMLButtonElement>('button[aria-controls="landing-track-grid"]');

    expect(root.querySelectorAll('.track-card')).toHaveLength(9);
    expect(toggle?.textContent?.trim()).toBe('Show more');
    expect(toggle?.getAttribute('aria-expanded')).toBe('false');
    expect(fixture.componentInstance.tracks()).toHaveLength(12);
    expect(fixture.componentInstance.tracks().map((track) => track.name)).toEqual(
      tracks.map((track) => track.name),
    );
  });

  it('reveals all returned tracks and changes the control to Show less', () => {
    const tracks = createTrackSet(12);
    const fixture = createWithTracks(tracks);
    const root = fixture.nativeElement as HTMLElement;
    const toggle = root.querySelector<HTMLButtonElement>('button[aria-controls="landing-track-grid"]')!;

    toggle.click();
    fixture.detectChanges();

    expect(root.querySelectorAll('.track-card')).toHaveLength(12);
    expect(toggle.textContent?.trim()).toBe('Show less');
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(fixture.componentInstance.tracks()).toHaveLength(12);
  });

  it('collapses back to nine tracks without removing data from the returned collection', () => {
    const tracks = createTrackSet(12);
    const fixture = createWithTracks(tracks);
    const root = fixture.nativeElement as HTMLElement;
    const toggle = root.querySelector<HTMLButtonElement>('button[aria-controls="landing-track-grid"]')!;

    toggle.click();
    fixture.detectChanges();
    toggle.click();
    fixture.detectChanges();

    expect(root.querySelectorAll('.track-card')).toHaveLength(9);
    expect(toggle.textContent?.trim()).toBe('Show more');
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(fixture.componentInstance.tracks()).toHaveLength(12);
    expect(fixture.componentInstance.tracks().map((track) => track.id)).toEqual(
      tracks.map((track) => track.id),
    );
  });

  it('shows a safe empty state when the API returns no tracks', () => {
    const fixture = createWithTracks([]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('.track-card')).toHaveLength(0);
    expect(root.querySelector('.empty-state[role="status"]')?.textContent).toContain(
      'No tracks match your current search.',
    );
  });

  it('shows a safe error state when the track API fails', () => {
    const fixture = TestBed.createComponent(LandingPage);
    const request = http.expectOne('/api/tracks');

    request.flush(null, {
      status: 503,
      statusText: 'Service Unavailable',
    });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelectorAll('.track-card')).toHaveLength(0);
    expect(root.querySelector('.empty-state[role="alert"]')?.textContent).toContain(
      'Something went wrong. Please try again.',
    );
  });

  it('keeps locale and theme behavior working', () => {
    const fixture = createWithTracks(createTrackSet(10));

    fixture.componentInstance.toggleLocale();
    fixture.componentInstance.toggleTheme();
    fixture.detectChanges();

    const root = (fixture.nativeElement as HTMLElement).querySelector('.landing-page');
    const toggle = root?.querySelector<HTMLButtonElement>('button[aria-controls="landing-track-grid"]');

    expect(document.documentElement.dir).toBe('rtl');
    expect(localStorage.getItem('eduflow.locale')).toBe('ar');
    expect(localStorage.getItem('iti-theme')).toBe('dark');
    expect(root?.getAttribute('data-theme')).toBe('dark');
    expect(toggle?.textContent?.trim()).toBe('عرض المزيد');
  });
});
