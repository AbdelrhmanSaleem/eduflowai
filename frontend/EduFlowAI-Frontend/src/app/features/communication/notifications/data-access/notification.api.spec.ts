import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { NotificationApi } from './notification.api';

describe('NotificationApi', () => {
  let api: NotificationApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [NotificationApi, provideHttpClient(), provideHttpClientTesting()],
    });

    api = TestBed.inject(NotificationApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the backend pagination, search, and type query parameters', () => {
    api
      .getNotifications({
        page: 2,
        pageSize: 10,
        search: 'national id',
        type: 'DocumentRejected',
      })
      .subscribe();

    const request = http.expectOne(
      (request) =>
        request.url === '/api/communication/notifications' &&
        request.params.get('Page') === '2' &&
        request.params.get('PageSize') === '10' &&
        request.params.get('Search') === 'national id' &&
        request.params.get('Type') === 'DocumentRejected',
    );

    expect(request.request.method).toBe('GET');
    request.flush({
      data: [],
      filters: null,
      currentPage: 2,
      totalPages: 2,
      pageSize: 10,
      totalCount: 12,
    });
  });

  it('normalizes numeric .NET enum values for the notification UI', () => {
    let type: string | undefined;

    api.getNotifications({ page: 1, pageSize: 10 }).subscribe((result) => {
      type = result.data[0]?.type;
    });

    http
      .expectOne(
        (request) =>
          request.url === '/api/communication/notifications' &&
          request.params.get('Page') === '1' &&
          request.params.get('PageSize') === '10',
      )
      .flush({
        data: [
          {
            id: 'notification-1',
            message: 'The uploaded document could not be approved.',
            type: 4,
            isRead: false,
            createdAt: '2026-08-01T10:00:00Z',
          },
        ],
        filters: null,
        currentPage: 1,
        totalPages: 1,
        pageSize: 10,
        totalCount: 1,
      });

    expect(type).toBe('DocumentRejected');
  });

  it('marks one notification as read through the notification-specific endpoint', () => {
    api.markAsRead('notification-1').subscribe();

    const request = http.expectOne('/api/communication/notifications/notification-1/read');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toBeNull();
    request.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('marks all notifications as read and returns the updated count', () => {
    let updatedCount: number | undefined;

    api.markAllAsRead().subscribe((response) => {
      updatedCount = response.updatedCount;
    });

    const request = http.expectOne('/api/communication/notifications/read-all');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toBeNull();
    request.flush({ updatedCount: 3 });

    expect(updatedCount).toBe(3);
  });
});
