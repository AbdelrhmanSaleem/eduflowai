import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';

import { LocaleStore } from '../../../../core/i18n/locale.store';
import { AssistantLauncherService } from '../../../ai/assistant/services/assistant-launcher.service';
import { TrackCatalogService } from '../../../admission/catalog/data-access/track-catalog.service';
import { TrackCatalogItem } from '../../../admission/catalog/models/track-catalog.model';

type Theme = 'light' | 'dark';

interface ProgramFact {
  readonly labelKey: string;
  readonly valueKey: string;
}

interface TrackFilterOption {
  readonly value: string;
  readonly labelKey: string;
}

interface ProgramDefinition {
  readonly id: string;
  readonly code: string;
  readonly nameKey: string;
  readonly descriptionKey: string;
  readonly durationMonths: number;
  readonly highlights: readonly ProgramFact[];
  readonly eligibility: readonly ProgramFact[];
}

const THEME_STORAGE_KEY = 'iti-theme';
const ALL_TRACKS_FILTER = 'all';
const TRACK_PREVIEW_LIMIT = 9;
const TRACK_EXPANSION_LABELS = {
  en: { showMore: 'Show more', showLess: 'Show less' },
  ar: { showMore: 'عرض المزيد', showLess: 'عرض أقل' },
} as const;

const SHARED_ELIGIBILITY: readonly ProgramFact[] = [
  {
    labelKey: 'landing.programs.gender',
    valueKey: 'landing.programs.malesAndFemales',
  },
  {
    labelKey: 'landing.programs.nationalities',
    valueKey: 'landing.programs.egyptian',
  },
  {
    labelKey: 'landing.programs.militaryService',
    valueKey: 'landing.programs.militaryStatus',
  },
  {
    labelKey: 'landing.programs.educationDegree',
    valueKey: 'landing.programs.bachelorDegree',
  },
];

const PROGRAMS: readonly ProgramDefinition[] = [
  {
    id: 'professional-training-program',
    code: 'PTP-9M',
    nameKey: 'landing.programs.professional.name',
    descriptionKey: 'landing.programs.professional.description',
    durationMonths: 9,
    highlights: [
      {
        labelKey: 'landing.programs.fees',
        valueKey: 'landing.programs.fullyFunded',
      },
      {
        labelKey: 'landing.programs.jobProfiles',
        valueKey: 'landing.programs.professional.jobProfiles',
      },
      {
        labelKey: 'landing.programs.graduates',
        valueKey: 'landing.programs.professional.graduates',
      },
      {
        labelKey: 'landing.programs.duration',
        valueKey: 'landing.programs.professional.duration',
      },
    ],
    eligibility: [
      ...SHARED_ELIGIBILITY,
      {
        labelKey: 'landing.programs.graduationYear',
        valueKey: 'landing.programs.professional.graduationYear',
      },
      {
        labelKey: 'landing.programs.grade',
        valueKey: 'landing.programs.fair',
      },
    ],
  },
  {
    id: 'intensive-code-camps',
    code: 'ICC-4M',
    nameKey: 'landing.programs.icc.name',
    descriptionKey: 'landing.programs.icc.description',
    durationMonths: 4,
    highlights: [
      {
        labelKey: 'landing.programs.fees',
        valueKey: 'landing.programs.fullyFunded',
      },
      {
        labelKey: 'landing.programs.jobProfiles',
        valueKey: 'landing.programs.icc.jobProfiles',
      },
      {
        labelKey: 'landing.programs.graduates',
        valueKey: 'landing.programs.icc.graduates',
      },
      {
        labelKey: 'landing.programs.duration',
        valueKey: 'landing.programs.icc.duration',
      },
    ],
    eligibility: [
      ...SHARED_ELIGIBILITY,
      {
        labelKey: 'landing.programs.graduationYear',
        valueKey: 'landing.programs.icc.graduationYear',
      },
      {
        labelKey: 'landing.programs.grade',
        valueKey: 'landing.programs.fair',
      },
    ],
  },
];

function initialTheme(): Theme {
  if (typeof localStorage !== 'undefined') {
    const savedTheme = localStorage.getItem(THEME_STORAGE_KEY);
    if (savedTheme === 'light' || savedTheme === 'dark') {
      return savedTheme;
    }
  }

  return typeof window !== 'undefined' &&
    typeof window.matchMedia === 'function' &&
    window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light';
}

