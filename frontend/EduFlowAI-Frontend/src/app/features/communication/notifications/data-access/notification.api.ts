import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { RuntimeConfig } from '../../../../core/config/runtime-config';

import {
  MarkAllReadResponse,
  NotificationItem,
  NotificationQuery,
  NotificationType,
  PaginatedResult,
} from '../models/notification.model';

type NotificationResponse = Omit<NotificationItem, 'type'> & {
  type: NotificationType | number | string;
};

const TYPES_BY_VALUE: Record<number, NotificationType> = {
  0: 'None',
  1: 'DocumentsRequired',
  2: 'DocumentReplacementRequested',
  3: 'DocumentApproved',
  4: 'DocumentRejected',
  5: 'ApplicationStatusChanged',
};

@Injectable({ providedIn: 'root' })
export class NotificationApi {
  private readonly http = inject(HttpClient);
  private readonly notificationsUrl =
    `${inject(RuntimeConfig).apiBaseUrl}/communication/notifications`;

  getNotifications(query: NotificationQuery): Observable<PaginatedResult<NotificationItem>> {
    let params = new HttpParams().set('Page', query.page).set('PageSize', query.pageSize);

    if (query.search) {
      params = params.set('Search', query.search);
    }

    if (query.type) {
      params = params.set('Type', query.type);
    }

    return this.http
      .get<PaginatedResult<NotificationResponse>>(this.notificationsUrl, {
        params,
      })
      .pipe(
        map((result) => ({
          ...result,
          data: result.data.map((notification) => ({
            ...notification,
            type: normalizeNotificationType(notification.type),
          })),
        })),
      );
  }

  markAsRead(notificationId: string): Observable<void> {
    return this.http.patch<void>(`${this.notificationsUrl}/${notificationId}/read`, null);
  }

  markAllAsRead(): Observable<MarkAllReadResponse> {
    return this.http.patch<MarkAllReadResponse>(`${this.notificationsUrl}/read-all`, null);
  }
}

export function normalizeNotificationType(
  value: NotificationType | number | string,
): NotificationType {
  if (typeof value === 'number') {
    return TYPES_BY_VALUE[value] ?? 'None';
  }

  if (/^\d+$/.test(value)) {
    return TYPES_BY_VALUE[Number(value)] ?? 'None';
  }

  return Object.values(TYPES_BY_VALUE).includes(value as NotificationType)
    ? (value as NotificationType)
    : 'None';
}
