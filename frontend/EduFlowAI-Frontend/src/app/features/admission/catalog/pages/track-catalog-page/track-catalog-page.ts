import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { TrackCatalogService } from '../../data-access/track-catalog.service';
import { TrackCatalogItem } from '../../models/track-catalog.model';
import { TrackCatalogCopy } from '../../track-catalog.copy';

@Component({
  selector: 'app-track-catalog-page',
  imports: [RouterLink],
  templateUrl: './track-catalog-page.html',
  styleUrl: './track-catalog-page.scss',
})
export class TrackCatalogPage implements OnInit {
  protected readonly copy = inject(TrackCatalogCopy);

  private readonly service = inject(TrackCatalogService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly tracks = signal<readonly TrackCatalogItem[]>([]);
  protected readonly query = signal('');
  protected readonly selectedCategory = signal<string | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  protected readonly categories = computed(() =>
    Array.from(
      new Set(
        this.tracks()
          .map((track) => track.category?.trim())
          .filter((category): category is string => Boolean(category)),
      ),
    ).sort((left, right) => left.localeCompare(right)),
  );

  protected readonly filteredTracks = computed(() => {
    const query = this.query().trim().toLocaleLowerCase();
    const selectedCategory = this.selectedCategory();

    return this.tracks().filter((track) => {
      if (selectedCategory && track.category !== selectedCategory) {
        return false;
      }

      if (!query) {
        return true;
      }

      const searchableValues = [
        track.name,
        track.description,
        track.category,
        track.minimumGrade,
        track.eligibilitySummary,
        track.totalHours?.toString(),
        track.graduationYearLimitYears?.toString(),
        track.intake?.toString(),
        track.year?.toString(),
        ...track.prerequisiteTopics,
        ...track.locations.flatMap((location) => [
          location.branchName,
          location.governorate,
        ]),
        ...track.offerings.flatMap((offering) => [
          offering.branchName,
          offering.governorate,
        ]),
      ];

      return searchableValues
        .filter((value): value is string => Boolean(value))
        .some((value) => value.toLocaleLowerCase().includes(query));
    });
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(false);

    this.service
      .getTracks()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (tracks) => this.tracks.set(tracks),
        error: () => {
          this.tracks.set([]);
          this.error.set(true);
        },
      });
  }

  protected onSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.query.set(input.value);
  }

  protected selectCategory(category: string | null): void {
    this.selectedCategory.set(category);
  }

  protected locationCount(track: TrackCatalogItem): number {
    return track.locations.length;
  }

  protected formatHours(track: TrackCatalogItem): string {
    if (track.totalHours === null) {
      return this.copy.text('notPublished');
    }

    return this.copy.text('hours', {
      count: track.totalHours.toLocaleString(),
    });
  }

  protected totalCapacity(track: TrackCatalogItem): number {
    return track.offerings.reduce(
      (total, offering) => total + offering.capacity,
      0,
    );
  }
}