@Component({
  selector: 'app-landing-page',
  imports: [RouterLink],
  templateUrl: './landing-page.html',
  styleUrls: [
    './landing-page.scss',
    './landing-page-hero.scss',
    './landing-page-programs.scss',
    './landing-page-sections.scss',
    './landing-page-footer.scss',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingPage {
  readonly locale = inject(LocaleStore);
  readonly programs = PROGRAMS;

  private readonly title = inject(Title);
  private readonly assistantLauncher = inject(AssistantLauncherService);
  private readonly trackCatalog = inject(TrackCatalogService);
  private readonly destroyRef = inject(DestroyRef);

  readonly theme = signal<Theme>(initialTheme());
  readonly mobileMenuOpen = signal(false);
  readonly tracks = signal<readonly TrackCatalogItem[]>([]);
  readonly tracksLoading = signal(true);
  readonly tracksLoadFailed = signal(false);
  readonly activeFilter = signal<string>(ALL_TRACKS_FILTER);
  readonly trackSearch = signal('');
  readonly tracksExpanded = signal(false);

  private readonly trackFilterOptions = computed<readonly TrackFilterOption[]>(() => {
    const categories = Array.from(
      new Set(
        this.tracks()
          .map((track) => track.category?.trim())
          .filter((category): category is string => Boolean(category)),
      ),
    ).sort((left, right) => left.localeCompare(right));

    return [
      { value: ALL_TRACKS_FILTER, labelKey: 'landing.tracks.filterAll' },
      ...categories.map((category) => ({ value: category, labelKey: category })),
    ];
  });

  get trackFilters(): readonly TrackFilterOption[] {
    return this.trackFilterOptions();
  }

  readonly filteredTracks = computed(() => {
    const filter = this.activeFilter();
    const query = this.trackSearch().trim().toLocaleLowerCase();

    return this.tracks().filter((track) => {
      if (filter !== ALL_TRACKS_FILTER && track.category !== filter) {
        return false;
      }

      if (!query) {
        return true;
      }

      const searchableText = [
        track.name,
        track.description,
        track.category,
        track.totalHours?.toString(),
        ...track.prerequisiteTopics,
        ...track.offerings.map((offering) => offering.branchName),
      ]
        .filter((value): value is string => Boolean(value))
        .join(' ')
        .toLocaleLowerCase();

      return searchableText.includes(query);
    });
  });

  readonly visibleTracks = computed(() =>
    this.tracksExpanded()
      ? this.filteredTracks()
      : this.filteredTracks().slice(0, TRACK_PREVIEW_LIMIT),
  );

  readonly canToggleTracks = computed(() => this.filteredTracks().length > TRACK_PREVIEW_LIMIT);

  readonly trackExpansionLabel = computed(() => {
    const labels = TRACK_EXPANSION_LABELS[this.locale.locale()];
    return this.tracksExpanded() ? labels.showLess : labels.showMore;
  });

  constructor() {
    effect(() => this.title.setTitle(this.locale.t('landing.pageTitle')));
    this.loadTracks();
  }

  toggleTheme(): void {
    const theme: Theme = this.theme() === 'light' ? 'dark' : 'light';
    this.theme.set(theme);

    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(THEME_STORAGE_KEY, theme);
    }
  }

  toggleLocale(): void {
    this.locale.toggle();
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((open) => !open);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  setActiveFilter(filter: string): void {
    this.activeFilter.set(filter);
    this.tracksExpanded.set(false);
  }

  updateTrackSearch(event: Event): void {
    if (event.target instanceof HTMLInputElement) {
      this.trackSearch.set(event.target.value);
      this.tracksExpanded.set(false);
    }
  }

  toggleTracks(): void {
    if (!this.tracksExpanded()) {
      this.tracksExpanded.set(true);
      return;
    }

    this.tracksExpanded.set(false);

    if (typeof document !== 'undefined') {
      document.getElementById('tracks')?.scrollIntoView?.({
        behavior: 'smooth',
        block: 'start',
      });
    }
  }

  availableBranchNames(track: TrackCatalogItem): readonly string[] {
    return Array.from(
      new Set(track.offerings.map((offering) => offering.branchName).filter(Boolean)),
    );
  }

  formatTotalHours(track: TrackCatalogItem): string {
    return track.totalHours === null ? '—' : `${track.totalHours.toLocaleString()}h`;
  }

  openAssistant(): void {
    this.assistantLauncher.open();
  }

  private loadTracks(): void {
    this.tracksLoading.set(true);
    this.tracksLoadFailed.set(false);

    this.trackCatalog
      .getTracks()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tracks) => {
          this.tracks.set(tracks);
          this.tracksExpanded.set(false);
          this.tracksLoading.set(false);
        },
        error: () => {
          this.tracks.set([]);
          this.tracksExpanded.set(false);
          this.tracksLoadFailed.set(true);
          this.tracksLoading.set(false);
        },
      });
  }
}
