import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, Subscription } from 'rxjs';

import { LocaleStore } from '../../../../../core/i18n/locale.store';
import { NotificationApi } from '../../data-access/notification.api';
import { NotificationRealtimeService } from '../../data-access/notification-realtime.service';
import {
  NotificationItem,
  NOTIFICATION_TYPES,
  NotificationType,
} from '../../models/notification.model';
import { NOTIFICATION_CENTER_COPY } from './notification-center.copy';

type SortOrder = 'desc' | 'asc';

interface NotificationAction {
  label: keyof (typeof NOTIFICATION_CENTER_COPY)['en']['actions'];
  route: string;
}

@Component({
  selector: 'app-notification-center-page',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './notification-center-page.html',
  styleUrl: './notification-center-page.scss',
})
export class NotificationCenterPage implements OnInit {
  protected readonly locale = inject(LocaleStore);
  protected readonly copy = computed(() => NOTIFICATION_CENTER_COPY[this.locale.locale()]);
  protected readonly notificationTypes = NOTIFICATION_TYPES;
  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly typeControl = new FormControl('', { nonNullable: true });
  protected readonly sortControl = new FormControl<SortOrder>('desc', {
    nonNullable: true,
  });

  protected readonly notifications = signal<NotificationItem[]>([]);
  protected readonly selectedId = signal<string | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(0);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = 10;
  protected readonly sortOrder = signal<SortOrder>('desc');
  protected readonly markingAllRead = signal(false);
  protected readonly readUpdateError = signal(false);
  protected readonly compactView = signal(false);
  protected readonly detailOpen = signal(false);

  protected readonly sortedNotifications = computed(() =>
    [...this.notifications()].sort((first, second) => {
      const difference = new Date(first.createdAt).getTime() - new Date(second.createdAt).getTime();
      return this.sortOrder() === 'asc' ? difference : -difference;
    }),
  );

  protected readonly selectedNotification = computed(
    () => this.notifications().find((item) => item.id === this.selectedId()) ?? null,
  );

  protected readonly firstVisibleItem = computed(() =>
    this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize + 1,
  );

  protected readonly lastVisibleItem = computed(() =>
    Math.min(this.page() * this.pageSize, this.totalCount()),
  );

  private readonly api = inject(NotificationApi);
  private readonly realtime = inject(NotificationRealtimeService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly markingReadIds = signal<ReadonlySet<string>>(new Set());
  private requestSubscription?: Subscription;
  private compactMediaQuery?: MediaQueryList;

  private readonly compactMediaListener = (event: MediaQueryListEvent): void => {
    this.setCompactView(event.matches);
  };

  ngOnInit(): void {
    this.initializeCompactView();

    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.filtersChanged());

    this.typeControl.valueChanges
      .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.filtersChanged());

    this.sortControl.valueChanges
      .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((order) => this.sortOrder.set(order));

    this.realtime.notificationCreated$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((notification) => this.addRealtimeNotification(notification));

