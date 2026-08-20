import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  of,
  startWith,
  Subject,
  switchMap,
  tap,
} from 'rxjs';

import { LocaleStore } from '../../../../../core/i18n/locale.store';
import { HumanReviewApi } from '../../data-access/human-review.api';
import {
  DOCUMENT_TYPES,
  DocumentStatus,
  DocumentType,
  HumanReviewDto,
  PaginatedResult,
  REVIEW_QUEUE_STATUSES,
} from '../../models/human-review.model';
import { HUMAN_REVIEW_COPY } from '../../human-review.copy';

@Component({
  selector: 'app-review-queue-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './review-queue-page.html',
  styleUrl: './review-queue-page.scss',
})
export class ReviewQueuePage implements OnInit {
  protected readonly locale = inject(LocaleStore);
  protected readonly copy = computed(() => HUMAN_REVIEW_COPY[this.locale.locale()]);
  protected readonly documentTypes = DOCUMENT_TYPES;
  protected readonly statuses = REVIEW_QUEUE_STATUSES;

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly typeControl = new FormControl('', { nonNullable: true });
  protected readonly statusControl = new FormControl('', { nonNullable: true });

  protected readonly page = signal<PaginatedResult<HumanReviewDto> | null>(null);
  protected readonly currentPage = signal(1);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly successMessage = signal('');
  protected readonly hasFilters = computed(() =>
    Boolean(this.searchControl.value.trim() || this.typeControl.value || this.statusControl.value),
  );
  protected readonly pageNumbers = computed(() => {
    const total = this.page()?.totalPages ?? 0;
    const current = this.currentPage();
    const start = Math.max(1, Math.min(current - 2, total - 4));
    return Array.from({ length: Math.min(5, total) }, (_, index) => Math.max(1, start) + index);
  });

  private readonly api = inject(HumanReviewApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload = new Subject<void>();

  constructor() {
    const outcome = this.router.getCurrentNavigation()?.extras.state?.['reviewOutcome'];
    if (outcome === 'approved') {
      this.successMessage.set(this.copy().queue.approvedSuccess);
    } else if (outcome === 'rejected') {
      this.successMessage.set(this.copy().queue.rejectedSuccess);
    } else if (outcome === 'replacement') {
      this.successMessage.set(this.copy().queue.replacementSuccess);
    }
  }

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.filtersChanged());

    for (const control of [this.typeControl, this.statusControl]) {
      control.valueChanges
        .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.filtersChanged());
    }

    this.reload
      .pipe(
        startWith(undefined),
        tap(() => {
          this.loading.set(true);
          this.error.set(false);
        }),
        switchMap(() =>
          this.api
            .getReviews({
              page: this.currentPage(),
              pageSize: 10,
              search: this.searchControl.value,
              type: this.typeControl.value,
              status: this.statusControl.value,
            })
            .pipe(catchError(() => of(null))),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((page) => {
        this.loading.set(false);
        if (!page) {
          this.error.set(true);
          return;
        }

        this.page.set(page);
        this.currentPage.set(page.currentPage || 1);
      });
  }

  protected refresh(): void {
    this.successMessage.set('');
    this.reload.next();
  }

  protected clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.typeControl.setValue('', { emitEvent: false });
    this.statusControl.setValue('', { emitEvent: false });
    this.currentPage.set(1);
    this.reload.next();
  }

  protected goToPage(page: number): void {
    const total = this.page()?.totalPages ?? 0;
    if (page < 1 || page > total || page === this.currentPage()) {
      return;
    }

    this.currentPage.set(page);
    this.reload.next();
  }

  protected applicantName(item: HumanReviewDto): string {
    return item.applicantName.trim() || this.copy().common.unknownApplicant;
  }

  protected initials(item: HumanReviewDto): string {
    const words = item.applicantName.trim().split(/\s+/).filter(Boolean);
    return words.length
      ? words
          .slice(0, 2)
          .map((word) => word[0])
          .join('')
          .toUpperCase()
      : '—';
  }

  protected documentTypeLabel(type: DocumentType): string {
    return this.copy().common.documentTypes[type];
  }

  protected statusLabel(status: DocumentStatus): string {
    return this.copy().common.statuses[status];
  }

  protected resultsLabel(): string {
    const page = this.page();
    if (!page || page.totalCount === 0) {
      return '';
    }

    const from = (page.currentPage - 1) * page.pageSize + 1;
    const to = Math.min(page.currentPage * page.pageSize, page.totalCount);
    return interpolate(this.copy().queue.results, { from, to, total: page.totalCount });
  }

  protected pageLabel(): string {
    return interpolate(this.copy().queue.page, {
      page: this.currentPage(),
      total: this.page()?.totalPages ?? 0,
    });
  }

  private filtersChanged(): void {
    this.currentPage.set(1);
    this.successMessage.set('');
    this.reload.next();
  }
}

function interpolate(value: string, params: Record<string, string | number>): string {
  return Object.entries(params).reduce(
    (result, [key, replacement]) => result.replaceAll(`{{${key}}}`, String(replacement)),
    value,
  );
}
