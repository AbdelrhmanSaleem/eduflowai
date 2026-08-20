import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { LocaleStore } from '../../../../../core/i18n/locale.store';
import { ReplacementRequestApi } from '../../data-access/replacement-request.api';
import {
  ReplacementRequestItem,
  REPLACEMENT_REQUEST_STATUSES,
  ReplacementRequestStatus,
} from '../../models/replacement-request.model';
import { REPLACEMENT_COPY } from '../../replacement.copy';

type SortOrder = 'desc' | 'asc';

@Component({
  selector: 'app-replacement-requests-list-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './replacement-requests-list-page.html',
  styleUrl: './replacement-requests-list-page.scss',
})
export class ReplacementRequestsListPage implements OnInit {
  protected readonly locale = inject(LocaleStore);
  protected readonly copy = computed(() => REPLACEMENT_COPY[this.locale.locale()]);
  protected readonly statuses = REPLACEMENT_REQUEST_STATUSES;

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly statusControl = new FormControl('', { nonNullable: true });
  protected readonly sortControl = new FormControl<SortOrder>('desc', { nonNullable: true });

  protected readonly requests = signal<ReplacementRequestItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  private readonly search = signal('');
  private readonly status = signal('');
  private readonly sort = signal<SortOrder>('desc');

  protected readonly filteredRequests = computed(() => {
    const search = this.search().trim().toLocaleLowerCase();
    const status = this.status();
    const direction = this.sort();

    return this.requests()
      .filter((request) => {
        const documentName = this.documentLabel(request.documentType).toLocaleLowerCase();
        return (!search || documentName.includes(search)) && (!status || request.status === status);
      })
      .sort((first, second) => {
        const difference =
          new Date(first.requestedAt).getTime() - new Date(second.requestedAt).getTime();
        return direction === 'asc' ? difference : -difference;
      });
  });

  protected readonly hasFilters = computed(() => Boolean(this.search() || this.status()));

  private readonly api = inject(ReplacementRequestApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const search = params.get('search') ?? '';
    const status = params.get('status') ?? '';
    const sort: SortOrder = params.get('sort') === 'asc' ? 'asc' : 'desc';

    this.searchControl.setValue(search, { emitEvent: false });
    this.statusControl.setValue(status, { emitEvent: false });
    this.sortControl.setValue(sort, { emitEvent: false });
    this.search.set(search);
    this.status.set(status);
    this.sort.set(sort);

    this.searchControl.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.search.set(value);
        this.updateQueryParams();
      });

    this.statusControl.valueChanges
      .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.status.set(value);
        this.updateQueryParams();
      });

    this.sortControl.valueChanges
      .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.sort.set(value);
        this.updateQueryParams();
      });

    this.loadRequests();
  }

  protected retry(): void {
    this.loadRequests();
  }

  protected clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.statusControl.setValue('', { emitEvent: false });
    this.search.set('');
    this.status.set('');
    this.updateQueryParams();
  }

  protected statusLabel(status: ReplacementRequestStatus): string {
    return this.copy().common.statuses[status];
  }

  protected documentLabel(type: string | null): string {
    if (!type) {
      return this.copy().common.unknownDocument;
    }

    return this.copy().common.documentTypes[type] ?? type.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.locale.locale() === 'ar' ? 'ar-EG' : 'en-US', {
      dateStyle: 'medium',
    }).format(new Date(value));
  }

  private loadRequests(): void {
    this.loading.set(true);
    this.error.set(false);

    this.api
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (requests) => {
          this.requests.set(requests);
          this.loading.set(false);
        },
        error: () => {
          this.requests.set([]);
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }

  private updateQueryParams(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: this.search().trim() || null,
        status: this.status() || null,
        sort: this.sort() === 'asc' ? 'asc' : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
