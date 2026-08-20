import { inject, Injectable } from '@angular/core';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { RuntimeConfig } from '../../../../core/config/runtime-config';
import { SignalrConnection } from '../../../../core/realtime/signalr-connection';
import {
  NotificationCreatedMessage,
  NotificationItem,
  NotificationType,
} from '../models/notification.model';
import { normalizeNotificationType } from './notification.api';

type NotificationCreatedEnvelope = Partial<NotificationCreatedMessage> & {
  NotificationId?: unknown;
  ApplicationId?: unknown;
  NotificationType?: unknown;
  Message?: unknown;
  CreatedAtUtc?: unknown;
};

@Injectable({ providedIn: 'root' })
export class NotificationRealtimeService {
  // private readonly connection: HubConnection =
  //   inject(SignalrConnection).create('/hubs/notifications');
  private readonly backendUrl =
    inject(RuntimeConfig).apiBaseUrl.replace(/\/api$/i, '');

  private readonly connection: HubConnection =
    inject(SignalrConnection).create(
      `${this.backendUrl}/hubs/notifications`,
    );
  private readonly created = new Subject<NotificationItem>();
  private subscribers = 0;
  private starting = false;
  private retryTimer?: ReturnType<typeof setTimeout>;

  readonly notificationCreated$ = new Observable<NotificationItem>((subscriber) => {
    this.subscribers += 1;
    const subscription = this.created.subscribe(subscriber);
    this.start();

    return () => {
      subscription.unsubscribe();
      this.subscribers = Math.max(0, this.subscribers - 1);
      if (this.subscribers === 0) {
        this.stop();
      }
    };
  });

  constructor() {
    this.connection.on('notification-created', (message: NotificationCreatedEnvelope) => {
      const notification = toNotificationItem(message);
      if (notification) {
        this.created.next(notification);
      }
    });

    this.connection.onclose(() => {
      if (this.subscribers > 0) {
        this.scheduleRetry();
      }
    });
  }

  private start(): void {
    if (
      typeof window === 'undefined' ||
      this.starting ||
      this.connection.state !== HubConnectionState.Disconnected
    ) {
      return;
    }

    this.starting = true;
    void this.connection
      .start()
      .catch(() => this.scheduleRetry())
      .finally(() => {
        this.starting = false;
      });
  }

  private scheduleRetry(): void {
    if (this.subscribers === 0 || this.retryTimer) {
      return;
    }

    this.retryTimer = setTimeout(() => {
      this.retryTimer = undefined;
      this.start();
    }, 5_000);
  }

  private stop(): void {
    if (this.retryTimer) {
      clearTimeout(this.retryTimer);
      this.retryTimer = undefined;
    }

    if (this.connection.state !== HubConnectionState.Disconnected) {
      void this.connection.stop().catch(() => undefined);
    }
  }
}

function toNotificationItem(envelope: NotificationCreatedEnvelope): NotificationItem | null {
  const notificationId = stringValue(envelope.notificationId ?? envelope.NotificationId);
  const createdAt = stringValue(envelope.createdAtUtc ?? envelope.CreatedAtUtc);
  const rawType = envelope.notificationType ?? envelope.NotificationType;

  if (!notificationId || !createdAt || !Number.isFinite(Date.parse(createdAt))) {
    return null;
  }

  const type =
    typeof rawType === 'number' || typeof rawType === 'string'
      ? normalizeNotificationType(rawType as NotificationType | number | string)
      : 'None';
  const rawMessage = envelope.message ?? envelope.Message;

  return {
    id: notificationId,
    message: typeof rawMessage === 'string' ? rawMessage : null,
    type,
    isRead: false,
    createdAt,
  };
}

function stringValue(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value : null;
}
