import { TestBed } from '@angular/core/testing';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { vi } from 'vitest';

import { SignalrConnection } from '../../../../core/realtime/signalr-connection';
import { NotificationRealtimeService } from './notification-realtime.service';

describe('NotificationRealtimeService', () => {
  it('connects to the notification hub and maps created events', async () => {
    const handlers = new Map<string, (payload: unknown) => void>();
    const connection = {
      state: HubConnectionState.Disconnected,
      on: vi.fn((eventName: string, handler: (payload: unknown) => void) => {
        handlers.set(eventName, handler);
      }),
      onclose: vi.fn(),
      start: vi.fn(async () => {
        connection.state = HubConnectionState.Connected;
      }),
      stop: vi.fn(async () => {
        connection.state = HubConnectionState.Disconnected;
      }),
    };
    const factory = {
      create: vi.fn(() => connection as unknown as HubConnection),
    };

    TestBed.configureTestingModule({
      providers: [NotificationRealtimeService, { provide: SignalrConnection, useValue: factory }],
    });

    const service = TestBed.inject(NotificationRealtimeService);
    const received = vi.fn();
    const subscription = service.notificationCreated$.subscribe(received);
    await Promise.resolve();

    handlers.get('notification-created')?.({
      NotificationId: 'notification-2',
      ApplicationId: 'application-1',
      NotificationType: 4,
      Message: 'The document was rejected.',
      CreatedAtUtc: '2026-08-04T10:00:00Z',
    });

    expect(factory.create).toHaveBeenCalledWith('/hubs/notifications');
    expect(connection.start).toHaveBeenCalledOnce();
    expect(received).toHaveBeenCalledWith({
      id: 'notification-2',
      message: 'The document was rejected.',
      type: 'DocumentRejected',
      isRead: false,
      createdAt: '2026-08-04T10:00:00Z',
    });

    subscription.unsubscribe();
    expect(connection.stop).toHaveBeenCalledOnce();
  });
});