    this.loadNotifications();
  }

  protected selectNotification(notification: NotificationItem): void {
    this.selectedId.set(notification.id);
    this.detailOpen.set(true);
    this.markNotificationAsRead(notification);
  }

  protected showInbox(): void {
    this.detailOpen.set(false);
  }

  protected markAllAsRead(): void {
    if (this.markingAllRead()) {
      return;
    }

    this.readUpdateError.set(false);
    this.markingAllRead.set(true);

    this.api
      .markAllAsRead()
      .pipe(
        finalize(() => this.markingAllRead.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notifications.update((notifications) =>
            notifications.map((notification) => ({
              ...notification,
              isRead: true,
            })),
          );
        },
        error: () => this.readUpdateError.set(true),
      });
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    this.page.set(page);
    this.loadNotifications();
  }

  protected retry(): void {
    this.loadNotifications();
  }

  protected typeLabel(type: NotificationType): string {
    return this.copy().types[type];
  }

  protected typeIcon(type: NotificationType): string {
    const icons: Record<NotificationType, string> = {
      None: 'notifications',
      DocumentsRequired: 'upload_file',
      DocumentReplacementRequested: 'published_with_changes',
      DocumentApproved: 'task_alt',
      DocumentRejected: 'error',
      ApplicationStatusChanged: 'sync_alt',
    };

    return icons[type];
  }

  protected notificationAction(type: NotificationType): NotificationAction | null {
    if (
      type === 'DocumentsRequired' ||
      type === 'DocumentApproved' ||
      type === 'DocumentRejected'
    ) {
      return {
        label: 'documents',
        route: '/applicant/documents',
      };
    }

    if (type === 'DocumentReplacementRequested') {
      return {
        label: 'replacements',
        route: '/replacement-requests',
      };
    }

    if (type === 'ApplicationStatusChanged') {
      return {
        label: 'applications',
        route: '/applications',
      };
    }

    return null;
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.locale.locale() === 'ar' ? 'ar-EG' : 'en-US', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  protected formatRelativeTime(value: string): string {
    const timestamp = new Date(value).getTime();
    const seconds = Math.round((timestamp - Date.now()) / 1000);
    const absoluteSeconds = Math.abs(seconds);
    let amount = seconds;
    let unit: Intl.RelativeTimeFormatUnit = 'second';

    if (absoluteSeconds >= 86_400) {
      amount = Math.round(seconds / 86_400);
      unit = 'day';
    } else if (absoluteSeconds >= 3_600) {
      amount = Math.round(seconds / 3_600);
      unit = 'hour';
    } else if (absoluteSeconds >= 60) {
      amount = Math.round(seconds / 60);
      unit = 'minute';
    }

    return new Intl.RelativeTimeFormat(this.locale.locale() === 'ar' ? 'ar-EG' : 'en-US', {
      numeric: 'auto',
    }).format(amount, unit);
  }

  private filtersChanged(): void {
    this.page.set(1);
    this.loadNotifications();
  }

  private addRealtimeNotification(notification: NotificationItem): void {
    if (
      this.notifications().some((item) => item.id === notification.id) ||
      !this.matchesCurrentFilters(notification)
    ) {
      return;
    }

    this.totalCount.update((count) => count + 1);
    this.totalPages.set(Math.ceil(this.totalCount() / this.pageSize));

    if (this.page() === 1) {
      this.notifications.update((notifications) =>
        [notification, ...notifications].slice(0, this.pageSize),
      );
    }
  }

  private matchesCurrentFilters(notification: NotificationItem): boolean {
    const selectedType = this.typeControl.value;
    if (selectedType && notification.type !== selectedType) {
      return false;
    }

    const search = this.searchControl.value.trim().toLocaleLowerCase();
    return !search || (notification.message ?? '').toLocaleLowerCase().includes(search);
  }

  private loadNotifications(): void {
    this.requestSubscription?.unsubscribe();
    this.loading.set(true);
    this.error.set(false);

    const search = this.searchControl.value.trim();
    const type = this.typeControl.value || undefined;

    this.requestSubscription = this.api
      .getNotifications({
        page: this.page(),
        pageSize: this.pageSize,
        search: search || undefined,
        type: type as Exclude<NotificationType, 'None'> | undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.notifications.set(result.data);
          this.totalPages.set(result.totalPages);
          this.totalCount.set(result.totalCount);

          const selectedStillVisible = result.data.some(
            (notification) => notification.id === this.selectedId(),
          );
          if (!selectedStillVisible) {
            this.selectedId.set(result.data[0]?.id ?? null);
          }

          this.detailOpen.set(false);

          const openedNotification = result.data.find(
            (notification) => notification.id === this.selectedId(),
          );
          if (openedNotification && !this.compactView()) {
            this.markNotificationAsRead(openedNotification);
          }

          this.loading.set(false);
        },
        error: () => {
          this.notifications.set([]);
          this.selectedId.set(null);
          this.totalPages.set(0);
          this.totalCount.set(0);
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }

  private initializeCompactView(): void {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return;
    }

    this.compactMediaQuery = window.matchMedia('(max-width: 55rem)');
    this.setCompactView(this.compactMediaQuery.matches);
    this.compactMediaQuery.addEventListener('change', this.compactMediaListener);
    this.destroyRef.onDestroy(() =>
      this.compactMediaQuery?.removeEventListener('change', this.compactMediaListener),
    );
  }

  private setCompactView(isCompact: boolean): void {
    this.compactView.set(isCompact);
    this.detailOpen.set(false);

    if (!isCompact) {
      const notification = this.selectedNotification();
      if (notification) {
        this.markNotificationAsRead(notification);
      }
    }
  }

  private markNotificationAsRead(notification: NotificationItem): void {
    if (notification.isRead || this.markingReadIds().has(notification.id)) {
      return;
    }

    this.readUpdateError.set(false);
    this.markingReadIds.update((ids) => new Set(ids).add(notification.id));

    this.api
      .markAsRead(notification.id)
      .pipe(
        finalize(() => {
          this.markingReadIds.update((ids) => {
            const nextIds = new Set(ids);
            nextIds.delete(notification.id);
            return nextIds;
          });
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notifications.update((notifications) =>
            notifications.map((item) =>
              item.id === notification.id ? { ...item, isRead: true } : item,
            ),
          );
        },
        error: () => this.readUpdateError.set(true),
      });
  }
}
